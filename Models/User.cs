using System;

namespace DashboardTeknikP1.Models
{
    public class User
    {
        public string UserID { get; set; }
        public string NamaLengkap { get; set; }
        public string PasswordHash { get; set; }
        public int RoleID { get; set; }
    }
}