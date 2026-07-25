using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using DashboardTeknikP1.Models;
using DashboardTeknikP1.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using ClosedXML.Excel;

namespace DashboardTeknikP1.Controllers
{
    [Authorize(Roles = "Administrator,Supervisor,Section,Teknisi,Dashboard")]
    public class TemuanController : Controller
    {
        private readonly TemuanRepository _repository;
        private readonly IWebHostEnvironment _env;

        // Dependency Injection untuk Repositori Temuan dan Environment
        public TemuanController(TemuanRepository repository, IWebHostEnvironment env)
        {
            _repository = repository;
            _env = env;
        }

        // ====================================================================
        // 1. HALAMAN DAFTAR TEMUAN (INDEX)
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var daftarTemuan = await _repository.GetAllTemuanAsync();
            return View(daftarTemuan);
        }

        // ====================================================================
        // 2. HALAMAN FORM INPUT BARU (CREATE - GET)
        // ====================================================================
        [Authorize(Roles = "Administrator,Section,Teknisi")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Ambil data mesin asli dari database melalui repositori
            var listMesin = await _repository.GetAllMesinAsync();

            // Titipkan data ke ViewBag agar opsi select/dropdown muncul di layar View
            ViewBag.DaftarMesin = listMesin;

            return View();
        }

