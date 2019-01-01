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
                if (SpecialCharacters.IsSingleQuote(c))
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
                if (SpecialCharacters.IsCurlyBracket(c))
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
    }
}