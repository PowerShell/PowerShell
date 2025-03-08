// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Buffers;
using System.Collections.Generic;
using System.Management.Automation.Language;

namespace System.Management.Automation
{
    /// <summary>
    /// Shared helper class for common completion helper methods.
    /// </summary>
    internal static class CompletionHelpers
    {
        private static readonly SearchValues<char> s_defaultCharsToCheck = SearchValues.Create("$`");

        private static readonly SearchValues<char> s_escapeCharsToCheck = SearchValues.Create("$[]`");

        /// <summary>
        /// Get matching completions from word to complete.
        /// This makes it easier to handle different variations of completions with consideration of quotes.
        /// </summary>
        /// <param name="wordToComplete">The word to complete.</param>
        /// <param name="possibleCompletionValues">The possible completion values to iterate.</param>
        /// <param name="toolTipMapping">The optional tool tip mapping delegate.</param>
        /// <param name="resultType">The optional completion result type. Default is Text.</param>
        /// <returns></returns>
        internal static IEnumerable<CompletionResult> GetMatchingResults(
            string wordToComplete,
            IEnumerable<string> possibleCompletionValues,
            Func<string, string> toolTipMapping = null,
            CompletionResultType resultType = CompletionResultType.Text)
        {
            string quote = HandleDoubleAndSingleQuote(ref wordToComplete);
            var pattern = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);

            foreach (string value in possibleCompletionValues)
            {
                if (pattern.IsMatch(value))
                {
                    string completionText = quote == string.Empty
                        ? value
                        : quote + value + quote;

                    string listItemText = value;

                    yield return new CompletionResult(
                        completionText,
                        listItemText,
                        resultType,
                        toolTip: toolTipMapping is null
                            ? listItemText
                            : toolTipMapping(value));
                }
            }
        }

        internal static string HandleDoubleAndSingleQuote(ref string wordToComplete)
        {
            string quote = string.Empty;

            if (!string.IsNullOrEmpty(wordToComplete) && (wordToComplete[0].IsSingleQuote() || wordToComplete[0].IsDoubleQuote()))
            {
                char frontQuote = wordToComplete[0];
                int length = wordToComplete.Length;

                if (length == 1)
                {
                    wordToComplete = string.Empty;
                    quote = frontQuote.IsSingleQuote() ? "'" : "\"";
                }
                else if (length > 1)
                {
                    if ((wordToComplete[length - 1].IsDoubleQuote() && frontQuote.IsDoubleQuote()) || (wordToComplete[length - 1].IsSingleQuote() && frontQuote.IsSingleQuote()))
                    {
                        wordToComplete = wordToComplete.Substring(1, length - 2);
                        quote = frontQuote.IsSingleQuote() ? "'" : "\"";
                    }
                    else if (!wordToComplete[length - 1].IsDoubleQuote() && !wordToComplete[length - 1].IsSingleQuote())
                    {
                        wordToComplete = wordToComplete.Substring(1);
                        quote = frontQuote.IsSingleQuote() ? "'" : "\"";
                    }
                }
            }

            return quote;
        }

        internal static bool CompletionRequiresQuotes(string completion, bool escape)
        {
            // If the tokenizer sees the completion as more than two tokens, or if there is some error, then
            // some form of quoting is necessary (if it's a variable, we'd need ${}, filenames would need [], etc.)

            Language.Token[] tokens;
            ParseError[] errors;
            Language.Parser.ParseInput(completion, out tokens, out errors);

            // Expect no errors and 2 tokens (1 is for our completion, the other is eof)
            // Or if the completion is a keyword, we ignore the errors
            bool requireQuote = !(errors.Length == 0 && tokens.Length == 2);
            if ((!requireQuote && tokens[0] is StringToken) ||
                (tokens.Length == 2 && (tokens[0].TokenFlags & TokenFlags.Keyword) != 0))
            {
                requireQuote = ContainsCharsToCheck(tokens[0].Text, escape);
            }

            return requireQuote;
        }

        private static bool ContainsCharsToCheck(ReadOnlySpan<char> text, bool escape)
            => text.ContainsAny(escape ? s_escapeCharsToCheck : s_defaultCharsToCheck);
    }
}
