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
        /// <summary>
        /// Defines the default set of characters to check for quoting.
        /// </summary>
        private static readonly SearchValues<char> s_defaultCharsToCheck = SearchValues.Create("$`");

        /// <summary>
        /// Defines the set of characters to check for quoting when escaping globbing path characters.
        /// </summary>
        private static readonly SearchValues<char> s_escapeGlobbingPathCharsToCheck = SearchValues.Create("$[]`");

        /// <summary>
        /// Get matching completions from word to complete.
        /// This makes it easier to handle different variations of completions with consideration of quotes.
        /// </summary>
        /// <param name="wordToComplete">The word to complete.</param>
        /// <param name="possibleCompletionValues">The possible completion values to iterate.</param>
        /// <param name="toolTipMapping">The optional tool tip mapping delegate.</param>
        /// <param name="listItemTextMapping">The optional list item text mapping delegate.</param>
        /// <param name="resultType">The optional completion result type. Default is Text.</param>
        /// <param name="escapeGlobbingPathChars">The optional toggle to escape globbing path chars.</param>
        /// <returns>Collection of matched completion results.</returns>
        internal static IEnumerable<CompletionResult> GetMatchingResults(
            string wordToComplete,
            IEnumerable<string> possibleCompletionValues,
            Func<string, string> toolTipMapping = null,
            Func<string, string> listItemTextMapping = null,
            CompletionResultType resultType = CompletionResultType.Text,
            bool escapeGlobbingPathChars = false)
        {
            string quote = HandleDoubleAndSingleQuote(ref wordToComplete);
            var pattern = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);

            foreach (string value in possibleCompletionValues)
            {
                if (!string.IsNullOrEmpty(value) && pattern.IsMatch(value))
                {
                    string completionText = QuoteCompletionText(completionText: value, quote, escapeGlobbingPathChars);
                    string listItemText = listItemTextMapping?.Invoke(value) ?? value;
                    string toolTip = toolTipMapping?.Invoke(value) ?? value;

                    yield return new CompletionResult(completionText, listItemText, resultType, toolTip);
                }
            }
        }

        /// <summary>
        /// Processes a given string to handle leading and trailing single or double quotes.
        /// </summary>
        /// <param name="wordToComplete">A reference to the string to process. 
        /// If the input starts and ends with matching quotes, the quotes are removed.
        /// If only the starting quote exists, it is retained.</param>
        /// <returns>
        /// A string representing the type of quote that was removed ("'" for single quote or "\"" for double quote),
        /// or an empty string if no quote was processed.
        /// </returns>
        /// <remarks>
        /// The method ensures that quotes are properly identified and handled based on their position and type.
        /// The input string is modified in-place.
        /// </remarks>
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

        /// <summary>
        /// Determines whether a given string completion requires quoting based on its content, parsing results, 
        /// and specific character checks.
        /// </summary>
        /// <param name="completion">The string to analyze for quoting requirements.</param>
        /// <param name="escapeGlobbingPathChars">
        /// A boolean flag indicating whether globbing path characters (e.g., *, ?, [, ]) should be escaped.
        /// </param>
        /// <returns>
        /// Returns <c>true</c> if the completion requires quoting; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method parses the input string to check for errors, token count, and content. 
        /// Quoting is determined based on the presence of errors, the number of tokens, 
        /// keywords, or specific special characters defined by the helper method.
        /// </remarks>
        internal static bool CompletionRequiresQuotes(string completion, bool escapeGlobbingPathChars)
        {
            // If the tokenizer sees the completion as more than two tokens, or if there is some error, then
            // some form of quoting is necessary (if it's a variable, we'd need ${}, filenames would need [], etc.)

            Parser.ParseInput(completion, out Token[] tokens, out ParseError[] errors);

            // Expect no errors and 2 tokens (1 is for our completion, the other is eof)
            // Or if the completion is a keyword, we ignore the errors
            bool requireQuote = !(errors.Length == 0 && tokens.Length == 2);
            if ((!requireQuote && tokens[0] is StringToken) ||
                (tokens.Length == 2 && (tokens[0].TokenFlags & TokenFlags.Keyword) != 0))
            {
                requireQuote = ContainsCharsToCheck(tokens[0].Text, escapeGlobbingPathChars);
            }

            return requireQuote;
        }

        /// <summary>
        /// Checks if the specified text contains any characters requiring quoting.
        /// </summary>
        /// <param name="text">The text to evaluate for special characters.</param>
        /// <param name="escapeGlobbingPathChars">
        /// A boolean flag indicating whether to use the globbing-specific set of characters to check.
        /// </param>
        /// <returns>
        /// Returns <c>true</c> if the text contains characters that require quoting; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method evaluates the text against a pre-defined set of characters for different contexts 
        /// (default or globbing path characters) to determine if quoting is necessary.
        /// </remarks>
        private static bool ContainsCharsToCheck(ReadOnlySpan<char> text, bool escapeGlobbingPathChars)
            => text.ContainsAny(escapeGlobbingPathChars ? s_escapeGlobbingPathCharsToCheck : s_defaultCharsToCheck);

        /// <summary>
        /// Quotes and optionally escapes the specified completion text based on the provided parameters.
        /// </summary>
        /// <param name="completionText">
        /// The text to be quoted and potentially escaped.
        /// </param>
        /// <param name="quote">
        /// The quote character to use for quoting the completion text. If this is null or empty,
        /// a single quote character (<c>'</c>) is used by default.
        /// </param>
        /// <param name="escapeGlobbingPathChars">
        /// A boolean value indicating whether globbing path characters should be escaped.
        /// If <c>true</c>, globbing-specific escape sequences will be applied to the text.
        /// </param>
        /// <returns>
        /// The quoted (and optionally escaped) version of the provided completion text.
        /// </returns>
        internal static string QuoteCompletionText(
            string completionText,
            string quote,
            bool escapeGlobbingPathChars)
        {
            if (CompletionRequiresQuotes(completionText, escapeGlobbingPathChars))
            {
                string quoteInUse = string.IsNullOrEmpty(quote) ? "'" : quote;

                completionText = quoteInUse == "'"
                    ? completionText.Replace("'", "''")
                    : completionText.Replace("`", "``").Replace("$", "`$");

                if (escapeGlobbingPathChars)
                {
                    completionText = quoteInUse == "'"
                        ? completionText.Replace("[", "`[").Replace("]", "`]")
                        : completionText.Replace("[", "``[").Replace("]", "``]");
                }

                return quoteInUse + completionText + quoteInUse;
            }

            return quote + completionText + quote;
        }
    }
}
