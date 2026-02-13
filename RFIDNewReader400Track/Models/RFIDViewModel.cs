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

        // public List<RFIDChestNoMappingDto> Groups { get; set; }
        public List<GroupDto> Groups { get; set; } = new();
        public string? UserId { get; set; }

        // public string EventName { get; set; }

        public List<DeviceConfigurationDto> IPDataResponse { get; set; } = new List<DeviceConfigurationDto>();

        public IEnumerable<RecruitmentEventDto> eventname { get; set; }

        public DeviceConfigurationDto DeviceConfiguration { get; set; }

        public RfidData[] RfidDataArray { get; set; }

        public bool IsConfigured { get; set; }
        public bool IsRunning { get; set; }
        public List<RFIDChestNoMappingDto> ChestLogData { get; set; }
    }
    public class GroupDto
    {
        public int groupid { get; set; }
        public string groupname { get; set; }
    }
    public class ChestRFIDDto
    {
        public string FirstName_English { get; set; }
        public string FatherName_English { get; set; }
        public string Surname_English { get; set; }
        public int CandidateID { get; set; }

        public string ChestNo { get; set; }
        public string TagId { get; set; }
        public string Barcode { get; set; }
        public string Gender { get; set; }
    }
}
