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

        /// <summary>
        /// Removes wrapping quotes from a string and returns the quote used, if present.
        /// </summary>
        /// <param name="wordToComplete">
        /// The string to process, potentially surrounded by single or double quotes.
        /// This parameter is updated in-place to exclude the removed quotes.
        /// </param>
        /// <returns>
        /// The type of quote detected (single or double), or an empty string if no quote is found.
        /// </returns>
        /// <remarks>
        /// This method checks for single or double quotes at the start and end of the string.
        /// If wrapping quotes are detected and match, both are removed; otherwise, only the front quote is removed.
        /// The string is updated in-place, and only matching front-and-back quotes are stripped.
        /// If no quotes are detected or the input is empty, the original string remains unchanged.
        /// </remarks>
        internal static string HandleDoubleAndSingleQuote(ref string wordToComplete)
        {
            if (string.IsNullOrEmpty(wordToComplete))
            {
                return string.Empty;
            }

            char frontQuote = wordToComplete[0];
            bool hasFrontSingleQuote = frontQuote.IsSingleQuote();
            bool hasFrontDoubleQuote = frontQuote.IsDoubleQuote();

            if (!hasFrontSingleQuote && !hasFrontDoubleQuote)
            {
                return string.Empty;
            }

            string quoteInUse = hasFrontSingleQuote ? "'" : "\"";

            int length = wordToComplete.Length;
            if (length == 1)
            {
                wordToComplete = string.Empty;
                return quoteInUse;
            }

            char backQuote = wordToComplete[length - 1];
            bool hasBackSingleQuote = backQuote.IsSingleQuote();
            bool hasBackDoubleQuote = backQuote.IsDoubleQuote();

            bool hasMatchingFrontAndBackQuote =
                (hasFrontSingleQuote && hasBackSingleQuote) || (hasFrontDoubleQuote && hasBackDoubleQuote);

            if (hasMatchingFrontAndBackQuote)
            {
                wordToComplete = wordToComplete.Substring(1, length - 2);
                return quoteInUse;
            }

            bool hasValidFrontQuoteAndNoMatchingBackQuote = 
                (hasFrontSingleQuote || hasFrontDoubleQuote) && !hasBackSingleQuote && !hasBackDoubleQuote;

            if (hasValidFrontQuoteAndNoMatchingBackQuote)
            {
                wordToComplete = wordToComplete.Substring(1);
                return quoteInUse;
            }

            return string.Empty;
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
