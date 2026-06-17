using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using DashboardTeknikP1.Repositories;
using DashboardTeknikP1.Models;
using OfficeOpenXml;
using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace DashboardTeknikP1.Controllers
{
    [Authorize]
    public class UploadController : Controller
    {
        private readonly UploadRepository _repository;

        public UploadController(UploadRepository repository)
        {
            _repository = repository;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ProcessUpload(IFormFile fileYP11, IFormFile fileYR21, IFormFile fileSP)
        {
            // =========================================================
            // 1. PROSES FILE YP11 (DOWNTIME)
            // =========================================================
            if (fileYP11 != null && fileYP11.Length > 0)
            {
                _repository.TruncateTable("tbl_SAP_YP11");
                var listData = new List<SAP_YP11>();

                using (var stream = new MemoryStream())
                {
                    fileYP11.CopyTo(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                        int rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var data = new SAP_YP11
                            {
                                WeekKalendarIndofood = worksheet.Cells[row, 1].Text,
                                FunctionLocation = worksheet.Cells[row, 2].Text,
                                NotificationType = worksheet.Cells[row, 3].Text,
                                NotificationDesc = worksheet.Cells[row, 4].Text,
                                NotificationDate = ParseDate(worksheet.Cells[row, 5].Text),
                                TotalDownTimeInMinutes = ParseDouble(worksheet.Cells[row, 6].Text),
                                DownTimeStartTime = ParseTime(worksheet.Cells[row, 5].Text, worksheet.Cells[row, 7].Text),
                                DownTimeEndTime = ParseTime(worksheet.Cells[row, 5].Text, worksheet.Cells[row, 8].Text),
                                ActivityText = worksheet.Cells[row, 9].Text,
                                WageGroup_GroupShift = worksheet.Cells[row, 10].Text,
                                MasterReceipt = worksheet.Cells[row, 11].Text,
                                ProcessOrder = worksheet.Cells[row, 12].Text,
                                WorkCenterPPDesc = worksheet.Cells[row, 13].Text,
                                DownTimeCode_ActivityCodeDesc = worksheet.Cells[row, 14].Text
                            };
                            listData.Add(data); // Simpan ke wadah dulu
                        }
                    }
                }
                // Kirim seluruh wadah untuk dibombardir ke SQL sekaligus
                _repository.InsertBulkYP11(listData);
            }

            // =========================================================
            // 2. PROSES FILE YR21 (PRODUKSI & EFFICIENCY)
            // =========================================================
            if (fileYR21 != null && fileYR21.Length > 0)
            {
                _repository.TruncateTable("tbl_SAP_YR21");
                var listData = new List<SAP_YR21>();

                using (var stream = new MemoryStream())
                {
                    fileYR21.CopyTo(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                        int rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var data = new SAP_YR21
                            {
                                WeekOfBasicFinishedDate = worksheet.Cells[row, 1].Text,
                                PostingDate = ParseDate(worksheet.Cells[row, 2].Text),
                                ResourceName = worksheet.Cells[row, 3].Text,
                                WageGroup = worksheet.Cells[row, 4].Text,
                                GroupName = worksheet.Cells[row, 5].Text,
                                PlannedHour = ParseDouble(worksheet.Cells[row, 6].Text),
                                ActualHour = ParseDouble(worksheet.Cells[row, 7].Text),
                                StdOutputPcs = ParseDouble(worksheet.Cells[row, 8].Text),
                                DelivQtyPcs = ParseDouble(worksheet.Cells[row, 9].Text),
                                EffectivityPO_Pct = ParseDouble(worksheet.Cells[row, 10].Text),
                                Efficiency_Pct = ParseDouble(worksheet.Cells[row, 11].Text),
                                Ach_Pct = ParseDouble(worksheet.Cells[row, 12].Text)
                            };
                            listData.Add(data);
                        }
                    }
                }
                _repository.InsertBulkYR21(listData);
            }

            // =========================================================
            // 3. PROSES FILE SPAREPART (SUKU CADANG)
            // =========================================================
            if (fileSP != null && fileSP.Length > 0)
            {
                _repository.TruncateTable("tbl_SAP_Sparepart");
                var rawList = new List<SAP_Sparepart>();

                using (var stream = new MemoryStream())
                {
                    fileSP.CopyTo(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                        int rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var data = new SAP_Sparepart
                            {
                                PlantCode = worksheet.Cells[row, 1].Text,
                                MType = worksheet.Cells[row, 2].Text,
                                MatGrp = worksheet.Cells[row, 3].Text,
                                MatrGroupDescription = worksheet.Cells[row, 4].Text,
                                SLoc = worksheet.Cells[row, 5].Text,
                                MaterialNo = worksheet.Cells[row, 6].Text,
                                MaterialNoDescription = worksheet.Cells[row, 7].Text,
                                TotalQtyStock = ParseDouble(worksheet.Cells[row, 8].Text),
                                BUn = worksheet.Cells[row, 9].Text,
                                MvgAvgPriceIDR = ParseDecimal(worksheet.Cells[row, 10].Text),
                                TotValuatedStockIDR = ParseDecimal(worksheet.Cells[row, 11].Text),
                                DateOfLastMvt = ParseDate(worksheet.Cells[row, 12].Text),
                                LamaTdkBergerakDay = ParseInt(worksheet.Cells[row, 13].Text),
                                StorBin = worksheet.Cells[row, 14].Text,
                                SafetyStock = 1 // Bisa disesuaikan dengan rule bisnis
                            };
                            rawList.Add(data);
                        }
                    }
                }

                // Kalkulasi Agregat menggunakan LINQ di memori C# (Jauh lebih cepat dari SQL)
                // Jika ada MaterialNo yang duplikat, stok dan harganya otomatis digabungkan.
                var groupedSpList = rawList
                    .GroupBy(x => x.MaterialNo ?? "UNKNOWN")
                    .Select(group =>
                    {
                        var firstItem = group.First();
                        firstItem.TotalQtyStock = group.Sum(x => x.TotalQtyStock);
                        firstItem.TotValuatedStockIDR = group.Sum(x => x.TotValuatedStockIDR);
                        firstItem.DateOfLastMvt = group.Max(x => x.DateOfLastMvt);
                        return firstItem;
                    }).ToList();

                _repository.InsertBulkSparepart(groupedSpList);
            }

            return RedirectToAction("Index");
        }

        // =========================================================
        // HELPER METHODS
        // =========================================================
        private DateTime ParseDate(string dateText)
        {
            dateText = dateText?.Trim() ?? "";
            CultureInfo idCulture = new CultureInfo("id-ID");
            if (DateTime.TryParse(dateText, idCulture, DateTimeStyles.None, out DateTime result))
                return result;
            return new DateTime(1900, 1, 1);
        }

        private double ParseDouble(string numberText)
        {
            numberText = numberText ?? "";
            if (double.TryParse(numberText, out double result))
                return result;
            return 0;
        }

        private decimal ParseDecimal(string decimalText)
        {
            decimalText = decimalText ?? "";
            if (decimal.TryParse(decimalText, out decimal result))
                return result;
            return 0;
        }

        private int ParseInt(string intText)
        {
            intText = intText ?? "";
            if (int.TryParse(intText, out int result))
                return result;
            return 0;
        }

        private DateTime ParseTime(string dateText, string timeText)
        {
            dateText = dateText?.Trim() ?? "";
            timeText = timeText?.Trim() ?? "";
            CultureInfo idCulture = new CultureInfo("id-ID");
            if (DateTime.TryParse($"{dateText} {timeText}", idCulture, DateTimeStyles.None, out DateTime result))
                return result;
            return new DateTime(1900, 1, 1);
        }
    }
}