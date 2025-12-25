namespace RFIDReaderPortal.Services
{
    public interface IRFIDDiscoveryService 
    {
        public  Task<(List<string> IpAddresses, string StatusMessage)> DiscoverRFIDReadersAsync();
    }
}