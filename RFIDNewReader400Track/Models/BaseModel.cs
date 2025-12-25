namespace RFIDReaderPortal.Models
{
    public class BaseModel
    {
        public string? OperationType { get; set; }
        public string? Server_Value { get; set; }
    }
    public class Outcome
    {
        public int OutcomeId { get; set; }
        public string OutcomeDetail { get; set; }
        public string? Tokens { get; set; }
        public string? Expiration { get; set; }
    }


    public class Result
    {
        public Outcome? Outcome { get; set; }
        public object? Data { get; set; }
        public Guid? UserId { get; set; }

    }
}
