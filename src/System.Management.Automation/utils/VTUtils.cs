// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace System.Management.Automation
{
    /// <summary>
    /// Class to help with VT escape sequences.
    /// </summary>
    public sealed class VTUtility
    {
        private static readonly string[] BackgroundColorMap = {
            "\x1b[40m", // Black
            "\x1b[44m", // DarkBlue
            "\x1b[42m", // DarkGreen
            "\x1b[46m", // DarkCyan
            "\x1b[41m", // DarkRed
            "\x1b[45m", // DarkMagenta
            "\x1b[43m", // DarkYellow
            "\x1b[47m", // Gray
            "\x1b[100m", // DarkGray
            "\x1b[104m", // Blue
            "\x1b[102m", // Green
            "\x1b[106m", // Cyan
            "\x1b[101m", // Red
            "\x1b[105m", // Magenta
            "\x1b[103m", // Yellow
            "\x1b[107m", // White
        };

        private static readonly string[] ForegroundColorMap = {
            "\x1b[30m", // Black
            "\x1b[34m", // DarkBlue
            "\x1b[32m", // DarkGreen
            "\x1b[36m", // DarkCyan
            "\x1b[31m", // DarkRed
            "\x1b[35m", // DarkMagenta
            "\x1b[33m", // DarkYellow
            "\x1b[37m", // Gray
            "\x1b[90m", // DarkGray
            "\x1b[94m", // Blue
            "\x1b[92m", // Green
            "\x1b[96m", // Cyan
            "\x1b[91m", // Red
            "\x1b[95m", // Magenta
            "\x1b[93m", // Yellow
            "\x1b[97m", // White
        };

        /// <summary>
        /// Return the VT escape sequence for a ConsoleColor.
        /// </summary>
        internal static string MapColorToEscapeSequence(ConsoleColor color, bool isBackground)
        {
            int index = (int)color;
            if (index < 0 || index >= ForegroundColorMap.Length)
            {
                return string.Empty;
            }

            return (isBackground ? BackgroundColorMap : ForegroundColorMap)[index];
        }

        /// <summary>
        /// Return the VT escape sequence for a foreground color.
        /// </summary>
        public static string MapForegroundColorToEscapeSequence(ConsoleColor foregroundColor)
            => MapColorToEscapeSequence(foregroundColor, isBackground: false);

        /// <summary>
        /// Return the VT escape sequence for a background color.
        /// </summary>
        public static string MapBackgroundColorToEscapeSequence(ConsoleColor backgroundColor)
            => MapColorToEscapeSequence(backgroundColor, isBackground: true);

        /// <summary>
        /// Return the VT escape sequence for a pair of foreground and background colors.
        /// </summary>
        public static string MapColorPairToEscapeSequence(ConsoleColor foregroundColor, ConsoleColor backgroundColor)
        {
            int foreIndex = (int)foregroundColor;
            int backIndex = (int)backgroundColor;

            if (foreIndex < 0 || backIndex < 0 ||
                foreIndex >= ForegroundColorMap.Length ||
                backIndex >= ForegroundColorMap.Length)
            {
                // Return empty string if either of the passed-in console colors is out of bound.
                return string.Empty;
            }

            string foreground = ForegroundColorMap[foreIndex];
            string background = BackgroundColorMap[backIndex];

            return string.Concat(
                foreground.AsSpan(start: 0, length: foreground.Length - 1),
                ";".AsSpan(),
                background.AsSpan(start: 2));
        }
    }
}
