using PrettyPrompt.Completion;
using PrettyPrompt.Consoles;
using PrettyPrompt.Documents;
using PrettyPrompt;
using RedisMan.Library.Commands;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisMan.Terminal;
public static partial class Repl
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
}
