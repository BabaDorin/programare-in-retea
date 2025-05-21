using System;
using System.IO;
using System.Linq; 
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.Net.Mime;
using System.Security.Cryptography.X509Certificates; 
using System.Threading.Tasks;

public static class EmailSenderLowLevel
{
    private const string SmtpHost = "smtp.gmail.com";
    private const int SmtpPort = 587; // STARTTLS port

    /// <summary>
    /// Reads and processes SMTP server responses, checking for expected success codes.
    /// Handles multi-line responses.
    /// </summary>
    private static async Task<string> ReadSmtpResponseAsync(StreamReader reader, params string[] expectedSuccessCodes)
    {
        StringBuilder responseBuilder = new StringBuilder();
        string line;
        try
        {
            while ((line = await reader.ReadLineAsync()) != null)
            {
                Console.WriteLine($"> {line}"); 
                responseBuilder.AppendLine(line);

                if (line.Length >= 3) 
                {
                    string code = line.Substring(0, 3);
                    if (line.Length == 3 || line[3] == ' ')
                    {
                        bool isSuccess = false;
                        if (expectedSuccessCodes != null && expectedSuccessCodes.Length > 0)
                        {
                            isSuccess = expectedSuccessCodes.Contains(code);
                        }
                        else
                        {
                            isSuccess = code.StartsWith("2") || code.StartsWith("3");
                        }

                        if (isSuccess)
                        {
                            return responseBuilder.ToString(); // Success
                        }
                        else
                        {
                            string expectedCodesStr = (expectedSuccessCodes != null && expectedSuccessCodes.Length > 0)
                                ? string.Join(", ", expectedSuccessCodes)
                                : "2xx or 3xx";
                            throw new Exception($"SMTP Error: Received code {code}. Expected one of ({expectedCodesStr}). Full response: {responseBuilder.ToString().Trim()}");
                        }
                    }
                    else if (line[3] == '-')
                    {
                        // Multi-line response, continue reading.
                    }
                    else
                    {
                        // Line format is unexpected (e.g. "<code>Xtext" where X is not ' ' or '-')
                        throw new Exception($"SMTP Error: Malformed response line. Full response: {responseBuilder.ToString().Trim()}");
                    }
                }
                else
                {
                    // Line too short to be a valid SMTP status line
                    throw new Exception($"SMTP Error: Received short or malformed line. Full response: {responseBuilder.ToString().Trim()}");
                }
            }
        }
        catch (IOException ex) // Catch ReadLineAsync specific IO exceptions
        {
            Console.WriteLine($"IOException during SMTP read: {ex.Message}");
            throw new Exception($"Connection error during SMTP read: {ex.Message}. Full response so far: {responseBuilder.ToString().Trim()}", ex);
        }
        // If loop exits due to reader.ReadLineAsync() returning null (stream closed)
        throw new IOException($"Connection closed prematurely by server. Full response so far: {responseBuilder.ToString().Trim()}");
    }

    /// <summary>
    /// Encodes a string for use in an email subject line according to RFC 2047 (basic UTF-8 Base64).
    /// </summary>
    private static string EncodeSubject(string subject)
    {
        if (string.IsNullOrEmpty(subject) || subject.All(c => c < 128)) // If null, empty, or pure ASCII
            return subject;

        return "=?utf-8?B?" + Convert.ToBase64String(Encoding.UTF8.GetBytes(subject)) + "?=";
    }

