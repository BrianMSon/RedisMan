//using IronSnappy;

//namespace RedisMan.Library.Serialization;

//[Serializer("snappy")]
//public class SnappySerializer : ISerializer
//{
//    public string Error { get; set; }
//    public byte[] Serialize(ref byte[] value)
//    {
//        return Snappy.Encode(value);
//    }

//    public byte[] Deserialize(ref byte[] bytes)
//    {
//        try
//        {
//            return Snappy.Decode(bytes);
//        }
//        catch (Exception ex)
//        {
//            Error = ex.Message;
//            return Array.Empty<byte>();
//        }
//    }
//}