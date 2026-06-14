using System;

namespace DashboardTeknikP1.Models
{
    public class SAP_YP11
    {
        public int RecordID { get; set; }
        public string WeekKalendarIndofood { get; set; }
        public string FunctionLocation { get; set; }
        public string NotificationType { get; set; }
        public string NotificationDesc { get; set; }
        public DateTime NotificationDate { get; set; }
        public double TotalDownTimeInMinutes { get; set; }
        public DateTime DownTimeStartTime { get; set; }
        public DateTime DownTimeEndTime { get; set; }
        public string ActivityText { get; set; }
        public string WageGroup_GroupShift { get; set; }
        public string MasterReceipt { get; set; }
        public string ProcessOrder { get; set; }
        public string WorkCenterPPDesc { get; set; }
        public string DownTimeCode_ActivityCodeDesc { get; set; }
        public DateTime UploadTimestamp { get; set; }
    }
}