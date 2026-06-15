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
            var detailsYP = _repository.GetDowntimeDetails();
            var detailsYR = _repository.GetProduksiDetails();

            ViewBag.DetailsJson = JsonSerializer.Serialize(detailsYP);
            ViewBag.Yr21Json = JsonSerializer.Serialize(detailsYR);
            
            // EWS Sparepart sementara tidak kita kirim ke view ini karena fokus ke YR21 & YP11
            return View();
        }
    }
}