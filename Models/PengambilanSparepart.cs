using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DashboardTeknikP1.Models
{
    public class PengambilanSparepart
    {
        public int PengambilanID { get; set; }
        public DateTime TanggalPengambilan { get; set; }
        public string MaterialNo { get; set; } = string.Empty;
        public double JumlahPengambilan { get; set; }
        public string TujuanPengambilan { get; set; } = string.Empty;
        public string NamaPengambil { get; set; } = string.Empty;
        public decimal HargaSatuanSaatIni { get; set; }
        public decimal TotalHarga { get; set; }
        public DateTime TanggalInput { get; set; }
        public string Status { get; set; } = "ESTIMASI"; // Nilai: ESTIMASI, KARANTINA

        [NotMapped]
        public string MaterialDesc { get; set; } = string.Empty;
    }
}