using System.ComponentModel.DataAnnotations;

namespace DashboardTeknikP1.Models
{
    public class SettingsViewModel
    {
        [Required(ErrorMessage = "Target Downtime harus diisi")]
        [Display(Name = "Target Maksimal Downtime (%)")]
        public string TargetDowntime { get; set; }

        [Required(ErrorMessage = "Durasi TV Mode harus diisi")]
        [Display(Name = "Durasi Slide Mode TV (milidetik)")]
        public string TvModeDuration { get; set; }
    }
}
