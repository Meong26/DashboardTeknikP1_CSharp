using System;

namespace DashboardTeknikP1.Models
{
    public class HmiLog
    {
        public int LogID { get; set; }
        public string UserID { get; set; }
        public string NamaLengkap { get; set; }
        public string RoleName { get; set; }
        public string HmiIp { get; set; }
        public string HmiName { get; set; }
        public DateTime WaktuAkses { get; set; }
    }
}
