using System;

namespace DashboardTeknikP1.Models
{
    public class SAP_Sparepart
    {
        public string Plant { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public string UoM { get; set; } = string.Empty;
        public decimal MovingUnitPrice { get; set; }
        public double CurrentStock { get; set; }
        public int SafetyStock { get; set; }
        public string MatType { get; set; } = string.Empty;
        public string StorLoct { get; set; } = string.Empty;
        public string? Priority { get; set; } = string.Empty;
    }
}