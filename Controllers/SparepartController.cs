using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Tasks;
using DashboardTeknikP1.Repositories;

namespace DashboardTeknikP1.Controllers
{
    public class SparepartController : Controller
    {
        private readonly SparepartRepository _sparepartRepo;

        public SparepartController(SparepartRepository sparepartRepo)
        {
            _sparepartRepo = sparepartRepo;
        }

        // 1. Fungsi ini sekarang HANYA meluncurkan HTML kosong secepat kilat
        public IActionResult Index()
        {
            // ViewBag.DataJson TELAH DIHAPUS agar HTML tidak bengkak
            return View();
        }

        // 2. Fungsi API Asinkronus untuk menyuplai data ke JavaScript di latar belakang
        [HttpGet]
        public async Task<IActionResult> GetApiData()
        {
            var dataDb = await _sparepartRepo.GetAllSparepartsAsync();
            
            // PropertyNamingPolicy = null memastikan format huruf besar/kecil (PascalCase) 
            // tidak diubah secara otomatis oleh .NET, agar JavaScript lama Anda tetap berfungsi.
            return Json(dataDb, new JsonSerializerOptions { PropertyNamingPolicy = null });
        }
    }
}