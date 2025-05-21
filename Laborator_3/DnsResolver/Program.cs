using DnsClient; 
using DnsClient.Protocol;
using System.Net;

public class DnsResolverApp
{
    private LookupClient _lookupClient;
    private List<IPAddress> _customDnsServers;

    public DnsResolverApp()
    {
        _lookupClient = new LookupClient();
        _customDnsServers = null;
    }

    public void SetDnsServer(string dnsIp)
    {
        if (IPAddress.TryParse(dnsIp, out IPAddress dnsServerAddress))
        {
            _customDnsServers = new List<IPAddress> { dnsServerAddress };
            _lookupClient = new LookupClient(new LookupClientOptions(_customDnsServers.ToArray())
            {
                Timeout = TimeSpan.FromSeconds(5), 
                UseCache = true 
            });
            Console.WriteLine($"DNS server set to {dnsIp}");
        }
        else
        {
            Console.WriteLine($"Invalid DNS server IP format: {dnsIp}");
        }
    }

    public async Task ResolveDomainAsync(string domain)
    {
        Console.WriteLine($"Resolving domain: {domain}...");
        try
        {
            HashSet<string> ips = new HashSet<string>();
            List<Task<IDnsQueryResponse>> tasks = new List<Task<IDnsQueryResponse>>();

            var clientToUse = _customDnsServers != null ? new LookupClient(_customDnsServers.ToArray()) : new LookupClient();

            IDnsQueryResponse response = await clientToUse.QueryAsync(domain, QueryType.A);

            if (response.HasError)
            {
                Console.WriteLine($"Error resolving domain {domain}: {response.ErrorMessage}");
                return;
            }

            foreach (ARecord aRecord in response.Answers.ARecords())
            {
                ips.Add(aRecord.Address.ToString());
            }

            if (ips.Any())
            {
                Console.WriteLine($"IP addresses for {domain}: {string.Join(", ", ips)}");
            }
            else
            {
                Console.WriteLine($"No 'A' records found for {domain}");
            }
        }
        catch (DnsResponseException ex) // Specific DnsClient.NET
        {
            Console.WriteLine($"DNS query error for {domain}: {ex.Message} (Code: {ex.Code})");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error resolving domain {domain}: {e.Message}");
        }
    }

    public async Task ResolveIpAsync(string ip)
    {
        Console.WriteLine($"Resolving IP (reverse DNS): {ip}...");
        if (!IPAddress.TryParse(ip, out IPAddress ipAddress))
        {
            Console.WriteLine($"Invalid IP address format: {ip}");
            return;
        }

        try
        {
            var clientToUse = _customDnsServers != null ? new LookupClient(_customDnsServers.ToArray()) : new LookupClient();
            var response = await clientToUse.QueryReverseAsync(ipAddress);

            if (response.HasError)
            {
                Console.WriteLine($"Error resolving IP {ip}: {response.ErrorMessage}");
                PerformSystemReverseLookup(ipAddress);
                return;
            }

            var ptrRecords = response.Answers.PtrRecords().ToList();
            if (ptrRecords.Any())
            {
                Console.WriteLine($"Domain names for IP {ip}: {string.Join(", ", ptrRecords.Select(ptr => ptr.PtrDomainName.Value.TrimEnd('.')))}");
            }
            else
            {
                Console.WriteLine($"No PTR records found for IP {ip}. Trying system reverse lookup...");
                PerformSystemReverseLookup(ipAddress);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error resolving IP {ip} with DnsClient: {e.Message}. Trying system reverse lookup...");
            PerformSystemReverseLookup(ipAddress);
        }
    }

    private void PerformSystemReverseLookup(IPAddress ipAddress)
    {
        try
        {
            Task<IPHostEntry> hostEntryTask = Dns.GetHostEntryAsync(ipAddress);
            hostEntryTask.Wait();
            IPHostEntry hostEntry = hostEntryTask.Result;

            List<string> hostnames = new List<string>();
            if (!string.IsNullOrEmpty(hostEntry.HostName) && hostEntry.HostName != ipAddress.ToString())
            {
                hostnames.Add(hostEntry.HostName.TrimEnd('.'));
            }
            foreach (var alias in hostEntry.Aliases)
            {
                if (!string.IsNullOrEmpty(alias) && alias != ipAddress.ToString())
                {
                    hostnames.Add(alias.TrimEnd('.'));
                }
            }

            if (hostnames.Any())
            {
                Console.WriteLine($"System resolved domains for IP {ipAddress}: {string.Join(", ", hostnames.Distinct())}");
            }
            else
            {
                Console.WriteLine($"System could not resolve IP {ipAddress} to a domain name.");
            }
        }
        catch (System.Net.Sockets.SocketException ex) // GetHostEntryAsync poate arunca SocketException
        {
            Console.WriteLine($"System error resolving IP {ipAddress}: {ex.Message} (Error Code: {ex.SocketErrorCode})");
        }
        catch (Exception e)
        {
            Console.WriteLine($"System error resolving IP {ipAddress}: {e.Message}");
        }
    }


    public static bool IsValidIp(string ip)
    {
        return IPAddress.TryParse(ip, out _);
    }

    public static async Task Main(string[] args)
    {
        string customDnsServer = null;
        string domainToResolve = null;
        string ipToResolve = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "-u":
                case "--dns":
                    if (i + 1 < args.Length)
                    {
                        customDnsServer = args[++i];
                    }
                    else
                    {
                        Console.WriteLine("Custom DNS server IP not specified after -u/--dns.");
                        return;
                    }
                    break;
                case "-d":
                case "--domain":
                    if (i + 1 < args.Length)
                    {
                        domainToResolve = args[++i];
                    }
                    else
                    {
                        Console.WriteLine("Domain not specified after -d/--domain.");
                        return;
                    }
                    break;
                case "-i":
                case "--ip":
                    if (i + 1 < args.Length)
                    {
                        ipToResolve = args[++i];
                    }
                    else
                    {
                        Console.WriteLine("IP address not specified after -i/--ip.");
                        return;
                    }
                    break;
            }
        }

        DnsResolverApp app = new DnsResolverApp();

        if (!string.IsNullOrEmpty(customDnsServer))
        {
            if (IsValidIp(customDnsServer))
            {
                app.SetDnsServer(customDnsServer);
            }
            else
            {
                Console.WriteLine($"Invalid DNS server IP: {customDnsServer}");
            }
        }

        if (!string.IsNullOrEmpty(domainToResolve))
        {
            await app.ResolveDomainAsync(domainToResolve);
        }
        else if (!string.IsNullOrEmpty(ipToResolve))
        {
            await app.ResolveIpAsync(ipToResolve);
        }
        else
        {
            Console.WriteLine("Please provide either a domain with '-d' or an IP with '-i'.");
            Console.WriteLine("Optionally, set a custom DNS server with '-u <ip>'.");
            Console.WriteLine("Example: DnsResolverApp.exe -u 8.8.8.8 -d google.com");
            Console.WriteLine("Example: DnsResolverApp.exe -i 142.250.180.14");
        }
    }
}