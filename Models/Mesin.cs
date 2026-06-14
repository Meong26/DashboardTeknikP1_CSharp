using System;

namespace DashboardTeknikP1.Models
{
    // Kelas ini adalah kembaran dari tabel tbl_Mesin di SQL Server
    public class Mesin
    {
        public string MesinID { get; set; }
        public string LineProduksi { get; set; }
        public string NamaMesin { get; set; }
    }
}