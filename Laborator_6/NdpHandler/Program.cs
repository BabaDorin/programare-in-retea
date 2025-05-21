using System;
using System.Net;
using System.Net.Sockets;
using System.Globalization; // For CultureInfo
using System.Text.RegularExpressions; // For input validation
using System.Threading.Tasks; // For asynchronous operations

public class NtpClientSharp
{
    private const string NtpServer = "pool.ntp.org";

    /// <summary>
    /// Fetches NTP time as UTC.
    /// </summary>
    /// <returns>DateTime in UTC if successful, otherwise DateTime.MinValue.</returns>
    private static async Task<DateTime> GetNtpUtcTimeAsync()
    {
        try
        {
            byte[] ntpData = new byte[48];
            ntpData[0] = 0x1B; // LeapIndicator=0 (no warning), VersionNum=3 (IPv4 only), Mode=3 (Client Mode)

            IPAddress[] addresses = await Dns.GetHostAddressesAsync(NtpServer);
            if (addresses.Length == 0)
            {
                Console.WriteLine($"Error: Could not resolve NTP server: {NtpServer}");
                return DateTime.MinValue;
            }

            IPEndPoint ipEndPoint = new IPEndPoint(addresses[0], 123); // NTP uses port 123

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);
                socket.ReceiveTimeout = 3000; // 3 seconds

                await socket.SendAsync(new ArraySegment<byte>(ntpData), SocketFlags.None);
                await socket.ReceiveAsync(new ArraySegment<byte>(ntpData), SocketFlags.None);
            }

            ulong intPart = BitConverter.ToUInt32(ntpData, 40);
            ulong fractPart = BitConverter.ToUInt32(ntpData, 44);

            // Convert big-endian to little-endian
            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

            // NTP time is seconds since 00:00:00 UTC on 1 January 1900.
            // DateTime ticks are 100 nanoseconds since 00:00:00 UTC on 1 January 0001.
            var ntpEpoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime networkDateTime = ntpEpoch.AddMilliseconds((long)milliseconds);

            return networkDateTime;
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Socket error fetching NTP time: {ex.Message}");
            return DateTime.MinValue;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching NTP time: {ex.Message}");
            return DateTime.MinValue;
        }
    }

    private static uint SwapEndianness(ulong x)
    {
        return (uint)(((x & 0x000000ff) << 24) +
                       ((x & 0x0000ff00) << 8) +
                       ((x & 0x00ff0000) >> 8) +
                       ((x & 0xff000000) >> 24));
    }

    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide your GMT offset (e.g., GMT+3 or GMT-7).");
            Console.WriteLine("Usage: NtpClientSharp GMT+/-X");
            return;
        }

        string gmtOffsetArg = args[0].Trim().ToUpperInvariant();

        var gmtRegex = new Regex(@"^GMT([+-])(\d{1,2})$");
        Match match = gmtRegex.Match(gmtOffsetArg);

        if (!match.Success)
        {
            Console.WriteLine("Invalid GMT offset format. Please use 'GMT+X' or 'GMT-X' where X is 0-14 (hours).");
            return;
        }

        string signStr = match.Groups[1].Value;
        string hoursStr = match.Groups[2].Value;

        if (!int.TryParse(hoursStr, out int offsetHours))
        {
            Console.WriteLine("Invalid GMT offset hours. Please use a number for X.");
            return;
        }

        if (offsetHours < 0 || offsetHours > 14)
        {
            Console.WriteLine("Invalid GMT offset value. Hours must be between 0 and 14.");
            return;
        }

        int sign = (signStr == "+") ? 1 : -1;
        TimeSpan offset = TimeSpan.FromHours(sign * offsetHours);

        DateTime utcTime = await GetNtpUtcTimeAsync();

        if (utcTime == DateTime.MinValue)
        {
            Console.WriteLine("Failed to retrieve NTP time.");
            return;
        }

        DateTime localTime = utcTime + offset;

        Console.WriteLine($"The exact time for {gmtOffsetArg} (UTC {utcTime:yyyy-MM-dd HH:mm:ss}) is: {localTime:yyyy-MM-dd HH:mm:ss}");
    }
}