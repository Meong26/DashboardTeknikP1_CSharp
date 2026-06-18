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
        public IActionResult ProcessUpload(IFormFile fileYP11, IFormFile fileYR21, IFormFile fileSP, IFormFile fileYP14)
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
                                NotificationDate = ParseDate(worksheet.Cells[row, 5]),
                                TotalDownTimeInMinutes = ParseDouble(worksheet.Cells[row, 6]),
                                DownTimeStartTime = ParseTime(worksheet.Cells[row, 5], worksheet.Cells[row, 7]),
                                DownTimeEndTime = ParseTime(worksheet.Cells[row, 5], worksheet.Cells[row, 8]),
                                ActivityText = worksheet.Cells[row, 9].Text,
                                WageGroup_GroupShift = worksheet.Cells[row, 10].Text,
                                MasterReceipt = worksheet.Cells[row, 11].Text,
                                ProcessOrder = worksheet.Cells[row, 12].Text,
                                WorkCenterPPDesc = worksheet.Cells[row, 13].Text,
                                DownTimeCode_ActivityCodeDesc = worksheet.Cells[row, 14].Text
                            };
                            listData.Add(data);
                        }
                    }
                }
                _repository.InsertBulkYP11(listData);
            }

            // =========================================================
            // 2. PROSES FILE YR21 (PRODUKSI)
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
                                PostingDate = ParseDate(worksheet.Cells[row, 2]),
                                ResourceName = worksheet.Cells[row, 3].Text,
                                WageGroup = worksheet.Cells[row, 4].Text,
                                GroupName = worksheet.Cells[row, 5].Text,
                                PlannedHour = ParseDouble(worksheet.Cells[row, 6]),
                                ActualHour = ParseDouble(worksheet.Cells[row, 7]),
                                StdOutputPcs = ParseDouble(worksheet.Cells[row, 8]),
                                DelivQtyPcs = ParseDouble(worksheet.Cells[row, 9]),
                                EffectivityPO_Pct = ParseDouble(worksheet.Cells[row, 10]),
                                Efficiency_Pct = ParseDouble(worksheet.Cells[row, 11]),
                                Ach_Pct = ParseDouble(worksheet.Cells[row, 12])
                            };
                            listData.Add(data);
                        }
                    }
                }
                _repository.InsertBulkYR21(listData);
            }

            // =========================================================
            // 3. PROSES FILE SPAREPART (SP) - KEMBALI KE 14 KOLOM
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
                                TotalQtyStock = ParseDouble(worksheet.Cells[row, 8]),
                                BUn = worksheet.Cells[row, 9].Text,
                                MvgAvgPriceIDR = ParseDecimal(worksheet.Cells[row, 10]),
                                TotValuatedStockIDR = ParseDecimal(worksheet.Cells[row, 11]),
                                DateOfLastMvt = ParseDate(worksheet.Cells[row, 12]),
                                LamaTdkBergerakDay = ParseInt(worksheet.Cells[row, 13]),
                                StorBin = worksheet.Cells[row, 14].Text,
                                SafetyStock = 1 // Aturan bawaan
                            };
                            rawList.Add(data);
                        }
                    }
                }

                var groupedSpList = rawList
                    .GroupBy(x => x.MaterialNo ?? "UNKNOWN")
                    .Select(group => {
                        var firstItem = group.First();
                        firstItem.TotalQtyStock = group.Sum(x => x.TotalQtyStock);
                        firstItem.TotValuatedStockIDR = group.Sum(x => x.TotValuatedStockIDR);
                        firstItem.DateOfLastMvt = group.Max(x => x.DateOfLastMvt);
                        return firstItem;
                    }).ToList();

                _repository.InsertBulkSparepart(groupedSpList);
            }

            // =========================================================
            // 4. PROSES FILE YP14 (BIAYA AKTUAL SPAREPART)
            // =========================================================
            if (fileYP14 != null && fileYP14.Length > 0)
            {
                _repository.TruncateTable("tbl_SAP_YP14");
                var listData = new List<SAP_YP14>();
                using (var stream = new MemoryStream())
                {
                    fileYP14.CopyTo(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                        int rowCount = worksheet.Dimension.Rows;
                        for (int row = 2; row <= rowCount; row++)
                        {
                            var data = new SAP_YP14
                            {
                                OrderType = worksheet.Cells[row, 1].Text,
                                OrderNo = worksheet.Cells[row, 2].Text,
                                Description = worksheet.Cells[row, 3].Text,
                                DocumentDate = ParseDate(worksheet.Cells[row, 4]),
                                MaterialNo = worksheet.Cells[row, 5].Text,
                                MaterialDescription = worksheet.Cells[row, 6].Text,
                                Qty = ParseDouble(worksheet.Cells[row, 7]),
                                PricePerUnit = ParseDecimal(worksheet.Cells[row, 8]),
                                UoM = worksheet.Cells[row, 9].Text,
                                MaterialCost = ParseDecimal(worksheet.Cells[row, 10]),
                                WorkCenter = worksheet.Cells[row, 11].Text,
                                EquipmentDescription = worksheet.Cells[row, 12].Text,
                                CostCenter = worksheet.Cells[row, 13].Text
                            };
                            listData.Add(data);
                        }
                    }
                }
                _repository.InsertBulkYP14(listData);
            }

            return RedirectToAction("Index");
        }

        // =========================================================
        // HELPER METHODS (MENGGUNAKAN NATIVE VALUE EPPLUS AGAR AMAN 100%)
        // =========================================================
        private DateTime ParseDate(ExcelRange cell)
        {
            var rawValue = cell.Value;
            
            // Prioritas 1: Jika Excel sudah mengenali sebagai DateTime/Angka Serial
            if (rawValue is DateTime dt) return dt;
            if (rawValue is double d) return DateTime.FromOADate(d);

            // Prioritas 2: Manipulasi teks string (Sapu Jagat format titik/strip YP14)
            string dateText = cell.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(dateText)) return new DateTime(1900, 1, 1);

            dateText = dateText.Replace(".", "/").Replace("-", "/");

            if (double.TryParse(dateText, out double serialDate) && serialDate > 10000)
                return DateTime.FromOADate(serialDate);

            CultureInfo idCulture = new CultureInfo("id-ID");
            if (DateTime.TryParse(dateText, idCulture, DateTimeStyles.None, out DateTime result))
                return result;

            CultureInfo enCulture = new CultureInfo("en-US");
            if (DateTime.TryParse(dateText, enCulture, DateTimeStyles.None, out DateTime resultEn))
                return resultEn;

            return new DateTime(1900, 1, 1);
        }

        private double ParseDouble(ExcelRange cell)
        {
            var rawValue = cell.Value;
            
            // Prioritas 1: Tangkap nilai asli tanpa peduli format tulisan
            if (rawValue is double d) return d;
            if (rawValue is int i) return Convert.ToDouble(i);
            if (rawValue is decimal dec) return Convert.ToDouble(dec);

            // Prioritas 2: Jika terpaksa berupa teks murni
            string text = cell.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(text)) return 0;

            if (double.TryParse(text, out double res)) return res;
            if (double.TryParse(text.Replace(",", "."), CultureInfo.InvariantCulture, out double res2)) return res2;
            
            return 0;
        }

        private decimal ParseDecimal(ExcelRange cell)
        {
            var rawValue = cell.Value;
            
            if (rawValue is decimal dec) return dec;
            if (rawValue is double d) return Convert.ToDecimal(d);
            if (rawValue is int i) return Convert.ToDecimal(i);

            string text = cell.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(text)) return 0;

            if (decimal.TryParse(text, out decimal res)) return res;
            if (decimal.TryParse(text.Replace(",", "."), CultureInfo.InvariantCulture, out decimal res2)) return res2;

            return 0;
        }

        private int ParseInt(ExcelRange cell)
        {
            var rawValue = cell.Value;
            
            if (rawValue is int i) return i;
            if (rawValue is double d) return Convert.ToInt32(d);

            string text = cell.Text?.Trim() ?? "";
            if (int.TryParse(text, out int res)) return res;
            
            if (double.TryParse(text.Replace(",", "."), CultureInfo.InvariantCulture, out double dResult))
                return Convert.ToInt32(dResult);

            return 0;
        }

        private DateTime ParseTime(ExcelRange dateCell, ExcelRange timeCell)
        {
            DateTime baseDate = ParseDate(dateCell);
            var rawTime = timeCell.Value;

            if (rawTime is DateTime timeDt)
                return new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, timeDt.Hour, timeDt.Minute, timeDt.Second);

            if (rawTime is double timeDouble && timeDouble < 1)
                return baseDate.Add(TimeSpan.FromDays(timeDouble));

            string timeText = timeCell.Text?.Trim() ?? "";
            timeText = timeText.Replace(".", ":");

            if (TimeSpan.TryParse(timeText, out TimeSpan parsedTime))
                return baseDate.Add(parsedTime);

            return baseDate;
        }
    }
}