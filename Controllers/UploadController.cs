using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using DashboardTeknikP1.Repositories;
using DashboardTeknikP1.Models;
using ClosedXML.Excel;
using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace DashboardTeknikP1.Controllers
{
    [Authorize(Roles = "Administrator,Section")]
    public class UploadController : Controller
    {
        private readonly UploadRepository _repository;

        public UploadController(UploadRepository repository)
        {
            _repository = repository;
        }

        public IActionResult Index()
        {
            ViewBag.LastUploadDates = _repository.GetLastUploadDates();
            return View();
        }

        [HttpPost]
        public IActionResult ProcessUpload(IFormFile fileYP11, IFormFile fileYR21, IFormFile fileSP, IFormFile fileYP14)
        {
            try 
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
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        int rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                        for (int row = 2; row <= rowCount; row++)
                        {
                            var data = new SAP_YP11
                            {
                                WeekKalendarIndofood = worksheet.Cell(row, 1).GetString(),
                                FunctionLocation = worksheet.Cell(row, 2).GetString(),
                                NotificationType = worksheet.Cell(row, 3).GetString(),
                                NotificationDesc = worksheet.Cell(row, 4).GetString(),
                                NotificationDate = ParseDate(worksheet.Cell(row, 5)),
                                TotalDownTimeInMinutes = ParseDouble(worksheet.Cell(row, 6)),
                                DownTimeStartTime = ParseTime(worksheet.Cell(row, 5), worksheet.Cell(row, 7)),
                                DownTimeEndTime = ParseTime(worksheet.Cell(row, 5), worksheet.Cell(row, 8)),
                                ActivityText = worksheet.Cell(row, 9).GetString(),
                                WageGroup_GroupShift = worksheet.Cell(row, 10).GetString(),
                                MasterReceipt = worksheet.Cell(row, 11).GetString(),
                                ProcessOrder = worksheet.Cell(row, 12).GetString(),
                                WorkCenterPPDesc = worksheet.Cell(row, 13).GetString(),
                                DownTimeCode_ActivityCodeDesc = worksheet.Cell(row, 14).GetString()
                            };
                            listData.Add(data);
                        }
                    }
                }
                _repository.InsertBulkYP11(listData);
                _repository.LogUpload("tbl_SAP_YP11");
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
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        int rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                        for (int row = 2; row <= rowCount; row++)
                        {
                            var data = new SAP_YR21
                            {
                                WeekOfBasicFinishedDate = worksheet.Cell(row, 1).GetString(),
                                PostingDate = ParseDate(worksheet.Cell(row, 2)),
                                ResourceName = worksheet.Cell(row, 3).GetString(),
                                WageGroup = worksheet.Cell(row, 4).GetString(),
                                GroupName = worksheet.Cell(row, 5).GetString(),
                                PlannedHour = ParseDouble(worksheet.Cell(row, 6)),
                                ActualHour = ParseDouble(worksheet.Cell(row, 7)),
                                StdOutputPcs = ParseDouble(worksheet.Cell(row, 8)),
                                DelivQtyPcs = ParseDouble(worksheet.Cell(row, 9)),
                                EffectivityPO_Pct = ParseDouble(worksheet.Cell(row, 10)),
                                Efficiency_Pct = ParseDouble(worksheet.Cell(row, 11)),
                                Ach_Pct = ParseDouble(worksheet.Cell(row, 12))
                            };
                            listData.Add(data);
                        }
                    }
                }
                _repository.InsertBulkYR21(listData);
                _repository.LogUpload("tbl_SAP_YR21");
            }

            // =========================================================
            // 3. PROSES FILE SPAREPART (SP) - STRUKTUR SIMPLIFIKASI BARU
            // =========================================================
            if (fileSP != null && fileSP.Length > 0)
            {
                var savedPriorities = _repository.GetExistingPriorities();

                _repository.TruncateTable("tbl_SAP_Sparepart");
                var rawList = new List<SAP_Sparepart>();
                using (var stream = new MemoryStream())
                {
                    fileSP.CopyTo(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        int rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                        for (int row = 2; row <= rowCount; row++)
                        {
                            var data = new SAP_Sparepart
                            {
                                Plant = worksheet.Cell(row, 1).GetString(),
                                Material = worksheet.Cell(row, 2).GetString(),
                                MaterialDescription = worksheet.Cell(row, 3).GetString(),
                                UoM = worksheet.Cell(row, 4).GetString(),
                                MovingUnitPrice = ParseDecimal(worksheet.Cell(row, 5)),
                                CurrentStock = ParseDouble(worksheet.Cell(row, 6)),
                                MatType = worksheet.Cell(row, 7).GetString(),
                                StorLoct = worksheet.Cell(row, 8).GetString(),
                                SafetyStock = 1
                            };
                            rawList.Add(data);
                        }
                    }
                }

                var groupedSpList = rawList
                    .GroupBy(x => x.Material ?? "UNKNOWN")
                    .Select(group => {
                        var firstItem = group.First();
                        string cleanMatNo = firstItem.Material != null ? firstItem.Material.Trim() : "";
                        firstItem.CurrentStock = group.Sum(x => x.CurrentStock);
                        
                        if (savedPriorities.Contains(cleanMatNo))
                        {
                            firstItem.Priority = "Y";
                        }
                        return firstItem;
                    }).ToList();

                _repository.InsertBulkSparepart(groupedSpList);
                _repository.LogUpload("tbl_SAP_Sparepart");
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
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        int rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                        
                        for (int row = 2; row <= rowCount; row++)
                        {
                            var data = new SAP_YP14
                            {
                                OrderType = worksheet.Cell(row, 1).GetString(),
                                OrderNo = worksheet.Cell(row, 2).GetString(),
                                Description = worksheet.Cell(row, 3).GetString(),
                                DocumentDate = ParseDate(worksheet.Cell(row, 4)),
                                MaterialNo = worksheet.Cell(row, 5).GetString(),
                                MaterialDescription = worksheet.Cell(row, 6).GetString(),
                                Qty = ParseDouble(worksheet.Cell(row, 7)),
                                PricePerUnit = ParseDecimal(worksheet.Cell(row, 8)),
                                UoM = worksheet.Cell(row, 9).GetString(),
                                MaterialCost = ParseDecimal(worksheet.Cell(row, 10)),
                                WorkCenter = worksheet.Cell(row, 11).GetString(),
                                EquipmentDescription = worksheet.Cell(row, 12).GetString(),
                                CostCenter = worksheet.Cell(row, 13).GetString(),
                                FuncLoc = worksheet.Cell(row, 14).GetString()
                            };
                            listData.Add(data);
                        }
                    }
                }
                _repository.InsertBulkYP14(listData);
                _repository.LogUpload("tbl_SAP_YP14");
            }

            TempData["SuccessMessage"] = "Data Excel SAP berhasil diunggah dan disimpan ke Database.";
            }
            catch (Exception ex) 
            {
                TempData["ErrorMessage"] = "Terjadi kesalahan sistem atau timeout saat memproses file. Pesan Error: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // =========================================================
        // HELPER METHODS (CLOSEDXML VERSION)
        // =========================================================
        private DateTime ParseDate(IXLCell cell)
        {
            var rawValue = cell.Value;
            
            if (rawValue.IsDateTime) return rawValue.GetDateTime();
            if (rawValue.IsNumber) return DateTime.FromOADate(rawValue.GetNumber());

            string dateText = cell.GetString()?.Trim() ?? "";
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

        private double ParseDouble(IXLCell cell)
        {
            var rawValue = cell.Value;
            
            if (rawValue.IsNumber) return rawValue.GetNumber();

            string text = cell.GetString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(text)) return 0;

            if (double.TryParse(text, out double res)) return res;
            if (double.TryParse(text.Replace(",", "."), CultureInfo.InvariantCulture, out double res2)) return res2;
            
            return 0;
        }

        private decimal ParseDecimal(IXLCell cell)
        {
            var rawValue = cell.Value;
            
            if (rawValue.IsNumber) return Convert.ToDecimal(rawValue.GetNumber());

            string text = cell.GetString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(text)) return 0;

            if (decimal.TryParse(text, out decimal res)) return res;
            if (decimal.TryParse(text.Replace(",", "."), CultureInfo.InvariantCulture, out decimal res2)) return res2;

            return 0;
        }

        private int ParseInt(IXLCell cell)
        {
            var rawValue = cell.Value;
            
            if (rawValue.IsNumber) return Convert.ToInt32(rawValue.GetNumber());

            string text = cell.GetString()?.Trim() ?? "";
            if (int.TryParse(text, out int res)) return res;
            
            if (double.TryParse(text.Replace(",", "."), CultureInfo.InvariantCulture, out double dResult))
                return Convert.ToInt32(dResult);

            return 0;
        }

        private DateTime ParseTime(IXLCell dateCell, IXLCell timeCell)
        {
            DateTime baseDate = ParseDate(dateCell);
            var rawTime = timeCell.Value;

            if (rawTime.IsDateTime)
            {
                var timeDt = rawTime.GetDateTime();
                return new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, timeDt.Hour, timeDt.Minute, timeDt.Second);
            }

            if (rawTime.IsNumber && rawTime.GetNumber() < 1)
                return baseDate.Add(TimeSpan.FromDays(rawTime.GetNumber()));

            string timeText = timeCell.GetString()?.Trim() ?? "";
            timeText = timeText.Replace(".", ":");

            if (TimeSpan.TryParse(timeText, out TimeSpan parsedTime))
                return baseDate.Add(parsedTime);

            return baseDate;
        }
    }
}