using RedisMan.Library.Models;
using RedisMan.Library.Values;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RedisMan.Library.Serialization;

namespace RedisMan.Library.Commands;

public class ParsedCommand
{
    public string Text { get; set; }
    public CommandDoc? Documentation { get; set; }
    public string Name { get; set; }
    public byte[]? CommandBytes { get; set; }
    public string[] Args { get; set; }
    public string Modifier { get; set; }
    public string Pipe { get; set; }
}

public class CommandParser
{
    private Documentation _documentation;
    private string[] commands;

    public CommandParser(Documentation documentation)
    {
        _documentation = documentation;
    }

    /// <summary>
    /// TODO: implement escape for "
    /// </summary>
    /// <param name="input"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public ReadOnlySpan<char> ParseToken(ReadOnlySpan<char> input, int start, out int end)
    {
        int charPos = start;
        //consume all spaces
        while (charPos < input.Length && input[charPos] == ' ') charPos++;
        
        if (charPos == input.Length) {
            end = charPos;
            return ReadOnlySpan<char>.Empty; //nothing to parse, what do we return here?
        }
        char parseUntil = input[charPos] == '"' ? '"' : ' ';
        if (parseUntil == '"') charPos++; //consume "
        int tokenStartsAt = charPos;

        while (charPos < input.Length && input[charPos] != parseUntil)
        {
            if (input[charPos] == '\\' && charPos + 1 < input.Length)
                charPos++;
            charPos++;
        }
        int tokenEndsAt = charPos;
        if (parseUntil == '"') charPos++; //consume ending "
        
        
        
        end = charPos;
        return input.Slice(tokenStartsAt, tokenEndsAt - tokenStartsAt);
    }
    
    public ParsedCommand Parse(ReadOnlySpan<char> original)
    {
        var parsed = new ParsedCommand();
        ISerializer serializer = null;
        bool shouldSerialize = false, shouldPipe = false;
        int modifierIndex = original.IndexOf("#:", StringComparison.Ordinal);
        
        
        
        if (modifierIndex != -1)
        {
            parsed.Modifier = original.Slice(modifierIndex+2, original.Length - modifierIndex - 2).ToString().Trim();
            if (!string.IsNullOrEmpty(parsed.Modifier))
            {
                serializer = ISerializer.GetSerializer(parsed.Modifier);
                shouldSerialize = true;
                
            }
        }
        if (modifierIndex == -1) modifierIndex = original.Length;
        
        
        int pipeIndex = original.IndexOf("|", StringComparison.Ordinal);
        if (pipeIndex != -1)
        {
            parsed.Pipe = original.Slice(pipeIndex+1, original.Length - pipeIndex - 1).ToString().Trim();
            if (!string.IsNullOrEmpty(parsed.Pipe))
            {
                shouldPipe = true;
            }
        }
        if (pipeIndex == -1) pipeIndex = original.Length;

        var input = shouldSerialize ? original.Slice(0, modifierIndex) :
            shouldPipe ? original.Slice(0, pipeIndex) :
            original;
        
        //var input = original.Slice(0, modifierIndex); //GET value#:gzip
        
        if (!string.IsNullOrEmpty(parsed.Modifier)) serializer = ISerializer.GetSerializer(parsed.Modifier);
        
        
        //example: FT.AGGREGATE projectsIdx  "concrete @County:{Anchorage}" GROUPBY 1 @opsplannum REDUCE COUNT 0 as results
        int start = 0;
        var sb = StringBuilderCache.Acquire();
        int tokens = 0;
        var args = new List<string>();
        while (start < input.Length)
        {
            var token = ParseToken(input, start, out start);
            if (token != ReadOnlySpan<char>.Empty)
            {
                string strToken = token.ToString();
                if (tokens == 0) parsed.Name = strToken;
                else args.Add(strToken);
                if (shouldSerialize)
                {
                    //SERIALIZE SET
                    if (tokens == 2 && parsed.Name == "SET")
                    {
                        var bytes = Encoding.ASCII.GetBytes(strToken);
                        var encoded = serializer.Serialize(ref bytes);
                        var encodedString = Encoding.ASCII.GetString(encoded);
                        sb.Append($"${encodedString.Length}\r\n{encodedString}\r\n");
                    } else sb.Append($"${strToken.Length}\r\n{strToken}\r\n");
                }
                else sb.Append($"${strToken.Length}\r\n{strToken}\r\n");
                
                tokens++;
            }
        }
        
        string respCommand = $"*{tokens}\r\n{StringBuilderCache.GetStringAndRelease(sb)}";
        
        
        
        parsed.CommandBytes = Encoding.UTF8.GetBytes(respCommand);
        parsed.Text = input.ToString();
        parsed.Args = args.ToArray();
        
        if (_documentation != null && !string.IsNullOrEmpty(parsed.Name))
        {
            string compoundCommand = parsed.Name + (parsed.Args.Length > 0 ? $" {parsed.Args[0]}" : "");
            //get the command documentation
            var command = _documentation.Docs
                .FirstOrDefault(d => string.Equals(d.Command, parsed.Name, StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(d.Command, compoundCommand, StringComparison.OrdinalIgnoreCase));
            parsed.Documentation = command;
        }
        return parsed;
    }

    

    public static ServerInfo ParseInfoOutput(RedisBulkString info)
    {
        ServerInfo serverInfo = new();
        ///These assignments are needed to avoid trimming errors on reflection
        serverInfo.RedisVersion = string.Empty;
        serverInfo.RedisMode = string.Empty;
        serverInfo.UsedMemoryHuman = string.Empty;
        serverInfo.TotalSystemMemoryHuman = string.Empty;
        
        
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
