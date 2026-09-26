// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;

// ReSharper disable UnusedMember.Global

namespace System.Management.Automation
{
    using Dbg = Diagnostics;

    internal static class StringOps
    {
        internal static string Add(string lhs, string rhs)
        {
            return string.Concat(lhs, rhs);
        }

        internal static string Add(string lhs, char rhs)
        {
            return string.Concat(lhs, rhs);
        }

        internal static string Multiply(string s, int times)
        {
            Diagnostics.Assert(s != null, "caller to verify argument is not null");

            // TODO: this should be a runtime error.
            ArgumentOutOfRangeException.ThrowIfNegative(times);

            if (times == 0 || s.Length == 0)
            {
                return string.Empty;
            }

            var context = LocalPipeline.GetExecutionContextFromTLS();
            if (context != null &&
                context.LanguageMode == PSLanguageMode.RestrictedLanguage && (s.Length * times) > 1024)
            {
                throw InterpreterError.NewInterpreterException(times, typeof(RuntimeException),
                    null, "StringMultiplyToolongInDataSection", ParserStrings.StringMultiplyToolongInDataSection, 1024);
            }

            if (s.Length == 1)
            {
                // A string of length 1 has special support in the BCL, so just use it.
                return new string(s[0], times);
            }

            return string.Create(s.Length * times, (s, times), (dst, args) =>
                {
                    ReadOnlySpan<char> src = args.s.AsSpan();
                    int length = src.Length;
                    for (int i = 0; i < args.times; i++)
                    {
                        src.CopyTo(dst);
                        dst = dst.Slice(length);
                    }
                });
        }

        internal static string FormatOperator(string formatString, object formatArgs)
        {
            try
            {
                object[] formatArgsArray = formatArgs as object[];
                return formatArgsArray != null
                           ? StringUtil.Format(formatString, formatArgsArray)
                           : StringUtil.Format(formatString, formatArgs);
            }
            catch (FormatException sfe)
            {
                // "error formatting a string: " + sfe.Message
                throw InterpreterError.NewInterpreterException(formatString, typeof(RuntimeException),
                    PositionUtilities.EmptyExtent, "FormatError", ParserStrings.FormatError, sfe.Message);
            }
        }

        // The following methods are used for the compatibility purpose between regular PowerShell and PowerShell on CSS

        /// <summary>
        /// StringComparison.InvariantCulture is not in CoreCLR, so we need to use
        ///    CultureInfo.InvariantCulture.CompareInfo.Compare(string, string, CompareOptions)
        /// to substitute
        ///    string.Compare(string, string, StringComparison)
        /// </summary>
        internal static int Compare(string strA, string strB, CultureInfo culture, CompareOptions option)
        {
            Diagnostics.Assert(culture != null, "Caller makes sure that 'culture' is not null.");
            return culture.CompareInfo.Compare(strA, strB, option);
        }

        /// <summary>
        /// Implements string division functionality for splitting strings.
        /// For strings: "hello world" / 2 -> ["hello", " world"] (divide into 2 parts)  
        /// For strings with array divisor: "abcdef" / [2,3] -> ["ab", "cde", "f"] (divide by lengths)
        /// </summary>
        /// <param name="lhs">Left-hand side string to divide.</param>
        /// <param name="rhs">Right-hand side divisor (integer or array of integers).</param>
        /// <returns>Array of string parts based on division logic.</returns>
        internal static string[] Divide(string lhs, object rhs)
        {
            if (string.IsNullOrEmpty(lhs))
            {
                                    return Array.Empty<string>();
            }

            // Handle integer divisor - divide string into N equal parts
            if (LanguagePrimitives.TryConvertTo<int>(rhs, out int divisor))
            {
                if (divisor <= 0)
                {
                    throw new ArgumentException("String division requires a positive divisor");
                }

                if (divisor == 1)
                {
                    return new string[] { lhs };
                }

                var result = new string[divisor];
                int charsPerGroup = lhs.Length / divisor;
                int remainder = lhs.Length % divisor;
                int currentIndex = 0;

                for (int i = 0; i < divisor; i++)
                {
                    int groupSize = charsPerGroup + (i < remainder ? 1 : 0);
                    
                    if (currentIndex < lhs.Length)
                    {
                        int actualSize = Math.Min(groupSize, lhs.Length - currentIndex);
                        result[i] = lhs.Substring(currentIndex, actualSize);
                        currentIndex += actualSize;
                    }
                    else
                    {
                        result[i] = string.Empty;
                    }
                }

                return result;
            }

            // Handle array divisor - divide by specified lengths
            if (rhs is object[] rhsArray)
            {
                var lengths = new List<int>();
                foreach (object item in rhsArray)
                {
                    if (LanguagePrimitives.TryConvertTo<int>(item, out int length) && length > 0)
                    {
                        lengths.Add(length);
                    }
                }

                if (lengths.Count == 0)
                {
                    throw new ArgumentException("String division requires positive integer lengths");
                }

                var result = new List<string>();
                int currentIndex = 0;

                foreach (int length in lengths)
                {
                    if (currentIndex >= lhs.Length) break;
                    
                    int actualLength = Math.Min(length, lhs.Length - currentIndex);
                    result.Add(lhs.Substring(currentIndex, actualLength));
                    currentIndex += actualLength;
                }

                // Add remaining characters if any
                if (currentIndex < lhs.Length)
                {
                    result.Add(lhs.Substring(currentIndex));
                }

                return result.ToArray();
            }

            throw new ArgumentException("String division requires an integer or array of integers as divisor");
        }

        /// <summary>
        /// StringComparison.InvariantCulture is not in CoreCLR, so we need to use
        ///    CultureInfo.InvariantCulture.CompareInfo.Compare(string, string, CompareOptions) == 0
        /// to substitute
        ///    string.Equals(string, string, StringComparison)
        /// </summary>
        internal static bool Equals(string strA, string strB, CultureInfo culture, CompareOptions option)
        {
            Diagnostics.Assert(culture != null, "Caller makes sure that 'culture' is not null.");
            return culture.CompareInfo.Compare(strA, strB, option) == 0;
        }
    }
}
