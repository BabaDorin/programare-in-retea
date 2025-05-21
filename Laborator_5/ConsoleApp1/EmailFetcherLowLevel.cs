using System;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Linq; 
using System.Collections.Generic; 
using System.Threading.Tasks;

public static class EmailFetcherLowLevel
{
    private static int _imapTagCounter = 0;
    private static string NextImapTag() => $"A{_imapTagCounter++:D3}";

    public static async Task FetchEmailsAsync(string emailUser, string emailPassword, string protocol, string folder = "INBOX", string searchCriteria = "ALL")
    {
        _imapTagCounter = 0; // Reset for each call
        Console.WriteLine($"[DEBUG] Attempting to fetch emails. User: {emailUser}, Protocol: {protocol}, Folder: {folder}, Search: {searchCriteria}");

        try
        {
            if (protocol.ToUpper() == "IMAP")
            {
                await FetchImapEmails(emailUser, emailPassword, folder, searchCriteria);
            }
            else if (protocol.ToUpper() == "POP3")
            {
                await FetchPop3Emails(emailUser, emailPassword);
            }
            else
            {
                Console.WriteLine("Protocol must be 'IMAP' or 'POP3'.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching emails: {ex.Message}");
            Console.WriteLine($"[DEBUG] Full exception details: {ex.ToString()}");
        }
    }

    private static async Task FetchImapEmails(string emailUser, string emailPassword, string folder, string searchCriteria)
    {
        const string ImapHost = "imap.gmail.com";
        const int ImapPort = 993; // Implicit SSL

        using (var client = new TcpClient())
        {
            Console.WriteLine($"[DEBUG_IMAP] Connecting to {ImapHost}:{ImapPort}...");
            await client.ConnectAsync(ImapHost, ImapPort);
            Console.WriteLine("[DEBUG_IMAP] Connected.");

            using (var stream = new SslStream(client.GetStream(), false,
                (sender, certificate, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors != SslPolicyErrors.None)
                    {
                        Console.WriteLine($"[WARN_IMAP] SSL Certificate Error: {sslPolicyErrors}");
                    }
                    Console.WriteLine("[WARN_IMAP] SSL certificate validation is bypassed. This is insecure for production.");
                    return true;
                }))
            {
                Console.WriteLine("[DEBUG_IMAP] Authenticating SSL/TLS as client...");
                await stream.AuthenticateAsClientAsync(ImapHost);
                Console.WriteLine("[DEBUG_IMAP] SSL/TLS Authenticated.");

                var reader = new StreamReader(stream, Encoding.ASCII);
                var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
                Console.WriteLine("[DEBUG_IMAP] StreamReader/Writer initialized with ASCII encoding.");

                Console.WriteLine("[DEBUG_IMAP] Reading initial server greeting...");
                string initialGreeting = await ReadImapResponse(reader); // Initial server greeting (no tag expected)
                Console.WriteLine($"[DEBUG_IMAP] Initial server greeting processed. Response: {initialGreeting.Trim()}");

                string tagLogin = NextImapTag();
                string loginCommand = $"{tagLogin} LOGIN \"{emailUser}\" \"{emailPassword}\"";
                Console.WriteLine($"<IMAP_CMD: {loginCommand}");
                await writer.WriteLineAsync(loginCommand);
                string loginResponse = await ReadImapResponse(reader, tagLogin);
                if (!loginResponse.Contains($"{tagLogin} OK")) throw new Exception($"IMAP Login failed. Response: {loginResponse.Trim()}");
                Console.WriteLine("[DEBUG_IMAP] Login successful.");

                string tagSelect = NextImapTag();
                string selectCommand = $"{tagSelect} SELECT \"{folder}\"";
                Console.WriteLine($"<IMAP_CMD: {selectCommand}");
                await writer.WriteLineAsync(selectCommand);
                string selectResponse = await ReadImapResponse(reader, tagSelect);
                if (!selectResponse.Contains($"{tagSelect} OK")) throw new Exception($"IMAP Failed to select folder: {folder}. Response: {selectResponse.Trim()}");
                Console.WriteLine($"[DEBUG_IMAP] Folder '{folder}' selected.");

                string tagSearch = NextImapTag();
                string searchCommand = $"{tagSearch} SEARCH {searchCriteria}";
                Console.WriteLine($"<IMAP_CMD: {searchCommand}");
                await writer.WriteLineAsync(searchCommand);
                string searchResponseFull = await ReadImapResponse(reader, tagSearch);

                var allMessageNumbers = new List<string>();
                MatchCollection searchMatches = Regex.Matches(searchResponseFull, @"\* SEARCH ([\d\s]*)", RegexOptions.IgnoreCase);
                foreach (Match m in searchMatches)
                {
                    if (m.Groups.Count > 1 && !string.IsNullOrWhiteSpace(m.Groups[1].Value))
                    {
                        allMessageNumbers.AddRange(m.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                    }
                }
                Console.WriteLine($"[DEBUG_IMAP] Search returned: {string.Join(" ", allMessageNumbers)}");

                var messageNumbersToFetch = allMessageNumbers.Distinct()
                                                             .Where(s => long.TryParse(s, out _))
                                                             .Select(long.Parse)
                                                             .OrderByDescending(n => n)
                                                             .Take(10)
                                                             .OrderBy(n => n)
                                                             .Select(n => n.ToString())
                                                             .ToList();

                if (messageNumbersToFetch.Any())
                {
                    Console.WriteLine($"Fetching details for up to {messageNumbersToFetch.Count} latest messages (of {allMessageNumbers.Count} found) in '{folder}'.");
                    foreach (var msgNum in messageNumbersToFetch)
                    {
                        string tagFetch = NextImapTag();
                        string fetchCommand = $"{tagFetch} FETCH {msgNum} BODY.PEEK[HEADER.FIELDS (FROM DATE SUBJECT)]";
                        Console.WriteLine($"<IMAP_CMD: {fetchCommand}");
                        await writer.WriteLineAsync(fetchCommand);
                        string fetchResponse = await ReadImapResponse(reader, tagFetch);
                        ParseAndDisplayImapEmailHeaders(msgNum, fetchResponse);
                    }
                }
                else
                {
                    Console.WriteLine($"No messages found in '{folder}' with criteria '{searchCriteria}'.");
                }

                string tagLogout = NextImapTag();
                string logoutCommand = $"{tagLogout} LOGOUT";
                Console.WriteLine($"<IMAP_CMD: {logoutCommand}");
                await writer.WriteLineAsync(logoutCommand);
                await ReadImapResponse(reader, tagLogout);

                Console.WriteLine("IMAP session closed.");
            }
        }
    }
    private static void ParseAndDisplayImapEmailHeaders(string msgNum, string rawFetchResponse)
    {
        string from = "N/A";
        string date = "N/A";
        string subject = "N/A";

        string headerBlock = rawFetchResponse;
        var bodyPeekMatch = Regex.Match(rawFetchResponse, @"BODY\.PEEK\[HEADER\.FIELDS \(FROM DATE SUBJECT\)\][^{]*(\{[\d]+\})?\r?\n([\s\S]*)", RegexOptions.IgnoreCase);
        if (bodyPeekMatch.Success && bodyPeekMatch.Groups.Count > 2)
        {
            headerBlock = bodyPeekMatch.Groups[bodyPeekMatch.Groups.Count - 1].Value; // Last capturing group should be the headers
        }


        using (StringReader sr = new StringReader(headerBlock))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (Regex.IsMatch(line, @"^\w\d+\s+(OK|NO|BAD)") || line.StartsWith("* "))
                {
                    if (!line.Contains("FETCH")) break; 
                }

                if (line.StartsWith("From: ", StringComparison.OrdinalIgnoreCase))
                    from = DecodeRfc2047(line.Substring("From: ".Length).Trim());
                else if (line.StartsWith("Date: ", StringComparison.OrdinalIgnoreCase))
                    date = line.Substring("Date: ".Length).Trim();
                else if (line.StartsWith("Subject: ", StringComparison.OrdinalIgnoreCase))
                    subject = DecodeRfc2047(line.Substring("Subject: ".Length).Trim());
            }
        }

