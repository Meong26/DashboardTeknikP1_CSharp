using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Text.Json;
using DashboardTeknikP1.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace DashboardTeknikP1.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly HomeRepository _repository;
        private readonly SettingRepository _settingRepo;
        private readonly IMemoryCache _cache;

        public HomeController(HomeRepository repository, SettingRepository settingRepo, IMemoryCache cache)
        {
            _repository = repository;
            _settingRepo = settingRepo;
            _cache = cache;
        }

        // 1. Luncurkan kerangka HTML kosong secepat kilat
        [Authorize(Roles = "Administrator,Manager,Supervisor,Section,Teknisi")]
        public async Task<IActionResult> Index()
        {
            ViewBag.TargetDowntime = await _settingRepo.GetSettingValueAsync("TargetDowntime", "1.5");
            ViewBag.TvModeDuration = await _settingRepo.GetSettingValueAsync("TvModeDuration", "10000");
            return View();
        }

        // Halaman TV Dashboard Mode
        [Authorize(Roles = "Administrator,Dashboard")]
        public async Task<IActionResult> TvDashboard()
        {
            ViewBag.TargetDowntime = await _settingRepo.GetSettingValueAsync("TargetDowntime", "1.5");
            ViewBag.TvModeDuration = await _settingRepo.GetSettingValueAsync("TvModeDuration", "10000");
            return View();
        }

        // 2. Jalur API khusus untuk menyuplai data ke Dashboard
        [Authorize(Roles = "Administrator,Manager,Supervisor,Section,Teknisi,Dashboard")]
        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            int currentYear = DateTime.Now.Year;
            string cacheKey = $"DashboardData_{currentYear}";

            // Coba ambil dari memori (RAM) Server
            if (!_cache.TryGetValue(cacheKey, out object resultData))
            {
                // Jika cache kosong (atau sudah kedaluwarsa), hajar ke Database
                var detailsYP = await _repository.GetDowntimeDetailsAsync(currentYear);
                var detailsYR = await _repository.GetProduksiDetailsAsync(currentYear);

                resultData = new
                {
                    ypData = detailsYP,
                    yrData = detailsYR
                };

                // Simpan ke Cache selama 1 menit (60 detik) untuk menangkal auto-refresh TV Dashboard
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(60));
                
                _cache.Set(cacheKey, resultData, cacheEntryOptions);
            }

            return Json(resultData, new JsonSerializerOptions { PropertyNamingPolicy = null });
        }
    }
}