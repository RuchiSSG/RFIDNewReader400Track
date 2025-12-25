using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;

namespace RFIDReaderPortal.Models
{
    public class RecruitmentEventDto
    {
        public Guid? Id { get; set; }
        public string? UserId { get; set; }
        public BaseModel? BaseModel { get; set; }

        public DateTime? createdDate { get; set; }
        public DateTime? updatedDate { get; set; }
        public string? isActive { get; set; }
        public string? eventName { get; set; }
        public string? eventUnit { get; set; }

        public string? recConfId { get; set; }
        public DataTable? DataTable { get; set; }

        public List<RecruitmentEvent> RecruitmentEvent { get; set; }

        public string DeviceID { get; set; }
        public string Location { get; set; }

        public string CategoryId { get; set; }
        public string Recruitment { get; set; }

      //  public string EventName { get; set; }

    }
    public class RecruitmentEvent
    {
        public string? minValue { get; set; }
        public string? maxValue { get; set; }
        public decimal? score { get; set; }

        public string? gender { get; set; }

    }
}
