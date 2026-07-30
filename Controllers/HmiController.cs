using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace DashboardTeknikP1.Controllers
{
    [Authorize(Roles = "Administrator,Manager,Supervisor,Section,Teknisi,Dashboard")]
    public class HmiController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DashboardTeknikP1.Repositories.HmiRepository _hmiRepo;

        public HmiController(DashboardTeknikP1.Repositories.HmiRepository hmiRepo, IHttpClientFactory httpClientFactory)
        {
            _hmiRepo = hmiRepo;
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Administrator,Supervisor,Section")]
        public async Task<IActionResult> Logs()
        {
            var logs = await _hmiRepo.GetRecentLogsAsync(200);
            return View(logs);
        }

        [HttpPost]
        public async Task<IActionResult> LogAccess([FromForm] string ip, [FromForm] string hmiName)
        {
            var userId = User.Identity?.Name ?? "Unknown";
            await _hmiRepo.InsertLogAsync(userId, ip ?? "Unknown IP", hmiName ?? "Unknown HMI");
            return Ok();
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> CheckStatus(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return Json(new { online = false, latencyMs = 0 });

            try
            {
                var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, 1000); 

                if (reply.Status == IPStatus.Success)
                {
                    try 
                    {
                        var client = _httpClientFactory.CreateClient();
                        client.Timeout = TimeSpan.FromSeconds(3);
                        var response = await client.GetAsync($"http://{ip}/");
                    }
                    catch { }
                    
                    return Json(new { online = true, latencyMs = reply.RoundtripTime });
                }
            }
            catch
            {
                // Fallback jika ping terhalang ICMP
            }

            return Json(new { online = false, latencyMs = 0 });
        }

    }
}
