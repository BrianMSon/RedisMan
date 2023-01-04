using System.IO.Compression;
using System.Text;

namespace RedisMan.Library.Serialization;


[Serializer("gzip")]
public class GZipSerializer : ISerializer
{
    public string Error { get; set; }
    public byte[] Serialize(ref byte[] value)
    {
        using var memoryStream = new MemoryStream();
        using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress))
        {
            gZipStream.Write(value);
        }
        return memoryStream.ToArray();
    }
    
    public byte[] Deserialize(ref byte[] bytes)
    {
        try
        {
            using var memoryStream = new MemoryStream(bytes);
            using var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            using var memoryStreamOutput = new MemoryStream();
            gZipStream.CopyTo(memoryStreamOutput);
            var outputBytes = memoryStreamOutput.ToArray();
            return outputBytes;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            return Array.Empty<byte>();
        }
    }
}

