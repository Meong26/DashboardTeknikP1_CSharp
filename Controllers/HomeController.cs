using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Text.Json;
using DashboardTeknikP1.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using DashboardTeknikP1.Helpers;
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
        [Authorize(Roles = "Administrator,Manager Teknik,Supervisor Teknik,Section Teknik,Teknisi")]
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
        [Authorize(Roles = "Administrator,Manager Teknik,Supervisor Teknik,Section Teknik,Teknisi,Dashboard")]
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetDashboardData(int? year)
        {
            int targetYear = year ?? DateTime.Now.Year;
            string cacheKey = $"DashboardData_{targetYear}";

            // Coba ambil dari memori (RAM) Server
            if (!_cache.TryGetValue(cacheKey, out object resultData))
            {
                // Jika cache kosong (atau sudah kedaluwarsa), hajar ke Database
                var detailsYP = await _repository.GetDowntimeDetailsAsync(targetYear);
                var detailsYR = await _repository.GetProduksiDetailsAsync(targetYear);

                resultData = new
                {
                    ypData = detailsYP,
                    yrData = detailsYR
                };

                // Simpan ke Cache selama 5 menit (300 detik) untuk mencegah CPU overload (Targeted Caching),
                // namun akan langsung terhapus secara otomatis jika user melakukan Upload Data Baru SAP
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(300))
                    .AddExpirationToken(new CancellationChangeToken(CacheSignal.TokenSource.Token));
                
                _cache.Set(cacheKey, resultData, cacheEntryOptions);
            }

            return Json(resultData, new JsonSerializerOptions { PropertyNamingPolicy = null });
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableYears()
        {
            var years = await _repository.GetAvailableYearsAsync();
            return Json(years);
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