        // ====================================================================
        // 3. PROSES SIMPAN DATA FORM (CREATE - POST)
        // ====================================================================
        [Authorize(Roles = "Administrator,Section,Teknisi")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("KodeMesin,Line,DeskripsiAbnormal,TindakanKorektif")] TemuanAbnormal model)
        {
            // Karena kita menggunakan Bind untuk over-posting, kita lengkapi properti lainnya
            model.UserID = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";
            model.TanggalInput = DateTime.Now;
            model.StatusTemuan = "OPEN";

            // Hapus validasi untuk properti yang diisi otomatis di atas (karena implicitly required oleh sistem)
            ModelState.Remove("UserID");
            ModelState.Remove("StatusTemuan");
            ModelState.Remove("TanggalInput");

            if (ModelState.IsValid)
            {
                // Simpan ke database
                await _repository.InsertTemuanAsync(model);

                // Kembali ke halaman Index setelah berhasil simpan
                return RedirectToAction("Index");
            }

            // PENTING: Jika gagal validasi, ViewBag harus diisi ulang agar dropdown tidak kosong
            ViewBag.DaftarMesin = await _repository.GetAllMesinAsync();

            return View(model);
        }

        // ====================================================================
        // 4. HALAMAN TUTUP LAPORAN (CLOSE - GET)
        // ====================================================================
        [Authorize(Roles = "Administrator,Section")]
        [HttpGet]
        public async Task<IActionResult> Close(int id)
        {
            var temuan = await _repository.GetTemuanByIdAsync(id);
            if (temuan == null)
            {
                return NotFound(); // Jika ID tidak ditemukan di database
            }

            // Pastikan hanya laporan yang masih OPEN yang bisa ditutup
            if (temuan.StatusTemuan.ToUpper() == "CLOSED")
            {
                return RedirectToAction("Index");
            }

            return View(temuan);
        }

        // ====================================================================
        // 5. PROSES EKSEKUSI TUTUP LAPORAN (CLOSE - POST)
        // ====================================================================
        [Authorize(Roles = "Administrator,Section")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int TemuanID, string TindakanKorektif)
        {
            string activeUser = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";
            // Eksekusi update ke database melalui repository
            await _repository.CloseTemuanAsync(TemuanID, TindakanKorektif, activeUser);

            // Kembalikan ke halaman utama log teknik
            return RedirectToAction("Index");
        }

        // ====================================================================
        // 6. API UNTUK FORM INLINE (AMBIL MASTER MESIN & RIWAYAT)
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> GetApiData()
        {
            var listMesin = await _repository.GetAllMesinAsync();
            var listTemuan = await _repository.GetAllTemuanAsync();
            
            // Format ulang data temuan agar mudah dibaca JavaScript
            var formattedTemuan = listTemuan.Select(t => new {
                t.TemuanID,
                TanggalFormated = t.TanggalInput.ToString("dd MMM yyyy HH:mm"),
                Pelapor = t.UserID,
                Line = t.Line ?? "-", 
                t.KodeMesin,
                NamaMesin = t.NamaMesin ?? "Mesin Tidak Dikenal",
                t.DeskripsiAbnormal,
                TindakanKorektif = string.IsNullOrEmpty(t.TindakanKorektif) ? "-" : t.TindakanKorektif,
                Status = t.StatusTemuan ?? "OPEN",
                TanggalClosedFormated = t.TanggalClosed?.ToString("dd MMM yyyy HH:mm") ?? "-",
                ClosedByName = t.ClosedByName ?? t.ClosedBy ?? "-"
            }).ToList();

            // PERBAIKAN: Tambahkan JsonSerializerOptions agar huruf besar/kecil (PascalCase) tidak diubah ke camelCase
            return Json(new { 
                mesin = listMesin, 
                history = formattedTemuan 
            }, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = null });
        }

        // ====================================================================
        // 7. API UNTUK SIMPAN MULTIPLE DATA (FORM INLINE EXCEL)
        // ====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDataFast([FromBody] List<TemuanAbnormal> payload)
        {
            if (payload == null || !payload.Any()) return BadRequest("Data laporan kosong.");

            string activeUser = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

            try
            {
                foreach (var item in payload)
                {
                    item.UserID = activeUser;
                    item.TanggalInput = DateTime.Now;
                    item.StatusTemuan = "OPEN";
                    if (string.IsNullOrEmpty(item.TindakanKorektif)) item.TindakanKorektif = "";

                    await _repository.InsertTemuanAsync(item);
                }
                return Ok("Berhasil disimpan");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // DTO Request Export Rencana Kerja PM
        public class RencanaKerjaExportRequest
        {
            public List<int> SelectedIds { get; set; } = new List<int>();
        }

        // ====================================================================
        // 8. EXPORT RENCANA KERJA PM (PREVENTIVE MAINTENANCE) KE EXCEL
        // ====================================================================
        [Authorize(Roles = "Administrator,Section")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportRencanaKerjaPM([FromBody] RencanaKerjaExportRequest request)
        {
            if (request == null || request.SelectedIds == null || !request.SelectedIds.Any())
            {
                return BadRequest("Tidak ada data temuan yang dipilih untuk rencana kerja.");
            }

            string templatePath = Path.Combine(_env.ContentRootPath, "Templates", "Form_PM.xlsx");
            FileInfo templateFile = new FileInfo(templatePath);

            if (!templateFile.Exists)
            {
                return BadRequest("Gagal mengunduh: File template Form_PM.xlsx tidak ditemukan di server.");
            }

            var selectedTemuan = await _repository.GetTemuanByIdsAsync(request.SelectedIds);
            if (!selectedTemuan.Any())
            {
                return BadRequest("Data temuan terpilih tidak ditemukan.");
            }

            DateTime now = DateTime.Now;
            CultureInfo idCulture = new CultureInfo("id-ID");
            
            // C3: Tanggal Cetak (misal: Jumat, 24 Juli 2026)
            string tanggalCetak = now.ToString("dddd, d MMMM yyyy", idCulture);

            // C4: Tanggal Hari Minggu Setelah Dokumen Dicetak
            int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilSunday == 0) daysUntilSunday = 7;
            DateTime tanggalMinggu = now.AddDays(daysUntilSunday);
            string tanggalRencanaPM = tanggalMinggu.ToString("dddd, d MMMM yyyy", idCulture);

            // Nama Login pengunduh
            string activeUserFull = User.Identity?.Name ?? "Admin/Section";

            using (var workbook = new XLWorkbook(templateFile.FullName))
            {
                var sheet = workbook.Worksheet(1);

                // Isikan Header Tanggal
                sheet.Cell("C3").Value = tanggalCetak;
                sheet.Cell("C4").Value = tanggalRencanaPM;

                // Isikan Baris Rencana Kerja
                int startRow = 8;
                for (int i = 0; i < selectedTemuan.Count; i++)
                {
                    int currentRow = startRow + i;
                    var item = selectedTemuan[i];

                    sheet.Cell(currentRow, 1).Value = i + 1;                         // A8: Nomor Increment
                    sheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    sheet.Cell(currentRow, 2).Value = "";                             // B8: Kosongkan

                    sheet.Cell(currentRow, 3).Value = item.Line ?? "";                // C8: Line
                    sheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    sheet.Cell(currentRow, 4).Value = item.KodeMesin ?? "";           // D8: KodeMesin dari Database
                    sheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    sheet.Cell(currentRow, 5).Value = item.DeskripsiAbnormal ?? "";   // E8: Deskripsi kendala
                    sheet.Cell(currentRow, 6).Value = "";                             // F8: Kosongkan
                    sheet.Cell(currentRow, 7).Value = "";                             // G8: Kosongkan
                    sheet.Cell(currentRow, 8).Value = "";                             // H8: Kosongkan
                    sheet.Cell(currentRow, 9).Value = "";                             // I8: Kosongkan
                }

                // C55: Nama Login
                sheet.Cell("C55").Value = activeUserFull;
                sheet.Cell("C55").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                // H55: Kosongkan
                sheet.Cell("H55").Value = "";

                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    workbook.SaveAs(ms);
                    fileBytes = ms.ToArray();
                }

                string fileName = $"Rencana_Kerja_PM_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}