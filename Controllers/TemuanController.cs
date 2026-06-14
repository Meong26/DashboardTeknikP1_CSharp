using Microsoft.AspNetCore.Mvc;
using DashboardTeknikP1.Models;
using DashboardTeknikP1.Repositories;

namespace DashboardTeknikP1.Controllers
{
    public class TemuanController : Controller
    {
        private readonly TemuanRepository _repository;

        public TemuanController(TemuanRepository repository)
        {
            _repository = repository;
        }

        // Halaman untuk melihat daftar semua laporan temuan (Untuk Supervisor)
        public IActionResult Index()
        {
            var daftarTemuan = _repository.GetAllTemuan();
            return View(daftarTemuan); // Mengirim data dari database ke layar View
        }

        // Halaman yang menampilkan Form Input kosong (Untuk Teknisi)
        public IActionResult Create()
        {
            return View();
        }

        // Method yang menangkap data saat teknisi menekan tombol "Simpan Laporan"
        [HttpPost]
        public IActionResult Create(TemuanAbnormal model)
        {
            
            ModelState.Remove("UserID");
            ModelState.Remove("StatusTemuan");

            // Validasi sederhana
            if (ModelState.IsValid)
            {
                // Simulasi sementara: Karena kita belum membuat sistem Login (Auth),
                // kita paksa (hardcode) UserID menggunakan data seed yang kita buat di SSMS.
                model.UserID = "ADMIN01";

                _repository.InsertTemuan(model);
                return RedirectToAction("Index"); // Kembali ke daftar laporan setelah sukses
            }
            
            return View(model); // Jika gagal/ada yang kosong, tampilkan form lagi
        }
    }
}