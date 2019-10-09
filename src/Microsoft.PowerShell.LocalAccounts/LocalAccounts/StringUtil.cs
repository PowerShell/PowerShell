// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Management.Automation.SecurityAccountsManager.Native;

namespace System.Management.Automation.SecurityAccountsManager
{
    /// <summary>
    /// Contains utility functions for formatting localizable strings.
    /// </summary>
    internal class StringUtil
    {
        /// <summary>
        /// Private constructor to precent auto-generation of a default constructor with greater accessability.
        /// </summary>
        private StringUtil()
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static string Format(string str)
        {
            return string.Format(CultureInfo.CurrentCulture, str);
        }

        internal static string Format(string fmt, string p0)
        {
            return string.Format(CultureInfo.CurrentCulture, fmt, p0);
        }

        internal static string Format(string fmt, string p0, string p1)
        {
            return string.Format(CultureInfo.CurrentCulture, fmt, p0, p1);
        }

        internal static string Format(string fmt, uint p0)
        {
            return string.Format(CultureInfo.CurrentCulture, fmt, p0);
        }

        internal static string Format(string fmt, int p0)
        {
            return string.Format(CultureInfo.CurrentCulture, fmt, p0);
        }

        internal static string FormatMessage(uint messageId, string[] args)
        {
            var message = new System.Text.StringBuilder(256);
            UInt32 flags = Win32.FORMAT_MESSAGE_FROM_SYSTEM;

            if (args == null)
                flags |= Win32.FORMAT_MESSAGE_IGNORE_INSERTS;
            else
                flags |= Win32.FORMAT_MESSAGE_ARGUMENT_ARRAY;

            var length = Win32.FormatMessage(flags, IntPtr.Zero, messageId, 0, message, 256, args);

            if (length > 0)
                return message.ToString();

            return null;
        }

        internal static string GetSystemMessage(uint messageId)
        {
            return FormatMessage(messageId, null);
        }
    }
}
