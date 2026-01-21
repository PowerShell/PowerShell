// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation.Host;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Internal
{
    internal static class StringUtil
    {
        internal static string Format(string format, object arg0)
            => string.Format(CultureInfo.CurrentCulture, format, arg0);

        internal static string Format(string format, object arg0, object arg1)
            => string.Format(CultureInfo.CurrentCulture, format, arg0, arg1);

        internal static string Format(string format, object arg0, object arg1, object arg2)
            => string.Format(CultureInfo.CurrentCulture, format, arg0, arg1, arg2);

        internal static string Format(string format, params object[] args)
            => string.Format(CultureInfo.CurrentCulture, format, args);

        internal static string TruncateToBufferCellWidth(PSHostRawUserInterface rawUI, string toTruncate, int maxWidthInBufferCells)
        {
            Dbg.Assert(rawUI != null, "need a reference");
            Dbg.Assert(maxWidthInBufferCells >= 0, "maxWidthInBufferCells must be positive");

            string result;
            int i = Math.Min(toTruncate.Length, maxWidthInBufferCells);

            while (true)
            {
                result = toTruncate.Substring(0, i);
                int cellCount = rawUI.LengthInBufferCells(result);
                if (cellCount <= maxWidthInBufferCells)
                {
                    // the segment from start..i fits

                    break;
                }
                else
                {
                    // The segment does not fit, back off a tad until it does
                    // We need to back off 1 by 1 because there could theoretically
                    // be characters taking more 2 buffer cells
                    --i;
                }
            }

            return result;
        }

        // Typical padding is at most a screen's width, any more than that and we won't bother caching.
        private const int IndentCacheMax = 120;

        private static readonly string[] s_indentCache = new string[IndentCacheMax];

        internal static string Padding(int countOfSpaces)
        {
            if (countOfSpaces >= IndentCacheMax)
                return new string(' ', countOfSpaces);

            var result = s_indentCache[countOfSpaces];

            if (result == null)
            {
                Interlocked.CompareExchange(ref s_indentCache[countOfSpaces], new string(' ', countOfSpaces), null);
                result = s_indentCache[countOfSpaces];
            }

            return result;
        }

        private const int DashCacheMax = 120;

        private static readonly string[] s_dashCache = new string[DashCacheMax];

        internal static string DashPadding(int count)
        {
            if (count >= DashCacheMax)
                return new string('-', count);

            var result = s_dashCache[count];

            if (result == null)
            {
                Interlocked.CompareExchange(ref s_dashCache[count], new string('-', count), null);
                result = s_dashCache[count];
            }

            return result;
        }

        /// <summary>
        /// Substring implementation that takes into account the VT escape sequences.
        /// </summary>
        /// <param name="str">String that may contain VT escape sequences.</param>
        /// <param name="startOffset">
        /// When the string doesn't contain VT sequences, it's the starting index.
        /// When the string contains VT sequences, it means starting from the 'n-th' char that doesn't belong to a escape sequence.
        /// </param>
        /// <returns>The requested substring.</returns>
        internal static string VtSubstring(this string str, int startOffset)
        {
            return VtSubstring(str, startOffset, int.MaxValue, prependStr: null, appendStr: null);
        }

        /// <summary>
        /// Substring implementation that takes into account the VT escape sequences.
        /// </summary>
        /// <param name="str">String that may contain VT escape sequences.</param>
        /// <param name="startOffset">
        /// When the string doesn't contain VT sequences, it's the starting index.
        /// When the string contains VT sequences, it means starting from the 'n-th' char that doesn't belong to a escape sequence.</param>
        /// <param name="length">Number of non-escape-sequence characters to be included in the substring.</param>
        /// <returns>The requested substring.</returns>
        internal static string VtSubstring(this string str, int startOffset, int length)
        {
            return VtSubstring(str, startOffset, length, prependStr: null, appendStr: null);
        }

        /// <summary>
        /// Substring implementation that takes into account the VT escape sequences.
        /// </summary>
        /// <param name="str">String that may contain VT escape sequences.</param>
        /// <param name="startOffset">
        /// When the string doesn't contain VT sequences, it's the starting index.
        /// When the string contains VT sequences, it means starting from the 'n-th' char that doesn't belong to a escape sequence.</param>
        /// <param name="prependStr">The string to be prepended to the substring.</param>
        /// <param name="appendStr">The string to be appended to the substring.</param>
        /// <returns>The requested substring.</returns>
        internal static string VtSubstring(this string str, int startOffset, string prependStr, string appendStr)
        {
            return VtSubstring(str, startOffset, int.MaxValue, prependStr, appendStr);
        }

        /// <summary>
        /// Substring implementation that takes into account the VT escape sequences.
        /// </summary>
        /// <param name="str">String that may contain VT escape sequences.</param>
        /// <param name="startOffset">
        /// When the string doesn't contain VT sequences, it's the starting index.
        /// When the string contains VT sequences, it means starting from the 'n-th' char that doesn't belong to a escape sequence.</param>
        /// <param name="length">Number of non-escape-sequence characters to be included in the substring.</param>
        /// <param name="prependStr">The string to be prepended to the substring.</param>
        /// <param name="appendStr">The string to be appended to the substring.</param>
        /// <returns>The requested substring.</returns>
        internal static string VtSubstring(this string str, int startOffset, int length, string prependStr, string appendStr)
        {
            var valueStrDec = new ValueStringDecorated(str);
            if (valueStrDec.IsDecorated)
            {
                // Handle strings with VT sequences.
                bool copyStarted = startOffset == 0;
                bool hasEscSeqs = false;
                bool firstNonEscChar = true;
                StringBuilder sb = new(capacity: str.Length);
                Dictionary<int, int> vtRanges = valueStrDec.EscapeSequenceRanges;

                for (int i = 0, offset = 0; i < str.Length; i++)
                {
                    // Keep all leading ANSI escape sequences.
                    if (vtRanges.TryGetValue(i, out int len))
                    {
                        hasEscSeqs = true;
                        sb.Append(str.AsSpan(i, len));

                        i += len - 1;
                        continue;
                    }

                    // OK, now we get a non-escape-sequence character.
                    if (copyStarted)
                    {
                        if (firstNonEscChar)
                        {
                            // Prepend the string before we copy the first non-escape-sequence character.
                            sb.Append(prependStr);
                            firstNonEscChar = false;
                        }

                        // Copy this character if we've started the copy.
                        sb.Append(str[i]);

                        // Increment 'offset' to keep track of number of non-escape-sequence characters we've copied.
                        offset++;
                    }
                    else if (++offset == startOffset)
                    {
                        // We've skipped enough non-escape-sequence characters, and will be copying the next one.
                        copyStarted = true;

                        // Reset 'offset' and from now on use it to track the number of copied non-escape-sequence characters.
                        offset = 0;
                        continue;
                    }

                    // If the number of copied non-escape-sequence characters has reached the specified length, done copying.
                    if (copyStarted && offset == length)
                    {
                        break;
                    }
                }

                if (hasEscSeqs)
                {
                    string resetStr = PSStyle.Instance.Reset;
                    bool endsWithReset = sb.EndsWith(resetStr);
                    if (endsWithReset)
                    {
                        // Append the given string before the reset VT sequence.
                        sb.Insert(sb.Length - resetStr.Length, appendStr);
                    }
                    else
                    {
                        // Append the given string and add the reset VT sequence.
                        sb.Append(appendStr).Append(resetStr);
                    }
                }
                else
                {
                    sb.Append(appendStr);
                }

                return sb.ToString();
            }

            // Handle strings without VT sequences.
            if (length == int.MaxValue)
            {
                length = str.Length - startOffset;
            }

            if (prependStr is null && appendStr is null)
            {
                return str.Substring(startOffset, length);
            }
            else
            {
                int capacity = length + (prependStr?.Length ?? 0) + (appendStr?.Length ?? 0);
                return new StringBuilder(prependStr, capacity)
                    .Append(str, startOffset, length)
                    .Append(appendStr)
                    .ToString();
            }
        }

        internal static bool EndsWith(this StringBuilder sb, string value)
        {
            if (sb.Length < value.Length)
            {
                return false;
            }

            int offset = sb.Length - value.Length;
            for (int i = 0; i < value.Length; i++)
            {
                if (sb[offset + i] != value[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
