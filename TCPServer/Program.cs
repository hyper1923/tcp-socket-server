using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


class Logging
{
    public static void Success(string message)
    {
        Console.WriteLine($"[SUCCESS] {message}");
    }

    public static void Error(string message)
    {
        Console.WriteLine($"[ERROR] {message}");
    }

    public static void Warning(string message)
    {
        Console.WriteLine($"[WARNING] {message}");
    }
}


class Packet : IDisposable
{

    void IDisposable.Dispose() { }
    string packetName = "null";
    dynamic packetData;
    
    public void SetPacketData(dynamic data)
    {
        packetData = data;
        string packetId = data["packetId"];
        packetName = packetId;
    }


    public dynamic GetPacketData()
    {
        return packetData;
    }

    public bool getPacketName(string _packetName)
    {
        if(_packetName == packetName)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

}

class Client
{
    public TcpClient client;
    byte[] receiveBuffer = new byte[1024];
    public bool isConnected = false;
    public Client(TcpClient b)
    {
        client = b;
        isConnected = true;
        client.GetStream().BeginRead(receiveBuffer, 0, receiveBuffer.Length, ReadData, null);
    }


    public void SendMessage(string message)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        Console.WriteLine($"INFO: Sended a bytes lenght: {buffer.Length}");
        try
        {
            client.GetStream().Write(buffer, 0, message.Length);
        }
        catch
        {
            Console.WriteLine($"{client.Client.RemoteEndPoint} has disconnected.");
            Thread.Sleep(1000);
            Environment.Exit(-1);
        }
    }


    public void ReadData(IAsyncResult _result)
    {
        try
        {
            int readedBytes = client.GetStream().EndRead(_result);
            byte[] trueBuffer = new byte[readedBytes];
            Array.Copy(receiveBuffer, trueBuffer, readedBytes);
            string bufferTranslatedToUTF8 = Encoding.UTF8.GetString(trueBuffer);
            dynamic data = JObject.Parse(bufferTranslatedToUTF8);
            using (Packet _packet = new Packet())
            {
                _packet.SetPacketData(data);
                HandlePackets(_packet);
            }
            receiveBuffer = new byte[1024];
            client.GetStream().BeginRead(receiveBuffer, 0, receiveBuffer.Length, ReadData, null);
        }
        catch
        {
            Logging.Error($"{client.Client.RemoteEndPoint} has disconnected.");
            client.Close();
            isConnected = false;
        }

            
    }


    void HandlePackets(Packet _packet)
    {
        if (_packet.getPacketName("Connect"))
        {
            SendMessage(@"{""packetId"" : ""Joined""}");
            Logging.Success("A user connected to server.");
        }
    }

}

class Server
{
    TcpListener listener;
    List<Client> tcpClients = new List<Client>();
    byte[] receiveBuffer = new byte[1024];




    public Server(string ip, int port)
    {
        IPAddress i = IPAddress.Parse(ip);
        listener = new TcpListener(i,port);
        Logging.Success($"Server starting in PORT:{port}");
        try { listener.Start(); } catch { Console.WriteLine($"ERROR: This port has in occupation.");Thread.Sleep(1000) ; Environment.Exit(-1);}
        Thread acceptClientsThread = new Thread(new ThreadStart(AcceptClients));
        Logging.Success($"{acceptClientsThread.ManagedThreadId} Thread has started.");
        Logging.Success($"Server started.");
        acceptClientsThread.Start();
        acceptClientsThread.Join();
    }



    public void AcceptClients()
    {
        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Logging.Success($"A client connected a server with: {client.Client.RemoteEndPoint}");
            tcpClients.Add(new Client(client));
        }
    }

}



class Program
{
    static void Main(string[] args)
    {
        Console.Title = "Server";
        Server s = new Server("127.0.0.1",3000);
    }
}
