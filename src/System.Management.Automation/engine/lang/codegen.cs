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
                // Use an enumerator, skipping the first character which has already been
                // evaluated with different rules, to determine if any remaining characters are 
                // not an indentifier successive character.
                CharEnumerator ce_name = name.GetEnumerator();
                for (ce_name.MoveNext(); ce_name.MoveNext();)
                {
                    if (!ce_name.Current.IsIdentifierFollow())
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
        /// <param name="quoteInUse">The character to be quoted with.</param>
        /// <param name="isLiteralArgument">Treat the argument as taken literally, wildcard escaping not required.</param>
        /// <returns>Content quoted and escaped if required for use as an argument value.</returns>
        public static string QuoteArgument(string value, char quoteInUse, bool isLiteralArgument) =>
            // WildcardPattern.Escape() fails to escape the escape character, so we use Replace() first
            // but not all CMDLets will process the fully escaped result correctly.
            string.IsNullOrEmpty(value) ?
                string.Empty :
                QuoteArgument(isLiteralArgument ? value : WildcardPattern.Escape(value.Replace("`", "``")), quoteInUse);

        /// <summary>
        /// Quote an argument, if needed, or if specifically requested, escaping characters accordingly.
        /// </summary>
        /// <param name="value">The content to be used as an argument value taken literally.</param>
        /// <param name="quoteInUse">The character to be quoted with.</param>
        /// <returns>Content quoted and escaped if required for use as an argument value.</returns>
        public static string QuoteArgument(string value, char quoteInUse)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            char quoteToUse = quoteInUse;
            if (quoteToUse == 0)
            {
                if (ShouldArgumentNotBeBareword(value))
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

        // A brute force method to determine when an argument's value cannot be used bareword.
        private static bool ShouldArgumentNotBeBareword(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            // Rules for allowable bareword arguments:
            // - Characters that cannot be at start of argument: '@','#','<','>'
            // - Patterns that cannot be at start of argument: 
            // - - /[1-6]>/
            // - - /<IsDash>(<IsDash>$|[_<IsIdentifierStart>])/
            var firstChar = value[0];
            var length = value.Length;
            bool requiresQuote = "@#<>".Contains(firstChar) ||
                length > 1 && ((uint)(firstChar - '1') <= 5 && value[1] == '>' ||
                firstChar.IsDash() && (value[1].IsIdentifierStart() || (length == 2 && value[1].IsDash())));

            if (!requiresQuote)
            {
                bool lastCharWasDollar = false;
                foreach (char c in value)
                {
                    // - Characters that cannot appear anywhere:
                    // - - ForceStartNewToken
                    // - - IsSingleQuote
                    // - - IsDoubleQuote
                    // - - Backtick ('\`')
                    if (c.ForceStartNewToken() || c.IsSingleQuote() || c.IsDoubleQuote() || c == '`')
                    {
                        requiresQuote = true;
                        break;
                    }

                    // - IsVariableStart characters cannot appear after a `$`
                    // Note `{` and `(` is handled by ForceStartNewToken()
                    if (lastCharWasDollar && c.IsVariableStart())
                    {
                        requiresQuote = true;
                        break;
                    }

                    lastCharWasDollar = c == '$';
                }
            }

            return requiresQuote;
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

            StringBuilder sb = new StringBuilder(value.Length);
            bool lastCharWasDollar = false;
            foreach (char c in value)
            {
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
