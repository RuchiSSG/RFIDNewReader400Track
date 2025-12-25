using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RFIDReaderPortal.Services
{
    public class RFIDDiscoveryService : IRFIDDiscoveryService
    {
        public async Task<(List<string> IpAddresses, string StatusMessage)> DiscoverRFIDReadersAsync()
        {
            var ipAddresses = new List<string>();
            string statusMessage = "";

            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    // Ensure the network interface is up and is an Ethernet interface
                    if (ni.OperationalStatus == OperationalStatus.Up &&
                        (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                         ni.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet) &&
                        !ni.Description.ToLower().Contains("virtual") &&
                        !ni.Description.ToLower().Contains("pseudo"))
                    {
                        var ipProps = ni.GetIPProperties();
                        foreach (var ip in ipProps.UnicastAddresses)
                        {
                            // Ensure the IP address is IPv4
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                ipAddresses.Add(ip.Address.ToString());
                                Console.WriteLine($"Found network interface IP: {ip.Address}");
                            }
                        }
                    }
                }

                // Check if any IP addresses were found
                if (ipAddresses.Count == 0)
                {
                    statusMessage = "No Ethernet connection detected. Please check your network cable or adapter.";
                    return (new List<string>(), statusMessage);
                }

                // If IP addresses are found, return them with a success message
                statusMessage = $"Successfully detected {ipAddresses.Count} network interface(s)";
            }
            catch (Exception ex)
            {
                statusMessage = $"An error occurred while detecting the network: {ex.Message}";
                Console.WriteLine($"Discovery error: {ex}");
                return (new List<string>(), statusMessage);
            }

            // Simulate asynchronous operation (if needed)
            await Task.Delay(100);
            return (ipAddresses, statusMessage);
        }
    }
}