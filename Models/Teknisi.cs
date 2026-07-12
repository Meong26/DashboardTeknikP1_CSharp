using System.ComponentModel.DataAnnotations;

namespace DashboardTeknikP1.Models
{
    public class Teknisi
    {
        [Key]
        public string NIK { get; set; } = string.Empty;
        public string Nama { get; set; } = string.Empty;
        public string Plant { get; set; } = string.Empty;
    }
}
