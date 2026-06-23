using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using DashboardTeknikP1.Models;

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

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Bypass password khusus untuk fase POC (karena hash di DB masih dummy)
            if (model.Password != "indofood123")
            {
                ModelState.AddModelError("", "Kata sandi salah. (Petunjuk: gunakan 'indofood123')");
                return View(model);
            }

            bool isUserValid = false;
            string namaLengkap = "";
            string roleName = "";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Tarik data NIK sekaligus menempelkan nama Jabatannya (JOIN)
                string query = @"SELECT u.NamaLengkap, r.RoleName 
                                 FROM tbl_Users u
                                 INNER JOIN tbl_Roles r ON u.RoleID = r.RoleID
                                 WHERE u.UserID = @UserID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", model.UserID);
                    
                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            isUserValid = true;
                            namaLengkap = reader["NamaLengkap"].ToString();
                            roleName = reader["RoleName"].ToString();
                        }
                    }
                }
            }

            if (isUserValid)
            {
                // Terbitkan "KTP Digital" (Claims)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, model.UserID), // NIK
                    new Claim(ClaimTypes.Name, namaLengkap),            // Nama Asli
                    new Claim(ClaimTypes.Role, roleName)                // Jabatan/Akses
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // Masukkan KTP ke dalam Cookie Browser
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "NIK Karyawan tidak terdaftar di sistem.");
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