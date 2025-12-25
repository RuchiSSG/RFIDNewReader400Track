using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RFIDReaderPortal.Models
{
    public class EventModel
    {
        public Guid? Id { get; set; }
        public string? UserId { get; set; }
        public string? recConfId { get; set; }

        public string? DeviceID { get; set; }
        public string? Location { get; set; }
        public string? Recruitment { get; set; }

        public string? EventName { get; set; }

    }

}
