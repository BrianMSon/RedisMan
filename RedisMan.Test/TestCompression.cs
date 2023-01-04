using System.Diagnostics;
using System.Text;
using RedisMan.Library.Serialization;

namespace RedisMan.Test;

public class TestCompression
{
    [Fact]
    public void TestSnappy()
    {
        ISerializer serializer = ISerializer.GetSerializer("snappy");
        string data = @"
            Lorem ipsum dolor sit amet, consectetur adipiscing elit.
            Phasellus nec nulla varius, condimentum lectus vel, tincidunt orci.
            Nullam eu ligula et nunc ultricies dignissim.
            Nunc vestibulum leo eget mauris luctus, placerat elementum tortor tristique.
            Integer accumsan massa dignissim libero sollicitudin ultrices ut non enim.
            Integer sed ipsum eu libero mollis aliquet.
            Sed eu erat condimentum, faucibus odio eget, vehicula erat.
            Integer maximus purus eu blandit congue.
            Pellentesque tincidunt elit eu quam ullamcorper, ac venenatis dui tristique.
            Ut congue lacus vitae consequat gravida.
            Morbi eu risus rutrum, scelerisque nunc sit amet, ornare eros.
            Morbi fringilla nisi ac elit interdum, a auctor enim venenatis.
            Quisque gravida magna in libero pellentesque dignissim.
            Nunc ut tellus ac turpis fringilla porttitor.
            Etiam tristique purus nec nibh sollicitudin ornare.
            Vestibulum eget nulla at sapien dictum lacinia elementum a eros.
            Vivamus bibendum ante et cursus tempor.
        ";
        var bytes = Encoding.UTF8.GetBytes(data);
        var compressed = serializer.Serialize(ref bytes);
        var decompressed = serializer.Deserialize(ref compressed);
        Assert.Equal(bytes.Length, decompressed.Length);;
        Assert.True(Enumerable.SequenceEqual(bytes, decompressed));
    }
    
    
    [Fact]
    public void TestGZip()
    {
        ISerializer serializer = ISerializer.GetSerializer("gzip");
        string data = @"
            Lorem ipsum dolor sit amet, consectetur adipiscing elit.
            Phasellus nec nulla varius, condimentum lectus vel, tincidunt orci.
            Nullam eu ligula et nunc ultricies dignissim.
            Nunc vestibulum leo eget mauris luctus, placerat elementum tortor tristique.
            Integer accumsan massa dignissim libero sollicitudin ultrices ut non enim.
            Integer sed ipsum eu libero mollis aliquet.
            Sed eu erat condimentum, faucibus odio eget, vehicula erat.
            Integer maximus purus eu blandit congue.
            Pellentesque tincidunt elit eu quam ullamcorper, ac venenatis dui tristique.
            Ut congue lacus vitae consequat gravida.
            Morbi eu risus rutrum, scelerisque nunc sit amet, ornare eros.
            Morbi fringilla nisi ac elit interdum, a auctor enim venenatis.
            Quisque gravida magna in libero pellentesque dignissim.
            Nunc ut tellus ac turpis fringilla porttitor.
            Etiam tristique purus nec nibh sollicitudin ornare.
            Vestibulum eget nulla at sapien dictum lacinia elementum a eros.
            Vivamus bibendum ante et cursus tempor.
        ";
        var bytes = Encoding.UTF8.GetBytes(data);
        var compressed = serializer.Serialize(ref bytes);
        var decompressed = serializer.Deserialize(ref compressed);
        Assert.Equal(bytes.Length, decompressed.Length);;
        Assert.True(Enumerable.SequenceEqual(bytes, decompressed));
    }
}