// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Language
{
    /// <summary>
    /// Contains utility methods for use in applications that generate PowerShell code.
    /// </summary>
    public static class CodeGeneration
    {
        /// <summary>
        /// Escapes content so that it is safe for inclusion in a single-quoted string.
        /// For example: "'" + EscapeSingleQuotedStringContent(userContent) + "'"
        /// </summary>
        /// <param name="value">The content to be included in a single-quoted string.</param>
        /// <returns>Content with all single-quotes escaped.</returns>
        public static string EscapeSingleQuotedStringContent(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                sb.Append(c);
                if (CharExtensions.IsSingleQuote(c))
                {
                    // double-up quotes to escape them
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Escapes content so that it is safe for inclusion in a block comment.
        /// For example: "&lt;#" + EscapeBlockCommentContent(userContent) + "#&gt;"
        /// </summary>
        /// <param name="value">The content to be included in a block comment.</param>
        /// <returns>Content with all block comment characters escaped.</returns>
        public static string EscapeBlockCommentContent(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("<#", "<`#")
                .Replace("#>", "#`>");
        }

        /// <summary>
        /// Escapes content so that it is safe for inclusion in a string that will later be used as a
        /// format string. If this is to be embedded inside of a single-quoted string, be sure to also
        /// call EscapeSingleQuotedStringContent.
        /// For example: "'" + EscapeSingleQuotedStringContent(EscapeFormatStringContent(userContent)) + "'" -f $args.
        /// </summary>
        /// <param name="value">The content to be included in a format string.</param>
        /// <returns>Content with all curly braces escaped.</returns>
        public static string EscapeFormatStringContent(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                sb.Append(c);
                if (CharExtensions.IsCurlyBracket(c))
                {
                    // double-up curly brackets to escape them
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Escapes content so that it is safe for inclusion in a string that will later be used in a variable
        /// name reference. This is only valid when used within PowerShell's curly brace naming syntax.
        ///
        /// For example: '${' + EscapeVariableName('value') + '}'
        /// </summary>
        /// <param name="value">The content to be included as a variable name.</param>
        /// <returns>Content with all curly braces and back-ticks escaped.</returns>
        public static string EscapeVariableName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("`", "``")
                .Replace("}", "`}")
                .Replace("{", "`{");
        }

        /// <summary>
        /// Single-quote and escape a member name if it requires quoting, otherwise passing it unmodified.
        /// </summary>
        /// <param name="name">The content to be used as a member name in a member access.</param>
        /// <returns>Content quoted and escaped if required for use as a member name.</returns>
        public static string QuoteMemberName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            // Determine if first character is not an indentifier start character.
            bool requiresQuote = !name[0].IsIdentifierStart();
            if (!requiresQuote)
            {
                // Step through remainder of name to determine if any remaining characters are 
                // not an indentifier successive character.
                for (int i = 1, nameLength = name.Length; i < nameLength; i++)
                {
                    if (!name[i].IsIdentifierFollow())
                    {
                        requiresQuote = true;
                        break;
                    }
                }
            }

            // quote the content if required.
            return requiresQuote ?
                "'" + EscapeSingleQuotedStringContent(name) + "'" :
                name;
        }

        /// <summary>
        /// Quote an argument, if needed, or if specifically requested, escaping characters accordingly,
        /// handling escaping of wildcard patterns when argument is not already taken literally.
        /// </summary>
        /// <param name="value">The content to be used as an argument value taken literally.</param>
        /// <param name="quoteInUse">The character to be quoted with, or an empty string for bareword.</param>
        /// <param name="isLiteralArgument">Treat the argument as taken literally, wildcard escaping not required.</param>
        /// <returns>Content quoted and escaped if required for use as an argument value.</returns>
        public static string QuoteArgument(string value, string quoteInUse, bool isLiteralArgument) =>
            // WildcardPattern.Escape() fails to escape the escape character, so we use Replace() first
            // but not all CMDLets will process the fully escaped result correctly.
            string.IsNullOrEmpty(value) ?
                string.Empty :
                QuoteArgument(isLiteralArgument ? value : WildcardPattern.Escape(value.Replace("`", "``")), quoteInUse);

        /// <summary>
        /// Quote an argument, if needed, or if specifically requested, escaping characters accordingly.
        /// </summary>
        /// <param name="value">The content to be used as an argument value taken literally.</param>
        /// <param name="quoteInUse">The character to be quoted with, or an empty string for bareword.</param>
        /// <returns>Content quoted and escaped if required for use as an argument value.</returns>
        public static string QuoteArgument(string value, string quoteInUse)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            char quoteToUse = string.IsNullOrWhiteSpace(quoteInUse) ? (char)0 : quoteInUse[0];
            if (quoteToUse == (char)0)
            {
                if (ArgRequiresQuote(value))
                {
                    // argument value not compatible with bareword
                    quoteToUse = '\'';
                }
                else
                {
                    // return unmodified argument value
                    return value;
                }
            }

            // quote and escape argument as needed
            return quoteToUse + (quoteToUse.IsDoubleQuote() ? EscapeDoubleQuotedStringContent(value) :
                EscapeSingleQuotedStringContent(value)) + quoteToUse;
        }

        internal static bool CmdRequiresQuote (string value) =>
            CmdRequiresQuote(value, false);

        // Test for conditions that a value being attempted to be provided to be used as a commnd name
        // to be parsed may need to be quoted in order to be correctly parsed.
        // IsExpandable refers to the target parsing condition, when false, target is without the `invoke`
        // operators, else the target is behind the `invoke` operators.
        internal static bool CmdRequiresQuote(string value, bool IsExpandable = false)
        {
            return _CommonRequiresQuote(value, IsExpandable, out Token tokenToCheck) ||
                !IsExpandable && (tokenToCheck.Kind == TokenKind.Number ||
                        tokenToCheck.Kind == TokenKind.Semi) || (tokenToCheck.TokenFlags & (TokenFlags.UnaryOperator | TokenFlags.Keyword)) != 0;
        }

        // Test for conditions that a value being attempted to be provided in a command line argument
        // may need to be quoted in order to be correctly parsed.
        private static bool ArgRequiresQuote(string value)
        {
            return _CommonRequiresQuote(value, true, out Token tokenToCheck) ||
                tokenToCheck.Kind == TokenKind.Redirection ||
                tokenToCheck.Kind == TokenKind.RedirectInStd ||
                tokenToCheck.Kind == TokenKind.Parameter;
        }

        // List of TokensKinds to which commonly require quoting in order to be used as a command name or
        // as an argument to a command.
        private static readonly TokenKind[] s_commonQuotedTokenKinds = {
            TokenKind.Variable,
            TokenKind.SplattedVariable,
            TokenKind.StringExpandable,
            TokenKind.StringLiteral,
            TokenKind.HereStringExpandable,
            TokenKind.HereStringLiteral,
            TokenKind.Comment };

        // Attempt to parse a command name or argument value and test for common reasons the value would
        // need to be quoted in order to be used as is if it were to be offered for parsing on a real command line.
        private static bool _CommonRequiresQuote(string value, bool IsExpandable, out Token tokenToCheck)
        {
            Parser.ParseInput((IsExpandable ? "&" : "") + value, out Token[] _tokens, out ParseError[] _parseerrors);
            Token _token = tokenToCheck = _tokens[IsExpandable ? 1 : 0];

            // Quoting is required if there are: parse errors, not correct number of returned tokens,
            // token.Kind is any of s_commonQuotedTokenKinds, contains expanded elements,
            // (StringToken)token.Value.Length didn't match the original value or the last character is a backtick,
            //  or the EOI token was misplaced.
            return _parseerrors.Length != 0 || _tokens.Length != (IsExpandable ? 3 : 2) ||
                Array.Exists(s_commonQuotedTokenKinds, tokenKind => tokenKind == _token.Kind) ||
                (IsExpandable && _tokens[1] is StringExpandableToken) ||
                (tokenToCheck is StringToken stringToken && (stringToken.Value.Length != value.Length || stringToken.Value.EndsWith('`'))) ||
                _tokens[IsExpandable ? 2 : 1].Kind != TokenKind.EndOfInput;
        }

        /// <summary>
        /// Escapes content so that it is safe for inclusion in a double-quoted string.
        /// For example: "\"" + EscapeDoubleQuotedStringContent(userContent) + "\""
        /// </summary>
        /// <param name="value">The content to be included in a double-quoted string.</param>
        /// <returns>Content with all backticks and double-quotes, and only valid variable `$` escaped.</returns>
        public static string EscapeDoubleQuotedStringContent(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            int valueLength = value.Length;
            StringBuilder sb = new StringBuilder(valueLength);
            bool lastCharWasDollar = false;
            for (int i = 0; i < valueLength; i++)
            {
                char c = value[i];
                if (lastCharWasDollar)
                {
                    if (c.IsVariableStart() || c == '{' || c == '(')
                    {
                        sb.Append('`');
                    }

                    sb.Append('$');
                }

                lastCharWasDollar = c == '$';
                if (!lastCharWasDollar)
                {
                    sb.Append(c);
                    if (c.IsDoubleQuote() || c == '`')
                    {
                        // double-up quotes & backticks to escape them
                        sb.Append(c);
                    }
                }
            }

            if (lastCharWasDollar)
            {
                sb.Append('$');
            }

            return sb.ToString();
        }
    }
}
