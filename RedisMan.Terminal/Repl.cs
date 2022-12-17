using PrettyPrompt;
using RedisMan.Library.Commands;
using RedisMan.Library.Values;
using RedisMan.Library;
using PrettyPrompt.Highlighting;
using PrettyPrompt.Consoles;
using PrettyPrompt.Documents;
using System.Collections.Immutable;
using System.Diagnostics;
using PrettyPrompt.Completion;
using System.Runtime.CompilerServices;
using System.CommandLine;
using System.Collections.ObjectModel;
using System;
using System.ComponentModel.DataAnnotations;
using System.Xml;
using System.Drawing;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RedisMan.Terminal;
/// <summary>
/// TODO:
///     - [X] Try https://github.com/waf/PrettyPrompt
///     - [X] Prompt Mode
///     - [X] Implement autocomplete with documentation
///     - [X] Implement tooltip
///     - [X] Prompt support not connected state (local commands)
///     - [ ] Span Array position numbers
///     - [X] support disconnects
///     - [X] reconnect
///     - [X] Fire and Forget Mode
///     - [X] Parse INFO on connect
///     - [X] Display databases and number of keys
///     - [X] Prevent sending dangerous commands
///     - [X] Local command to autolist all keys (safely)
///     - [X] View command to automatically view data regardless of type\
///     - [ ] implement safe VIEW command
///     - [ ] CHECK DB2 key opsviewer:tools:phlytest:PHLY18-00126 , why so many nulls
///     - [ ] local command to save command output to file
///     - [ ] pipe commands to shell
///     - [X] Test Remote git repository
/// </summary>



public static partial class Repl
{
    

    static (IReadOnlyList<OverloadItem> Overloads, int ArgumentIndex) EmptyOverload() => (Array.Empty<OverloadItem>(), 0);




    private static Task<KeyPressCallbackResult?> PressedF1(string text, int caret, CancellationToken cancellationToken)
    {
        string wordUnderCursor = GetWordAtCaret(text, caret).ToLower();

        // since we return a null KeyPressCallbackResult here, the user will remain on the current prompt
        // and will still be able to edit the input.
        // if we were to return a non-null result, this result will be returned from ReadLineAsync(). This
        // is useful if we want our custom keypress to submit the prompt and control the output manually.
        return Task.FromResult<KeyPressCallbackResult?>(null);

        // local functions
        static string GetWordAtCaret(string text, int caret)
        {
            var words = text.Split(new[] { ' ', '\n' });
            string wordAtCaret = string.Empty;
            int currentIndex = 0;
            foreach (var word in words)
            {
                if (currentIndex < caret && caret < currentIndex + word.Length)
                {
                    wordAtCaret = word;
                    break;
                }
                currentIndex += word.Length + 1; // +1 due to word separator
            }

            return wordAtCaret;
        }
    }

    public static async Task PrintRedisValues(IEnumerable<RedisValue> values)
    {
        int i = 0;
        foreach (var value in values)
        {
            i++;
            Console.Write($"{i + 1})");
            await PrintRedisValue(value, "  ");
            if (i % 100 == 0)
            {
                var sb = new StringBuilder();
                sb.Append($"Continue Listing?");
                sb.Append($" {WithColor("(Y/N)", AnsiColor.Yellow)} ");
                sb.Append(AnsiEscapeCodes.Reset);
                Console.Write(sb.ToString());
                var consoleKey = Console.ReadKey();
                if (consoleKey.KeyChar != 'Y' && consoleKey.KeyChar != 'y')
                {
                    break;
                }
                Console.WriteLine();
            }
        }
    }