    public static async Task SendEmailAsync(string fromEmail, string toEmail, string subject, string body, string attachmentFilePath, string password)
    {
        TcpClient client = null;
        NetworkStream networkStream = null;
        SslStream sslStream = null;
        StreamReader reader = null;
        StreamWriter writer = null;

        try
        {
            Console.WriteLine($"[DEBUG] Connecting to {SmtpHost}:{SmtpPort}...");
            client = new TcpClient();
            await client.ConnectAsync(SmtpHost, SmtpPort);
            Console.WriteLine("[DEBUG] Connected to SMTP server.");

            networkStream = client.GetStream();
            // Use ASCII for SMTP commands
            reader = new StreamReader(networkStream, Encoding.ASCII);
            writer = new StreamWriter(networkStream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };

            Console.WriteLine("[DEBUG] Reading initial SMTP greeting...");
            await ReadSmtpResponseAsync(reader, "220"); // Expect 220

            Console.WriteLine("[DEBUG] Sending EHLO...");
            await writer.WriteLineAsync("EHLO [127.0.0.1]"); // Using IP literal; a FQDN is often preferred if available
            await ReadSmtpResponseAsync(reader, "250"); // Expect 250

            Console.WriteLine("[DEBUG] Sending STARTTLS...");
            await writer.WriteLineAsync("STARTTLS");
            await ReadSmtpResponseAsync(reader, "220"); // Expect 220 ready to start TLS

            Console.WriteLine("[DEBUG] Attempting SSL/TLS handshake (AuthenticateAsClientAsync)...");
            var sslOptions = new SslClientAuthenticationOptions
            {
                TargetHost = SmtpHost,
                // Optionally specify enabled SSL/TLS protocols, e.g.:
                // EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    // WARNING: Bypassing certificate validation. DO NOT USE IN PRODUCTION.
                    // For production, implement proper certificate validation.
                    if (sslPolicyErrors != SslPolicyErrors.None)
                    {
                        Console.WriteLine($"WARNING: SSL certificate validation error: {sslPolicyErrors}");
                    }
                    Console.WriteLine("WARNING: SSL certificate validation is currently bypassed. This is insecure for production.");
                    return true;
                },
                AllowRenegotiation = true // Generally not needed/recommended unless specific server compatibility issues arise. Default is false.
            };

            sslStream = new SslStream(networkStream, false); // false = SslStream owns and disposes networkStream
            await sslStream.AuthenticateAsClientAsync(sslOptions);
            Console.WriteLine("[DEBUG] SSL/TLS handshake complete. Connection is now encrypted.");

            // Re-initialize reader and writer with the now-encrypted SslStream, using ASCII for commands
            reader = new StreamReader(sslStream, Encoding.ASCII);
            writer = new StreamWriter(sslStream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };

            // CRUCIAL: Send EHLO again over the encrypted connection
            Console.WriteLine("[DEBUG] Sending EHLO after STARTTLS...");
            await writer.WriteLineAsync("EHLO [127.0.0.1]"); // Or your preferred domain identifier
            await ReadSmtpResponseAsync(reader, "250"); // Expect 250

            Console.WriteLine("[DEBUG] Sending AUTH LOGIN...");
            await writer.WriteLineAsync("AUTH LOGIN");
            await ReadSmtpResponseAsync(reader, "334"); // Expect 334 for username prompt

            Console.WriteLine("[DEBUG] Sending username (Base64 encoded)...");
            await writer.WriteLineAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(fromEmail)));
            await ReadSmtpResponseAsync(reader, "334"); // Expect 334 for password prompt

            Console.WriteLine("[DEBUG] Sending password (Base64 encoded)...");
            await writer.WriteLineAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(password)));
            await ReadSmtpResponseAsync(reader, "235"); // Expect 235 for authentication successful

            Console.WriteLine("[DEBUG] Sending MAIL FROM...");
            await writer.WriteLineAsync($"MAIL FROM:<{fromEmail}>");
            await ReadSmtpResponseAsync(reader, "250"); // Expect 250 Ok

            Console.WriteLine("[DEBUG] Sending RCPT TO...");
            await writer.WriteLineAsync($"RCPT TO:<{toEmail}>");
            await ReadSmtpResponseAsync(reader, "250"); // Expect 250 Ok

            Console.WriteLine("[DEBUG] Sending DATA command...");
            await writer.WriteLineAsync("DATA");
            await ReadSmtpResponseAsync(reader, "354"); // Expect 354 Start mail input

            // Construct MIME message
            var mimeBuilder = new StringBuilder();
            mimeBuilder.AppendLine($"From: {fromEmail}");
            mimeBuilder.AppendLine($"To: {toEmail}");
            mimeBuilder.AppendLine($"Subject: {EncodeSubject(subject)}");
            mimeBuilder.AppendLine("MIME-Version: 1.0");

            if (!string.IsNullOrEmpty(attachmentFilePath) && File.Exists(attachmentFilePath))
            {
                string boundary = "----=_Part_" + Guid.NewGuid().ToString("N");
                mimeBuilder.AppendLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
                mimeBuilder.AppendLine(); // Empty line before the first boundary part

                // Text part
                mimeBuilder.AppendLine($"--{boundary}");
                mimeBuilder.AppendLine("Content-Type: text/plain; charset=utf-8");
                mimeBuilder.AppendLine("Content-Transfer-Encoding: 8bit"); // Or quoted-printable / base64 for more complex text
                mimeBuilder.AppendLine();
                mimeBuilder.AppendLine(body);
                mimeBuilder.AppendLine();

                // Attachment part
                string attachmentFileName = Path.GetFileName(attachmentFilePath);
                mimeBuilder.AppendLine($"--{boundary}");
                mimeBuilder.AppendLine($"Content-Type: {MediaTypeNames.Application.Octet}; name=\"{attachmentFileName}\"");
                mimeBuilder.AppendLine("Content-Transfer-Encoding: base64");
                mimeBuilder.AppendLine($"Content-Disposition: attachment; filename=\"{attachmentFileName}\"");
                mimeBuilder.AppendLine();

                byte[] fileBytes = await File.ReadAllBytesAsync(attachmentFilePath);
                mimeBuilder.AppendLine(Convert.ToBase64String(fileBytes));
                mimeBuilder.AppendLine();
                mimeBuilder.AppendLine($"--{boundary}--"); // Closing boundary
            }
            else
            {
                // Plain text email without attachment
                mimeBuilder.AppendLine("Content-Type: text/plain; charset=utf-8");
                mimeBuilder.AppendLine("Content-Transfer-Encoding: 8bit");
                mimeBuilder.AppendLine();
                mimeBuilder.AppendLine(body);
            }

            Console.WriteLine("[DEBUG] Sending email data (MIME content)...");
            await writer.WriteAsync(mimeBuilder.ToString()); // Send the constructed MIME message

            Console.WriteLine("[DEBUG] Sending DATA termination sequence (CRLF.CRLF)...");
            await writer.WriteLineAsync("."); // Terminate DATA with <CRLF>.<CRLF>

            await ReadSmtpResponseAsync(reader, "250"); // Expect 250 Ok, message accepted

            Console.WriteLine("[DEBUG] Sending QUIT command...");
            await writer.WriteLineAsync("QUIT");
            await ReadSmtpResponseAsync(reader, "221"); // Expect 221 Bye

            Console.WriteLine("Email sent successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            // For more detailed debugging, you might want to log ex.ToString() for stack trace.
            Console.WriteLine($"[DEBUG] Full exception: {ex.ToString()}");
        }
        finally
        {
            // Dispose resources in an order that respects dependencies.
            // StreamWriter and StreamReader should be disposed, which might also dispose their base stream.
            // SslStream should be disposed, which will dispose the NetworkStream if leaveInnerStreamOpen was false.
            // TcpClient should be disposed, which ensures the connection is closed and its NetworkStream is disposed if not already.
            writer?.Dispose();
            reader?.Dispose();
            sslStream?.Dispose(); // If non-null, disposes the underlying NetworkStream due to leaveInnerStreamOpen=false
            // No need to dispose networkStream separately if sslStream handles it.
            client?.Dispose(); // Disposes the TcpClient and its associated resources.
            Console.WriteLine("[DEBUG] Resources disposed.");
        }
    }

    // Example Main method (remove or modify if this class is part of a larger project)
    // public static async Task Main(string[] args)
    // {
    //     if (args.Length < 6)
    //     {
    //         Console.WriteLine("Usage: dotnet run <fromEmail> <toEmail> <subject> <body> <attachmentFilePath> <password>");
    //         return;
    //     }
    //     string from = args[0];
    //     string to = args[1];
    //     string subj = args[2];
    //     string mailBody = args[3];
    //     string filePath = args[4];
    //     string pass = args[5];

    //     // For testing without an attachment, pass an empty string or a non-existent path for filePath
    //     // e.g., filePath = ""; 
    //     // Ensure the file exists if a path is provided, or handle it.
    //     if (!string.IsNullOrEmpty(filePath) && !File.Exists(filePath)) {
    //         Console.WriteLine($"Attachment file not found: {filePath}. Sending without attachment.");
    //         filePath = null; // Send without attachment
    //     }


    //     await SendEmailAsync(from, to, subj, mailBody, filePath, pass);
    // }
}