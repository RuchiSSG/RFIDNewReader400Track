namespace RFIDReaderPortal.Models
{
    public class RecruitmentDto
    {
        public Guid? Id { get; set; }
        public string? post { get; set; }
        public string? place { get; set; }
        public string? year { get; set; }
        public string? UserId { get; set; }
        public BaseModel? BaseModel { get; set; }

        public DateTime? createdDate { get; set; }
        public DateTime? updatedDate { get; set; }
        public string? isActive { get; set; }

        public string? RecruitmentName { get; set; }

        public string? RecruitId { get; set; }

        public Outcome? outcome { get; set; } // Add reference to Outcome
        public Result? Result { get; set; } // Add reference to Result
    }
}
