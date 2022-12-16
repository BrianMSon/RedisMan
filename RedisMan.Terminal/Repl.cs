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
///     - [ ] Display databases and number of keys
///     - [ ] prevent sending dangerous commands
///     - [ ] local command to autolist all keys (safely)
///     - [ ] local command to save command output to file
///     - [ ] pipe commands to shell
///     - [ ] View command to automatically view data regardless of type
///     - [X] Test Remote git repository
/// </summary>



public static class Repl
{
    public class RedisPromptCallBack : PromptCallbacks
    {
        Documentation _documentation;
        public RedisPromptCallBack(Documentation documentation)
        {
            _documentation = documentation;
        }

        protected override IEnumerable<(KeyPressPattern Pattern, KeyPressCallbackAsync Callback)> GetKeyPressCallbacks()
        {
            // registers functions to be called when the user presses a key. The text
            // currently typed into the prompt, along with the caret position within
            // that text are provided as callback parameters.
            yield return (new(ConsoleModifiers.Control, ConsoleKey.F1), PressedF1);
        }


        protected override async Task<(IReadOnlyList<OverloadItem>, int ArgumentIndex)> GetOverloadsAsync(string text, int caret, CancellationToken cancellationToken)
        {
            if (caret > 0)
            {
                var commandParts = text.Split(' ');
                if (commandParts.Length > 1)
                {
                    var command = _documentation.Get(commandParts[0]);
                    if (command != null)
                    {
                        var items = new List<OverloadItem>(1);
                        var overloadItem = GetOverloadCommandDocumentation(command, text, caret);
                        items.Add(overloadItem);
                        return await Task.FromResult((items, 0));
                    }
                }
            }

            return EmptyOverload();
        }


        protected override Task<IReadOnlyList<CompletionItem>> GetCompletionItemsAsync(string text, int caret, TextSpan spanToBeReplaced, CancellationToken cancellationToken)
        {
            // demo completion algorithm callback
            // populate completions and documentation for autocompletion window
            var typedWord = text.AsSpan(spanToBeReplaced.Start, spanToBeReplaced.Length).ToString();
            if (spanToBeReplaced.Start == 0)
            {
                return Task.FromResult<IReadOnlyList<CompletionItem>>(
                    _documentation.Docs
                    .Select(command =>
                    {
                        var displayText = command.Command;//new FormattedString(fruit.Name, new FormatSpan(0, fruit.Name.Length, fruit.Highlight));
                        var formattedDescription = GetFormattedCommandString(command);
                        return new CompletionItem(
                            replacementText: command.Command,
                            displayText: displayText,
                            commitCharacterRules: new[] { new CharacterSetModificationRule(CharacterSetModificationKind.Add, new[] { ' ' }.ToImmutableArray()) }.ToImmutableArray(),
                            getExtendedDescription: _ => Task.FromResult(formattedDescription)
                        );
                    })
                    .ToArray()
                );
            }
            else
            {
                return Task.FromResult<IReadOnlyList<CompletionItem>>(new CompletionItem[] { });
            }
        }



    }

    static (IReadOnlyList<OverloadItem> Overloads, int ArgumentIndex) EmptyOverload() => (Array.Empty<OverloadItem>(), 0);

    private static FormattedString GetFormattedCommandString(CommandDoc commandDoc)
    {
        var formatBuilder = new FormattedStringBuilder();
        formatBuilder.Append(commandDoc.Command, new FormatSpan(0, commandDoc.Command.Length, AnsiColor.BrightYellow));
        formatBuilder.Append(' ');
        formatBuilder.Append(commandDoc.Arguments, new FormatSpan(0, commandDoc.Arguments.Length, AnsiColor.Yellow));
        formatBuilder.Append('\n');
        formatBuilder.Append(commandDoc.Summary, new FormatSpan(0, commandDoc.Summary.Length, AnsiColor.Blue));
        return formatBuilder.ToFormattedString();
    }

