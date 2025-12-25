namespace RFIDReaderPortal.Models
{
    public class GetAsyncResponse
    {
        public List<DeviceConfigurationDto> Data { get; set; }
        public string NewToken { get; set; }
    }
}
