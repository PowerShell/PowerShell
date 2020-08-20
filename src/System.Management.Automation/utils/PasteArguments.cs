// Copyright (c) Microsoft Corporation.
// Licensed to the .NET Foundation under one or more agreements.
// Fetched from https://github.com/dotnet/runtime/blob/a3befe1917526e5c735cd8a528cbee280fd66df2/src/libraries/System.Private.CoreLib/src/System/PasteArguments.cs

using System.Collections.Generic;
using System.Text;

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// Utility class for quoting arguments according to MSVCRT rules, pre- or post- 2008.
    /// The same rules are used by CoreFX for cmdline parsing too.
    /// </summary>
    public static class PasteArguments
    {
        /// <summary>
        /// Append one argument to the cmdline being built, without separator.
        /// </summary>
        /// <remarks>
        /// This version can be used incrementally for mixing quoted and unquoted runs.
        /// </remarks>
        /// <param name="stringBuilder">The cmdline being built.</param>
        /// <param name="argument">The argument to append.</param>
        /// <param name="forceQuote">When true, force the argument to be quoted. Normally only empty ones and those with spaces or quotes are quoted. May help avoid globbing.</param>
        public static void AppendArgument(StringBuilder stringBuilder, string argument, bool forceQuote)
        {
            // Parsing rules for non-argv[0] arguments:
            //   - Backslash is a normal character except followed by a quote.
            //   - 2N backslashes followed by a quote ==> N literal backslashes followed by unescaped quote
            //   - 2N+1 backslashes followed by a quote ==> N literal backslashes followed by a literal quote
            //   - Parsing stops at first whitespace outside of quoted region.
            //   - (post 2008 rule): A closing quote followed by another quote ==> literal quote, and parsing remains in quoting mode.
            if (!forceQuote && argument.Length != 0 && IsLiteralSafe(argument))
            {
                // Simple case - no quoting or changes needed.
                stringBuilder.Append(argument);
            }
            else
            {
                // Sanitize for incremental use.
                if (stringBuilder[stringBuilder.Length - 1] == Quote)
                {
                    // Already quoting. Do not add a quote; just reuse by trimming the existing one.
                    // "abc""def" is different from "abcdef". We want the latter.
                    stringBuilder.Length -= 1;
                }
                else
                {
                    // If the stuff is unquoted, we need to double the trailing backslashes since we
                    // are putting a quote here.
                    int numBackSlash = 0;
                    for (int i = stringBuilder.Length - 1; i > 0 && stringBuilder[i] == Backslash; i--)
                    {
                        numBackSlash++;
                    }

                    stringBuilder.Append(Backslash, numBackSlash);
                    stringBuilder.Append(Quote);
                }

                int idx = 0;
                while (idx < argument.Length)
                {
                    char c = argument[idx++];
                    if (c == Backslash)
                    {
                        int numBackSlash = 1;
                        while (idx < argument.Length && argument[idx] == Backslash)
                        {
                            idx++;
                            numBackSlash++;
                        }

                        if (idx == argument.Length)
                        {
                            // We'll emit an end quote after this so must double the number of backslashes.
                            stringBuilder.Append(Backslash, numBackSlash * 2);
                        }
                        else if (argument[idx] == Quote)
                        {
                            // Backslashes will be followed by a quote. Must double the number of backslashes.
                            stringBuilder.Append(Backslash, (numBackSlash * 2) + 1);
                            stringBuilder.Append(Quote);
                            idx++;
                        }
                        else
                        {
                            // Backslash will not be followed by a quote, so emit as normal characters.
                            stringBuilder.Append(Backslash, numBackSlash);
                        }

                        continue;
                    }

                    if (c == Quote)
                    {
                        // Escape the quote so it appears as a literal. This also guarantees that we won't end up generating a closing quote followed
                        // by another quote (which parses differently pre-2008 vs. post-2008.)
                        stringBuilder.Append(Backslash);
                        stringBuilder.Append(Quote);
                        continue;
                    }

                    stringBuilder.Append(c);
                }

                stringBuilder.Append(Quote);
            }
        }

        /// <summary>
        /// Repastes a set of arguments into a linear cmdline.
        /// </summary>
        /// <param name="arguments">The arguments to paste in.</param>
        /// <param name="forceQuote">When true, force all arguments to be quoted.</param>
        /// <returns>
        /// A string that parses back into the originals under pre- or post-2008 VC parsing rules.
        /// </returns>
        /// <remarks>This does not take into account argv[0] on Windows since we don't seem to need that with startInfo.</remarks>
        public static string Paste(IEnumerable<string> arguments, bool forceQuote)
        {
            var stringBuilder = new StringBuilder();
            foreach (string argument in arguments)
            {
                AppendArgument(stringBuilder, argument, forceQuote);
                stringBuilder.Append(' ');
            }

            // Remove the final space
            if (stringBuilder.Length > 1)
            {
                stringBuilder.Length -= 1;
            }

            return stringBuilder.ToString();
        }

        private static bool IsLiteralSafe(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (NoLiteral.Contains(c))
                {
                    return false;
                }
            }

            return true;
        }

        /* This might need to be replacable for batch files. */
        private const string NoLiteral = " \t\"";
        private const char Quote = '\"';
        private const char Backslash = '\\';
    }
}
