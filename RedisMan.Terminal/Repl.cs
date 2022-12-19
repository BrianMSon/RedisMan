using PrettyPrompt;
using RedisMan.Library.Commands;
using RedisMan.Library.Values;
using RedisMan.Library;
using PrettyPrompt.Highlighting;
using PrettyPrompt.Consoles;
using PrettyPrompt.Completion;
using System.Text;

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
///     - [X] implement safe VIEW command
///     - [X] CHECK DB2 key opsviewer:tools:phlytest:PHLY18-00126 , why so many nulls
///     - [ ] local command to save command output to file
///     - [ ] pipe commands to shell
///     - [X] Test Remote git repository
///     - [X] Implement AUTH
///     - [ ] Implement 
/// </summary>



public static partial class Repl
{
    private static (IReadOnlyList<OverloadItem> Overloads, int ArgumentIndex) EmptyOverload() => (Array.Empty<OverloadItem>(), 0);




    private static Task<KeyPressCallbackResult?> PressedF1(string text, int caret, CancellationToken cancellationToken)
    {
        var wordUnderCursor = GetWordAtCaret(text, caret).ToLower();

        // since we return a null KeyPressCallbackResult here, the user will remain on the current prompt
        // and will still be able to edit the input.
        // if we were to return a non-null result, this result will be returned from ReadLineAsync(). This
        // is useful if we want our custom keypress to submit the prompt and control the output manually.
        return Task.FromResult<KeyPressCallbackResult?>(null);

        // local functions
        static string GetWordAtCaret(string text, int caret)
        {
            var words = text.Split(' ', '\n');
            var wordAtCaret = string.Empty;
            var currentIndex = 0;
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

    private static async Task PrintRedisValues(IEnumerable<RedisValue> values, int warningAt = 100)
    {
        var i = 0;
        foreach (var value in values)
        {
            i++;
            Console.Write($"{i})");
            await PrintRedisValue(value, "  ");
            if (i % warningAt != 0) continue;
            var sb = new StringBuilder();
            sb.Append("Continue Listing?");
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

    private static async Task PrintRedisValue(RedisValue value, string padding = "", bool color = true)
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


    public static async Task Run(string? host, int? port, string? commands, string? username, string? password)
    {
        var documentation = new Documentation();
        documentation.Generate();

        var _ip = host ?? "127.0.0.1";
        var _port = port ?? 6379;
        Connection? connection = null;

        // Enable history
        //ReadLine.HistoryEnabled = true;
        //Configure UI and ReadLine
        //ReadLine.AutoCompletionHandler = new UI.AutoCompletionHandler(documentation);


        var keyBindings = new KeyBindings(
                    //commitCompletion: new(new(ConsoleKey.Enter), new(ConsoleKey.Tab)),
                    //triggerCompletionList: new KeyPressPatterns(new(ConsoleModifiers.Control, ConsoleKey.Spacebar), new(ConsoleModifiers.Control, ConsoleKey.J),
                    triggerOverloadList: new KeyPressPatterns(new KeyPressPattern(character: ' ')));

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
                connection = Connection.Connect(_ip, _port, password ?? string.Empty, username ?? string.Empty);
                PrintConnectedInfo(connection);
                UpdatePrompt(connection, promptConfiguration);
            }
            catch (Exception ex)
            {
                PrintError(ex);
            }
            
        }
        

        //fire and forget implementation, only happens when commands are sent through args[]
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

                if (connection is { IsConnected: true })
                {
                    //do not send local commands to the server
                    if (command.Documentation is not { Group: "application" })
                    {
                        var allowToExecute = true;
                        if (documentation.IsCommandDangerous(command.Name))
                        {
                            allowToExecute = AskForDangerousExecution(command);

                            
                        }
                        if (allowToExecute)
                        {
                            connection.Send(command);
                            var value = connection.Receive();
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
                if (command.Documentation is { Group: "application" })
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
                            var newHost = command.Args[0];
                            var sPort = command.Args[1];
                            Console.WriteLine($"Connecting to {Underline(newHost)}:{Underline(sPort)}");
                            if (int.TryParse(sPort, out var intPort))
                            {
                                try
                                {
                                    string conPassword = string.Empty;
                                    string conUsername = string.Empty;
                                    //Legaccy Authentication
                                    if (command.Args.Length == 3) conPassword = command.Args[2];
                                    //New ACL Authentication
                                    if (command.Args.Length == 4)
                                    {
                                        conUsername = command.Args[2];
                                        conPassword = command.Args[3];
                                    }
                                    connection = Connection.Connect(newHost, intPort, conPassword, conUsername);
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

                    if (new string[] { "HELP", "?" }.Contains(doc.Command))
                    {
                        PrintHelp();
                        continue;
                    }

                    if (connection != null)
                    {
                        if (command.Name == "SAFEKEYS")
                        {
                            var pattern = command.Args.Length > 0 ? command.Args[0] : "";
                            var keys = connection.SafeKeys(pattern);
                            await PrintRedisValues(keys, 100);
                        }

                        if (command is { Name: "VIEW", Args.Length: > 0 })
                        {
                            var (type, value, enumerable) = connection.GetKeyValue(command);
                            if (value != null)
                            {
                                await PrintRedisValue(value);
                            }
                            if (enumerable != null)
                            {
                                int warningAt = 50;
                                if (type == "stream") warningAt = 10;
                                if (type == "hash") warningAt = 10;
                                await PrintRedisValues(enumerable, warningAt);
                            }
                        }

                        if (command is { Name: "EXPORT", Args.Length: > 0 })
                        {
                            string cmdToExport = String.Join(' ', command.Args);
                            var subCommand = commandParser.Parse((cmdToExport));
                            if (subCommand is not null)
                                Export(connection, subCommand);
                            
                        }
                    }
                }


            }
        }

    }

    private static async Task Export(Connection connection, ParsedCommand command)
    {
        string outputFilename = "output.txt";
        if (command is { Name: "VIEW", Args.Length: > 0 })
        {
            var (type, value, enumerable) = connection.GetKeyValue(command);
            if (value != null)
            {
                //await PrintRedisValue(value);
                //output single value to text file
            }
            if (enumerable != null)
            {
                //await PrintRedisValues(enumerable, warningAt);
                //output collection to text file
            }
        } 
        else
        {
            connection.Send(command);
            var value = connection.Receive();
            //save single value to file
            //await PrintRedisValue(value);            
        }
    }

    private static bool AskForDangerousExecution(ParsedCommand command)
    {
        var execute = false;
        var sb = new StringBuilder();
        sb.Append($"The command {Bold(command.Name)} is considered dangerous to execute, execute anyway?");
        if (command.Name == "KEYS") sb.Append($" You can execute {Underline("SCAN")} or {Underline("SEARCH")}");
        sb.Append($" {WithColor("(Y/N)", AnsiColor.Yellow)} ");
        sb.Append(AnsiEscapeCodes.Reset);
        Console.Write(sb.ToString());
        var consoleKey = Console.ReadKey();
        if (consoleKey.KeyChar is 'Y' or 'y')
        {
            execute = true;
        }
        Console.WriteLine();
        return execute;
    }

    private static void UpdatePrompt(Connection? connection, PromptConfiguration prompt)
    {
        if (connection is not { IsConnected: true })
        {
            var promptFormat = "DISCONNECTED> ";
            var promptLength = promptFormat.Length;
            prompt.Prompt = new FormattedString(promptFormat, new FormatSpan(0, promptLength - 2, AnsiColor.Red),
                new FormatSpan(promptLength - 2, 1, AnsiColor.Yellow));
        } 
        else
        {
            var promptFormat = $"{connection.Host}:{connection.Port}> ";
            var promptLength = promptFormat.Length;
            prompt.Prompt = new FormattedString(promptFormat, new FormatSpan(0, promptLength - 2, AnsiColor.Red),
                new FormatSpan(promptLength - 2, 1, AnsiColor.Yellow));
        }
    }

}
