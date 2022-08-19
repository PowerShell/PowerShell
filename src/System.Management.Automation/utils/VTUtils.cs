// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace System.Management.Automation
{
    /// <summary>
    /// Class to help with VT escape sequences.
    /// </summary>
    public static class VTUtility
    {
        private static readonly string[] BackgroundColorMap =
            {
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

        private static readonly string[] ForegroundColorMap =
            {
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
        /// <param name="color">The <see cref="ConsoleColor"/> to be mapped from.</param>
        /// <param name="isBackground">Whether or not it's a background color.</param>
        /// <returns>The VT escape sequence representing the color. Or, an empty string if the passed-in color is invalid.</returns>
        internal static string MapColorToEscapeSequence(ConsoleColor color, bool isBackground)
        {
            int index = (int)color;
            if (index < 0 || index >= ForegroundColorMap.Length)
            {
                // Return empty string if the passed-in console color is out of bound.
                return string.Empty;
            }

            return (isBackground ? BackgroundColorMap : ForegroundColorMap)[index];
        }

        /// <summary>
        /// Return the VT escape sequence for a foreground color.
        /// </summary>
        /// <param name="foregroundColor">The foreground color to be mapped from.</param>
        /// <returns>The VT escape sequence representing the foreground color. Or, an empty string if the passed-in color is invalid.</returns>
        public static string MapForegroundColorToEscapeSequence(ConsoleColor foregroundColor)
            => MapColorToEscapeSequence(foregroundColor, isBackground: false);

        /// <summary>
        /// Return the VT escape sequence for a background color.
        /// </summary>
        /// <param name="backgroundColor">The background color to be mapped from.</param>
        /// <returns>The VT escape sequence representing the background color. Or, an empty string if the passed-in color is invalid.</returns>
        public static string MapBackgroundColorToEscapeSequence(ConsoleColor backgroundColor)
            => MapColorToEscapeSequence(backgroundColor, isBackground: true);

        /// <summary>
        /// Return the VT escape sequence for a pair of foreground and background colors.
        /// </summary>
        /// <param name="foregroundColor">The foreground color of the color pair.</param>
        /// <param name="backgroundColor">The background color of the color pair.</param>
        /// <returns>The VT escape sequence representing the foreground and background color pair. Or, an empty string if either of the passed-in colors is invalid.</returns>
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
