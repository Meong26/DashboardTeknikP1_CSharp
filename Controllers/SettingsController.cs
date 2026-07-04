using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DashboardTeknikP1.Models;
using DashboardTeknikP1.Repositories;

namespace DashboardTeknikP1.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class SettingsController : Controller
    {
        private readonly SettingRepository _settingRepo;

        public SettingsController(SettingRepository settingRepo)
        {
            _settingRepo = settingRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new SettingsViewModel
            {
                TargetDowntime = await _settingRepo.GetSettingValueAsync("TargetDowntime", "1.5"),
                TvModeDuration = await _settingRepo.GetSettingValueAsync("TvModeDuration", "10000")
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(SettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            // Update ke database via repository
            await _settingRepo.UpdateSettingAsync("TargetDowntime", model.TargetDowntime);
            await _settingRepo.UpdateSettingAsync("TvModeDuration", model.TvModeDuration);

            // Set notifikasi sukses
            TempData["SuccessMessage"] = "Pengaturan berhasil disimpan.";

            // Tetap di halaman pengaturan
            return RedirectToAction("Index");
        }
    }
}
