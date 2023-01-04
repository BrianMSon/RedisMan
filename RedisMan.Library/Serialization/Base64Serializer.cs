using System.Text;

namespace RedisMan.Library.Serialization;
/// <summary>
/// ASCII BASE64 Serializer
/// </summary>
[Serializer("base64")]
public class Base64Serializer : ISerializer
{
    public string Error { get; set; }
    public byte[] Serialize(ref byte[] value)
    {
        var b64Str = Convert.ToBase64String(value);
        return Encoding.ASCII.GetBytes(b64Str);
    }
    
    public byte[] Deserialize(ref byte[] bytes)
    {
        var asciiBase64 = Encoding.ASCII.GetString(bytes);
        return System.Convert.FromBase64String(asciiBase64);
    }
}