    public static async Task PrintRedisValue(RedisValue value, string padding = "", bool color = true)
    {
        if (value is RedisArray array)
        {
            if (!string.IsNullOrEmpty(padding)) Console.WriteLine();
            for (int i = 0; i < array.Values.Count; i++)
            {
                Console.Write($"{padding}{i + 1})");
                await PrintRedisValue(array.Values[i], "  ");
            }
        }
        else
        {
            var consoleFormat = new ConsoleFormat();
            string outputText = "";
            switch (value.Type)
            {
                case Library.Values.ValueType.String:
                    outputText = $"{padding}{value.Value}";
                    consoleFormat = new ConsoleFormat(Foreground: AnsiColor.BrightBlue);
                    break;
                case Library.Values.ValueType.Null:
                    outputText = $"{padding}null";
                    consoleFormat = new ConsoleFormat(Foreground: AnsiColor.BrightBlack);
                    break;
                case Library.Values.ValueType.BulkString:
                    if (value.Value is null)
                    {
                        outputText = $"{padding}(nil)";
                        consoleFormat = new ConsoleFormat(Foreground: AnsiColor.BrightBlack);
                    }
                    else
                    {
                        outputText = $"{padding}{value.Value}";
                        consoleFormat = new ConsoleFormat(Foreground: AnsiColor.BrightBlue);
                    }
                    break;
                case Library.Values.ValueType.Integer:
                    outputText = $"{padding}{value.Value}";
                    consoleFormat = new ConsoleFormat(Foreground: AnsiColor.Magenta);
                    break;
                case Library.Values.ValueType.Error:
                    outputText = $"{padding}{value.Value}";
                    consoleFormat = new ConsoleFormat(Foreground: AnsiColor.Red, Bold: true);
                    break;
            }
            if (color)
                Console.WriteLine(AnsiEscapeCodes.Reset + AnsiEscapeCodes.ToAnsiEscapeSequenceSlow(consoleFormat) + outputText + AnsiEscapeCodes.Reset);
            else
                Console.WriteLine(outputText);
        }
    }

    





    private static string Underline(string word) =>
        AnsiEscapeCodes.ToAnsiEscapeSequenceSlow(new ConsoleFormat(Underline: true)) + word + AnsiEscapeCodes.Reset;

    private static string Bold(string word) =>
        AnsiEscapeCodes.ToAnsiEscapeSequenceSlow(new ConsoleFormat(Bold: true)) + word + AnsiEscapeCodes.Reset;

    private static string WithColor(string word, AnsiColor color) =>
        AnsiEscapeCodes.ToAnsiEscapeSequenceSlow(new ConsoleFormat(Foreground: color)) + word + AnsiEscapeCodes.Reset;

    private static string WithFormat(string word, in ConsoleFormat format) =>
        AnsiEscapeCodes.ToAnsiEscapeSequenceSlow(format) + word + AnsiEscapeCodes.Reset;


