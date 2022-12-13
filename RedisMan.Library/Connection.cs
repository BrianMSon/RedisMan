using RedisMan.Library.Values;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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

    public bool IsConnected { get => TcpClient.Connected; }


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
        return connection;
    }



    public void Dispose()
    {
        TcpClient.Dispose();
    }

    public void Send(string commands)
    {
        string[] commandParts = commands.Split(' ');

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
        while (!Stream.DataAvailable)
            Thread.Sleep(100);

        return Parser.Parse();
    }
}