        Console.WriteLine($"--- Email (IMAP MsgNum: {msgNum}) ---");
        Console.WriteLine($"  From: {from}");
        Console.WriteLine($"  Date: {date}");
        Console.WriteLine($"  Subject: {subject}");
        Console.WriteLine($"------------------------------------");
    }


    private static async Task FetchPop3Emails(string emailUser, string emailPassword)
    {
        const string Pop3Host = "pop.gmail.com";
        const int Pop3Port = 995;

        using (var client = new TcpClient())
        {
            Console.WriteLine($"[DEBUG_POP3] Connecting to {Pop3Host}:{Pop3Port}...");
            await client.ConnectAsync(Pop3Host, Pop3Port);
            Console.WriteLine("[DEBUG_POP3] Connected.");

            using (var stream = new SslStream(client.GetStream(), false,
                (sender, certificate, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors != SslPolicyErrors.None) Console.WriteLine($"[WARN_POP3] SSL Certificate Error: {sslPolicyErrors}");
                    Console.WriteLine("[WARN_POP3] SSL certificate validation is bypassed. This is insecure for production.");
                    return true;
                }))
            {
                Console.WriteLine("[DEBUG_POP3] Authenticating SSL/TLS as client...");
                await stream.AuthenticateAsClientAsync(Pop3Host);
                Console.WriteLine("[DEBUG_POP3] SSL/TLS Authenticated.");

                var reader = new StreamReader(stream, Encoding.ASCII); 
                var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
                Console.WriteLine("[DEBUG_POP3] StreamReader/Writer initialized with ASCII encoding.");


                await ReadPop3Response(reader); 

                Console.WriteLine($"<POP3_CMD: USER {emailUser}");
                await writer.WriteLineAsync($"USER {emailUser}");
                await ReadPop3Response(reader);

                Console.WriteLine($"<POP3_CMD: PASS ********"); 
                await writer.WriteLineAsync($"PASS {emailPassword}");
                await ReadPop3Response(reader);
                Console.WriteLine("[DEBUG_POP3] Login successful.");


                Console.WriteLine($"<POP3_CMD: STAT");
                await writer.WriteLineAsync("STAT");
                string statResponse = await ReadPop3Response(reader);
                var statMatch = Regex.Match(statResponse, @"\+OK (\d+) (\d+)");
                int numMessages = statMatch.Success ? int.Parse(statMatch.Groups[1].Value) : 0;

                int messagesToRetrieve = Math.Min(numMessages, 10);
                int startMessageIndex = Math.Max(1, numMessages - messagesToRetrieve + 1);

                Console.WriteLine($"Number of messages on server: {numMessages}. Fetching details for latest {messagesToRetrieve} (index {startMessageIndex} to {numMessages}).");

                for (int i = startMessageIndex; i <= numMessages; i++)
                {
                    Console.WriteLine($"<POP3_CMD: RETR {i}");
                    await writer.WriteLineAsync($"RETR {i}");
                    string retrInitialResponse = await ReadPop3Response(reader);
                    if (retrInitialResponse.StartsWith("+OK", StringComparison.OrdinalIgnoreCase))
                    {
                        string messageContent = await ReadPop3MultiLineResponse(reader);
                        ParseAndDisplayPop3EmailHeaders(i, messageContent);
                    }
                    else
                    {
                        Console.WriteLine($"POP3 Error retrieving message {i}: {retrInitialResponse}");
                    }
                }

                Console.WriteLine($"<POP3_CMD: QUIT");
                await writer.WriteLineAsync("QUIT");
                await ReadPop3Response(reader);

                Console.WriteLine("POP3 session closed.");
            }
        }
    }
    private static void ParseAndDisplayPop3EmailHeaders(int msgIndex, string rawMessageContent)
    {
        string from = "N/A";
        string date = "N/A";
        string subject = "N/A";

        using (StringReader sr = new StringReader(rawMessageContent))
        {
            string line;
            bool inHeaders = true;
            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    inHeaders = false;
                    break;
                }
                if (!inHeaders) continue;


                if (line.StartsWith("From: ", StringComparison.OrdinalIgnoreCase))
                    from = DecodeRfc2047(line.Substring("From: ".Length).Trim());
                else if (line.StartsWith("Date: ", StringComparison.OrdinalIgnoreCase))
                    date = line.Substring("Date: ".Length).Trim();
                else if (line.StartsWith("Subject: ", StringComparison.OrdinalIgnoreCase))
                    subject = DecodeRfc2047(line.Substring("Subject: ".Length).Trim());
            }
        }

        Console.WriteLine($"--- Email (POP3 Index: {msgIndex}) ---");
        Console.WriteLine($"  From: {from}");
        Console.WriteLine($"  Date: {date}");
        Console.WriteLine($"  Subject: {subject}");
        Console.WriteLine($"------------------------------------");
    }

    private static async Task<string> ReadImapResponse(StreamReader reader, string expectedTag = null)
    {
        StringBuilder response = new StringBuilder();
        string line;
        bool finalResponseReceived = false;
        int consecutiveEmptyLineReads = 0;
        const int maxEmptyLines = 5; 

        Console.WriteLine($"[DEBUG_IMAP_READ] Waiting for response{(expectedTag != null ? " for tag " + expectedTag : " (initial greeting)")}...");

        while ((line = await reader.ReadLineAsync()) != null)
        {
            Console.WriteLine($">IMAP_RECV: {line}");
            response.AppendLine(line);

            if (string.IsNullOrWhiteSpace(line))
            {
                consecutiveEmptyLineReads++;
                if (consecutiveEmptyLineReads > maxEmptyLines && expectedTag != null)
                {
                    Console.WriteLine($"[WARN_IMAP_READ] Exceeded {maxEmptyLines} consecutive empty/whitespace lines for tag {expectedTag}. Assuming end of response or issue.");
                    break;
                }
                continue; 
            }
            else
            {
                consecutiveEmptyLineReads = 0;
            }

            if (expectedTag != null)
            {
                if (line.StartsWith(expectedTag + " ")) // Tag followed by a space
                {
                    string status = line.Substring(expectedTag.Length + 1);
                    if (status.StartsWith("OK") || status.StartsWith("NO") || status.StartsWith("BAD"))
                    {
                        finalResponseReceived = true;
                        break;
                    }
                }
            }
            else
            {
              
                if (line.StartsWith("* OK") || line.StartsWith("* PREAUTH") || line.StartsWith("* BYE"))
                {
                    finalResponseReceived = true;
                    break;
                }
            }
        }

        if (line == null)
        {
            Console.WriteLine("[WARN_IMAP_READ] Stream closed by server (ReadLineAsync returned null).");
            if (response.Length == 0) throw new IOException($"IMAP connection closed prematurely by server. No response received for: {(expectedTag ?? "initial greeting")}");
        }

        if (!finalResponseReceived && expectedTag != null)
        {
            Console.WriteLine($"[WARN_IMAP_READ] Expected tagged response '{expectedTag}' not definitively found by marker. Full collected response for this read: \n{response.ToString().TrimEnd()}");
        }
        else if (!finalResponseReceived && expectedTag == null && response.Length == 0)
        {
            throw new IOException($"IMAP connection closed or no initial greeting received.");
        }

        return response.ToString();
    }

    private static async Task<string> ReadPop3Response(StreamReader reader)
    {
        string line = await reader.ReadLineAsync();
        if (line == null) throw new IOException("POP3 connection closed or no response received.");

        Console.WriteLine($">POP3_RECV: {line}");
        if (!line.StartsWith("+OK", StringComparison.OrdinalIgnoreCase) && !line.StartsWith("-ERR", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"POP3 Unexpected response: {line}");
        }
        if (line.StartsWith("-ERR", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[ERROR_POP3] Server returned error: {line}");
            throw new Exception($"POP3 Server Error: {line}");
        }
        return line;
    }

    private static async Task<string> ReadPop3MultiLineResponse(StreamReader reader)
    {
        StringBuilder content = new StringBuilder();
        string line;
        Console.WriteLine("[DEBUG_POP3_READMULTILINE] Reading multi-line response...");
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line == ".")
            {
                Console.WriteLine("[DEBUG_POP3_READMULTILINE] End of multi-line response detected (.).");
                break;
            }

            if (line.StartsWith("..")) // Dot stuffing
            {
                content.AppendLine(line.Substring(1));
            }
            else
            {
                content.AppendLine(line);
            }
        }
        if (line == null) Console.WriteLine("[WARN_POP3_READMULTILINE] Stream closed by server while reading multi-line response.");
        return content.ToString();
    }

    public static string DecodeRfc2047(string encodedString)
    {
        if (string.IsNullOrWhiteSpace(encodedString) || !encodedString.Contains("=?"))
        {
            return encodedString;
        }

        return Regex.Replace(encodedString, @"=\?([A-Za-z0-9_-]+)\?([BbQq])\?([^\?]+)\?=",
            match => {
                try
                {
                    string charset = match.Groups[1].Value;
                    string encodingType = match.Groups[2].Value.ToUpperInvariant();
                    string encodedText = match.Groups[3].Value;
                    Encoding enc = Encoding.GetEncoding(charset); // Can throw if charset not supported

                    if (encodingType == "B") // Base64
                    {
                        byte[] bytes = Convert.FromBase64String(encodedText);
                        return enc.GetString(bytes);
                    }
                    else if (encodingType == "Q")
                    {
                        encodedText = encodedText.Replace('_', ' ');
                        var qpDecodedBytes = new List<byte>();
                        for (int i = 0; i < encodedText.Length; i++)
                        {
                            if (encodedText[i] == '=' && i + 2 < encodedText.Length)
                            {
                                string hex = encodedText.Substring(i + 1, 2);
                                if (Regex.IsMatch(hex, @"^[0-9A-F]{2}$", RegexOptions.IgnoreCase))
                                {
                                    qpDecodedBytes.Add(Convert.ToByte(hex, 16));
                                    i += 2;
                                }
                                else // Malformed =.. sequence, treat '=' literally
                                {
                                    qpDecodedBytes.Add((byte)encodedText[i]);
                                }
                            }
                            else
                            {
                                qpDecodedBytes.Add((byte)encodedText[i]);
                            }
                        }
                        return enc.GetString(qpDecodedBytes.ToArray());
                    }
                    return match.Value; // Unknown encoding type, return original encoded word
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN_DECODE] Failed to decode RFC2047 part '{match.Value}': {ex.Message}");
                    return match.Value; // Return original on error
                }
            });
    }
}