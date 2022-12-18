using System.CommandLine;

namespace RedisMan.Terminal;

/// <summary>
/// TODO:
///     - [X] Implement System.CommandLine
///     - [X] args[] parser, for connection and command to execute
///     - [-] Repl.cs
///     - [-] CommandBuilder
///     - [ ] Connection.cs
///     - [X] RESPParser.cs
///     - [ ] Gui.cs Mode
///     - [X] Fix "Trim unused code"  for Reflection
/// </summary>
internal class Program
{
    static int Main(string[] args)
    {
       
        var hostOption = new Option<string?>(name: "--host", description: "host/ip address to conect to.", getDefaultValue: () => "127.0.0.1");
        hostOption.AddAlias("-h");
        var portOption = new Option<int?>(name: "--port", description: "port to connect to.", getDefaultValue: () => 6379);
        portOption.AddAlias("-p");
        var commandOption = new Option<string?>("--command", description: "Command to Execute");
        commandOption.AddAlias("-c");

        var guiCommand = new Command("gui", description: "Terminal GUI")
        {
            hostOption, portOption
        };


        var rootCmd = new RootCommand()
        {
            hostOption, portOption, commandOption
        };
        rootCmd.AddCommand(guiCommand);

        rootCmd.SetHandler(Repl.Run, hostOption, portOption, commandOption);
        guiCommand.SetHandler(Gui.Run, hostOption, portOption);

        return rootCmd.Invoke(args);
    }
}

