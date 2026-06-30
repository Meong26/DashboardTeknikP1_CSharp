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

        public HomeController(HomeRepository repository)
        {
            _repository = repository;
        }

        // 1. Luncurkan kerangka HTML kosong secepat kilat
        public IActionResult Index()
        {
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