using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Xml.Linq;

namespace RedisMan.Library.Commands;
public class Documentation
{
    private readonly string[] _dangerousCommands = {
        "FLUSHDB", "FLUSHALL", "KEYS", "PEXPIRE", "DEL", "CONFIG", 
        "SHUTDOWN", "BGREWRITEAOF", "BGSAVE", "SAVE", "SPOP", "SREM", "RENAME", "DEBUG"
    };

    public List<CommandDoc> Docs { get; private set; } = new();


    public void Generate()
    {
        var filename = "simple_commands.json";
        if (File.Exists(filename))
        {
            string json = File.ReadAllText(filename);
            /* This doesnt work in rider, so i will disable it for now
            var docs = (List<CommandDoc>)JsonSerializer.Deserialize(
                json,
                typeof(List<CommandDoc>),
                DocumentationReaderContext.Default);
            */
            var docs = JsonSerializer.Deserialize<List<CommandDoc>>(json);
            if (docs != null) Docs = docs;
        }

        Docs.Add(new CommandDoc()
        {
            Arguments = "",
            Command = "EXIT",
            Group = "application",
            Since = "",
            Summary = "Closess the application"

        });

        Docs.Add(new CommandDoc()
        {
            Arguments = "ip [port] <[password] | [username] [password]>",
            Command = "CONNECT",
            Group = "application",
            Since = "",
            Summary = "Connects to another server"

        });

        Docs.Add(new CommandDoc()
        {
            Arguments = "[command]",
            Command = "HELP",
            Group = "application",
            Since = "",
            Summary = "Prints Help Message"

        });

        Docs.Add(new CommandDoc()
        {
            Arguments = "",
            Command = "CLEAR",
            Group = "application",
            Since = "",
            Summary = "Clears Console"

        });

        Docs.Add(new CommandDoc()
        {
            Arguments = "[pattern]",
            Command = "SAFEKEYS",
            Group = "application",
            Since = "",
            Summary = "Safe version of KEYS, client implementation."

        });

        Docs.Add(new CommandDoc()
        {
            Arguments = "key",
            Command = "VIEW",
            Group = "application",
            Since = "",
            Summary = "View key value regardless of type."

        });
        
        
        Docs.Add(new CommandDoc()
        {
            Arguments = "command",
            Command = "EXPORT",
            Group = "application",
            Since = "",
            Summary = "Export the output of the command to a filename"

        });
    }


    public List<string> GetCommands(string pattern)
    {
        List<string> commands = new();
        foreach (var doc in Docs)
        {
            if (doc.Command.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                commands.Add(doc.Command);
            }
        }
        return commands;
    }

    public List<CommandDoc> Search(string pattern)
    {
        List<CommandDoc> commands = new();
        foreach (var doc in Docs)
        {
            if (doc.Command.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                commands.Add(doc);
            }
        }
        return commands;
    }

    public CommandDoc? Get(string command)
    {
        List<CommandDoc> commands = new();
        foreach (var doc in Docs)
        {
            if (doc.Command.Equals(command, StringComparison.OrdinalIgnoreCase))
            {
                return doc;
            }
        }


        return null;
    }

    public List<CommandDoc> Search(Regex pattern)
    {
        List<CommandDoc> commands = new();
        foreach (var doc in Docs)
        {
            if (pattern.IsMatch(doc.Command))
            {
                commands.Add(doc);
            } 
            else if (pattern.IsMatch(doc.Summary))
            {
                commands.Add(doc);
            }
        }
        return commands;
    }

    public bool IsCommandDangerous(string command)
    {
        if (_dangerousCommands.Contains(command.ToUpperInvariant()))
        {
            return true;
        }

        return false;
    }
    private void Parse(string filename)
    {
        
    }
}


public static class DocumentationReader
{
    
}

/* This doesnt work in rider, i will disable it for now
[JsonSerializable(typeof(List<CommandDoc>))]
public partial class DocumentationReaderContext : JsonSerializerContext
{
}
*/