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

            // 2. TERAPKAN KAMUS
            string currentWeekStr = GetIndofoodWeekString(DateTime.Now);

            decimal costTotal = listFixData.Sum(x => x.MaterialCost);
            double outputTotal = listYr21.Sum(x => x.DelivQtyPcs);
            double ratioTotal = outputTotal > 0 ? (double)costTotal / outputTotal : 0;

            var outputPerWeek = listYr21
                .Where(x => !string.IsNullOrEmpty(x.WeekOfBasicFinishedDate) && x.WeekOfBasicFinishedDate.Length >= 6)
                .GroupBy(x => GetIndofoodWeekString(x.PostingDate))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.DelivQtyPcs));

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
                outputPerWeek = outputPerWeek
            };
        }
    }
}
