using System.Text;
using PrettyPrompt.Consoles;
using PrettyPrompt.Highlighting;
using RedisMan.Library;
using RedisMan.Library.Commands;
using RedisMan.Library.Serialization;
using RedisMan.Library.Values;
using static RedisMan.Terminal.PrintHelpers;
using ValueType = RedisMan.Library.Values.ValueType;

namespace RedisMan.Terminal;

public static class PrintHelpers
{
    public static string Underline(string word) =>
        AnsiEscapeCodes.ToAnsiEscapeSequenceSlow(new ConsoleFormat(Underline: true)) + word + AnsiEscapeCodes.Reset;

    public static string Bold(string word) =>
        AnsiEscapeCodes.ToAnsiEscapeSequenceSlow(new ConsoleFormat(Bold: true)) + word + AnsiEscapeCodes.Reset;

    public static string WithColor(string word, AnsiColor color) =>
        AnsiEscapeCodes.ToAnsiEscapeSequenceSlow(new ConsoleFormat(Foreground: color)) + word + AnsiEscapeCodes.Reset;

    public static string WithFormat(string word, in ConsoleFormat format) =>
        AnsiEscapeCodes.ToAnsiEscapeSequenceSlow(format) + word + AnsiEscapeCodes.Reset;
}

/// <summary>
/// Writes values to different outputs, types might be added in the future
/// so its a good idea to have these functions in one place
/// </summary>
public static class ValueOutput
{
    public static async Task PrintRedisValues(IEnumerable<RedisValue> values, int warningAt = 100, string type = "")
    {
        var i = 0;
        foreach (var value in values)
        {
            i++;
            Console.Write($"{i}) "); //no need to show array data for hashes
            await PrintRedisValue(value, "", type: type);  //no need to print the cursor in redis hashes
            
            if (i % warningAt != 0) continue;
            var sb = new StringBuilder();
            sb.Append("Continue Listing?");
            sb.Append($" {WithColor("(Y/N)", AnsiColor.Yellow)} ");
            sb.Append(AnsiEscapeCodes.Reset);
            Console.Write(sb.ToString());
            var consoleKey = Console.ReadKey();
            Console.WriteLine();
            if (consoleKey.KeyChar != 'Y' && consoleKey.KeyChar != 'y')
            {
                break;
            }

            
        }
    }
    
    public static async Task PrintRedisHash(IEnumerable<RedisValue> values, int warningAt = 100)
    {
        var i = 0;
        foreach (var value in values)
        {
            i++;
            if (value.Type == ValueType.BulkString) //bulkstring are cursors from the HSCAN command 
            {
                continue;
            }
            await PrintRedisValue(value, "", type:"hash", newline: false); 
            
            if (i % warningAt != 0) continue;
            var sb = new StringBuilder();
            sb.Append("Continue Listing?");
            sb.Append($" {WithColor("(Y/N)", AnsiColor.Yellow)} ");
            sb.Append(AnsiEscapeCodes.Reset);
            Console.Write(sb.ToString());
            var consoleKey = Console.ReadKey();
            Console.WriteLine();
            if (consoleKey.KeyChar != 'Y' && consoleKey.KeyChar != 'y')
            {
                return;
            }
            
        }
    }
    
    public static async Task PrintRedisStream(IEnumerable<RedisValue> values, int warningAt = 100)
    {
        var i = 0;
        foreach (var value in values)
        {
            i++;
            if (value is RedisArray array)
            {
                await PrintRedisValue(array.Values[0], padding: "", newline: false);
                await PrintRedisValue(array.Values[1], " ", type: "stream",
                    newline: false); //no need to print the cursor in redis hashes

            }
            
            if (i % warningAt != 0) continue;
            var sb = new StringBuilder();
            sb.Append("Continue Listing?");
            sb.Append($" {WithColor("(Y/N)", AnsiColor.Yellow)} ");
            sb.Append(AnsiEscapeCodes.Reset);
            Console.Write(sb.ToString());
            var consoleKey = Console.ReadKey();
            Console.WriteLine();
            if (consoleKey.KeyChar != 'Y' && consoleKey.KeyChar != 'y')
            {
                return;
            }
            

        }
    }    

