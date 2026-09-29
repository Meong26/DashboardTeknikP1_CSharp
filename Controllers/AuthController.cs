using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using DashboardTeknikP1.Models;
using DashboardTeknikP1.Helpers;
using Dapper;

using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;

namespace DashboardTeknikP1.Controllers
{
    public class AuthController : Controller
    {
        private readonly string _connectionString;
        private readonly IMemoryCache _cache;

        public AuthController(IConfiguration configuration, IMemoryCache cache)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _cache = cache;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        private class UserAuthDto
        {
            public string UserID { get; set; } = string.Empty;
            public string NamaLengkap { get; set; } = string.Empty;
            public string PasswordHash { get; set; } = string.Empty;
            public string RoleName { get; set; } = string.Empty;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // ==========================================
            // RATE LIMITING & BRUTE-FORCE PROTECTION
            // ==========================================
            string lockoutKey = $"Lockout_{model.UserID}";
            string attemptKey = $"LoginAttempts_{model.UserID}";

            if (_cache.TryGetValue(lockoutKey, out _))
            {
                ModelState.AddModelError(string.Empty, "Akun dikunci sementara karena terlalu banyak percobaan gagal. Silakan tunggu 5 menit.");
                return View(model);
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT u.UserID, u.NamaLengkap, u.PasswordHash, r.RoleName 
                                 FROM tbl_Users u
                                 INNER JOIN tbl_Roles r ON u.RoleID = r.RoleID
                                 WHERE u.UserID = @UserID AND u.IsActive = 1";

                var userRecord = await conn.QueryFirstOrDefaultAsync<UserAuthDto>(query, new { UserID = model.UserID });

                if (userRecord != null)
                {
                    string storedHash = userRecord.PasswordHash ?? "";
                    bool isValid = HashHelper.VerifyPassword(model.Password, storedHash);

                    if (isValid)
                    {
                        // SEAMLESS AUTO-MIGRATION: Jika kata sandi masih menggunakan legacy SHA-256, upgrade ke PBKDF2 (Salted)
                        if (HashHelper.IsLegacyHash(storedHash))
                        {
                            string newPbkdf2Hash = HashHelper.HashPassword(model.Password);
                            string updateQuery = "UPDATE tbl_Users SET PasswordHash = @NewHash WHERE UserID = @UserID";
                            await conn.ExecuteAsync(updateQuery, new { NewHash = newPbkdf2Hash, UserID = model.UserID });
                        }

                        string namaLengkap = userRecord.NamaLengkap;
                        string roleName = userRecord.RoleName;

                        // Validasi Keamanan Lapis 1
                        string[] allowedRoles = { "Administrator", "Manager Teknik", "Supervisor Teknik", "Section Teknik", "Teknisi", "Dashboard", "WHS.SP", "Admin Teknik" };
                        
                        if (!allowedRoles.Contains(roleName))
                        {
                            ModelState.AddModelError(string.Empty, $"Akses Ditolak. Divisi/Role '{roleName}' tidak diizinkan masuk ke aplikasi Teknik P1.");
                            return View(model);
                        }

                        // Terbitkan "KTP Digital" (Claims)
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, model.UserID), 
                            new Claim(ClaimTypes.Name, namaLengkap),            
                            new Claim(ClaimTypes.Role, roleName)                
                        };

                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);

                        // Masukkan KTP ke dalam Cookie Browser
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                        // Login Sukses: Bersihkan hitungan gagal
                        _cache.Remove(attemptKey);

                        if (roleName == "Dashboard")
                        {
                            return RedirectToAction("TvDashboard", "Home");
                        }
                        else if (roleName == "WHS.SP")
                        {
                            return RedirectToAction("Index", "Sparepart");
                        }
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        // Kata sandi salah: Tambah hitungan gagal
                        int attempts = _cache.TryGetValue(attemptKey, out int currentAttempts) ? currentAttempts : 0;
                        attempts++;

                        if (attempts >= 5)
                        {
                            _cache.Set(lockoutKey, true, TimeSpan.FromMinutes(5));
                            _cache.Remove(attemptKey);
                            ModelState.AddModelError(string.Empty, "Akun dikunci sementara karena terlalu banyak percobaan gagal. Silakan tunggu 5 menit.");
                        }
                        else
                        {
                            _cache.Set(attemptKey, attempts, TimeSpan.FromMinutes(10));
                            ModelState.AddModelError(string.Empty, $"Kata sandi salah. Percobaan tersisa: {5 - attempts}");
                        }
                        return View(model);
                    }
                }
                else
                {
                    // User tidak ditemukan
                    ModelState.AddModelError(string.Empty, "NIK / User ID tidak ditemukan atau tidak aktif.");
                    return View(model);
                }
            }
        }

        public async Task<IActionResult> Logout()
        {
            // Hancurkan KTP Digital
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
