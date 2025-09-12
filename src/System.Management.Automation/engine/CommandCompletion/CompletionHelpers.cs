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

        private const string SingleQuote = "'";
        private const string DoubleQuote = "\"";

        /// <summary>
        /// Get matching completions from word to complete.
        /// This makes it easier to handle different variations of completions with consideration of quotes.
        /// </summary>
        /// <param name="wordToComplete">The word to complete.</param>
        /// <param name="possibleCompletionValues">The possible completion values to iterate.</param>
        /// <param name="displayInfoMapper">The optional completion display info mapper delegate for tool tip and list item text.</param>
        /// <param name="resultType">The optional completion result type. Default is Text.</param>
        /// <param name="matchStrategy">The optional match strategy delegate.</param>
        /// <returns>List of matching completion results.</returns>
        internal static IEnumerable<CompletionResult> GetMatchingResults(
            string wordToComplete,
            IEnumerable<string> possibleCompletionValues,
            CompletionDisplayInfoMapper displayInfoMapper = null,
            CompletionResultType resultType = CompletionResultType.Text,
            MatchStrategy matchStrategy = null)
        {
            displayInfoMapper ??= DefaultDisplayInfoMapper;
            matchStrategy ??= DefaultMatch;

            string quote = HandleDoubleAndSingleQuote(ref wordToComplete);
            if (quote != SingleQuote)
            {
                wordToComplete = NormalizeToExpandableString(wordToComplete);
            }

            foreach (string value in possibleCompletionValues)
            {
                if (matchStrategy(value, wordToComplete))
                {
                    string completionText = QuoteCompletionText(value, quote);

                    (string toolTip, string listItemText) = displayInfoMapper(value);

                    yield return new CompletionResult(completionText, listItemText, resultType, toolTip);
                }
            }
        }

        /// <summary>
        /// Provides the display information for a completion result.
        /// This delegate is used to map a string value to its corresponding display information.
        /// </summary>
        /// <param name="value">The input value to be mapped</param>
        /// <returns>Completion display info containing tool tip and list item text.</returns>
        internal delegate (string ToolTip, string ListItemText) CompletionDisplayInfoMapper(string value);

        /// <summary>
        /// Provides the default display information for a completion result.
        /// Defaults to using the input value for both the tool tip and list item text.
        /// </summary>
        /// <returns>Completion display info containing tool tip and list item text.</returns>
        internal static readonly CompletionDisplayInfoMapper DefaultDisplayInfoMapper = value
            => (value, value);

        /// <summary>
        /// Normalizes the input string to an expandable string format for PowerShell.
        /// </summary>
        /// <param name="value">The input string to be normalized.</param>
        /// <returns>The normalized string with special characters replaced by their PowerShell escape sequences.</returns>
        /// <remarks>
        /// This method replaces special characters in the input string with their PowerShell equivalent escape sequences:
        /// <list type="bullet">
        ///     <item><description>Replaces "\r" (carriage return) with "`r".</description></item>
        ///     <item><description>Replaces "\n" (newline) with "`n".</description></item>
        ///     <item><description>Replaces "\t" (tab) with "`t".</description></item>
        ///     <item><description>Replaces "\0" (null) with "`0".</description></item>
        ///     <item><description>Replaces "\a" (bell) with "`a".</description></item>
        ///     <item><description>Replaces "\b" (backspace) with "`b".</description></item>
        ///     <item><description>Replaces "\u001b" (escape character) with "`e".</description></item>
        ///     <item><description>Replaces "\f" (form feed) with "`f".</description></item>
        ///     <item><description>Replaces "\v" (vertical tab) with "`v".</description></item>
        /// </list>
        /// </remarks>
        internal static string NormalizeToExpandableString(string value)
            => value
                .Replace("\r", "`r")
                .Replace("\n", "`n")
                .Replace("\t", "`t")
                .Replace("\0", "`0")
                .Replace("\a", "`a")
                .Replace("\b", "`b")
                .Replace("\u001b", "`e")
                .Replace("\f", "`f")
                .Replace("\v", "`v");

        /// <summary>
        /// Defines a strategy for determining if a value matches a word or pattern.
        /// </summary>
        /// <param name="value">The input string to check for a match.</param>
        /// <param name="wordToComplete">The word or pattern to match against.</param>
        /// <returns>
        /// <c>true</c> if the value matches the specified word or pattern; otherwise, <c>false</c>.
        /// </returns>
        internal delegate bool MatchStrategy(string value, string wordToComplete);

        /// <summary>
        /// Determines if the given value matches the specified word using a literal, case-insensitive prefix match.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the value starts with the word (case-insensitively); otherwise, <c>false</c>.
        /// </returns>
        internal static readonly MatchStrategy LiteralMatchOrdinalIgnoreCase = (value, wordToComplete)
            => value.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines if the given value matches the specified word using wildcard pattern matching.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the value matches the word as a wildcard pattern; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Wildcard pattern matching allows for flexible matching, where wilcards can represent
        /// multiple characters in the input. This strategy is case-insensitive.
        /// </remarks>
        internal static readonly MatchStrategy WildcardPatternMatchIgnoreCase = (value, wordToComplete)
            => WildcardPattern
                .Get(wordToComplete + "*", WildcardOptions.IgnoreCase)
                .IsMatch(value);

        /// <summary>
        /// Determines if the given value matches the specified word considering wildcard characters literally.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the value matches either the literal normalized word or the wildcard pattern with escaping;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This strategy first attempts a literal prefix match for performance and, if unsuccessful, escapes the word to complete to
        /// handle any problematic wildcard characters before performing a wildcard match.
        /// </remarks>
        internal static readonly MatchStrategy WildcardPatternEscapeMatch = (value, wordToComplete)
            => LiteralMatchOrdinalIgnoreCase(value, wordToComplete) ||
               WildcardPatternMatchIgnoreCase(value, WildcardPattern.Escape(wordToComplete));

        /// <summary>
        /// Determines if the given value matches the specified word taking into account wildcard characters.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the value matches either the literal normalized word or the wildcard pattern; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This strategy attempts a literal match first for performance and, if unsuccessful, evaluates the word against a wildcard pattern.
        /// </remarks>
        internal static readonly MatchStrategy DefaultMatch = (value, wordToComplete)
            => LiteralMatchOrdinalIgnoreCase(value, wordToComplete) ||
               WildcardPatternMatchIgnoreCase(value, wordToComplete);

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

            string quoteInUse = hasFrontSingleQuote ? SingleQuote : DoubleQuote;

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

        /// <summary>
        /// Determines whether the given text contains an escaped newline string.
        /// </summary>
        /// <param name="text">The input string to check for escaped newlines.</param>
        /// <returns>
        /// <c>true</c> if the text contains the escaped Unix-style newline string ("`n") or
        /// the Windows-style newline string ("`r`n"); otherwise, <c>false</c>.
        /// </returns>
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
                return DoubleQuote + completionText + DoubleQuote;
            }

            if (!CompletionRequiresQuotes(completionText))
            {
                return quote + completionText + quote;
            }

            string quoteInUse = string.IsNullOrEmpty(quote) ? SingleQuote : quote;

            if (quoteInUse == SingleQuote)
            {
                completionText = CodeGeneration.EscapeSingleQuotedStringContent(completionText);
            }

            return quoteInUse + completionText + quoteInUse;
        }
    }
}
