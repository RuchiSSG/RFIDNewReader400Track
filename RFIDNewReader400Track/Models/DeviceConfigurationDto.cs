using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;

namespace RFIDReaderPortal.Models
{
    public class DeviceConfigurationDto
    {
        public RfidData rfidData { get; set; }
        public BaseModel? BaseModel { get; set; }
        public Guid? Id { get; set; }
        public string? RecruitId { get; set; }
        public string? EventId { get; set; }

       // public string? eventName { get; set; }

        public string? eventName { get; set; }

        public string? categoryId { get; set; }

        public string? UserId { get; set; }
        public string? DeviceId { get; set; }
        
        public string? Location { get; set; }

        public dynamic outcome { get; set; }
        public string? IsActive { get; set; }
        public string? sessionid { get; set; }
        public string? ipaddress { get; set; }
        public string? statusmessage { get; set; }


    }
}