    private static OverloadItem GetOverloadCommandDocumentation(CommandDoc commandDoc, string text, int caret)
    {

        var fmArguments = new FormattedStringBuilder();
        fmArguments.Append(commandDoc.Command, new FormatSpan(0, commandDoc.Command.Length, AnsiColor.BrightYellow));
        fmArguments.Append(' ');
        fmArguments.Append(commandDoc.Arguments, new FormatSpan(0, commandDoc.Arguments.Length, AnsiColor.Yellow));

        var fmSummary = new FormattedString(commandDoc.Summary, new FormatSpan(0, commandDoc.Summary.Length, AnsiColor.BrightBlue));
        var fmSince = new FormattedString(commandDoc.Since, new FormatSpan(0, commandDoc.Since.Length, AnsiColor.Blue));

        return new OverloadItem(fmArguments.ToFormattedString(), fmSummary, fmSince, Array.Empty<OverloadItem.Parameter>());
    }


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

    public static async Task PrintRedisValue(RedisValue value, string padding = "", bool color = true)
    {
        if (value is RedisArray array)
        {
            if (!string.IsNullOrEmpty(padding)) Console.WriteLine();
            for (int i = 0; i < array.Values.Count; i++)
            {
                //Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{padding}{i + 1})");
                await PrintRedisValue(array.Values[i], "  ");
                //Console.ForegroundColor = ConsoleColor.Blue;
                //Console.WriteLine(array.Values[i].Value);
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

    public static async Task PrintMessage(string message, FormatSpan[] formats)
    {

    }

    public static void PrintConnectedInfo(Connection connection)
    {
        var serverInfo = connection.ServerInfo;
        /*foreach (var section in serverInfo.Keys)
        {
            Console.WriteLine($"{WithFormat(section, new ConsoleFormat(Bold: true, Foreground: AnsiColor.BrightBlue))}");
            foreach (var entry in serverInfo[section])
            {
                Console.WriteLine($"{Bold(entry.Key)}={entry.Value}");
            }
        }*/
    }

    private static void PrintError(Exception ex)
    {
        
        var format = new ConsoleFormat(Bold: true, Foreground: AnsiColor.BrightRed);
        Console.Error.Write(AnsiEscapeCodes.Reset);
        Console.Error.Write(AnsiEscapeCodes.ToAnsiEscapeSequenceSlow(format) + "Error: " + AnsiEscapeCodes.Reset);
        Console.Error.WriteLine(AnsiEscapeCodes.ToAnsiEscapeSequenceSlow(new ConsoleFormat(Foreground: AnsiColor.Red)) + ex.Message + AnsiEscapeCodes.Reset);
    }

    private static void PrintHelp()
    {
        Console.WriteLine(
$@"
This is a Test Help Message


Header
===============
Text with {Underline("Format")} at the end.

Another Header
=================
Another Text
"
        );


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

        string promptFormat = $"{_ip}:{_port}> ";
        int promptLength = promptFormat.Length;

 
        var keyBindings = new KeyBindings(
                    //commitCompletion: new(new(ConsoleKey.Enter), new(ConsoleKey.Tab)),
                    //triggerCompletionList: new KeyPressPatterns(new(ConsoleModifiers.Control, ConsoleKey.Spacebar), new(ConsoleModifiers.Control, ConsoleKey.J),
                    triggerOverloadList: new(new KeyPressPattern(' ')));

        var promptConfiguration = new PromptConfiguration(
                prompt: new FormattedString(promptFormat, new FormatSpan(0, promptLength - 2, AnsiColor.Red), new FormatSpan(promptLength - 2, 1, AnsiColor.Yellow)),
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
                        connection.Send(command);


                        RedisValue value = connection.Receive();
                        await PrintRedisValue(value);
                    }
                } 
                else
                {
                    Console.WriteLine($"Disconnected, try connecting using {Underline("CONNECT")}");
                }


                // evaluate built in commands
                if (command.Documentation != null && command.Documentation.Group == "application")
                {
                    var doc = command.Documentation;
                    if (doc.Command == "EXIT") { Environment.Exit(0); }
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
                                    PrintError(ex);
                                }
                            }
                            
                        }
                    }
                    if (new[] { "HELP", "?" }.Contains(doc.Command))
                    {
                        PrintHelp();
                        continue;
                    }
                }


            }
        }

        if (connection != null) connection.Close();
    }
}
