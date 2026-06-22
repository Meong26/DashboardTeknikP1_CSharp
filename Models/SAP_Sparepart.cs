using System;

namespace DashboardTeknikP1.Models
{
    public class SAP_Sparepart
    {
        public string PlantCode { get; set; }
        public string MType { get; set; }
        public string MatGrp { get; set; }
        public string MatrGroupDescription { get; set; }
        public string SLoc { get; set; }
        public string MaterialNo { get; set; }
        public string MaterialNoDescription { get; set; }
        public double TotalQtyStock { get; set; }
        public string BUn { get; set; }
        public decimal MvgAvgPriceIDR { get; set; }
        public decimal TotValuatedStockIDR { get; set; }
        public DateTime DateOfLastMvt { get; set; }
        public int LamaTdkBergerakDay { get; set; }
        public string StorBin { get; set; }
        public int SafetyStock { get; set; }
        public DateTime LastUploadTimestamp { get; set; }
        public string? Priority { get; set; } = string.Empty;
    }
}