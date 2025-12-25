namespace RFIDReaderPortal.Models
{
    public class RFIDChestNoMappingDto
    {
        public BaseModel? BaseModel { get; set; }
        public string? Id { get; set; }
        public string? RFID { get; set; }
        public string? UserId { get; set; }
        public string? ChestNo { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? eventId { get; set; }
        public string? Position { get; set; }
        public string? DeviceName { get; set; }
        public string? RecruitId { get; set; }
        public string? currentDateTime { get; set; }
    }
}
