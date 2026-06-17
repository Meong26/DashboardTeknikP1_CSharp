using System.ComponentModel.DataAnnotations;

namespace DashboardTeknikP1.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "NIK wajib diisi")]
        public string UserID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kata sandi wajib diisi")]
        public string Password { get; set; } = string.Empty;
    }
}