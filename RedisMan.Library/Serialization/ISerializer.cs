namespace RedisMan.Library.Serialization;

public interface ISerializer
{
    string Error { get; set; }
    byte[] Serialize(ref byte[] value);
    byte[] Deserialize(ref byte[] bytes);

    public static ISerializer GetSerializer(string name) => name switch 
    {
        "snappy" => new SnappySerializer(),
        "gzip" => new GZipSerializer(),
        "base64" => new Base64Serializer(),
        _ => null
    };
}