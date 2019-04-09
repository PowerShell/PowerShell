// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

            if (times < 0)
            {
                // TODO: this should be a runtime error.
                throw new ArgumentOutOfRangeException("times");
            }

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

            // Convert the string to a char array, use the array multiplication code,
            // then construct a new string from the resulting char array.  This uses
            // extra memory compared to the naive algorithm, but is faster (measured
            // against a V2 CLR, should be measured against V4 as the StringBuilder
            // implementation changed.)
            return new string(ArrayOps.Multiply(s.ToCharArray(), (uint)times));
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
