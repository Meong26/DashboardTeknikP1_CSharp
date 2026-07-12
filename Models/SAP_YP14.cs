using System;

namespace DashboardTeknikP1.Models
{
    public class SAP_YP14
    {
        public int RecordID { get; set; }
        public string OrderType { get; set; } = string.Empty;
        public string OrderNo { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DocumentDate { get; set; }
        public string MaterialNo { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public double Qty { get; set; }
        public decimal PricePerUnit { get; set; }
        public string UoM { get; set; } = string.Empty;
        public decimal MaterialCost { get; set; }
        public string WorkCenter { get; set; } = string.Empty;
        public string EquipmentDescription { get; set; } = string.Empty;
        public string CostCenter { get; set; } = string.Empty;
        public string FuncLoc { get; set; } = string.Empty;
    }
}