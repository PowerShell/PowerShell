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

        /// <summary>
        /// Get matching completions from word to complete.
        /// This makes it easier to handle different variations of completions with consideration of quotes.
        /// </summary>
        /// <param name="wordToComplete">The word to complete.</param>
        /// <param name="possibleCompletionValues">The possible completion values to iterate.</param>
        /// <param name="toolTipMapping">The optional tool tip mapping delegate.</param>
        /// <param name="listItemTextMapping">The optional list item text mapping delegate.</param>
        /// <param name="resultType">The optional completion result type. Default is Text.</param>
        /// <returns>List of matching completion results.</returns>
        internal static IEnumerable<CompletionResult> GetMatchingResults(
            string wordToComplete,
            IEnumerable<string> possibleCompletionValues,
            Func<string, string> toolTipMapping = null,
            Func<string, string> listItemTextMapping = null,
            CompletionResultType resultType = CompletionResultType.Text)
        {
            string quote = HandleDoubleAndSingleQuote(ref wordToComplete);

            foreach (string value in possibleCompletionValues)
            {
                if (IsMatch(value, wordToComplete))
                {
                    string completionText = QuoteCompletionText(value, quote);
                    string toolTip = toolTipMapping?.Invoke(value) ?? value;
                    string listItemText = listItemTextMapping?.Invoke(value) ?? value;

                    yield return new CompletionResult(completionText, listItemText, resultType, toolTip);
                }
            }
        }

        /// <summary>
        /// Determines whether the given value matches the specified word or pattern.
        /// </summary>
        /// <param name="value">The input string to check for a match.</param>
        /// <param name="wordToComplete">The word or partial word to compare against the input string.</param>
        /// <returns>
        /// Returns <c>true</c> if the value matches the wordToComplete or the generated wildcard pattern; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The method performs the following checks:
        /// 1. If the value contains escaped newline characters, the wordToComplete is normalized 
        ///    and unescaped, and a case-insensitive prefix match is performed.
        /// 2. If either the value or wordToComplete contains wildcard characters, a case-insensitive 
        ///    prefix match is performed. This is to protect against issues in WildcardPatternParser.Parse()
        ///    where strings like '[*' throw WildcardPatternException from wildcards not being escaped.
        /// 3. If neither of the above conditions apply, a wildcard pattern is generated from the 
        ///    wordToComplete, appending a wildcard character (*). The pattern is then used to match the value.
        /// </remarks>
        internal static bool IsMatch(string value, string wordToComplete)
        {
            if (ContainsEscapedNewlineString(value))
            {
                string normalizedWord = WildcardPattern.Unescape(wordToComplete.ReplaceLineEndings("`"));
                return value.StartsWith(normalizedWord, StringComparison.OrdinalIgnoreCase);
            }
            else if (WildcardPattern.ContainsWildcardCharacters(value) ||
                     WildcardPattern.ContainsWildcardCharacters(wordToComplete))
            {
                return value.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase);
            }

            return WildcardPattern
                .Get(wordToComplete + "*", WildcardOptions.IgnoreCase)
                .IsMatch(value);
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

            bool hasBothFrontAndBackQuotes =
                (hasFrontSingleQuote && hasBackSingleQuote) || (hasFrontDoubleQuote && hasBackDoubleQuote);

            if (hasBothFrontAndBackQuotes)
            {
                wordToComplete = wordToComplete.Substring(1, length - 2);
                return quoteInUse;
            }

            bool hasFrontQuoteAndNoBackQuote = 
                (hasFrontSingleQuote || hasFrontDoubleQuote) && !hasBackSingleQuote && !hasBackDoubleQuote;

            if (hasFrontQuoteAndNoBackQuote)
            {
                wordToComplete = wordToComplete.Substring(1);
                return quoteInUse;
            }

            return string.Empty;
        }

        /// <summary>
        /// Determines whether the specified completion string requires quotes.
        /// Quoting is required if:
        /// <list type="bullet">
        ///   <item><description>There are parsing errors in the input string.</description></item>
        ///   <item><description>The parsed token count is not exactly two (the input token + EOF).</description></item>
        ///   <item><description>The first token is a string or a PowerShell keyword containing special characters.</description></item>
        ///   <item><description>The first token is a semi colon or comma token.</description></item>
        /// </list>
        /// </summary>
        /// <param name="completion">The input string to analyze for quoting requirements.</param>
        /// <returns><c>true</c> if the string requires quotes, <c>false</c> otherwise.</returns>
        internal static bool CompletionRequiresQuotes(string completion)
        {
            Parser.ParseInput(completion, out Token[] tokens, out ParseError[] errors);

            bool isExpectedTokenCount = tokens.Length == 2;

            bool requireQuote = errors.Length > 0 || !isExpectedTokenCount;

            Token firstToken = tokens[0];
            bool isStringToken = firstToken is StringToken;
            bool isKeywordToken = (firstToken.TokenFlags & TokenFlags.Keyword) != 0;
            bool isSemiToken = firstToken.Kind == TokenKind.Semi;
            bool isCommaToken = firstToken.Kind == TokenKind.Comma;

            if ((!requireQuote && isStringToken) || (isExpectedTokenCount && isKeywordToken))
            {
                requireQuote = ContainsCharsToCheck(firstToken.Text);
            }

            else if (isExpectedTokenCount && (isSemiToken || isCommaToken))
            {
                requireQuote = true;
            }

            return requireQuote;
        }

        private static bool ContainsEscapedNewlineString(string text)
            => text.Contains("`n", StringComparison.Ordinal);

        private static bool ContainsCharsToCheck(ReadOnlySpan<char> text)
            => text.ContainsAny(s_defaultCharsToCheck);

        /// <summary>
        /// Quotes a given completion text.
        /// </summary>
        /// <param name="completionText">
        /// The text to be quoted.
        /// </param>
        /// <param name="quote">
        /// The quote character to use for enclosing the text. Defaults to a single quote if not provided.
        /// </param>
        /// <returns>
        /// The quoted <paramref name="completionText"/>.
        /// </returns>
        internal static string QuoteCompletionText(string completionText, string quote)
        {
            // Escaped newlines e.g. `r`n need be surrounded with double quotes
            if (ContainsEscapedNewlineString(completionText))
            {
                return "\"" + completionText + "\"";
            }

            if (!CompletionRequiresQuotes(completionText))
            {
                return quote + completionText + quote;
            }

            string quoteInUse = string.IsNullOrEmpty(quote) ? "'" : quote;

            if (quoteInUse == "'")
            {
                completionText = CodeGeneration.EscapeSingleQuotedStringContent(completionText);
            }

            return quoteInUse + completionText + quoteInUse;
        }
    }
}
