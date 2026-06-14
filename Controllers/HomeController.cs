using Microsoft.AspNetCore.Mvc;
using DashboardTeknikP1.Repositories;
using System.Text.Json;

namespace DashboardTeknikP1.Controllers
{
    public class HomeController : Controller
    {
        private readonly HomeRepository _repository;

        public HomeController(HomeRepository repository)
        {
            _repository = repository;
        }

        public IActionResult Index()
        {
            var summary = _repository.GetDowntimeSummary();
            var details = _repository.GetDowntimeDetails();
            var ewsParts = _repository.GetEwsSpareparts();

            // Amankan data ke ViewBag dengan konversi JSON agar bisa dibaca langsung oleh JavaScript Chart
            ViewBag.SummaryJson = JsonSerializer.Serialize(summary);
            ViewBag.DetailsJson = JsonSerializer.Serialize(details);
            ViewBag.EwsParts = ewsParts;

            return View();
        }
    }
}