    public static async Task PrintRedisValue(RedisValue value, string padding = "", 
        bool color = true, string type = "", bool newline = true, ISerializer serializer = null)
    {
        string GetDeserialized(string value)
        {
            if (serializer != null)
            {
                var bytes = Encoding.ASCII.GetBytes(value);
                var deserialized = serializer.Deserialize(ref bytes);
                return Encoding.ASCII.GetString(deserialized);
            }
            else return value;
        }
        
        if (value is RedisArray array)
        {
            if (!string.IsNullOrEmpty(padding)) Console.WriteLine();
            int visualIdx = 1;
            for (int i = 0; i < array.Values.Count; )
            {
                if (type == "") Console.Write($"{padding}{visualIdx})");
                if (type == "hash") Console.Write("#");
                if (type == "stream") Console.Write("@");
                await PrintRedisValue(array.Values[i], padding + "  ", newline: false );
                i++;
                //FORMAT RESP2 Array hashes
                if (type is "hash" or "stream" && i < array.Values.Count)
                {
                    Console.Write("=");
                    await PrintRedisValue(array.Values[i], padding + "  ", newline: false);
                    i++;
                }

                visualIdx++;
                Console.WriteLine();                
            }
        }
        else
        {
            var consoleFormat = new ConsoleFormat();
            string outputText = "";
            switch (value.Type)
            {
                case Library.Values.ValueType.String:
                    outputText = $"{padding}\"{GetDeserialized(value.Value)}\"";
                    consoleFormat = new ConsoleFormat(Foreground: AnsiColor.BrightBlue);
                    break;
                case Library.Values.ValueType.Null:
                    outputText = $"{padding}null";
                    consoleFormat = new ConsoleFormat(Foreground: AnsiColor.BrightBlack);
                    break;
                case Library.Values.ValueType.BulkString:
                    if (value.Value is null)
                    {
                        outputText = $"{padding}null";
                        consoleFormat = new ConsoleFormat(Foreground: AnsiColor.BrightBlack);
                    }
                    else
                    {
                        outputText = $"{padding}\"{GetDeserialized(value.Value)}\"";
                        consoleFormat = new ConsoleFormat(Foreground: AnsiColor.BrightBlue);
                    }

                    break;
                case Library.Values.ValueType.Integer:
                    outputText = $"{padding}{GetDeserialized(value.Value)}";
                    consoleFormat = new ConsoleFormat(Foreground: AnsiColor.Magenta);
                    break;
                case Library.Values.ValueType.Error:
                    outputText = $"{padding}{GetDeserialized(value.Value)}";
                    consoleFormat = new ConsoleFormat(Foreground: AnsiColor.Red, Bold: true);
                    break;
            }

            if (color)
                Console.Write(AnsiEscapeCodes.Reset + AnsiEscapeCodes.ToAnsiEscapeSequenceSlow(consoleFormat) +
                                  outputText + AnsiEscapeCodes.Reset);
            else
                Console.Write(outputText);
            
            if (newline) Console.WriteLine();
        }
    }


    public static async Task ExportAsync(Connection connection, string filename, ParsedCommand command)
    {
        //is this a valid filename?
        if (filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return;
        await using var fs = new FileStream(filename, FileMode.Create);
        await using var stream = new StreamWriter(fs);
        if (command is { Name: "VIEW", Args.Length: > 0 })
        {
            var (type, value, enumerable) = connection.GetKeyValue(command);
            if (value != null)
            {
                await WriteValueAsync(stream, value, type);
                //output single value to text file
            }

            if (enumerable != null)
            {
                foreach (var element in enumerable)
                {
                    //ignore HSCAN iterator BulkString
                    if (type == "hash" && element.Type == ValueType.BulkString) 
                    {
                        continue;
                    }
                    await WriteValueAsync(stream, element, type);
                }
            }
        }
        else
        {
            connection.Send(command);
            var value = connection.Receive();
            await WriteValueAsync(stream, value, "");
            //save single value to file
            //await PrintRedisValue(value);            
        }
    }

    /// <summary>
    /// TODO: pass ISerializer and deserialize
    /// </summary>
    /// <param name="output"></param>
    /// <param name="value"></param>
    /// <param name="type"></param>
    public static async Task WriteValueAsync(StreamWriter output, RedisValue value, string type)
    {
        if (value is RedisArray array)
        {
            for (int i = 0; i < array.Values.Count; )
            {
                await WriteValueAsync(output, array.Values[i], type);
                i++;
                if (type == "hash" && i < array.Values.Count)
                {

                    await output.WriteAsync("=");
                    await WriteValueAsync(output, array.Values[i], type);
                    i++;
                }

                await output.WriteLineAsync();
            }
        }
        else
        {
            string outputText = "";
            switch (value.Type)
            {
                case Library.Values.ValueType.String:
                    outputText = $"\"{value.Value}\"";
                    break;
                case Library.Values.ValueType.Null:
                    outputText = $"(null)";
                    break;
                case Library.Values.ValueType.BulkString:
                    if (value.Value is null)
                        outputText = $"(null)";
                    else
                        outputText = $"\"{value.Value}\"";
                    break;
                case Library.Values.ValueType.Integer:
                    outputText = $"{value.Value}";
                    break;
                case Library.Values.ValueType.Error:
                    outputText = $"{value.Value}";
                    break;
            }

            await output.WriteAsync(outputText);
        }
    }
}