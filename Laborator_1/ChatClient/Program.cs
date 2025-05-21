using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class ChatClient
{
    public static async Task Main(string[] args)
    {
        string serverIp = "127.0.0.1";
        int port = 12345;
        TcpClient client = null;

        try
        {
            client = new TcpClient();
            await client.ConnectAsync(serverIp, port);
            Console.WriteLine($"Connected to server at {serverIp}:{port}");

            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            // Start a task to listen for messages from the server
            Task receiveTask = Task.Run(async () =>
            {
                try
                {
                    while (client.Connected)
                    {
                        string serverMessage = await reader.ReadLineAsync();
                        if (serverMessage == null)
                        {
                            Console.WriteLine("Server connection closed.");
                            break;
                        }
                        Console.WriteLine($"{serverMessage}");
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine("Lost connection to the server (IO).");
                }
                catch (ObjectDisposedException)
                {
                    Console.WriteLine("Lost connection to the server (Disposed).");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error receiving message: {e.Message}");
                }
                finally
                {
                    Console.WriteLine("Disconnected from server. Press Enter to exit.");
                }
            });

            Console.WriteLine("Enter messages to send, or type 'exit' to disconnect:");
            string messageToSend;
            while ((messageToSend = Console.ReadLine()) != null && client.Connected)
            {
                if (messageToSend.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Disconnecting from the server.");
                    break;
                }
                if (!string.IsNullOrEmpty(messageToSend))
                {
                    await writer.WriteLineAsync(messageToSend);
                }
            }

            if (!receiveTask.IsCompleted)
            {
                await Task.WhenAny(receiveTask, Task.Delay(1000));
            }

        }
        catch (SocketException e)
        {
            Console.WriteLine($"SocketException: {e.Message}");
            Console.WriteLine("Failed to connect to the server. Ensure the server is running and accessible.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
        }
        finally
        {
            client?.Close();
            Console.WriteLine("Client application terminated.");
        }
    }
}