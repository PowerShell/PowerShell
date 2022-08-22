// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace System.Management.Automation
{
    /// <summary>
    /// Class to help with VT escape sequences.
    /// </summary>
    [Obsolete("This class is deprecated. Use 'System.Management.Automation.PSStyle' instead.", error: true)]
    public sealed class VTUtility
    {
        /// <summary>
        /// Available VT escape codes other than colors.
        /// </summary>
        public enum VT
        {
            /// <summary>Reset the text style.</summary>
            Reset,

            /// <summary>Invert the foreground and background colors.</summary>
            Inverse
        }

        private static readonly Dictionary<ConsoleColor, string> ForegroundColorMap = new Dictionary<ConsoleColor, string>
        {
            { ConsoleColor.Black, "\x1b[30m" },
            { ConsoleColor.Gray, "\x1b[37m" },
            { ConsoleColor.Red, "\x1b[91m" },
            { ConsoleColor.Green, "\x1b[92m" },
            { ConsoleColor.Yellow, "\x1b[93m" },
            { ConsoleColor.Blue, "\x1b[94m" },
            { ConsoleColor.Magenta, "\x1b[95m" },
            { ConsoleColor.Cyan, "\x1b[96m" },
            { ConsoleColor.White, "\x1b[97m" },
            { ConsoleColor.DarkRed, "\x1b[31m" },
            { ConsoleColor.DarkGreen, "\x1b[32m" },
            { ConsoleColor.DarkYellow, "\x1b[33m" },
            { ConsoleColor.DarkBlue, "\x1b[34m" },
            { ConsoleColor.DarkMagenta, "\x1b[35m" },
            { ConsoleColor.DarkCyan, "\x1b[36m" },
            { ConsoleColor.DarkGray, "\x1b[90m" },
        };

        private static readonly Dictionary<VT, string> VTCodes = new Dictionary<VT, string>
        {
            { VT.Reset, "\x1b[0m" },
            { VT.Inverse, "\x1b[7m" }
        };

        /// <summary>
        /// Return the VT escape sequence for a ConsoleColor.
        /// </summary>
        /// <param name="color">
        /// The ConsoleColor to return the equivalent VT escape sequence.
        /// </param>
        /// <returns>
        /// The requested VT escape sequence.
        /// </returns>
        public static string GetEscapeSequence(ConsoleColor color)
        {
            string value = string.Empty;
            ForegroundColorMap.TryGetValue(color, out value);
            return value;
        }

        /// <summary>
        /// Return the VT escape sequence for a supported VT enum value.
        /// </summary>
        /// <param name="vt">
        /// The VT code to return the VT escape sequence.
        /// </param>
        /// <returns>
        /// The requested VT escape sequence.
        /// </returns>
        public static string GetEscapeSequence(VT vt)
        {
            string value = string.Empty;
            VTCodes.TryGetValue(vt, out value);
            return value;
        }
    }
}
