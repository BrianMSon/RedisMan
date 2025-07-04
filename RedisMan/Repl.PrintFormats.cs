﻿using PrettyPrompt.Completion;
using PrettyPrompt.Consoles;
using PrettyPrompt.Highlighting;
using RedisMan.Library;
using RedisMan.Library.Commands;
using static RedisMan.PrintHelpers;

namespace RedisMan;
public static partial class Repl
{
    private static void PrintError(Exception ex)
    {
        var format = new ConsoleFormat(Bold: true, Foreground: AnsiColor.BrightRed);
        Console.Error.Write(AnsiEscapeCodes.Reset);
        Console.Error.Write(AnsiEscapeCodes.ToAnsiEscapeSequenceSlow(format) + "Error: " + AnsiEscapeCodes.Reset);
        Console.Error.WriteLine(AnsiEscapeCodes.ToAnsiEscapeSequenceSlow(new ConsoleFormat(Foreground: AnsiColor.Red)) + ex.StackTrace + AnsiEscapeCodes.Reset);
    }

    private static void PrintConnectedInfo(Connection? connection)
    {
        var serverInfo = connection.ServerInfo;
        if (serverInfo != null /*&& (serverInfo.KeySpace?.Any() ?? false)*/)
        {
            Console.WriteLine($"Connected to Redis {Bold(serverInfo.RedisVersion.ToString())} {Bold(serverInfo.RedisMode)}");
            Console.WriteLine($"OS: {serverInfo.Os}");
            Console.WriteLine($"Memory: {Underline(serverInfo.UsedMemoryHuman)} / {Underline(serverInfo.TotalSystemMemoryHuman)}");
            Console.WriteLine($"Available Databases:");
            foreach (var ks in serverInfo.KeySpace)
            {
                Console.WriteLine($" {WithColor(ks.DBName, AnsiColor.White)} ({WithColor(ks.Keys.ToString(), AnsiColor.White)} Total Keys)");
            }
        }
    }

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

    private static void PrintHelp()
    {
        Console.WriteLine($"This is a Test\r\nHelp Message");
        Console.WriteLine($"");
        Console.WriteLine($"");
        Console.WriteLine($"Header");
        Console.WriteLine($"===============");
        Console.WriteLine($"");
        Console.WriteLine($"Text with {Underline("Format")} at the end.");
        Console.WriteLine($"");
        Console.WriteLine($"Another Header");
        Console.WriteLine($"=================");
        Console.WriteLine($"Another Text");
    }
}
