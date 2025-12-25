namespace RFIDReaderPortal.Models
{
    public class RfidData
    {
        public string TagId { get; set; }
        public DateTime Timestamp { get; set; }
        public List<DateTime> LapTimes { get; set; }
        public byte Rssi { get; set; }
        public byte Antenna { get; set; }

        public int LapCount => LapTimes?.Count ?? 0;

        public TimeSpan? LastLapDuration
        {
            get
            {
                if (LapTimes == null || LapTimes.Count < 2)
                    return null;

                return LapTimes[LapTimes.Count - 1] - LapTimes[LapTimes.Count - 2];
            }
        }
        //public string? TagId { get; set; }
        //public string? LapNo { get; set; }
        //public DateTime Timestamp { get; set; }
        //// NEW —— store all laps (timestamps)
        //public List<DateTime> LapTimes { get; set; } = new List<DateTime>();
    }
}