using RedisMan.Library.Commands;
using RedisMan.Library.Models;
using RedisMan.Library.Values;

using System.Net.Sockets;
using System.Text;

namespace RedisMan.Library;


public class Connection : IDisposable
{
    
    public string Host { get; set; }
    public int Port { get; set; }
    public int ReceiveTimeout { get; set; } = 1000;
    private TcpClient TcpClient { get; set; }
    public NetworkStream Stream { get; set; }
    public RESPParser Parser { get; set; }
    public ServerInfo ServerInfo { get; set; }

    public bool IsConnected { get => Stream.Socket.Connected; }


    //connectionstring= redis://user:password@host:port/dbnum:
    public Connection()
    {

    }

    public void TryConnecting()
    {
        TcpClient = new TcpClient(AddressFamily.InterNetwork);
        TcpClient.Client.ReceiveTimeout = ReceiveTimeout;
        TcpClient.Client.NoDelay = true;

        TcpClient.Connect(Host, Port);

        Stream = TcpClient.GetStream();
        if (Parser is null) Parser = new RESPParser();
        Parser.Reset(Stream);
    }


    /// <summary>
    /// Closes the connection, but it doesnt it
    /// </summary>
    public void Close()
    {
        TcpClient.Close();
    }

    public static Connection Connect(string host, int port)
    {
        var connection = new Connection()
        {
            Host = host,
            Port = port
        };

        connection.TryConnecting();

        ///After connecting, grab server information if available
        if (connection.IsConnected)
        {
            connection.GetServerInfo();
        }

        return connection;
    }



    public void Dispose()
    {
        TcpClient.Dispose();
    }

    public void Send(ParsedCommand command)
    {
        Send(command.Text);
    }

    public void Send(string command)
    {
        string[] commandParts = command.Split(' ');

        //TODO: Create class to parse messages and get byte array
        var commandBuilder = new StringBuilder();
        commandBuilder.Append($"*{commandParts.Length}\r\n");
        foreach (var part in commandParts)
        {
            commandBuilder.Append($"${part.Length}\r\n{part}\r\n");
        }

        var messageBytes = Encoding.UTF8.GetBytes(commandBuilder.ToString());

        Stream.Write(messageBytes, 0, messageBytes.Length);
        // Receive ack.
    }

    public RedisValue Receive()
    {
        //wait until data is available here
        while (!Stream.DataAvailable && Stream.Socket.Connected)
            Thread.Sleep(100);

        return Parser.Parse();
    }

    private void GetServerInfo()
    {
        Send("INFO");
        RedisValue value = Receive();
        if (value != null && value is RedisBulkString bulkString)
        {
            ServerInfo = CommandParser.ParseInfoOutput(bulkString);
        }
    }

    public IEnumerable<RedisValue> SafeKeys(string pattern)
    {
        string cursor = "0";
        bool stillReading = true;
        while (stillReading)
        {
            Send($"SCAN {cursor}{(!string.IsNullOrWhiteSpace(pattern) ? $" MATCH {pattern}" : "")}");
            RedisValue value = Receive();

            if (value is RedisArray array)
            {
                if (array.Length > 1)
                {
                    cursor = array.Values[0].Value;
                    if (array.Values[1] is RedisArray sub)
                    {
                        foreach (var key in sub.Values)
                        {
                            yield return key;
                        }
                    } else stillReading= false;
                }
                else stillReading = false;
            }

            if (cursor == "0") stillReading = false;
        }
        
    }
}
