using PrettyPrompt.Completion;
using PrettyPrompt.Consoles;
using PrettyPrompt.Documents;
using PrettyPrompt;
using RedisMan.Library.Commands;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace RedisMan.Terminal;
public static partial class Repl
{
    [SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
    [SuppressMessage("ReSharper", "HeapView.DelegateAllocation")]
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
            yield return (new KeyPressPattern(ConsoleModifiers.Control, ConsoleKey.F1), PressedF1);
        }


        protected override async Task<(IReadOnlyList<OverloadItem>, int ArgumentIndex)> GetOverloadsAsync(string text, int caret, CancellationToken cancellationToken)
        {
            if (caret <= 0) return EmptyOverload();
            var commandParts = text.Split(' ');
            if (commandParts.Length <= 1) return EmptyOverload();
            var command = _documentation.Get(commandParts[0]);
            if (command == null) return EmptyOverload();
            var items = new List<OverloadItem>(1);
            var overloadItem = GetOverloadCommandDocumentation(command, text, caret);
            items.Add(overloadItem);
            return await Task.FromResult((items, 0));
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

            return Task.FromResult<IReadOnlyList<CompletionItem>>(Array.Empty<CompletionItem>());
        }



    }
}
