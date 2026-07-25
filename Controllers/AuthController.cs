using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using DashboardTeknikP1.Models;
using DashboardTeknikP1.Helpers;
using Dapper;

namespace DashboardTeknikP1.Controllers
{
    public class AuthController : Controller
    {
        private readonly string _connectionString;

        public AuthController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
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
                }
            }

            ModelState.AddModelError("", "NIK Karyawan atau Kata Sandi tidak sesuai.");
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            // Hancurkan KTP Digital
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
