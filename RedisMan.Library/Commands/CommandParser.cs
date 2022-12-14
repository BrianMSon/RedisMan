using System;
using System.Collections.Generic;
using System.Linq;
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
            _args = _text.Trim().Split(' ')[1..];
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
}
