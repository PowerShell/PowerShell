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
        /// Handles and processes a string that starts and/or ends with single or double quotes.
        /// Removes the front quote if present, and optionally the back quote if it matches the front quote.
        /// Updates the input string by reference and returns the type of quote used.
        /// </summary>
        /// <param name="wordToComplete">
        /// The string to process, which may be surrounded by single or double quotes.
        /// This parameter is passed by reference and is updated to exclude processed quotes.
        /// </param>
        /// <returns>
        /// Returns a string representing the quote type used (single or double) if a front quote is detected.
        /// Returns an empty string if no front quote is found or if the input is empty.
        /// </returns>
        /// <remarks>
        /// The method detects single (' or ') and double (" or ") quotes, removes them as needed,
        /// and modifies the input string in-place. It ensures quotes are matched and only strips
        /// the back quote if it matches the front quote.
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

            bool hasFrontQuote = hasFrontSingleQuote || hasFrontDoubleQuote;

            if (!hasFrontQuote)
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

            bool hasFrontAndBackSingleQuote = hasFrontSingleQuote && hasBackSingleQuote;
            bool hasFrontAndBackDoubleQuote = hasFrontDoubleQuote && hasBackDoubleQuote;

            bool hasMatchingFrontAndBackQuote = hasFrontAndBackSingleQuote || hasFrontAndBackDoubleQuote;

            wordToComplete = hasMatchingFrontAndBackQuote
                ? wordToComplete.Substring(1, length - 2)
                : wordToComplete.Substring(1);

            return quoteInUse;
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
