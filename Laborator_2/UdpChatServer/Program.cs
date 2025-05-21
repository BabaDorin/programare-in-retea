using System.Net;
using System.Net.Sockets;
using System.Text;

public class UdpChatServer
{
    private static readonly HashSet<IPEndPoint> clients = new HashSet<IPEndPoint>();
    private static UdpClient udpListener;

    public static async Task Main(string[] args)
    {
        string host = "0.0.0.0";
        int port = 12345; 

        try
        {
            IPAddress localAddr = IPAddress.Parse(host);
            udpListener = new UdpClient(new IPEndPoint(localAddr, port));
            Console.WriteLine($"UDP server listening on {host}:{port}");

            while (true)
            {
                UdpReceiveResult receivedResult = await udpListener.ReceiveAsync();
                IPEndPoint clientEndPoint = receivedResult.RemoteEndPoint;
                string message = Encoding.UTF8.GetString(receivedResult.Buffer);

                // Adaugă clientul la listă dacă e nou
                bool newClient = false;
                lock (clients)
                {
                    if (!clients.Contains(clientEndPoint))
                    {
                        clients.Add(clientEndPoint);
                        newClient = true;
                    }
                }
                if (newClient)
                {
                    Console.WriteLine($"New client added: {clientEndPoint}");
                    byte[] welcomeMsg = Encoding.UTF8.GetBytes($"Welcome {clientEndPoint}! You are connected.");
                    await udpListener.SendAsync(welcomeMsg, welcomeMsg.Length, clientEndPoint);
                }


                Console.WriteLine($"Received from {clientEndPoint}: {message}");

                if (message.StartsWith("/pm"))
                {
                    HandlePrivateMessage(message, clientEndPoint);
                }
                else if (message.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    lock (clients)
                    {
                        clients.Remove(clientEndPoint);
                    }
                    Console.WriteLine($"Client {clientEndPoint} disconnected.");
                    byte[] exitAck = Encoding.UTF8.GetBytes("You have been disconnected.");
                    await udpListener.SendAsync(exitAck, exitAck.Length, clientEndPoint);
                }
                else
                {
                    BroadcastMessage(message, clientEndPoint);
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
            udpListener?.Close();
            Console.WriteLine("Server stopped.");
        }
    }

    private static async void HandlePrivateMessage(string fullMessage, IPEndPoint senderEndPoint)
    {
        try
        {
            string[] parts = fullMessage.Split(new char[] { ' ' }, 3);
            if (parts.Length < 3)
            {
                string errorMsg = "Invalid /pm format. Use: /pm <ip>:<port> <message_content>";
                byte[] errorData = Encoding.UTF8.GetBytes(errorMsg);
                await udpListener.SendAsync(errorData, errorData.Length, senderEndPoint);
                Console.WriteLine($"Invalid private message format from {senderEndPoint}: {fullMessage}");
                return;
            }

            string recipientAddressStr = parts[1]; // <ip>:<port>
            string privateMsgContent = parts[2];

            string[] recipientIpPort = recipientAddressStr.Split(':');
            if (recipientIpPort.Length != 2)
            {
                string errorMsg = "Invalid recipient address format in /pm. Use: <ip>:<port>";
                byte[] errorData = Encoding.UTF8.GetBytes(errorMsg);
                await udpListener.SendAsync(errorData, errorData.Length, senderEndPoint);
                return;
            }

            IPAddress recipientIp = IPAddress.Parse(recipientIpPort[0]);
            int recipientPort = int.Parse(recipientIpPort[1]);
            IPEndPoint recipientEndPoint = new IPEndPoint(recipientIp, recipientPort);

            bool recipientFound = false;
            lock (clients) // Sincronizăm accesul la lista de clienți
            {
                recipientFound = clients.Contains(recipientEndPoint);
            }


            if (recipientFound)
            {
                string messageToSend = $"Private message from {senderEndPoint}: {privateMsgContent}";
                byte[] dataToSend = Encoding.UTF8.GetBytes(messageToSend);
                await udpListener.SendAsync(dataToSend, dataToSend.Length, recipientEndPoint);
                Console.WriteLine($"Private message sent from {senderEndPoint} to {recipientEndPoint}");
            }
            else
            {
                string errorMsg = $"Error: Client {recipientEndPoint} not found or not active.";
                byte[] errorData = Encoding.UTF8.GetBytes(errorMsg);
                await udpListener.SendAsync(errorData, errorData.Length, senderEndPoint);
                Console.WriteLine($"Recipient {recipientEndPoint} for PM from {senderEndPoint} not found.");
            }
        }
        catch (FormatException fe)
        {
            string errorMsg = "Invalid IP address or port format in /pm.";
            byte[] errorData = Encoding.UTF8.GetBytes(errorMsg);
            await udpListener.SendAsync(errorData, errorData.Length, senderEndPoint);
            Console.WriteLine($"Format error processing private message from {senderEndPoint}: {fullMessage} - {fe.Message}");
        }
        catch (Exception e)
        {
            string errorMsg = "Error processing private message.";
            byte[] errorData = Encoding.UTF8.GetBytes(errorMsg);
            await udpListener.SendAsync(errorData, errorData.Length, senderEndPoint);
            Console.WriteLine($"Error processing private message from {senderEndPoint}: {e.Message}");
        }
    }

    private static async void BroadcastMessage(string message, IPEndPoint senderEndPoint)
    {
        string broadcastMessageContent = $"Public message from {senderEndPoint}: {message}";
        byte[] data = Encoding.UTF8.GetBytes(broadcastMessageContent);

        List<IPEndPoint> clientsCopy;
        lock (clients)
        {
            clientsCopy = new List<IPEndPoint>(clients);
        }

        foreach (IPEndPoint clientEP in clientsCopy)
        {
            if (!clientEP.Equals(senderEndPoint))
            {
                try
                {
                    await udpListener.SendAsync(data, data.Length, clientEP);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error broadcasting to client {clientEP}: {e.Message}");
                }
            }
        }
    }
}