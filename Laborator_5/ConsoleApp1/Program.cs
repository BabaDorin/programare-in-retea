using System;
using System.IO;
using System.Linq; 
using System.Text;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        string command = args[0].ToLower();

        try
        {
            if (command == "send")
            {
                // Expected: send <from> <to> <subject> <body> <filename (or "NONE")> <password>
                if (args.Length < 7)
                {
                    Console.WriteLine("Usage: send <from_email> <to_email> <subject> <body> <filename_or_NONE> <password>");
                    return;
                }
                string fromEmail = args[1];
                string toEmail = args[2];
                string subject = args[3];
                string body = args[4];
                string filename = args[5];
                string password = args[6];

                if (filename.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                {
                    filename = null;
                }

                await EmailSenderLowLevel.SendEmailAsync(fromEmail, toEmail, subject, body, filename, password);
            }
            else if (command == "fetch")
            {
                // Expected: fetch <user> <password> [-p <protocol>] [-f <folder>] [-s <search>]
                if (args.Length < 3)
                {
                    Console.WriteLine("Usage: fetch <email_user> <email_password> [-p <protocol>] [-f <folder>] [-s <search>]");
                    return;
                }
                string emailUser = args[1];
                string emailPassword = args[2];
                string protocol = "IMAP"; // Default
                string folder = "INBOX";  // Default for IMAP
                string searchCriteria = "ALL"; // Default for IMAP

                for (int i = 3; i < args.Length; i++)
                {
                    switch (args[i].ToLower())
                    {
                        case "-p":
                        case "--protocol":
                            if (i + 1 < args.Length)
                            {
                                protocol = args[++i];
                            }
                            else
                            {
                                Console.WriteLine("Error: Protocol not specified after -p.");
                                return;
                            }
                            break;
                        case "-f":
                        case "--folder":
                            if (i + 1 < args.Length)
                            {
                                folder = args[++i];
                            }
                            else
                            {
                                Console.WriteLine("Error: Folder not specified after -f.");
                                return;
                            }
                            break;
                        case "-s":
                        case "--search":
                            if (i + 1 < args.Length)
                            {
                                searchCriteria = args[++i];
                            }
                            else
                            {
                                Console.WriteLine("Error: Search criteria not specified after -s.");
                                return;
                            }
                            break;
                        default:
                            Console.WriteLine($"Unknown argument: {args[i]}");
                            PrintUsage();
                            return;
                    }
                }
                await EmailFetcherLowLevel.FetchEmailsAsync(emailUser, emailPassword, protocol, folder, searchCriteria);
            }
            else
            {
                Console.WriteLine($"Unknown command: {command}");
                PrintUsage();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unhandled error occurred: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("\nUsage:");
        Console.WriteLine("  dotnet run send <from_email> <to_email> <subject> <body> <filename_or_NONE> <password>");
        Console.WriteLine("  dotnet run fetch <email_user> <email_password> [-p <protocol>] [-f <folder>] [-s <search>]");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  dotnet run send sender@example.com receiver@example.com \"Hello from C#\" \"Test Body\" NONE your_password");
        Console.WriteLine("  dotnet run send sender@example.com receiver@example.com \"With Attachment\" \"File attached\" un_fisier.txt your_password");
        Console.WriteLine("  dotnet run fetch user@example.com your_password");
        Console.WriteLine("  dotnet run fetch user@example.com your_password -p IMAP -f INBOX -s \"FROM 'no-reply@accounts.google.com'\"");
        Console.WriteLine("  dotnet run fetch user@example.com your_password -p POP3");
    }
}