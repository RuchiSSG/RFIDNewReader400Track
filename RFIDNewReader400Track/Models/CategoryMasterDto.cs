using System.Data;

namespace RFIDReaderPortal.Models
{
    public class CategoryMasterDto
    {
        public BaseModel? BaseModel { get; set; }
        public string? Id { get; set; }
        public string? categoryId { get; set; }
        public string? recConfId { get; set; }
        public string? UserId { get; set; }
        public DateTime? createdDate { get; set; }
        public DateTime? updatedDate { get; set; }
        public string?  Location { get; set; }
        public string?  DeviceID { get; set; }
        public string? eventName { get; set; }
        public string? IsActive { get; set; }
        public DataTable? DataTable { get; set; }
        public string? CategoryName { get; set; }
        //public List<Categoryins> Categoryins { get; set; }

    }

    //public class Categoryins
    //{

    //    public string? CategoryName { get; set; }

    //}
}
