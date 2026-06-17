using Microsoft.AspNetCore.Mvc;
using System;
using DashboardTeknikP1.Models;
using DashboardTeknikP1.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace DashboardTeknikP1.Controllers
{
    [Authorize]
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
        public IActionResult Index()
        {
            var daftarTemuan = _repository.GetAllTemuan();
            return View(daftarTemuan);
        }

        // ====================================================================
        // 2. HALAMAN FORM INPUT BARU (CREATE - GET)
        // ====================================================================
        [HttpGet]
        public IActionResult Create()
        {

            // Ambil data mesin asli dari database melalui repositori
            var listMesin = _repository.GetAllMesin();

            // Titipkan data ke ViewBag agar opsi select/dropdown muncul di layar View
            ViewBag.DaftarMesin = listMesin;

            return View();
        }

        // ====================================================================
        // 3. PROSES SIMPAN DATA FORM (CREATE - POST)
        // ====================================================================
        [HttpPost]
        public IActionResult Create(TemuanAbnormal model)
        {
            // Bersihkan validasi Model agar tidak error saat dikirim kosong dari form
            ModelState.Remove("UserID");
            ModelState.Remove("StatusTemuan");
            ModelState.Remove("TanggalInput");

            if (ModelState.IsValid)
            {
                // Hardcode & otomatisasi sementara di sisi Backend
                model.UserID = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";
                model.TanggalInput = DateTime.Now;
                model.StatusTemuan = "OPEN";

                // Simpan ke database
                _repository.InsertTemuan(model);

                // Kembali ke halaman Index setelah berhasil simpan
                return RedirectToAction("Index");
            }

            // PENTING: Jika gagal validasi, ViewBag harus diisi ulang agar dropdown tidak kosong
            ViewBag.DaftarMesin = _repository.GetAllMesin();

            return View(model);
        }

        // ====================================================================
        // 4. HALAMAN TUTUP LAPORAN (CLOSE - GET)
        // ====================================================================
        [HttpGet]
        public IActionResult Close(int id)
        {
            var temuan = _repository.GetTemuanById(id);
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
        [HttpPost]
        public IActionResult Close(int TemuanID, string TindakanKorektif)
        {
            // Eksekusi update ke database melalui repository
            _repository.CloseTemuan(TemuanID, TindakanKorektif);

            // Kembalikan ke halaman utama log teknik
            return RedirectToAction("Index");
        }
    }
}