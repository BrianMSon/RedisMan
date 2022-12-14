using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace RedisMan.Library.Commands;
public class Documentation
{
    public List<CommandDoc> Docs { get; set; }


    public Documentation()
    {
        Docs = new List<CommandDoc>();
    }

    public void Generate()
    {
        string filename = "simple_commands.json";
        if (File.Exists(filename))
        {
            string json = File.ReadAllText(filename);
            var docs = (List<CommandDoc>)JsonSerializer.Deserialize(
                json,
                typeof(List<CommandDoc>),
                DocumentationReaderContext.Default);
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
            Arguments = "ip port",
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

    private void Parse(string filename)
    {
        
    }
}


public static class DocumentationReader
{
    
}


[JsonSerializable(typeof(List<CommandDoc>))]
public partial class DocumentationReaderContext : JsonSerializerContext
{
}
