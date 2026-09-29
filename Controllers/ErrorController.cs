using Microsoft.AspNetCore.Mvc;

namespace DashboardTeknikP1.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            ViewBag.StatusCode = statusCode;
            return View("Index");
        }

        [Route("Error")]
        public IActionResult Error()
        {
            ViewBag.StatusCode = 500;
            return View("Index");
        }
    }
}
