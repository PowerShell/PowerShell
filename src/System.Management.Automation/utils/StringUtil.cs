// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Management.Automation.Host;
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
    }
}
