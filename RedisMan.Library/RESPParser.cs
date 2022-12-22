using RedisMan.Library.Values;
using System.ComponentModel;
using System.Net.Sockets;
using System.Text;

namespace RedisMan.Library;

/// <summary>
/// TODO:
///     - [X] Parse Simple Types
///     - [X] Parse Arrays
///     - [X] Make it Right
///     - [ ] Rent and reuse char[] data
///     - [ ] Make it Fast (Span<T>)
/// </summary>
public class RespParser : IDisposable
{
    const int BUFFER_SIZE = 1024;
    char[]? data; //rent this memory
    byte[] buffer;
    private int _pos = 0;

    private bool eof = false;
    private NetworkStream? _stream;

    public RespParser()
    {
        buffer = new byte[BUFFER_SIZE];
        data = new char[BUFFER_SIZE];
    }


    /// <summary>
    /// Resets the parser with a new connection
    /// this has to be done when a connection is lost if its mean to be reused
    /// </summary>
    /// <param name="stream"></param>
    public void Reset(NetworkStream stream)
    {
        if (_stream is not null) _stream.Dispose();
        //FIX data first time
        if (data is not null) data.AsSpan().Fill('\0');
        buffer.AsSpan().Fill(0);
        _stream = stream;
    }

    private char PeekChar()
    {
        if (_pos >= data.Length || _pos == -1)
        {
            ReadNextChunk();
        }
        return data[_pos];
    }

    private void ReadNextChunk()
    {
        if (_stream.DataAvailable)
        {
            int received = _stream.Read(buffer);
            data = Encoding.UTF8.GetChars(buffer);
            _pos = 0;
        }
        else
        {
            eof = true;
        }
    }

    private char ReadChar()
    {
        if (_pos >= data.Length || _pos == -1)
        {
            ReadNextChunk();
        }
        if (eof) return '\0';
        return data[_pos++];
    }

    public string ParseString()
    {
        var builder = StringBuilderCache.Acquire();
        char chr;
        while ((chr = ReadChar()) != '\r' && PeekChar() != '\n')
            builder.Append(chr);
        ReadChar(); //consume '\n'
        return StringBuilderCache.GetStringAndRelease(builder);
    }

    private List<RedisValue> ParseArray(out int length)
    {
        List<RedisValue> array = new List<RedisValue>();
        var builder = StringBuilderCache.Acquire();
        char chr;
        while ((chr = ReadChar()) != '\r' && PeekChar() != '\n')
            builder.Append(chr);
        ReadChar(); //Consume \n
        if (int.TryParse(StringBuilderCache.GetStringAndRelease(builder), out length))
        {
            for (int i = 0;i< length;i++)
                array.Add(ParseValue());
        }
        return array;
    }

    public string? ParseBulkString(out int length)
    {
        //Consume number of characters in this string
        var builder = StringBuilderCache.Acquire();
        char chr;
        while ((chr = ReadChar()) != '\r' && PeekChar() != '\n')
            builder.Append(chr);
        ReadChar(); //Consume \n

        //Parse the number of characters to read
        if (int.TryParse(StringBuilderCache.GetStringAndRelease(builder), out length))
        {
            builder = StringBuilderCache.Acquire();
            if (length > 0)
            {
                for (int i = 0; i < length; i++)
                {
                    builder.Append(ReadChar()); //Read Binary Safe Characters
                }
            }
            ReadChar(); //Consume \r
            ReadChar(); //Consume \n
            return StringBuilderCache.GetStringAndRelease(builder);
        }
        
        return null;
    }

    public string ParseError()
    {
        return ParseString();
    }

    public string ParseInt()
    {
        return ParseString();
    }

    public RedisValue ParseValue()
    {
        var typeChar = ReadChar();
        var redisValue = RedisValue.FromByte(typeChar);
        switch (redisValue.Type)
        {
            case Values.ValueType.BulkString:
                {
                    var redisBulkString = (RedisBulkString)redisValue;
                    int length = 0;
                    redisBulkString.Value = ParseBulkString(out length);
                    redisBulkString.Length = length;
                    break;
                }
            case Values.ValueType.Array:
                {
                    var redisArray = (RedisArray)redisValue;
                    int length = 0;
                    redisArray.Values = ParseArray(out length);
                    redisArray.Length = length;
                    break;
                }
            case Values.ValueType.String:
                redisValue.Value = ParseString();
                break;
            case Values.ValueType.Integer:
                redisValue.Value = ParseInt();
                break;
            case Values.ValueType.Error:
                redisValue.Value = ParseError();
                break;
        }

        return redisValue;
    }

    public RedisValue Parse()
    {
        if (_stream.DataAvailable)
        {
            //we receive the first
            int received = _stream.Read(buffer);
            data = Encoding.UTF8.GetChars(buffer);
            _pos = 0;
            if (data.Length > 0)
            {
                return ParseValue();
            }
            
            //fist time, we get the type here


            /*while (stream.DataAvailable) 
            {
                received = stream.Read(buffer);
                Console.WriteLine(Encoding.UTF8.GetString(buffer));
            }*/

        }

        return RedisValue.Null;
    }

    public void Dispose()
    {
        _stream!.Dispose();
    }
}

