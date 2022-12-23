using System.Diagnostics;
using RedisMan.Library.Commands;
using RedisMan.Library.Models;
using RedisMan.Library.Values;

using System.Net.Sockets;
using System.Text;
using ValueType = RedisMan.Library.Values.ValueType;

namespace RedisMan.Library;


public class Connection : IDisposable
{
    
    public string Host { get; set; }
    public int Port { get; set; }
    public int ReceiveTimeout { get; set; } = 1000;
    private TcpClient TcpClient { get; set; }
    public NetworkStream Stream { get; set; }
    public RespParser Parser { get; set; }
    public ServerInfo ServerInfo { get; set; }
    

    public bool IsConnected { get => Stream.Socket.Connected; }

    public bool? IsAuthenticated { get; set; } = null;


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
        if (Parser is null) Parser = new RespParser();
        Parser.Reset(Stream);
    }


    /// <summary>
    /// Closes the connection, but it doesnt it
    /// </summary>
    public void Close()
    {
        TcpClient.Close();
    }

    public static Connection? Connect(string host, int port, string password = "", string username = "")
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
            //Legacy connection
            if (!string.IsNullOrWhiteSpace(password) && string.IsNullOrWhiteSpace(username))
            {
                connection.Send($"AUTH {password}");
                if (connection.Receive() is RedisString value)
                {
                    connection.IsAuthenticated = value.Value == "OK";
                }
            }
            
            //new ACL connection
            if (!string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(username))
            {
                connection.Send($"AUTH {username} {password}");
                if (connection.Receive() is RedisString value)
                {
                    connection.IsAuthenticated = value.Value == "OK";
                }
            }
            
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

    /// <summary>
    /// Tries to read a value from the connection, until a timeout is reached
    /// </summary>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public RedisValue Receive(int timeout = -1)
    {
        var sw = Stopwatch.StartNew();
        //wait until data is available here
        while (!Stream.DataAvailable && Stream.Socket.Connected)
        {
            if (timeout > 0 && sw.Elapsed.Seconds > timeout)
            {
                sw.Stop();
                return new RedisError() { Value = "Command Timeout" };
            }
            Thread.Sleep(100);
        }

        return Parser.Parse();
    }
    
    
    public IEnumerable<RedisValue> Subscribe(CancellationToken cancelToken)
    {
        //wait until data is available here
        while (!cancelToken.IsCancellationRequested && Stream.Socket.Connected)
        {
            while (!Stream.DataAvailable && Stream.Socket.Connected) Thread.Sleep(100);
            yield return Parser.Parse();            
        }

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
    
    private IEnumerable<RedisValue> SafeSets(string key, string pattern = "")
    {
        string cursor = "0";
        bool stillReading = true;
        while (stillReading)
        {
            Send($"SSCAN {key} {cursor}{(!string.IsNullOrWhiteSpace(pattern) ? $" MATCH {pattern}" : "")}");
            if (Receive() is RedisArray array)
            {
                if (array.Length > 1)
                {
                    cursor = array.Values[0].Value;
                    if (array.Values[1] is RedisArray sub)
                    {
                        foreach (var element in sub.Values)
                        {
                            yield return element;
                        }
                    } else stillReading= false;
                }
                else stillReading = false;
            }

            if (cursor == "0") stillReading = false;
        }
    }

    private IEnumerable<RedisValue> SafeSortedSets(string key, string pattern = "")
    {
        string cursor = "0";
        bool stillReading = true;
        while (stillReading)
        {
            Send($"ZSCAN {key} {cursor}{(!string.IsNullOrWhiteSpace(pattern) ? $" MATCH {pattern}" : "")}");
            if (Receive() is RedisArray array)
            {
                if (array.Length > 1)
                {
                    cursor = array.Values[0].Value;
                    if (array.Values[1] is RedisArray sub)
                    {
                        foreach (var element in sub.Values)
                        {
                            yield return element;
                        }
                    } else stillReading= false;
                }
                else stillReading = false;
            }

            if (cursor == "0") stillReading = false;
        }
    }
    
    private IEnumerable<RedisValue> SafeHash(string key, string pattern = "")
    {
        string cursor = "0";
        bool stillReading = true;
        while (stillReading)
        {
            Send($"HSCAN {key} {cursor}{(!string.IsNullOrWhiteSpace(pattern) ? $" MATCH {pattern}" : "")}");
            if (Receive() is RedisArray array)
            {
                if (array.Length > 1)
                {
                    cursor = array.Values[0].Value;
                }
                else stillReading = false;
                
                foreach (var element in array.Values)
                {
                    yield return element;    
                }
            }

            if (cursor == "0") stillReading = false;
        }
    }
    
    
    private IEnumerable<RedisValue> SafeStream(string key)
    {
        string cursor = "-";
        bool stillReading = true;
        while (stillReading)
        {
            Send($"XRANGE {key} {cursor} + COUNT 50");
            if (Receive() is RedisArray array) //top level, contains only pairs
            {
                if (array.Length > 0)
                {
                    if (array.Values[0] is RedisArray pair)
                    {
                        cursor = pair.Values[0].Value + "1";
                    }
                    else stillReading = false;

                    foreach (var element in array.Values)
                    {
                        yield return element;    
                    }
                    
                }
                else stillReading = false;
            }
            else stillReading = false;
        }
    }
    
    private IEnumerable<RedisValue>? SafeList(string key)
    {
        Send($"LLEN {key}");
        var value = Receive();
        int llen = 0;
        if (value.Type == ValueType.Integer)
        {
            int.TryParse(value.Value, out llen);
        }
        

        for (int i = 0; i < llen; i+=100)
        {
            int to = i + 100;
            Send($"LRANGE {key} {i} {to}");
            if (Receive() is RedisArray array)
            {
                foreach (var el in array.Values)
                {
                    yield return el;
                }                
            }
        }
    }

    /// <summary>
    /// Gets a Key value regardless of type
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public (string type, RedisValue? value, IEnumerable<RedisValue>? collection) GetKeyValue(ParsedCommand command)
    {
        var key = command.Args[0];
        Send($"TYPE {key}");
        var value = Receive(); //keyName
        if (value is RedisString stringValue)
        {
            var keyType = stringValue.Value ?? "";
            switch (keyType)
            {
                case "string":
                    Send($"GET {key}");
                    return (keyType,Receive(), null);
                case "list":
                    return (keyType,null, SafeList(key));
                case "set":
                    return (keyType,null, SafeSets(key));
                case "zset":
                    return (keyType,null, SafeSortedSets(key));
                case "hash":
                    return (keyType,null, SafeHash(key));
                case "stream":
                    return (keyType,null, SafeStream(key));
            }
        }
        return ("", RedisValue.Null, null);
        //types that can be returned
    }
}
