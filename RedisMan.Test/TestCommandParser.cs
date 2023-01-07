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
        Assert.Equal(command.Modifier, null);
        
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

    [Fact]
    public void TestModifier()
    {
        var parser = new CommandParser(null);
        var commandMod1 = parser.Parse("GET INFO#:gzip");
        Assert.Equal(commandMod1.Name, "GET");
        Assert.Equal(commandMod1.Args.Length, 1);
        Assert.Equal(commandMod1.Args[0], "INFO");
        Assert.Equal(commandMod1.Modifier, "gzip");
        
        var commandMod2 = parser.Parse("   GET INFO #:snappy");
        Assert.Equal(commandMod2.Name, "GET");
        Assert.Equal(commandMod2.Args.Length, 1);
        Assert.Equal(commandMod2.Args[0], "INFO");
        Assert.Equal(commandMod2.Modifier, "snappy");
    }
    
    [Fact]
    public void TestEncoding()
    {
        var parser = new CommandParser(null);
        var commandMod1 = parser.Parse("SET VALUE TEST#:gzip");
        Assert.Equal(commandMod1.Name, "SET");
        Assert.Equal(commandMod1.Args.Length, 2);
        Assert.Equal(commandMod1.Args[0], "VALUE");
        Assert.Equal(commandMod1.Modifier, "gzip");
        
        
        var commandB64 = parser.Parse("SET VALUE TEST#:base64");
        Assert.Equal(commandB64.Name, "SET");
        Assert.Equal(commandB64.Args.Length, 2);
        Assert.Equal(commandB64.Args[0], "VALUE");
        Assert.Equal(commandB64.Modifier, "base64");
        
    }
    
    
    [Fact]
    public void TestPiping()
    {
        var parser = new CommandParser(null);
        var commandMod1 = parser.Parse("GET VALUE TEST | sort.exe");
        Assert.Equal(commandMod1.Name, "GET");
        Assert.Equal(commandMod1.Args.Length, 2);
        Assert.Equal(commandMod1.Args[0], "VALUE");
        Assert.Equal(commandMod1.Pipe, "sort.exe");
        
        var command2 = parser.Parse("GET VALUE TEST | complex.exe dosomething");
        Assert.Equal(command2.Name, "GET");
        Assert.Equal(command2.Args.Length, 2);
        Assert.Equal(command2.Args[0], "VALUE");
        Assert.Equal(command2.Pipe, "complex.exe dosomething");
        

        
    }
}