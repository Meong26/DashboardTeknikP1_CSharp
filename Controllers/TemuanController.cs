using Microsoft.AspNetCore.Mvc;
using System;
using DashboardTeknikP1.Models;
using DashboardTeknikP1.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace DashboardTeknikP1.Controllers
{
    [Authorize(Roles = "Administrator,Supervisor,Section,Teknisi,Dashboard")]
    public class TemuanController : Controller
    {
        private readonly TemuanRepository _repository;

        // Dependency Injection untuk Repositori Temuan
        public TemuanController(TemuanRepository repository)
        {
            _repository = repository;
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
    }
}