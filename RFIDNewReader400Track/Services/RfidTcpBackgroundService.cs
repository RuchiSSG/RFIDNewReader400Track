using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

public class RfidTcpBackgroundService : BackgroundService
{
    // EPC → Last Seen
    public static ConcurrentDictionary<string, DateTime> Tags = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, 9090);
        listener.Start();

        Console.WriteLine("✅ TCP Server listening on port 9090");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);
                _ = Task.Run(() => HandleClientAsync(client, stoppingToken), stoppingToken);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        Console.WriteLine("🔌 RFID Reader Connected");

        using (client)
        using (var stream = client.GetStream())
        {
            byte[] buffer = new byte[4096];

            while (!token.IsCancellationRequested && client.Connected)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (bytesRead <= 0) break;

                // 1️⃣ Convert RAW → HEX STRING
                string hexData = BitConverter.ToString(buffer, 0, bytesRead)
                    .Replace("-", "")
                    .ToUpper();

                Console.WriteLine($"📥 RAW HEX: {hexData}");

                // 2️⃣ Extract EPCs
                var epcs = ExtractEpcs(hexData);

                foreach (var epc in epcs)
                {
                    Tags[epc] = DateTime.Now;
                    Console.WriteLine($"🏷️ EPC RECEIVED: {epc}");
                }
            }
        }

        Console.WriteLine("❌ RFID Reader Disconnected");
    }

    //private static List<string> ExtractEpcs(string hexData)
    //{
    //    var epcs = new List<string>();

    //    // EPC always starts with E2 and is 12 bytes (24 hex chars)
    //    const int EPC_HEX_LENGTH = 24;

    //    for (int i = 0; i <= hexData.Length - EPC_HEX_LENGTH; i += 2)
    //    {
    //        if (hexData.Substring(i, 2) == "E2")
    //        {
    //            string epc = hexData.Substring(i, EPC_HEX_LENGTH);

    //            // Optional sanity check
    //            if (epc.StartsWith("E2"))
    //            {
    //                epcs.Add(epc);
    //            }
    //        }
    //    }

    //    return epcs;
    //}
    private static List<string> ExtractEpcs(string hex)
    {
        const int EPC_LEN = 24; // 12 bytes
        const string EPC_PREFIX = "E280117000000212AC9";

        HashSet<string> result = new();

        if (string.IsNullOrWhiteSpace(hex))
            return result.ToList();

        hex = hex.ToUpperInvariant();

        for (int i = 0; i <= hex.Length - EPC_LEN; i += 2)
        {
            string candidate = hex.Substring(i, EPC_LEN);

            if (
                candidate.StartsWith(EPC_PREFIX) &&
                candidate.All(c => "0123456789ABCDEF".Contains(c))
            )
            {
                result.Add(candidate);
            }
        }

        return result.ToList();
    }




}