    public static async Task Run(string? host, int? port, string? commands)
    {
        var documentation = new Documentation();
        documentation.Generate();

        string _ip = host ?? "127.0.0.1";
        int _port = port ?? 6379;
        Connection connection = null;

        // Enable history
        //ReadLine.HistoryEnabled = true;
        //Configure UI and ReadLine
        //ReadLine.AutoCompletionHandler = new UI.AutoCompletionHandler(documentation);


        var keyBindings = new KeyBindings(
                    //commitCompletion: new(new(ConsoleKey.Enter), new(ConsoleKey.Tab)),
                    //triggerCompletionList: new KeyPressPatterns(new(ConsoleModifiers.Control, ConsoleKey.Spacebar), new(ConsoleModifiers.Control, ConsoleKey.J),
                    triggerOverloadList: new(new KeyPressPattern(' ')));

        var promptConfiguration = new PromptConfiguration(
                prompt: "Not Connected>",
                keyBindings: keyBindings);



        await using var prompt = new Prompt(
            callbacks: new RedisPromptCallBack(documentation),
            configuration:  promptConfiguration);

        var commandParser = new CommandParser(documentation);

        //connect by grabbing last connection configuration
        if (connection == null)
        {
            try
            {
                connection = Connection.Connect(_ip, _port);
                PrintConnectedInfo(connection);
                UpdatePrompt(connection, promptConfiguration);
            }
            catch (Exception ex)
            {
                PrintError(ex);
            }
            
        }
        

        ///fire and forget implementation, only happens when commands are sent through args[]
        if (connection != null && !string.IsNullOrWhiteSpace(commands))
        {
            //commands
            var command = commandParser.Parse(commands);
            if (command != null)
            {
                connection.Send(command);
                RedisValue value = connection.Receive();
                await PrintRedisValue(value);
                connection?.Close();
                Environment.Exit(0);
            }
        }


        while (true)
        {
            //string input = ReadLine.Read($"{_ip}:{_port}> ");
            //"{_ip}:{_port}> "
            var response = await prompt.ReadLineAsync();
            // Send message.
            if (response.IsSuccess && !string.IsNullOrWhiteSpace(response.Text))
            {
                var command = commandParser.Parse(response.Text);
                if (command == null)
                {
                    Console.WriteLine($"Error Parsing {Underline(response.Text)}");
                    continue;
                }
                string input = command.Text;

                if (connection != null && connection.IsConnected)
                {
                    //do not send local commands to the server
                    if (command.Documentation == null || command.Documentation.Group != "application")
                    {
                        bool allowToExecute = true;
                        if (documentation.IsCommandDangerous(command.Name))
                        {
                            allowToExecute = AskforDangerousExecution(command);

                            
                        }
                        if (allowToExecute)
                        {
                            connection.Send(command);
                            RedisValue value = connection.Receive();
                            await PrintRedisValue(value);
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Disconnected, try connecting using {Underline("CONNECT")}");
                    UpdatePrompt(connection, promptConfiguration);
                }


                // evaluate built in commands
                if (command.Documentation != null && command.Documentation.Group == "application")
                {
                    var doc = command.Documentation;
                    if (doc.Command == "EXIT") {
                        connection?.Close();
                        Environment.Exit(0); 
                    }
                    if (doc.Command == "CLEAR") { Console.Clear(); continue; }
                    if (doc.Command == "CONNECT") { 
                        if (command.Args.Length > 1)
                        {
                            string newHost = command.Args[0];
                            string sPort = command.Args[1];
                            Console.WriteLine($"Connecting to {Underline(newHost)}:{Underline(sPort)}");
                            if (int.TryParse(sPort, out int intPort))
                            {
                                try
                                {
                                    connection = Connection.Connect(newHost, intPort);
                                    PrintConnectedInfo(connection);
                                } 
                                catch (Exception ex)
                                {
                                    connection = null;
                                    PrintError(ex);
                                }
                                UpdatePrompt(connection, promptConfiguration);
                            }
                            
                        }
                    }

                    if (new[] { "HELP", "?" }.Contains(doc.Command))
                    {
                        PrintHelp();
                        continue;
                    }

                    if (connection != null)
                    {
                        if (command.Name == "SAFEKEYS")
                        {
                            string pattern = command.Args.Length > 0 ? command.Args[0] : "";
                            var keys = connection.SafeKeys(pattern);
                            await PrintRedisValues(keys);
                        }

                        if (command.Name == "VIEW" && command.Args.Length > 0)
                        {
                            var value = connection.GetKeyValue(command);
                            await PrintRedisValue(value);
                        }
                    }
                }


            }
        }

    }

    private static bool AskforDangerousExecution(ParsedCommand command)
    {
        bool execute = false;
        var sb = new StringBuilder();
        sb.Append($"The command {Bold(command.Name)} is considered dangerous to execute, execute anyway?");
        if (command.Name == "KEYS") sb.Append($" You can execute {Underline("SCAN")} or {Underline("SEARCH")}");
        sb.Append($" {WithColor("(Y/N)", AnsiColor.Yellow)} ");
        sb.Append(AnsiEscapeCodes.Reset);
        int messageLength = sb.Length;
        Console.Write(sb.ToString());
        var consoleKey = Console.ReadKey();
        if (consoleKey.KeyChar == 'Y' || consoleKey.KeyChar == 'y')
        {
            execute = true;
        }
        Console.WriteLine();
        return execute;
    }

    private static void UpdatePrompt(Connection? connection, PromptConfiguration prompt)
    {
        if (connection == null || !connection.IsConnected)
        {
            string promptFormat = $"DISCONNECTD> ";
            int promptLength = promptFormat.Length;
            prompt.Prompt = new FormattedString(promptFormat, new FormatSpan(0, promptLength - 2, AnsiColor.Red), new FormatSpan(promptLength - 2, 1, AnsiColor.Yellow));
        } 
        else
        {
            string promptFormat = $"{connection.Host}:{connection.Port}> ";
            int promptLength = promptFormat.Length;
            prompt.Prompt = new FormattedString(promptFormat, new FormatSpan(0, promptLength - 2, AnsiColor.Red), new FormatSpan(promptLength - 2, 1, AnsiColor.Yellow));
        }
        
    }
}
