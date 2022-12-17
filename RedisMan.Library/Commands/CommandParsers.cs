using RedisMan.Library.Models;
using RedisMan.Library.Values;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RedisMan.Library.Commands;

public class ParsedCommand
{
    string? _text;
    byte[]? _bytes;
    string[] _args;
    CommandDoc _doc;
    public string Text {
        get => _text ?? string.Empty;
        set {
            _text = value;
            _bytes = System.Text.Encoding.UTF8.GetBytes(Text);
            string[] parts = _text.Trim().Split(' ');
            Name = parts[0];
            _args = parts[1..];
        }
    }
    public CommandDoc? Documentation {
        get => _doc;
        set
        {
            _doc = value;
            if (!string.IsNullOrEmpty(_text))
            {
                if (_doc != null)
                {
                    string argsPart = _text.Substring(_doc.Command.Length);
                    _args = argsPart.Trim().Split(' ');
                } 
            }
        } 
    }
    public string Name { get; set; }
    public byte[]? CommandBytes { get => _bytes; }
    public string[] Args { get => _args;  }
}

public class CommandParser
{
    private Documentation _documentation;
    private string[] commands;

    public CommandParser(Documentation documentation)
    {
        _documentation = documentation;
    }


    public ParsedCommand? Parse(string input)
    {
        var parsed = new ParsedCommand();
        parsed.Text = input;
        //split command parts
        string[] parts = input.Split(' ');
        //check if the command exists
        if (parts.Length > 0)
        {
            string compoundCommand = parts[0] + (parts.Length > 1 ? $" {parts[1]}" : "");
            //get the command documentation
            var command = _documentation.Docs
                .FirstOrDefault(d => string.Equals(d.Command, parts[0], StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(d.Command, compoundCommand, StringComparison.OrdinalIgnoreCase));
            parsed.Documentation = command;
        }

        //get the command bytes

        return parsed;
    }

    public static ServerInfo ParseInfoOutput(RedisBulkString info)
    {
        ServerInfo serverInfo = new();
        int ParseInt(string value)
        {
            if (int.TryParse(value, out int parsedInt))
                return parsedInt;
            return 0;
        }

        //Get all ServerInfoAttributes
        Dictionary<string, PropertyInfo> properties = new();
        Type type = serverInfo.GetType();
        foreach (var p in type.GetProperties())
        {
            var attr = p.GetCustomAttributes(typeof(ServerInfoNameAttribute), true);
            if (attr.Length == 1)
            {
                var attrFirst = attr.First() as ServerInfoNameAttribute;
                properties.Add(attrFirst.Name, p);
            }
        }

        if (!string.IsNullOrWhiteSpace(info.Value))
        {
            var reader = new StringReader(info.Value);
            string line ;
            string section = string.Empty;
            if (reader != null)
            {
                serverInfo.IsAvaible = true;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    //New section
                    if (line[0] == '#')
                    {
                        section = line.Substring(2);
                    }

                    //new vaue
                    if (line.Contains(':'))
                    {
                        string[] values = line.Trim().Split(':');
                        if (values.Length > 1)
                        {
                            if (section == "Keyspace")
                            {
                                var keySpace = new KeySpaceDB();
                                keySpace.DBName = values[0];
                                var subSections = values[1].Split(',');
                                foreach (var subsection in  subSections)
                                {
                                    var pair = subsection.Split('=');
                                    if (pair[0] == "keys") keySpace.Keys = ParseInt(pair[1]);
                                    if (pair[0] == "expires") keySpace.Expires = ParseInt(pair[1]);
                                    if (pair[0] == "avg_ttl") keySpace.AvgTtl = ParseInt(pair[1]);
                                }
                                serverInfo.KeySpace.Add(keySpace);
                            } 
                            else
                            {
                                if (properties.ContainsKey(values[0]))
                                {
                                    var property = properties[values[0]];
                                    if (property.PropertyType == typeof(string))
                                        property.SetValue(serverInfo, values[1]);
                                    if (property.PropertyType == typeof(int))
                                        property.SetValue(serverInfo, ParseInt(values[1]));
                                }
                            }
                        }
                    }
                }
            }
            
        }

        return serverInfo;
    }
}
