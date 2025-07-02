using System.IO.Pipes;
using PrettyPrompt;
using RedisMan.Library.Commands;
using RedisMan.Library.Values;
using RedisMan.Library;
using PrettyPrompt.Highlighting;
using PrettyPrompt.Consoles;
using PrettyPrompt.Completion;
using System.Text;
using RedisMan.Library.Serialization;
using static RedisMan.PrintHelpers;

namespace RedisMan;

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
///     - [ ] Use HELLO on CONNECT, instead of INFO and AUTH
///     - [X] Fire and Forget Mode
///     - [X] Parse INFO on connect
///     - [X] Display databases and number of keys
///     - [X] Prevent sending dangerous commands
///     - [X] Local command to autolist all keys (safely)
///     - [X] VIEW: View command to automatically view data regardless of type\
///     - [X] VIEW: implement safe VIEW command
///     - [X] VIEW: Implement format for output
///     - [X] EXPORT TEST every possible type
///     - [X] Send commands arguments split by "" or spaces
///     - [ ] VIEW AND Export: Create specific functions to write and print hashes, streams, sets, zsets, etc
///         - [X] Hashes
///         - [X] Streams
///         - [X] Sets
///         - [ ] Sorted Sets
///     - [X] CHECK DB2 key opsviewer:tools:phlytest:PHLY18-00126 , why so many nulls
///     - [X] local command to save command output to file
///     - [X] Test Remote git repository
///     - [X] Implement AUTH
///     - [ ] Implement RESP3
///     - [X] Check Why it fails with FT.SEARCH 
///     - [X] Implement SUBSCRIBE
///     - [X] Implement XREAD, BLPOP, BRPOP, and blocking operations in general
///     - [X] Implement Deserialization Options
///     - [X] GZIP deserialization crashes, dont close app on crash, then figure out why it fails
///     - [ ] universal GET HSET command that allows using ISerializer
///     - [X] pipe commands to shell
///     - [ ] Implement TLS?
///     - [ ] Readme and documentation
///     - [ ] Bugfixes
/// </summary>
public static partial class Repl
{
    const int DEFAULT_TIMEOUT = 15;
    private static (IReadOnlyList<OverloadItem> Overloads, int ArgumentIndex) EmptyOverload() =>
        (Array.Empty<OverloadItem>(), 0);

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
            configuration: promptConfiguration);

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
                ISerializer serializer = ISerializer.GetSerializer(command.Modifier);
                connection.Send(command);
                RedisValue value = connection.Receive(DEFAULT_TIMEOUT);
                await ValueOutput.PrintRedisValue(value, serializer: serializer);
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
                ISerializer serializer = ISerializer.GetSerializer(command.Modifier);
                if (command == null)
                {
                    Console.WriteLine($"Error Parsing {Underline(response.Text)}");
                    continue;
                }

               
                if (connection is { IsConnected: true })
                {
                    //do not send local commands to the server
                    var cancelSource = new CancellationTokenSource();
                    var cancelToken = cancelSource.Token;
                    if (command.Name is "SUBSCRIBE")
                    {
                        connection.Send(command);
                        foreach (var value in connection.Subscribe(cancelToken))
                        {
                            if (Console.KeyAvailable)
                            {
                                if (Console.ReadKey().KeyChar == 'q')
                                {
                                    cancelSource.Cancel();
                                }
                            }
                                
                            if (!string.IsNullOrEmpty(command.Pipe)) ValueOutput.PipeRedisValue(command, value);
                            else await ValueOutput.PrintRedisValue(value, serializer: serializer);
                        }
                    }
                    else if (command.Name is "BLPOP" or "BRPOP" or "XREAD" or "BZPOPMIN" or "BZPOPMAX")
                    {
                        connection.Send(command);
                        var value = connection.Receive();
                        if (!string.IsNullOrEmpty(command.Pipe)) ValueOutput.PipeRedisValue(command, value);
                        else await ValueOutput.PrintRedisValue(value, serializer: serializer);
                        
                    }
                    else if (command.Documentation is not { Group: "application" })
                    {
                        var allowToExecute = true;
                        //if (documentation.IsCommandDangerous(command.Name))
                        //{
                        //    allowToExecute = AskForDangerousExecution(command);
                        //}

                        if (command.Name == "QUIT")
                        {
                            connection?.Close();
                            Environment.Exit(0);
                        }

                        if (allowToExecute)
                        {
                            connection.Send(command);
                            var value = connection.Receive(DEFAULT_TIMEOUT);
                            if (!string.IsNullOrEmpty(command.Pipe))
                            {
                                ValueOutput.PipeRedisValue(command, value);
                            }
                            else
                            {
                                await ValueOutput.PrintRedisValue(value, serializer: serializer);

                                //---------------------------------------------------------
                                // client info 결과에서 db값 추출
                                if (command.Text.ToLower() == "client info")
                                {
                                    // value에서 db= 형태의 문자열 추출 출력
                                    var dbMatch = value.Value.Split(' ').FirstOrDefault(s => s.StartsWith("db="));
                                    if (dbMatch != null)
                                    {
                                        var dbNumber = dbMatch.Split('=')[1];
                                        if (int.TryParse(dbNumber, out var dbIndex))
                                        {
                                            // 현재 연결된 데이터베이스 인덱스 출력
                                            Console.WriteLine($"Current DB Index: {dbIndex}");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"Failed to parse DB index from: {dbMatch}");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("No DB index found in client info response.");
                                    }
                                }
                                //---------------------------------------------------------
                            }
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
                    if (doc.Command == "EXIT")
                    {
                        connection?.Close();
                        Environment.Exit(0);
                    }

                    if (doc.Command == "CLEAR")
                    {
                        Console.Clear();
                        continue;
                    }

                    if (doc.Command == "CONNECT")
                    {
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
                            await ValueOutput.PrintRedisValues(keys, 100);
                        }

                        if (command is { Name: "VIEW", Args.Length: > 0 })
                        {
                            var (type, value, enumerable) = connection.GetKeyValue(command);
                            if (value != null)
                            {
                                await ValueOutput.PrintRedisValue(value, type: type, serializer: serializer);
                            }

                            if (enumerable != null)
                            {
                                if (type == "stream")
                                    await ValueOutput.PrintRedisStream(enumerable, 10);
                                else if (type == "hash") 
                                    await ValueOutput.PrintRedisHash(enumerable, 10);
                                else  await ValueOutput.PrintRedisValues(enumerable, 50, type: type);
                            }
                        }

                        if (command is { Name: "EXPORT", Args.Length: > 0 })
                        {
                            string filename = command.Args[0];
                            string cmdToExport = String.Join(' ', command.Args[1..]);
                            var subCommand = commandParser.Parse(cmdToExport);
                            if (subCommand is not null)
                                await ValueOutput.ExportAsync(connection, filename,subCommand);
                        }
                    }
                }
            }
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