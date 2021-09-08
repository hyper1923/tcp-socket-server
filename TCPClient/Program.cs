using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text;
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
        if (_packetName == packetName)
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
    TcpClient client;
    byte[] receiveBuffer = new byte[1024];
    public bool isConnected = false;

    public Client(string ip, int port)
    {
        client = new TcpClient();
        client.Connect(ip, port);
        client.GetStream().BeginRead(receiveBuffer, 0,1024,HandleData,null);
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
            Console.WriteLine($"ERROR: The server is closed.");
            Thread.Sleep(1000);
            Environment.Exit(-1);
        }
    }


    public void HandleData(IAsyncResult _result)
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
            client.GetStream().BeginRead(receiveBuffer, 0, 1024, HandleData, null);
        }
        catch
        {
            Logging.Error($"Server has closed.");
            client.Close();
            isConnected = false;
        }
    }


    public void HandlePackets(Packet _packet)
    {
        if (_packet.getPacketName("Joined"))
        {
            Logging.Success("Successfully joined the server.");
        }
    }

}


class Program
{
    static void Main(string[] args)
    {
        Client c = new Client("127.0.0.1",3000);
        c.SendMessage(@"{""packetId"" : ""Connect""}");

        Console.ReadLine();
    }
}

