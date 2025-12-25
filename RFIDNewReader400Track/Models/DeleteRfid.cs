namespace RFIDReaderPortal.Models
{
    public class DeleteRfid
    {
        public Outcome1 outcome { get; set; }
    }
    public class Outcome1
    {
        public bool success { get; set; }
        public string tokens { get; set; }
    }
}
