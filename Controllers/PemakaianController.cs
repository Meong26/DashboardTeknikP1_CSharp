using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using DashboardTeknikP1.Models;
using DashboardTeknikP1.Repositories;
using DashboardTeknikP1.Services;
using Microsoft.AspNetCore.Authorization;

namespace DashboardTeknikP1.Controllers
{
    [Authorize]
    public class PemakaianController : Controller
    {
        private readonly PemakaianRepository _pemakaianRepo;
        private readonly PemakaianService _pemakaianService;

        public PemakaianController(PemakaianRepository pemakaianRepo, PemakaianService pemakaianService)
        {
            _pemakaianRepo = pemakaianRepo;
            _pemakaianService = pemakaianService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SaveData([FromBody] List<PengambilanSparepart> payload)
        {
            if (payload == null || !payload.Any()) return BadRequest("Tidak ada data.");
            try
            {
                foreach (var item in payload)
                {
                    item.TanggalInput = DateTime.Now;
                    item.TotalHarga = item.HargaSatuanSaatIni * (decimal)item.JumlahPengambilan;
                    item.Status = "ESTIMASI"; 
                }
                _pemakaianRepo.InsertBulkPengambilan(payload);
                return Ok("Saved");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpGet]
        public async Task<IActionResult> GetHistoryData()
        {
            try
            {
                int currentYear = DateTime.Now.Year;
                var allHistory = await _pemakaianRepo.GetAllHistoryAsync(currentYear);
                var sapRawTuple = await _pemakaianRepo.GetRawSapDataAsync(currentYear);
                
                var resultData = _pemakaianService.ProcessHistoryData(allHistory, sapRawTuple.Item1, sapRawTuple.Item2);

                return Json(resultData, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPost]
        public async Task<IActionResult> QuarantineItems([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Pilih item terlebih dahulu.");
            await _pemakaianRepo.ProcessBulkQuarantineAsync(ids);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RestoreItem(int id)
        {
            try
            {
                await _pemakaianRepo.RestoreToEstimasiAsync(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReturItem(int id)
        {
            await _pemakaianRepo.DeletePengambilanAsync(id);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ShiftToNextWeek(int id)
        {
            await _pemakaianRepo.ShiftToNextWeekAsync(id);
            return Ok();
        }
    }
}