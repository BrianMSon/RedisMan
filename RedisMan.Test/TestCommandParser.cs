using System.Text;
using RedisMan.Library.Commands;

namespace RedisMan.Test;

public class TestCommandParser
{
    [Fact]
    public void TestCommandParsing()
    {
        var parser = new CommandParser(null);
        var command = parser.Parse("FT.AGGREGATE projectsIdx  \"concrete @County:{Anchorage}\" GROUPBY 1 @opsplannum REDUCE COUNT 0 AS results");
        Assert.Equal(command.Name, "FT.AGGREGATE");
        Assert.Equal(command.Args.Length, 10);
        Assert.Equal(command.Args[0],"projectsIdx");
        
        var infoCommand = parser.Parse("INFO");
        Assert.Equal(infoCommand.Name, "INFO");
        Assert.Equal(infoCommand.Args.Length, 0);
        Assert.Equal(infoCommand.CommandBytes,Encoding.UTF8.GetBytes("*1\r\n$4\r\nINFO\r\n"));
        
        var getCommand = parser.Parse("GET in\"fo");
        Assert.Equal(getCommand.Name, "GET");
        Assert.Equal(getCommand.Args.Length, 1);
        Assert.Equal(getCommand.Args[0], "in\"fo");
        Assert.Equal(getCommand.CommandBytes,Encoding.UTF8.GetBytes("*2\r\n$3\r\nGET\r\n$5\r\nin\"fo\r\n"));
        
        var trailingCommand = parser.Parse("     GET in\"fo");
        Assert.Equal(trailingCommand.Name, "GET");
        Assert.Equal(trailingCommand.Args.Length, 1);
        Assert.Equal(trailingCommand.Args[0], "in\"fo");
        Assert.Equal(trailingCommand.CommandBytes,Encoding.UTF8.GetBytes("*2\r\n$3\r\nGET\r\n$5\r\nin\"fo\r\n"));
        
        var spacedCommand = parser.Parse("     GET in\"fo        ");
        Assert.Equal(spacedCommand.Name, "GET");
        Assert.Equal(spacedCommand.Args.Length, 1);
        Assert.Equal(spacedCommand.Args[0], "in\"fo");
        Assert.Equal(spacedCommand.CommandBytes,Encoding.UTF8.GetBytes("*2\r\n$3\r\nGET\r\n$5\r\nin\"fo\r\n"));        
        
    }
}