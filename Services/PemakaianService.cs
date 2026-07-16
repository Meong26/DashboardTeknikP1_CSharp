using System;
using System.Collections.Generic;
using System.Linq;
using DashboardTeknikP1.Models;

namespace DashboardTeknikP1.Services
{
    public class PemakaianService
    {
        public object ProcessHistoryData(
            List<PengambilanSparepart> allHistory, 
            List<SAP_YP14> listFixData, 
            List<SAP_YR21> listYr21)
        {
            // 1. BANGUN KAMUS KALENDER DINAMIS
            var dictCalendar = new Dictionary<DateTime, string>();
            foreach (var yr in listYr21.Where(x => !string.IsNullOrEmpty(x.WeekOfBasicFinishedDate) && x.WeekOfBasicFinishedDate.Length >= 6))
            {
                DateTime tgl = yr.PostingDate.Date;
                if (!dictCalendar.ContainsKey(tgl))
                {
                    string rawWeek = yr.WeekOfBasicFinishedDate; 
                    string yearPart = rawWeek.Substring(0, 4);
                    string weekPart = int.Parse(rawWeek.Substring(4, 2)).ToString(); 
                    dictCalendar[tgl] = $"{yearPart}-W{weekPart}";
                }
            }

            // Fungsi Penerjemah Tanggal -> Minggu Indofood
            string GetIndofoodWeekString(DateTime targetDate)
            {
                DateTime dateOnly = targetDate.Date;
                
                if (dictCalendar.TryGetValue(dateOnly, out string exactWeek)) return exactWeek;

                for (int i = 1; i <= 3; i++)
                {
                    if (dictCalendar.TryGetValue(dateOnly.AddDays(-i), out string backWeek)) return backWeek;
                    if (dictCalendar.TryGetValue(dateOnly.AddDays(i), out string fwdWeek)) return fwdWeek;
                }

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

            string GetPlantFromFuncLoc(string funcLoc)
            {
                if (string.IsNullOrEmpty(funcLoc)) return "";
                
                if (funcLoc.StartsWith("2808-P1-PRODU00-")) return "Plant 1";
                if (funcLoc.StartsWith("2808-P2-PRODU00-")) return "Plant 2";
                if (funcLoc.StartsWith("2808-P3-PRODU00-")) return "Plant 3";
                
                return "";
            }

            string GetPlantFromResourceName(string resourceName)
            {
                if (string.IsNullOrEmpty(resourceName)) return "";
                
                string prefix = resourceName.TrimStart();
                if (prefix.Length >= 5)
                {
                    string firstFive = prefix.Substring(0, 5);
                    var p1 = new[] { "L. 01", "L. 02", "L. 03", "L. 04", "L. 05", "L. 06", "L. 07", "L. 08", "L. 09" };
                    if (p1.Contains(firstFive)) return "Plant 1";

                    if (firstFive.StartsWith("L. ") && int.TryParse(firstFive.Substring(3, 2), out int num5))
                    {
                        if (num5 >= 10 && num5 <= 30) return "Plant 2";
                        if (num5 >= 31 && num5 <= 33) return "Plant 3";
                    }
                }

                // Fallback super aman jika format spasi berantakan (misal "L.01" atau "L.  01")
                var match = System.Text.RegularExpressions.Regex.Match(resourceName, @"L\.?\s*0*(\d+)");
                if (match.Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out int num))
                    {
                        if (num >= 1 && num <= 9) return "Plant 1";
                        if (num >= 10 && num <= 30) return "Plant 2";
                        if (num >= 31 && num <= 33) return "Plant 3";
                    }
                }
                return "";
            }

            // 2. TERAPKAN KAMUS
            string currentWeekStr = GetIndofoodWeekString(DateTime.Now);

            decimal costTotal = listFixData.Sum(x => x.MaterialCost);
            double outputTotal = listYr21.Sum(x => x.DelivQtyPcs);
            double ratioTotal = outputTotal > 0 ? (double)costTotal / outputTotal : 0;

            var outputPerWeek = listYr21
                .Where(x => !string.IsNullOrEmpty(x.WeekOfBasicFinishedDate) && x.WeekOfBasicFinishedDate.Length >= 6)
                .GroupBy(x => GetIndofoodWeekString(x.PostingDate))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.DelivQtyPcs));

            var outputPerWeekPlant = listYr21
                .Where(x => !string.IsNullOrEmpty(x.WeekOfBasicFinishedDate) && x.WeekOfBasicFinishedDate.Length >= 6)
                .GroupBy(x => new { Week = GetIndofoodWeekString(x.PostingDate), Plant = GetPlantFromResourceName(x.ResourceName) })
                .ToDictionary(g => $"{g.Key.Week}|{g.Key.Plant}", g => g.Sum(x => x.DelivQtyPcs));

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
                    Plant = x.Plant,
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
                x.Plant,
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
                    Plant = GetPlantFromFuncLoc(x.FuncLoc),
                    WeekYearKey = weekKey,
                    WeekDisplay = GetDisplayWeek(weekKey, " Aktual")
                };
            }).ToList();

            return new { 
                currentWeek = currentWeekStr, 
                dataEstimasi = resultEstimasi,
                dataKarantina = resultKarantina,
                dataFix = resultFix,
                costLastWeek = costTotal,
                ratioLastWeek = ratioTotal,
                outputPerWeek = outputPerWeek,
                outputPerWeekPlant = outputPerWeekPlant
            };
        }
    }
}
