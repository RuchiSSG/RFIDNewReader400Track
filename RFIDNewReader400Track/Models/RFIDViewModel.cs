using RFIDReaderPortal.Services;
using System.Xml.Linq;

namespace RFIDReaderPortal.Models
{
    public class RFIDViewModel
    {

        public IEnumerable<RecruitmentEventDto> Events { get; set; }

       // public dynamic Recruitments { get; set; }
        public IEnumerable<CategoryMasterDto> Categories { get; set; }

        public List<RecruitmentDto> Recruitments { get; set; }  

        public List<string> ReaderIPs { get; set; }
        public string? StatusMessage { get; set; }


        public string? UserId { get; set; }

       // public string EventName { get; set; }

        public List<DeviceConfigurationDto> IPDataResponse { get; set; } = new List<DeviceConfigurationDto>();

        public IEnumerable<RecruitmentEventDto> eventname { get; set; }

        public DeviceConfigurationDto DeviceConfiguration { get; set; }

        public RfidData[] RfidDataArray { get; set; }

        public bool IsConfigured { get; set; }
        public bool IsRunning { get; set; }

    }
}
