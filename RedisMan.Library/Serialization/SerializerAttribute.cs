namespace RedisMan.Library.Serialization;

public class SerializerAttribute : Attribute
{
    public string Name { get; set; }
    public SerializerAttribute(string name)
    {
        Name = name;
    }
}