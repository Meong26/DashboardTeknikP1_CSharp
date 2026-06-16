using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using DashboardTeknikP1.Repositories;

namespace DashboardTeknikP1.Controllers
{
    public class SparepartController : Controller
    {
        private readonly SparepartRepository _sparepartRepo;

        // Tangkap IConfiguration dari kerangka kerja .NET
        public SparepartController(IConfiguration configuration)
        {
            // Teruskan configuration ke Repository
            _sparepartRepo = new SparepartRepository(configuration);
        }

        public IActionResult Index()
        {
            var dataDb = _sparepartRepo.GetAllSpareparts();

            // Ubah menjadi JSON agar bisa dikonsumsi oleh JavaScript di View
            ViewBag.DataJson = JsonSerializer.Serialize(dataDb);

            return View();
        }
    }
}