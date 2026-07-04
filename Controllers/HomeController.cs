using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Text.Json;
using DashboardTeknikP1.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace DashboardTeknikP1.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly HomeRepository _repository;
        private readonly SettingRepository _settingRepo;

        public HomeController(HomeRepository repository, SettingRepository settingRepo)
        {
            _repository = repository;
            _settingRepo = settingRepo;
        }

        // 1. Luncurkan kerangka HTML kosong secepat kilat
        public async Task<IActionResult> Index()
        {
            ViewBag.TargetDowntime = await _settingRepo.GetSettingValueAsync("TargetDowntime", "1.5");
            ViewBag.TvModeDuration = await _settingRepo.GetSettingValueAsync("TvModeDuration", "10000");
            return View();
        }

        // Halaman TV Dashboard Mode
        public async Task<IActionResult> TvDashboard()
        {
            ViewBag.TargetDowntime = await _settingRepo.GetSettingValueAsync("TargetDowntime", "1.5");
            ViewBag.TvModeDuration = await _settingRepo.GetSettingValueAsync("TvModeDuration", "10000");
            return View();
        }

        // 2. Jalur API khusus untuk menyuplai data ke Dashboard
        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            int currentYear = System.DateTime.Now.Year;
            // Menarik kedua data secara paralel (opsional, tapi ditunggu berurutan di sini)
            var detailsYP = await _repository.GetDowntimeDetailsAsync(currentYear);
            var detailsYR = await _repository.GetProduksiDetailsAsync(currentYear);

            // Menggabungkan dua data ke dalam satu objek JSON (ypData dan yrData)
            var resultData = new
            {
                ypData = detailsYP,
                yrData = detailsYR
            };

            return Json(resultData, new JsonSerializerOptions { PropertyNamingPolicy = null });
        }
    }
}