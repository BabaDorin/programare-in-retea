using System.Net;
using System.Net.Sockets;
using System.Text;

public class ChatServer
{
    private static readonly List<TcpClient> clients = new List<TcpClient>();
    private static readonly List<string> conversationHistory = new List<string>();
    private static TcpListener server = null;
    private const int MAX_CLIENTS = 5;

    public static async Task Main(string[] args)
    {
        string ipAddress = "127.0.0.1";
        int port = 12345;

        try
        {
            IPAddress localAddr = IPAddress.Parse(ipAddress);
            server = new TcpListener(localAddr, port);
            server.Start();
            Console.WriteLine($"Server started and listening on {ipAddress}:{port}");
            Console.WriteLine($"Waiting for connections (max {MAX_CLIENTS})...");

            while (true)
            {
                if (clients.Count < MAX_CLIENTS)
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
                    HandleClientConnected(client);
                }
                else
                {
                    Console.WriteLine("Maximum clients reached. Pausing acceptance of new connections.");
                    await Task.Delay(1000);
                }
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine($"SocketException: {e.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
        }
        finally
        {
            server?.Stop();
            Console.WriteLine("Server stopped.");
        }
    }

    private static void HandleClientConnected(TcpClient tcpClient)
    {
        lock (clients)
        {
            clients.Add(tcpClient);
        }

        IPEndPoint clientEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
        Console.WriteLine($"Client connected: {clientEndPoint?.Address}:{clientEndPoint?.Port}");
        Console.WriteLine($"Active connections: {clients.Count}");

        try
        {
            NetworkStream stream = tcpClient.GetStream();
            StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            lock (conversationHistory)
            {
                if (conversationHistory.Any())
                {
                    foreach (string msg in conversationHistory)
                    {
                        writer.WriteLine(msg);
                    }
                }
                else
                {
                    writer.WriteLine("No previous messages.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending history to {clientEndPoint}: {ex.Message}");
        }


        Task.Run(async () => await HandleClientComm(tcpClient));
    }


    private static async Task HandleClientComm(TcpClient tcpClient)
    {
        NetworkStream stream = null;
        StreamReader reader = null;
        IPEndPoint clientEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
        string clientIdentifier = $"{clientEndPoint?.Address}:{clientEndPoint?.Port}";

        try
        {
            stream = tcpClient.GetStream();
            reader = new StreamReader(stream, Encoding.UTF8);

            while (tcpClient.Connected)
            {
                string message = await reader.ReadLineAsync();
                if (message == null)
                {
                    break;
                }

                string formattedMessage = $"Client {clientIdentifier}: {message}";
                Console.WriteLine(formattedMessage);

                lock (conversationHistory)
                {
                    conversationHistory.Add(formattedMessage);
                }

                await BroadcastMessage(formattedMessage, tcpClient);
            }
        }
        catch (IOException)
        {
            // Happens when client disconnects abruptly or network error
            Console.WriteLine($"Client {clientIdentifier} connection lost (IO).");
        }
        catch (ObjectDisposedException)
        {
            // Stream or client might have been disposed
            Console.WriteLine($"Client {clientIdentifier} connection lost (Disposed).");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error with client {clientIdentifier}: {e.Message}");
        }
        finally
        {
            Console.WriteLine($"Client {clientIdentifier} disconnected.");
            lock (clients)
            {
                clients.Remove(tcpClient);
            }
            tcpClient.Close();
            Console.WriteLine($"Active connections: {clients.Count}");
        }
    }

    private static async Task BroadcastMessage(string message, TcpClient sender)
    {
        List<TcpClient> clientsCopy;
        lock (clients)
        {
            clientsCopy = new List<TcpClient>(clients);
        }

        foreach (TcpClient client in clientsCopy)
        {
            if (client != sender && client.Connected)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                    await writer.WriteLineAsync(message);
                }
                catch (IOException)
                {
                    Console.WriteLine($"Failed to send message to a client (IO). It might have disconnected.");
                }
                catch (ObjectDisposedException)
                {
                    Console.WriteLine($"Failed to send message to a client (Disposed). It might have disconnected.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error broadcasting to a client: {e.Message}");
                }
            }
        }
    }
}