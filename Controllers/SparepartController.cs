using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using DashboardTeknikP1.Repositories;
using DashboardTeknikP1.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Microsoft.AspNetCore.Authorization;

namespace DashboardTeknikP1.Controllers
{
    [Authorize]
    public class SparepartController : Controller
    {
        private readonly SparepartRepository _sparepartRepo;

        public SparepartController(SparepartRepository sparepartRepo)
        {
            _sparepartRepo = sparepartRepo;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetApiData()
        {
            var dataDb = await _sparepartRepo.GetAllSparepartsAsync();
            return Json(dataDb, new JsonSerializerOptions { PropertyNamingPolicy = null });
        }

        // DTO untuk menerima kiriman data dari Frontend
        public class PRItemInput
        {
            public string MaterialNo { get; set; } = string.Empty;
            public double Qty { get; set; }
            public string Remark { get; set; } = string.Empty;
        }

        public class PRRequestModel
        {
            public List<PRItemInput> Items { get; set; } = new List<PRItemInput>();
        }

        [HttpPost]
        public async Task<IActionResult> ExportPR([FromBody] PRRequestModel request)
        {
            if (request == null || !request.Items.Any())
            {
                return BadRequest("Tidak ada material yang dipilih.");
            }

            // 1. Lokasi "Cetakan" Excel Anda di server
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Format_PR.xlsx");
            FileInfo templateFile = new FileInfo(templatePath);

            if (!templateFile.Exists)
            {
                return BadRequest("Gagal mengunduh: File template Format_PR.xlsx tidak ditemukan di server.");
            }

            var allSpareparts = await _sparepartRepo.GetAllSparepartsAsync();

            // 2. Buka "Cetakan" menggunakan EPPlus
            using (var package = new ExcelPackage(templateFile))
            {
                // Jadikan Sheet 1 sebagai MASTER CETAKAN (Murni, tidak boleh diisi langsung)
                var baseTemplateSheet = package.Workbook.Worksheets[0];

                // Batas maksimal baris item per halaman agar tidak menabrak baris 37
                int maxItemsPerPage = 23;
                int totalItems = request.Items.Count;

                // Hitung butuh berapa halaman (Misal: 25 item / 23 = 1.08 -> dibulatkan ke atas jadi 2 halaman)
                int totalPages = (int)Math.Ceiling((double)totalItems / maxItemsPerPage);

                string namaPemesan = User.Identity?.Name ?? "Djaka";
                string tanggalCetak = $"Date : {DateTime.Now.ToString("dd/MM/yyyy")}";

                // 3. Looping Pembuatan Halaman
                for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
                {
                    // Fotokopi Master Cetakan menjadi Sheet baru (Halaman 1, Halaman 2, dst)
                    var currentSheet = package.Workbook.Worksheets.Add($"Halaman {pageIndex + 1}", baseTemplateSheet);

                    // Suntikkan Data Statis (Header & Footer) ke Sheet saat ini
                    currentSheet.Cells["J5"].Value = tanggalCetak;
                    currentSheet.Cells["J5"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    currentSheet.Cells["G37"].Value = $"({namaPemesan})";

                    // Ambil potongan data khusus untuk halaman ini saja (23 item per potong)
                    var itemsForThisPage = request.Items.Skip(pageIndex * maxItemsPerPage).Take(maxItemsPerPage).ToList();

                    int startRow = 7;
                    for (int i = 0; i < itemsForThisPage.Count; i++)
                    {
                        int currentRow = startRow + i;
                        var inputItem = itemsForThisPage[i];
                        
                        // PERUBAHAN: Gunakan Material, MaterialDescription, UoM
                        var dbItem = allSpareparts.FirstOrDefault(x => x.Material != null && x.Material.Trim() == inputItem.MaterialNo.Trim());
                        int globalNumber = (pageIndex * maxItemsPerPage) + i + 1;
                        
                        currentSheet.Cells[currentRow, 2].Value = globalNumber;
                        currentSheet.Cells[currentRow, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        currentSheet.Cells[currentRow, 5].Value = inputItem.MaterialNo;
                        currentSheet.Cells[currentRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        
                        currentSheet.Cells[currentRow, 6].Value = dbItem?.MaterialDescription ?? "-";

                        currentSheet.Cells[currentRow, 7].Value = inputItem.Qty;
                        currentSheet.Cells[currentRow, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        currentSheet.Cells[currentRow, 8].Value = dbItem?.UoM ?? "PC";
                        currentSheet.Cells[currentRow, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        currentSheet.Cells[currentRow, 9].Value = "14 Hari";
                        currentSheet.Cells[currentRow, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        currentSheet.Cells[currentRow, 12].Value = inputItem.Remark;
                    }
                }

                // 4. Hapus Master Cetakan kosongnya agar tidak ikut terunduh
                package.Workbook.Worksheets.Delete(baseTemplateSheet);

                // 5. Kembalikan hasilnya ke browser sebagai file Excel baru
                var fileBytes = package.GetAsByteArray();
                string fileName = $"PR_Technical_P1_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> UpdatePriorities([FromBody] List<string> priorityMaterials)
        {
            try
            {
                if (priorityMaterials == null) priorityMaterials = new List<string>();
                await _sparepartRepo.SavePrioritiesBulkAsync(priorityMaterials);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}