using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using DashboardTeknikP1.Repositories;
using DashboardTeknikP1.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;

namespace DashboardTeknikP1.Controllers
{
    [Authorize(Roles = "Administrator,Supervisor,Section,Teknisi,WHS.SP,Dashboard")]
    public class SparepartController : Controller
    {
        private readonly SparepartRepository _sparepartRepo;
        private readonly IWebHostEnvironment _env;

        public SparepartController(SparepartRepository sparepartRepo, IWebHostEnvironment env)
        {
            _sparepartRepo = sparepartRepo;
            _env = env;
        }

        [Authorize(Roles = "Administrator,Supervisor,Section,Teknisi,WHS.SP")]
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

        [Authorize(Roles = "Administrator,Supervisor,Section")]
        [HttpPost]
        public async Task<IActionResult> ExportPR([FromBody] PRRequestModel request)
        {
            if (request == null || !request.Items.Any())
            {
                return BadRequest("Tidak ada material yang dipilih.");
            }

            // 1. Lokasi "Cetakan" Excel Anda di server
            string templatePath = Path.Combine(_env.ContentRootPath, "Templates", "Format_PR.xlsx");
            FileInfo templateFile = new FileInfo(templatePath);

            if (!templateFile.Exists)
            {
                return BadRequest("Gagal mengunduh: File template Format_PR.xlsx tidak ditemukan di server.");
            }

            var allSpareparts = await _sparepartRepo.GetAllSparepartsAsync();

            // 2. Buka "Cetakan" menggunakan ClosedXML
            using (var workbook = new XLWorkbook(templateFile.FullName))
            {
                // Jadikan Sheet 1 sebagai MASTER CETAKAN (Murni, tidak boleh diisi langsung)
                var baseTemplateSheet = workbook.Worksheet(1);

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
                    var currentSheet = baseTemplateSheet.CopyTo($"Halaman {pageIndex + 1}");

                    // Suntikkan Data Statis (Header & Footer) ke Sheet saat ini
                    currentSheet.Cell("H5").Value = tanggalCetak;
                    currentSheet.Cell("H5").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    currentSheet.Cell("E37").Value = $"({namaPemesan})";

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
                        
                        currentSheet.Cell(currentRow, 2).Value = globalNumber;
                        currentSheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        currentSheet.Cell(currentRow, 3).Value = inputItem.MaterialNo;
                        currentSheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        
                        currentSheet.Cell(currentRow, 4).Value = dbItem?.MaterialDescription ?? "-";

                        currentSheet.Cell(currentRow, 5).Value = inputItem.Qty;
                        currentSheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        currentSheet.Cell(currentRow, 6).Value = dbItem?.UoM ?? "PC";
                        currentSheet.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        currentSheet.Cell(currentRow, 7).Value = "14 Hari";
                        currentSheet.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        currentSheet.Cell(currentRow, 8).Value = dbItem?.CurrentStock ?? 0;
                        currentSheet.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        currentSheet.Cell(currentRow, 10).Value = dbItem?.StorLoct ?? "-";
                        currentSheet.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    
                        currentSheet.Cell(currentRow, 12).Value = inputItem.Remark;
                    }
                }

                // 4. Hapus Master Cetakan kosongnya agar tidak ikut terunduh
                baseTemplateSheet.Delete();

                // 5. Kembalikan hasilnya ke browser sebagai file Excel baru
                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    workbook.SaveAs(ms);
                    fileBytes = ms.ToArray();
                }
                
                string fileName = $"PR_Technical_P1_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
        
        [Authorize(Roles = "Administrator,Supervisor,Section")]
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
