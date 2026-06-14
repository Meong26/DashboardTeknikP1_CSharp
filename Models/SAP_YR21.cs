using System;

namespace DashboardTeknikP1.Models
{
    public class SAP_YR21
    {
        public int RecordID { get; set; }
        public string WeekOfBasicFinishedDate { get; set; }
        public DateTime PostingDate { get; set; }
        public string ResourceName { get; set; }
        public string WageGroup { get; set; }
        public string GroupName { get; set; }
        public double PlannedHour { get; set; }
        public double ActualHour { get; set; }
        public double StdOutputPcs { get; set; }
        public double DelivQtyPcs { get; set; }
        public double EffectivityPO_Pct { get; set; }
        public double Efficiency_Pct { get; set; }
        public double Ach_Pct { get; set; }
        public DateTime UploadTimestamp { get; set; }
    }
}