namespace RFIDReaderPortal.Models
{
    public class ConfigurationViewModel
    {
        public IEnumerable<RecruitmentEventDto> Events { get; set; }
        public IEnumerable<RecruitmentDto> Recruitments { get; set; }

        public IEnumerable<RFIDViewModel> rfid { get; set; }

        public List<string> ReaderIPs { get; set; }
    }
}
