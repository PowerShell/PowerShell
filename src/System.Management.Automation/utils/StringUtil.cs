// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation.Host;
using System.Threading;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Internal
{
    internal static
    class StringUtil
    {
        internal static
        string
        Format(string formatSpec, object o)
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, formatSpec, o);
        }

        internal static
        string
        Format(string formatSpec, object o1, object o2)
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, formatSpec, o1, o2);
        }

        internal static
        string
        Format(string formatSpec, params object[] o)
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, formatSpec, o);
        }

        internal static
        string
        TruncateToBufferCellWidth(PSHostRawUserInterface rawUI, string toTruncate, int maxWidthInBufferCells)
        {
            Dbg.Assert(rawUI != null, "need a reference");
            Dbg.Assert(maxWidthInBufferCells >= 0, "maxWidthInBufferCells must be positive");

            string result;
            int i = Math.Min(toTruncate.Length, maxWidthInBufferCells);

            do
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
            } while (true);

            return result;
        }

        // Typical padding is at most a screen's width, any more than that and we won't bother caching.
        private const int IndentCacheMax = 120;
        private static readonly string[] IndentCache = new string[IndentCacheMax];
        internal static string Padding(int countOfSpaces)
        {
            if (countOfSpaces >= IndentCacheMax)
                return new string(' ', countOfSpaces);

            var result = IndentCache[countOfSpaces];

            if (result == null)
            {
                Interlocked.CompareExchange(ref IndentCache[countOfSpaces], new string(' ', countOfSpaces), null);
                result = IndentCache[countOfSpaces];
            }

            return result;
        }

        private const int DashCacheMax = 120;
        private static readonly string[] DashCache = new string[DashCacheMax];
        internal static string DashPadding(int count)
        {
            if (count >= DashCacheMax)
                return new string('-', count);

            var result = DashCache[count];

            if (result == null)
            {
                Interlocked.CompareExchange(ref DashCache[count], new string('-', count), null);
                result = DashCache[count];
            }

            return result;
        }
    }
}

