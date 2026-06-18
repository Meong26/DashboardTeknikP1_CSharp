using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using DashboardTeknikP1.Models;
using DashboardTeknikP1.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace DashboardTeknikP1.Controllers
{
    [Authorize]
    public class PemakaianController : Controller
    {
        private readonly PemakaianRepository _pemakaianRepo;

        public PemakaianController(PemakaianRepository pemakaianRepo)
        {
            _pemakaianRepo = pemakaianRepo;
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
                var allHistory = await _pemakaianRepo.GetAllHistoryAsync();
                var sapRawTuple = await _pemakaianRepo.GetRawSapDataAsync();
                
                var listFixData = sapRawTuple.Item1;
                var listYr21 = sapRawTuple.Item2;

                int currentIsoYear = System.Globalization.ISOWeek.GetYear(DateTime.Now);
                int currentIsoWeek = System.Globalization.ISOWeek.GetWeekOfYear(DateTime.Now);
                string currentWeekStr = $"{currentIsoYear}-W{currentIsoWeek}";
                
                DateTime targetWeeksAgo = DateTime.Now.AddDays(-7);
                int prevWeekYear = System.Globalization.ISOWeek.GetYear(targetWeeksAgo);
                int prevWeekNo = System.Globalization.ISOWeek.GetWeekOfYear(targetWeeksAgo);

                // Filter Data SAP YP14 (Minggu Lalu)
                var filteredFixData = listFixData.Where(x => 
                    System.Globalization.ISOWeek.GetYear(x.DocumentDate) == prevWeekYear && 
                    System.Globalization.ISOWeek.GetWeekOfYear(x.DocumentDate) == prevWeekNo
                ).ToList();

                double outputLastWeek = listYr21.Where(x => 
                    System.Globalization.ISOWeek.GetYear(x.PostingDate) == prevWeekYear && 
                    System.Globalization.ISOWeek.GetWeekOfYear(x.PostingDate) == prevWeekNo
                ).Sum(x => x.DelivQtyPcs);

                decimal costLastWeek = filteredFixData.Sum(x => x.MaterialCost);
                double ratioLastWeek = outputLastWeek > 0 ? (double)costLastWeek / outputLastWeek : 0;

                // ====================================================================
                // PEMISAHAN DATA BERDASARKAN STATUS NYATA DI DATABASE
                // ====================================================================
                var listEstimasi = allHistory.Where(x => x.Status.ToUpper() == "ESTIMASI").ToList();
                var listKarantina = allHistory.Where(x => x.Status.ToUpper() == "KARANTINA").ToList();

                var resultEstimasi = listEstimasi.Select(x => {
                    int isoYear = System.Globalization.ISOWeek.GetYear(x.TanggalPengambilan);
                    int isoWeek = System.Globalization.ISOWeek.GetWeekOfYear(x.TanggalPengambilan);
                    return new {
                        x.PengambilanID,
                        TanggalFormated = x.TanggalPengambilan.ToString("dd MMM yyyy"),
                        x.MaterialNo,
                        MaterialDesc = x.MaterialDesc, // Menggunakan kueri join dari database master
                        TujuanPengambilan = x.TujuanPengambilan,
                        NamaPengambil = x.NamaPengambil,
                        x.JumlahPengambilan,
                        HargaSatuanFormated = x.HargaSatuanSaatIni.ToString("N0"),
                        TotalHargaNumeric = (double)x.TotalHarga,
                        TotalHargaFormated = x.TotalHarga.ToString("N0"),
                        WeekYearKey = $"{isoYear}-W{isoWeek}",
                        WeekDisplay = $"Week {isoWeek} ({isoYear})"
                    };
                }).ToList();

                var resultKarantina = listKarantina.Select(x => new {
                    x.PengambilanID,
                    TanggalFormated = x.TanggalPengambilan.ToString("dd MMM yyyy"),
                    x.MaterialNo,
                    x.MaterialDesc,
                    x.TujuanPengambilan,
                    x.NamaPengambil,
                    x.JumlahPengambilan,
                    TotalHargaFormated = x.TotalHarga.ToString("N0")
                }).ToList();

                var resultFix = filteredFixData.Select(x => new {
                    PengambilanID = 0,
                    TanggalFormated = x.DocumentDate.ToString("dd MMM yyyy"),
                    MaterialNo = x.MaterialNo,
                    MaterialDesc = x.MaterialDescription,
                    TujuanPengambilan = x.EquipmentDescription, 
                    OrderNo = x.OrderNo, 
                    JumlahPengambilan = x.Qty,
                    HargaSatuanFormated = x.PricePerUnit.ToString("N0"),
                    TotalHargaNumeric = (double)x.MaterialCost,
                    TotalHargaFormated = x.MaterialCost.ToString("N0"),
                    WeekYearKey = "SAP_ACTUAL",
                    WeekDisplay = "Data SAP (Minggu Lalu)"
                }).ToList();

                return Json(new { 
                    currentWeek = currentWeekStr, 
                    dataEstimasi = resultEstimasi,
                    dataKarantina = resultKarantina, // Disuplai ke pop-up modal karantina
                    dataFix = resultFix,
                    costLastWeek = costLastWeek,
                    ratioLastWeek = ratioLastWeek
                }, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        // POST ACTION: Pindahkan item terpilih ke Status Karantina
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

        // POST ACTION: Hapus permanen dari database (Retur)
        [HttpPost]
        public async Task<IActionResult> ReturItem(int id)
        {
            await _pemakaianRepo.DeletePengambilanAsync(id);
            return Ok();
        }

        // POST ACTION: Ubah tanggal +7 Hari & kembalikan status ke Estimasi
        [HttpPost]
        public async Task<IActionResult> ShiftToNextWeek(int id)
        {
            await _pemakaianRepo.ShiftToNextWeekAsync(id);
            return Ok();
        }
    }
}