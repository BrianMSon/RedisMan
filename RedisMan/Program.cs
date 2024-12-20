using System;
using RedisMan.Library.Commands;

namespace RedisMan;

/// <summary>
/// TODO:
///     - [ ] Implement custom argument parser
///     - [X] args[] parser, for connection and command to execute
///     - [-] Repl.cs
///     - [-] CommandBuilder
///     - [ ] Connection.cs
///     - [X] RESPParser.cs
///     - [ ] Gui.cs Mode
///     - [X] Fix "Trim unused code" for Reflection
/// </summary>
internal class Program
{
    static int Main(string[] args)
    {
        string host = "127.0.0.1";
        int port = 6379;
        string? command = null;
        string? username = null;
        string? password = null;
        bool isGuiMode = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--host":
                case "-h":
                    if (i + 1 < args.Length) host = args[++i];
                    else Console.WriteLine("Missing value for --host");
                    break;
                case "--port":
                case "-p":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int parsedPort))
                        port = parsedPort;
                    else Console.WriteLine("Invalid or missing value for --port");
                    break;
                case "--command":
                case "-c":
                    if (i + 1 < args.Length) command = args[++i];
                    else Console.WriteLine("Missing value for --command");
                    break;
                case "--username":
                case "-u":
                    if (i + 1 < args.Length) username = args[++i];
                    else Console.WriteLine("Missing value for --username");
                    break;
                case "--password":
                    if (i + 1 < args.Length) password = args[++i];
                    else Console.WriteLine("Missing value for --password");
                    break;
                case "gui":
                    isGuiMode = true;
                    break;
                default:
                    Console.WriteLine($"Unknown argument: {args[i]}");
                    break;
            }
        }

        if (isGuiMode)
        {
            Gui.Run(host, port);
            return 0;
        }
        else
        {
            _ = Repl.Run(host, port, command, username, password);
            return 0;
        }
    }
}
