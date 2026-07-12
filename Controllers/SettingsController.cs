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
        private readonly TeknisiRepository _teknisiRepo;

        public SettingsController(SettingRepository settingRepo, TeknisiRepository teknisiRepo)
        {
            _settingRepo = settingRepo;
            _teknisiRepo = teknisiRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new SettingsViewModel
            {
                TargetDowntime = await _settingRepo.GetSettingValueAsync("TargetDowntime", "1.5"),
                TvModeDuration = await _settingRepo.GetSettingValueAsync("TvModeDuration", "10000"),
                TeknisiList = await _teknisiRepo.GetAllTeknisiAsync()
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeknisi(Teknisi model)
        {
            try
            {
                await _teknisiRepo.InsertTeknisiAsync(model);
                TempData["SuccessMessage"] = "Teknisi berhasil ditambahkan.";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal menambah teknisi: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTeknisi(Teknisi model)
        {
            try
            {
                await _teknisiRepo.UpdateTeknisiAsync(model);
                TempData["SuccessMessage"] = "Teknisi berhasil diupdate.";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal mengupdate teknisi: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTeknisi(string nik)
        {
            try
            {
                await _teknisiRepo.DeleteTeknisiAsync(nik);
                TempData["SuccessMessage"] = "Teknisi berhasil dihapus.";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal menghapus teknisi: " + ex.Message;
            }
            return RedirectToAction("Index");
        }
    }
}
