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

                // ==============================================================
                // 1. BANGUN KAMUS KALENDER DINAMIS DARI DATA YR21 SAP
                // ==============================================================
                var dictCalendar = new Dictionary<DateTime, string>();
                foreach (var yr in listYr21.Where(x => !string.IsNullOrEmpty(x.WeekOfBasicFinishedDate) && x.WeekOfBasicFinishedDate.Length >= 6))
                {
                    DateTime tgl = yr.PostingDate.Date;
                    if (!dictCalendar.ContainsKey(tgl))
                    {
                        string rawWeek = yr.WeekOfBasicFinishedDate; // cth: "202601"
                        string yearPart = rawWeek.Substring(0, 4);
                        string weekPart = int.Parse(rawWeek.Substring(4, 2)).ToString(); // Ubah "01" jadi "1"
                        dictCalendar[tgl] = $"{yearPart}-W{weekPart}";
                    }
                }

                // Fungsi Penerjemah Tanggal -> Minggu Indofood
                string GetIndofoodWeekString(DateTime targetDate)
                {
                    DateTime dateOnly = targetDate.Date;
                    
                    // Prioritas 1: Tanggal tersebut ada di produksi YR21
                    if (dictCalendar.TryGetValue(dateOnly, out string exactWeek)) return exactWeek;

                    // Prioritas 2: Toleransi jika pabrik libur (Maju/Mundur max 3 hari) cari tanggal terdekat
                    for (int i = 1; i <= 3; i++)
                    {
                        if (dictCalendar.TryGetValue(dateOnly.AddDays(-i), out string backWeek)) return backWeek;
                        if (dictCalendar.TryGetValue(dateOnly.AddDays(i), out string fwdWeek)) return fwdWeek;
                    }

                    // Prioritas 3: Fallback (jika YR21 belum diupload sama sekali di server)
                    int isoY = System.Globalization.ISOWeek.GetYear(dateOnly);
                    int isoW = System.Globalization.ISOWeek.GetWeekOfYear(dateOnly);
                    int adjW = isoW <= 2 ? 1 : isoW - 1; 
                    return $"{isoY}-W{adjW}";
                }

                string GetDisplayWeek(string weekKey, string suffix = "") 
                {
                    var parts = weekKey.Split("-W");
                    if(parts.Length == 2) return $"Week {parts[1]} ({parts[0]}){suffix}";
                    return weekKey + suffix;
                }

                // ==============================================================
                // 2. TERAPKAN KAMUS KALENDER KE SELURUH DATA
                // ==============================================================
                string currentWeekStr = GetIndofoodWeekString(DateTime.Now);

                decimal costTotal = listFixData.Sum(x => x.MaterialCost);
                double outputTotal = listYr21.Sum(x => x.DelivQtyPcs);
                double ratioTotal = outputTotal > 0 ? (double)costTotal / outputTotal : 0;

                var outputPerWeek = listYr21
                    .Where(x => !string.IsNullOrEmpty(x.WeekOfBasicFinishedDate) && x.WeekOfBasicFinishedDate.Length >= 6)
                    .GroupBy(x => GetIndofoodWeekString(x.PostingDate))
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.DelivQtyPcs));

                var listEstimasi = allHistory.Where(x => x.Status.ToUpper() == "ESTIMASI").ToList();
                var listKarantina = allHistory.Where(x => x.Status.ToUpper() == "KARANTINA").ToList();

                var resultEstimasi = listEstimasi.Select(x => {
                    string weekKey = GetIndofoodWeekString(x.TanggalPengambilan);
                    return new {
                        x.PengambilanID,
                        TanggalFormated = x.TanggalPengambilan.ToString("dd MMM yyyy"),
                        x.MaterialNo,
                        MaterialDesc = x.MaterialDesc, 
                        TujuanPengambilan = x.TujuanPengambilan,
                        NamaPengambil = x.NamaPengambil,
                        x.JumlahPengambilan,
                        HargaSatuanFormated = x.HargaSatuanSaatIni.ToString("N0"),
                        TotalHargaNumeric = (double)x.TotalHarga,
                        TotalHargaFormated = x.TotalHarga.ToString("N0"),
                        WeekYearKey = weekKey,
                        WeekDisplay = GetDisplayWeek(weekKey)
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

                var resultFix = listFixData.Select(x => {
                    string weekKey = GetIndofoodWeekString(x.DocumentDate);
                    return new {
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
                        WeekYearKey = weekKey,
                        WeekDisplay = GetDisplayWeek(weekKey, " Aktual")
                    };
                }).ToList();

                return Json(new { 
                    currentWeek = currentWeekStr, 
                    dataEstimasi = resultEstimasi,
                    dataKarantina = resultKarantina,
                    dataFix = resultFix,
                    costLastWeek = costTotal,
                    ratioLastWeek = ratioTotal,
                    outputPerWeek = outputPerWeek
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