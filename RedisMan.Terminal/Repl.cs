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

namespace RedisMan.Terminal;
/// <summary>
/// TODO:
///     - [X] Try https://github.com/waf/PrettyPrompt
///     - [X] Prompt Mode
///     - [X] Implement autocomplete with documentation
///     - [ ] Implement tooltip
///     - [ ] Prompt support not connected state (local commands)
///     - [ ] Fire and Forget Mode
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


    public static async Task Run(string? host, int? port, string? commands)
    {
        var documentation = new Documentation();
        documentation.Generate();

        string _ip = host ?? "127.0.0.1";
        int _port = port ?? 6379;
        using var connection = Connection.Connect(_ip, _port);

        // Enable history
        //ReadLine.HistoryEnabled = true;
        //Configure UI and ReadLine
        //ReadLine.AutoCompletionHandler = new UI.AutoCompletionHandler(documentation);

        var defaultColor = Console.ForegroundColor;
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

        


        while (true)
        {
            //string input = ReadLine.Read($"{_ip}:{_port}> ");
            //"{_ip}:{_port}> "
            var response = await prompt.ReadLineAsync();

            // Send message.
            if (response.IsSuccess)
            {
                string input = response.Text;
                if (input == "q") break;
                connection.Send(input);


                RedisValue value = connection.Receive();

                if (value is RedisArray array)
                {
                    for (int i = 0; i < array.Values.Count; i++)
                    {
                        //Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"{i + 1}) {array.Values[i].Value}");
                        //Console.ForegroundColor = ConsoleColor.Blue;
                        //Console.WriteLine(array.Values[i].Value);
                    }
                }
                else
                {
                    Console.WriteLine(value.Value);
                }
            }
        }

        connection.Close();
    }
}
