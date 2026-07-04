using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DashboardTeknikP1.Models
{
    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "NIK/UserID harus diisi")]
        [Display(Name = "NIK / User ID")]
        public string UserID { get; set; }

        [Required(ErrorMessage = "Nama Lengkap harus diisi")]
        [Display(Name = "Nama Lengkap")]
        public string NamaLengkap { get; set; }

        [Required(ErrorMessage = "Jabatan/Role harus dipilih")]
        [Display(Name = "Jabatan / Akses")]
        public int RoleID { get; set; }

        [Required(ErrorMessage = "Kata Sandi awal harus diisi")]
        [Display(Name = "Kata Sandi")]
        [MinLength(6, ErrorMessage = "Kata sandi minimal 6 karakter")]
        public string Password { get; set; }
        
        public IEnumerable<Role>? AvailableRoles { get; set; }
    }

    public class UserEditViewModel
    {
        [Required]
        public string UserID { get; set; }

        [Required(ErrorMessage = "Nama Lengkap harus diisi")]
        [Display(Name = "Nama Lengkap")]
        public string NamaLengkap { get; set; }

        [Required(ErrorMessage = "Jabatan/Role harus dipilih")]
        [Display(Name = "Jabatan / Akses")]
        public int RoleID { get; set; }

        public IEnumerable<Role>? AvailableRoles { get; set; }
    }

    public class UserChangePasswordViewModel
    {
        [Required]
        public string UserID { get; set; }
        
        public string? NamaLengkap { get; set; } // Hanya untuk ditampillkan

        [Required(ErrorMessage = "Kata Sandi Baru harus diisi")]
        [Display(Name = "Kata Sandi Baru")]
        [MinLength(6, ErrorMessage = "Kata sandi minimal 6 karakter")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Konfirmasi Kata Sandi harus diisi")]
        [Display(Name = "Konfirmasi Kata Sandi")]
        [Compare("NewPassword", ErrorMessage = "Kata sandi dan konfirmasi tidak cocok.")]
        public string ConfirmPassword { get; set; }
    }
}
