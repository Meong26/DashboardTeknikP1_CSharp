using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DashboardTeknikP1.Models;
using DashboardTeknikP1.Repositories;
using DashboardTeknikP1.Helpers;

namespace DashboardTeknikP1.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class UsersController : Controller
    {
        private readonly UserRepository _userRepo;

        public UsersController(UserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userRepo.GetAllUsersAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new UserCreateViewModel
            {
                AvailableRoles = await _userRepo.GetAllRolesAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserID = model.UserID,
                    NamaLengkap = model.NamaLengkap,
                    RoleID = model.RoleID,
                    PasswordHash = HashHelper.ComputeSha256Hash(model.Password)
                };

                bool success = await _userRepo.AddUserAsync(user);
                if (success)
                {
                    TempData["SuccessMessage"] = "Pengguna baru berhasil ditambahkan.";
                    return RedirectToAction("Index");
                }
                ModelState.AddModelError("UserID", "NIK/User ID sudah terdaftar.");
            }
            
            model.AvailableRoles = await _userRepo.GetAllRolesAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userRepo.GetUserByIdAsync(id);
            if (user == null) return NotFound();

            var model = new UserEditViewModel
            {
                UserID = user.UserID,
                NamaLengkap = user.NamaLengkap,
                RoleID = user.RoleID,
                AvailableRoles = await _userRepo.GetAllRolesAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserID = model.UserID,
                    NamaLengkap = model.NamaLengkap,
                    RoleID = model.RoleID
                };

                await _userRepo.UpdateUserAsync(user);
                TempData["SuccessMessage"] = "Data pengguna berhasil diperbarui.";
                return RedirectToAction("Index");
            }
            
            model.AvailableRoles = await _userRepo.GetAllRolesAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            
            await _userRepo.DeleteUserAsync(id);
            TempData["SuccessMessage"] = "Akun pengguna berhasil dinonaktifkan.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userRepo.GetUserByIdAsync(id);
            if (user == null) return NotFound();

            var model = new UserChangePasswordViewModel
            {
                UserID = user.UserID,
                NamaLengkap = user.NamaLengkap
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(UserChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                string hashed = HashHelper.ComputeSha256Hash(model.NewPassword);
                await _userRepo.ChangePasswordAsync(model.UserID, hashed);
                
                TempData["SuccessMessage"] = "Kata sandi berhasil diubah.";
                return RedirectToAction("Index");
            }
            
            return View(model);
        }
    }
}
