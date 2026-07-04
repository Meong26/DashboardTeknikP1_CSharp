using System;

namespace DashboardTeknikP1.Models
{
    public class SystemSetting
    {
        public int Id { get; set; }
        public string SettingKey { get; set; }
        public string SettingValue { get; set; }
        public string Description { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
