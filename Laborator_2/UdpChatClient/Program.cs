using System.Net;
using System.Net.Sockets;
using System.Text;

public class UdpChatClient
{
    private static UdpClient udpClient;
    private static IPEndPoint serverEndPoint;
    private static IPEndPoint localEndPoint;

    public static async Task Main(string[] args)
    {
        string serverHost = "127.0.0.1"; 
        int serverPort = 12345;         

        try
        {
            udpClient = new UdpClient();
            udpClient.Connect(serverHost, serverPort);
            serverEndPoint = (IPEndPoint)udpClient.Client.RemoteEndPoint; // Obține IPEndPoint al serverului
            localEndPoint = (IPEndPoint)udpClient.Client.LocalEndPoint; // Obține IPEndPoint local al clientului

            Console.WriteLine($"UDP Client started. Your address: {localEndPoint}");
            Console.WriteLine($"Sending messages to server at {serverEndPoint}");
            Console.WriteLine("Type 'exit' to quit.");
            Console.WriteLine("To send a private message, use the format: /pm <ip>:<port> <message_content>");
            Console.WriteLine("You can get other client <ip>:<port> details from server logs if needed for /pm.");


            // Trimite un mesaj inițial pentru ca serverul să înregistreze clientul
            // (opțional, dar util pentru ca serverul să știe de client imediat)
            byte[] initialMessage = Encoding.UTF8.GetBytes($"Hello from {localEndPoint}");
            await udpClient.SendAsync(initialMessage, initialMessage.Length);

            // Task pentru a primi mesaje
            CancellationTokenSource cts = new CancellationTokenSource();
            Task receiveTask = Task.Run(async () => await ReceiveMessages(cts.Token));

            // Bucla principală pentru a trimite mesaje
            while (true)
            {
                string messageToSend = Console.ReadLine();

                if (string.IsNullOrEmpty(messageToSend)) continue;

                byte[] dataToSend = Encoding.UTF8.GetBytes(messageToSend);
                await udpClient.SendAsync(dataToSend, dataToSend.Length); 

                if (messageToSend.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Exiting...");
                    cts.Cancel(); // Semnalează task-ului de recepție să se oprească
                    break;
                }
            }

            await receiveTask;
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
            udpClient?.Close();
            Console.WriteLine("Client application terminated.");
        }
    }

    private static async Task ReceiveMessages(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (udpClient.Available > 0)
                {
                    UdpReceiveResult receivedResult = await udpClient.ReceiveAsync();
                    string serverMessage = Encoding.UTF8.GetString(receivedResult.Buffer);
                    Console.WriteLine($"\n{serverMessage}"); // Afișează mesajul de la server
                }
                else
                {
                    // Așteaptă puțin pentru a nu consuma CPU inutil dacă nu sunt mesaje
                    await Task.Delay(50, token);
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // UdpClient a fost închis, normal la ieșire
            Console.WriteLine("Receive task stopping: UdpClient closed.");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Receive task stopping: Operation cancelled.");
        }
        catch (SocketException se) when (se.SocketErrorCode == SocketError.ConnectionReset || se.SocketErrorCode == SocketError.Interrupted)
        {
            // Aceste erori pot apărea când socket-ul e închis forțat
            Console.WriteLine("Receive task stopping: Socket closed or reset.");
        }
        catch (Exception e)
        {
            if (!token.IsCancellationRequested) // Nu afișa erori dacă e o oprire intenționată
            {
                Console.WriteLine($"Error receiving message: {e.Message}");
            }
        }
    }
}