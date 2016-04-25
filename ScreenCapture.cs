/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.PowerShell.Internal;

namespace Microsoft.PowerShell
{
    public partial class PSConsoleReadLine
    {
        private static void InvertLines(int start, int count)
        {
            var buffer = ReadBufferLines(start, count);
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i].ForegroundColor = (ConsoleColor)((int)buffer[i].ForegroundColor ^ 7);
                buffer[i].BackgroundColor = (ConsoleColor)((int)buffer[i].BackgroundColor ^ 7);
            }
            _singleton._console.WriteBufferLines(buffer, ref start, false);
        }

        /// <summary>
        /// Start interactive screen capture - up/down arrows select lines, enter copies
        /// selected text to clipboard as text and html
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static void CaptureScreen(ConsoleKeyInfo? key = null, object arg = null)
        {
            int selectionTop = _singleton._console.CursorTop;
            int selectionHeight = 1;
            int currentY = selectionTop;
            Internal.IConsole console = _singleton._console;

            // We'll keep the current selection line (currentY) at least 4 lines
            // away from the top or bottom of the window.
            const int margin = 5;
            Func<bool> tooCloseToTop = () => { return (currentY - console.WindowTop) < margin; };
            Func<bool> tooCloseToBottom = () => { return ((console.WindowTop + console.WindowHeight) - currentY) < margin; };

            // Current lines starts out selected
            InvertLines(selectionTop, selectionHeight);
            bool done = false;
            while (!done)
            {
                var k = ReadKey();
                switch (k.Key)
                {
                case ConsoleKey.K:
                case ConsoleKey.UpArrow:
                    if (tooCloseToTop())
                        ScrollDisplayUpLine();

                    if (currentY > 0)
                    {
                        currentY -= 1;
                        if ((k.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift)
                        {
                            if (currentY < selectionTop)
                            {
                                // Extend selection up, only invert newly selected line.
                                InvertLines(currentY, 1);
                                selectionTop = currentY;
                                selectionHeight += 1;
                            }
                            else if (currentY >= selectionTop)
                            {
                                // Selection shortend 1 line, invert unselected line.
                                InvertLines(currentY + 1, 1);
                                selectionHeight -= 1;
                            }
                            break;
                        }
                        goto updateSelectionCommon;
                    }
                    break;

                case ConsoleKey.J:
                case ConsoleKey.DownArrow:
                    if (tooCloseToBottom())
                        ScrollDisplayDownLine();

                    if (currentY < (console.BufferHeight - 1))
                    {
                        currentY += 1;
                        if ((k.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift)
                        {
                            if (currentY == (selectionTop + selectionHeight))
                            {
                                // Extend selection down, only invert newly selected line.
                                InvertLines(selectionTop + selectionHeight, 1);
                                selectionHeight += 1;
                            }
                            else if (currentY == (selectionTop + 1))
                            {
                                // Selection shortend 1 line, invert unselected line.
                                InvertLines(selectionTop, 1);
                                selectionTop = currentY;
                                selectionHeight -= 1;
                            }
                            break;
                        }
                        goto updateSelectionCommon;
                    }
                    break;

                updateSelectionCommon:
                    // Shift not pressed - unselect current selection
                    InvertLines(selectionTop, selectionHeight);
                    selectionTop = currentY;
                    selectionHeight = 1;
                    InvertLines(selectionTop, selectionHeight);
                    break;

                case ConsoleKey.Enter:
                    InvertLines(selectionTop, selectionHeight);
                    DumpScreenToClipboard(selectionTop, selectionHeight);
                    ScrollDisplayToCursor();
                    return;

                case ConsoleKey.Escape:
                    done = true;
                    continue;

                case ConsoleKey.C:
                case ConsoleKey.G:
                    if (k.Modifiers == ConsoleModifiers.Control)
                    {
                        done = true;
                        continue;
                    }
                    Ding();
                    break;
                default:
                    Ding();
                    break;
                }
            }
            InvertLines(selectionTop, selectionHeight);
            ScrollDisplayToCursor();
        }

        private const string CmdColorTable = @"
\red0\green0\blue0;
\red0\green0\blue128;
\red0\green128\blue0;
\red0\green128\blue128;
\red128\green0\blue0;
\red128\green0\blue128;
\red128\green128\blue0;
\red192\green192\blue192;
\red128\green128\blue128;
\red0\green0\blue255;
\red0\green255\blue0;
\red0\green255\blue255;
\red255\green0\blue0;
\red255\green0\blue255;
\red255\green255\blue0;
\red255\green255\blue255;
";

        private const string PowerShellColorTable = @"
\red1\green36\blue86;
\red0\green0\blue128;
\red0\green128\blue0;
\red0\green128\blue128;
\red128\green0\blue0;
\red1\green36\blue86;
\red238\green237\blue240;
\red192\green192\blue192;
\red128\green128\blue128;
\red0\green0\blue255;
\red0\green255\blue0;
\red0\green255\blue255;
\red255\green0\blue0;
\red255\green0\blue255;
\red255\green255\blue0;
\red255\green255\blue255;
";

        private static string GetRTFColorFromColorRef(NativeMethods.COLORREF colorref)
        {
            return string.Concat("\\red", colorref.R.ToString("D"),
                                 "\\green", colorref.G.ToString("D"),
                                 "\\blue", colorref.B.ToString("D"), ";");
        }

        private static string GetColorTable()
        {
            var handle = NativeMethods.GetStdHandle((uint) StandardHandleId.Output);
            var csbe = new NativeMethods.CONSOLE_SCREEN_BUFFER_INFO_EX
            {
                cbSize = Marshal.SizeOf(typeof(NativeMethods.CONSOLE_SCREEN_BUFFER_INFO_EX))
            };
            if (NativeMethods.GetConsoleScreenBufferInfoEx(handle, ref csbe))
            {
                return GetRTFColorFromColorRef(csbe.Black) +
                       GetRTFColorFromColorRef(csbe.DarkBlue) +
                       GetRTFColorFromColorRef(csbe.DarkGreen) +
                       GetRTFColorFromColorRef(csbe.DarkCyan) +
                       GetRTFColorFromColorRef(csbe.DarkRed) +
                       GetRTFColorFromColorRef(csbe.DarkMagenta) +
                       GetRTFColorFromColorRef(csbe.DarkYellow) +
                       GetRTFColorFromColorRef(csbe.Gray) +
                       GetRTFColorFromColorRef(csbe.DarkGray) +
                       GetRTFColorFromColorRef(csbe.Blue) +
                       GetRTFColorFromColorRef(csbe.Green) +
                       GetRTFColorFromColorRef(csbe.Cyan) +
                       GetRTFColorFromColorRef(csbe.Red) +
                       GetRTFColorFromColorRef(csbe.Magenta) +
                       GetRTFColorFromColorRef(csbe.Yellow) +
                       GetRTFColorFromColorRef(csbe.White);
            }

            // A bit of a hack if the above failed - assume PowerShell's color scheme if the
            // background color is Magenta, otherwise we assume the default scheme.
            return _singleton._console.BackgroundColor == ConsoleColor.DarkMagenta
                ? PowerShellColorTable
                : CmdColorTable;
        }

        private static void DumpScreenToClipboard(int top, int count)
        {
            var buffer = ReadBufferLines(top, count);
            var bufferWidth = _singleton._console.BufferWidth;

            var dataObject = new DataObject();
            var textBuffer = new StringBuilder(buffer.Length + count);

            var rtfBuffer = new StringBuilder();
            rtfBuffer.Append(@"{\rtf\ansi{\fonttbl{\f0 Consolas;}}");

            var colorTable = GetColorTable();
            rtfBuffer.AppendFormat(@"{{\colortbl;{0}}}{1}", colorTable, Environment.NewLine);
            rtfBuffer.Append(@"\f0 \fs18 ");

            var charInfo = buffer[0];
            var fgColor = (int)charInfo.ForegroundColor;
            var bgColor = (int)charInfo.BackgroundColor;
            rtfBuffer.AppendFormat(@"{{\cf{0}\chshdng0\chcbpat{1} ", fgColor + 1, bgColor + 1);
            for (int i = 0; i < count; i++)
            {
                var spaces = 0;
                var rtfSpaces = 0;
                for (int j = 0; j < bufferWidth; j++)
                {
                    charInfo = buffer[i * bufferWidth + j];
                    if ((int)charInfo.ForegroundColor != fgColor || (int)charInfo.BackgroundColor != bgColor)
                    {
                        if (rtfSpaces > 0)
                        {
                            rtfBuffer.Append(' ', rtfSpaces);
                            rtfSpaces = 0;
                        }
                        fgColor = (int)charInfo.ForegroundColor;
                        bgColor = (int)charInfo.BackgroundColor;
                        rtfBuffer.AppendFormat(@"}}{{\cf{0}\chshdng0\chcbpat{1} ", fgColor + 1, bgColor + 1);
                    }

                    var c = (char)charInfo.UnicodeChar;
                    if (c == ' ')
                    {
                        // Trailing spaces are skipped, we'll add them back if we find a non-space
                        // before the end of line
                        ++spaces;
                        ++rtfSpaces;
                    }
                    else
                    {
                        if (spaces > 0)
                        {
                            textBuffer.Append(' ', spaces);
                            spaces = 0;
                        }
                        if (rtfSpaces > 0)
                        {
                            rtfBuffer.Append(' ', rtfSpaces);
                            rtfSpaces = 0;
                        }

                        textBuffer.Append(c);
                        switch (c)
                        {
                        case '\\': rtfBuffer.Append(@"\\"); break;
                        case '\t': rtfBuffer.Append(@"\tab"); break;
                        case '{':  rtfBuffer.Append(@"\{"); break;
                        case '}':  rtfBuffer.Append(@"\}"); break;
                        default:   rtfBuffer.Append(c); break;
                        }
                    }
                }
                rtfBuffer.AppendFormat(@"\shading0 \cbpat{0} \par{1}", bgColor + 1, Environment.NewLine);
                textBuffer.Append(Environment.NewLine);
            }
            rtfBuffer.Append("}}");

            dataObject.SetData(DataFormats.Text, textBuffer.ToString());
            dataObject.SetData(DataFormats.Rtf, rtfBuffer.ToString());
            ExecuteOnSTAThread(() => Clipboard.SetDataObject(dataObject, copy: true));
        }
    }
}
