using System;

namespace DashboardTeknikP1.Models
{
    public class TemuanAbnormal
    {
        public int TemuanID { get; set; }
        public DateTime TanggalInput { get; set; }
        public string UserID { get; set; } = string.Empty;
        public string KodeMesin { get; set; } = string.Empty;
        public string Line { get; set; } = string.Empty;
        public string NamaMesin { get; set; } = string.Empty; 
        public string DeskripsiAbnormal { get; set; } = string.Empty;
        public string TindakanKorektif { get; set; } = string.Empty;
        public string StatusTemuan { get; set; } = string.Empty;
        public DateTime? TanggalClosed { get; set; }
        public string? ClosedBy { get; set; }
        public string? ClosedByName { get; set; }
    }
}