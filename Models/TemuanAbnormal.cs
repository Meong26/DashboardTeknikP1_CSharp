using System;

namespace DashboardTeknikP1.Models
{
    public class TemuanAbnormal
    {
        public int TemuanID { get; set; }
        public DateTime TanggalInput { get; set; }
        public string UserID { get; set; }
        public string MesinID { get; set; }
        public string DeskripsiAbnormal { get; set; }
        public string TindakanKorektif { get; set; }
        public string StatusTemuan { get; set; }
    }
}