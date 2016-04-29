/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.PowerShell.Internal
{
#pragma warning disable 1591

    internal static class NativeMethods
    {
        public const uint MAPVK_VK_TO_VSC   = 0x00;
        public const uint MAPVK_VSC_TO_VK   = 0x01;
        public const uint MAPVK_VK_TO_CHAR  = 0x02;
        
        public const byte VK_SHIFT          = 0x10;
        public const byte VK_CONTROL        = 0x11;
        public const byte VK_ALT            = 0x12;
        public const uint MENU_IS_ACTIVE    = 0x01;
        public const uint MENU_IS_INACTIVE  = 0x00; // windows key

        public const uint ENABLE_PROCESSED_INPUT = 0x0001;
        public const uint ENABLE_LINE_INPUT      = 0x0002;
        public const uint ENABLE_WINDOW_INPUT    = 0x0008;
        public const uint ENABLE_MOUSE_INPUT     = 0x0010;

        public const int FontTypeMask = 0x06;
        public const int TrueTypeFont = 0x04;

        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);  // WinBase.h

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetStdHandle(uint handleId);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetConsoleMode(IntPtr hConsoleOutput, out uint dwMode);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetConsoleMode(IntPtr hConsoleOutput, uint dwMode);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ScrollConsoleScreenBuffer(IntPtr hConsoleOutput,
            ref SMALL_RECT lpScrollRectangle,
            IntPtr lpClipRectangle,
            COORD dwDestinationOrigin,
            ref CHAR_INFO lpFill);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool WriteConsole(IntPtr hConsoleOutput, string lpBuffer, uint nNumberOfCharsToWrite, out uint lpNumberOfCharsWritten, IntPtr lpReserved);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetConsoleCtrlHandler(BreakHandler handlerRoutine, bool add);

        [DllImport("KERNEL32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool WriteConsoleOutput(IntPtr consoleOutput, CHAR_INFO[] buffer, COORD bufferSize, COORD bufferCoord, ref SMALL_RECT writeRegion);

        [DllImport("KERNEL32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ReadConsoleOutput(IntPtr consoleOutput, [Out] CHAR_INFO[] buffer, COORD bufferSize, COORD bufferCoord, ref SMALL_RECT readRegion);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int ToUnicode(uint uVirtKey, uint uScanCode, byte[] lpKeyState,
           [MarshalAs(UnmanagedType.LPArray)] [Out] char[] chars, int charMaxCount, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern short VkKeyScan(char @char);

        [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        public static extern uint GetConsoleOutputCP();

        [DllImport("User32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("GDI32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool TranslateCharsetInfo(IntPtr src, out CHARSETINFO Cs, uint options);

        [DllImport("GDI32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool GetTextMetrics(IntPtr hdc, out TEXTMETRIC tm);

        [DllImport("GDI32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool GetCharWidth32(IntPtr hdc, uint first, uint last, out int width);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateFile
        (
            string fileName,
            uint desiredAccess,
            uint ShareModes,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFileWin32Handle
        );

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool GetCurrentConsoleFontEx(IntPtr consoleOutput, bool bMaximumWindow, ref CONSOLE_FONT_INFO_EX consoleFontInfo);

        [StructLayout(LayoutKind.Sequential)]
        internal struct COLORREF
        {
            internal uint ColorDWORD;

            internal uint R
            {
                get { return ColorDWORD & 0xff; }
            }

            internal uint G
            {
                get { return (ColorDWORD >> 8) & 0xff; }
            }

            internal uint B
            {
                get { return (ColorDWORD >> 16) & 0xff; }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CONSOLE_SCREEN_BUFFER_INFO_EX
        {
            internal int cbSize;
            internal COORD dwSize;
            internal COORD dwCursorPosition;
            internal ushort wAttributes;
            internal SMALL_RECT srWindow;
            internal COORD dwMaximumWindowSize;
            internal ushort wPopupAttributes;
            internal bool bFullscreenSupported;
            internal COLORREF Black;
            internal COLORREF DarkBlue;
            internal COLORREF DarkGreen;
            internal COLORREF DarkCyan;
            internal COLORREF DarkRed;
            internal COLORREF DarkMagenta;
            internal COLORREF DarkYellow;
            internal COLORREF Gray;
            internal COLORREF DarkGray;
            internal COLORREF Blue;
            internal COLORREF Green;
            internal COLORREF Cyan;
            internal COLORREF Red;
            internal COLORREF Magenta;
            internal COLORREF Yellow;
            internal COLORREF White;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput,
            ref CONSOLE_SCREEN_BUFFER_INFO_EX csbe);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput,
            ref CONSOLE_SCREEN_BUFFER_INFO_EX csbe);
    }

    public delegate bool BreakHandler(ConsoleBreakSignal ConsoleBreakSignal);

    public enum ConsoleBreakSignal : uint
    {
        CtrlC     = 0,
        CtrlBreak = 1,
        Close     = 2,
        Logoff    = 5,
        Shutdown  = 6,
        None      = 255,
    }

    internal enum CHAR_INFO_Attributes : ushort
    {
        COMMON_LVB_LEADING_BYTE = 0x0100,
        COMMON_LVB_TRAILING_BYTE = 0x0200
    }

    public enum StandardHandleId : uint
    {
        Error  = unchecked((uint)-12),
        Output = unchecked((uint)-11),
        Input  = unchecked((uint)-10),
    }

    [Flags]
    internal enum AccessQualifiers : uint
    {
        // From winnt.h
        GenericRead = 0x80000000,
        GenericWrite = 0x40000000
    }

    internal enum CreationDisposition : uint
    {
        // From winbase.h
        CreateNew = 1,
        CreateAlways = 2,
        OpenExisting = 3,
        OpenAlways = 4,
        TruncateExisting = 5
    }

    [Flags]
    internal enum ShareModes : uint
    {
        // From winnt.h
        ShareRead = 0x00000001,
        ShareWrite = 0x00000002
    }

    public struct SMALL_RECT
    {
        public short Left;
        public short Top;
        public short Right;
        public short Bottom;

#if !CORECLR
        [ExcludeFromCodeCoverage]
#endif
        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", Left, Top, Right, Bottom);
        }
    }

    internal struct COORD
    {
        public short X;
        public short Y;

#if !CORECLR
        [ExcludeFromCodeCoverage]
#endif
        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{0},{1}", X, Y);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FONTSIGNATURE
    {
        //From public\sdk\inc\wingdi.h

        // fsUsb*: A 128-bit Unicode subset bitfield (USB) identifying up to 126 Unicode subranges
        internal uint fsUsb0;
        internal uint fsUsb1;
        internal uint fsUsb2;
        internal uint fsUsb3;
        // fsCsb*: A 64-bit, code-page bitfield (CPB) that identifies a specific character set or code page.
        internal uint fsCsb0;
        internal uint fsCsb1;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CHARSETINFO
    {
        //From public\sdk\inc\wingdi.h
        internal uint ciCharset;   // Character set value.
        internal uint ciACP;       // ANSI code-page identifier.
        internal FONTSIGNATURE fs;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct TEXTMETRIC
    {
        //From public\sdk\inc\wingdi.h
        public int tmHeight;
        public int tmAscent;
        public int tmDescent;
        public int tmInternalLeading;
        public int tmExternalLeading;
        public int tmAveCharWidth;
        public int tmMaxCharWidth;
        public int tmWeight;
        public int tmOverhang;
        public int tmDigitizedAspectX;
        public int tmDigitizedAspectY;
        public char tmFirstChar;
        public char tmLastChar;
        public char tmDefaultChar;
        public char tmBreakChar;
        public byte tmItalic;
        public byte tmUnderlined;
        public byte tmStruckOut;
        public byte tmPitchAndFamily;
        public byte tmCharSet;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct CONSOLE_FONT_INFO_EX
    {
        internal int cbSize;
        internal int nFont;
        internal short FontWidth;
        internal short FontHeight;
        internal int FontFamily;
        internal int FontWeight;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        internal string FontFace;
    }

    public struct CHAR_INFO
    {
#if CORECLR
        public char UnicodeChar;
        public ConsoleColor ForegroundColor;
        public ConsoleColor BackgroundColor;

        public CHAR_INFO(char c, ConsoleColor foreground, ConsoleColor background)
        {
            UnicodeChar = c;
            ForegroundColor = foreground;
            BackgroundColor = background;
        }
#else
        public ushort UnicodeChar;
        public ushort Attributes;

        public CHAR_INFO(char c, ConsoleColor foreground, ConsoleColor background)
        {
            UnicodeChar = c;
            Attributes = (ushort)(((int)background << 4) | (int)foreground);
        }

        [ExcludeFromCodeCoverage]
        public ConsoleColor ForegroundColor
        {
            get { return (ConsoleColor)(Attributes & 0xf); }
            set { Attributes = (ushort)((Attributes & 0xfff0) | ((int)value & 0xf)); }
        }

        [ExcludeFromCodeCoverage]
        public ConsoleColor BackgroundColor
        {
            get { return (ConsoleColor)((Attributes & 0xf0) >> 4); }
            set { Attributes = (ushort)((Attributes & 0xff0f) | (((int)value & 0xf) << 4)); }
        }

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append((char)UnicodeChar);
            if (ForegroundColor != Console.ForegroundColor)
                sb.AppendFormat(" fg: {0}", ForegroundColor);
            if (BackgroundColor != Console.BackgroundColor)
                sb.AppendFormat(" bg: {0}", BackgroundColor);
            return sb.ToString();
        }

        [ExcludeFromCodeCoverage]
        public override bool Equals(object obj)
        {
            if (!(obj is CHAR_INFO))
            {
                return false;
            }

            var other = (CHAR_INFO)obj;
            return this.UnicodeChar == other.UnicodeChar && this.Attributes == other.Attributes;
        }

        [ExcludeFromCodeCoverage]
        public override int GetHashCode()
        {
            return UnicodeChar.GetHashCode() + Attributes.GetHashCode();
        }
#endif
    }

    internal static class ConsoleKeyInfoExtension 
    {
        public static string ToGestureString(this ConsoleKeyInfo key)
        {
            var mods = key.Modifiers;

            var sb = new StringBuilder();
            if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                sb.Append("Ctrl");
            }
            if (key.Modifiers.HasFlag(ConsoleModifiers.Alt))
            {
                if (sb.Length > 0)
                    sb.Append("+");
                sb.Append("Alt");
            }

#if CORECLR
            if (sb.Length > 0)
                sb.Append("+");
            if ((key.Key >= ConsoleKey.D0 && key.Key <= ConsoleKey.D9)
                || (key.Key >= ConsoleKey.Oem1 && key.Key <= ConsoleKey.Oem8))
            {
                sb.Append(key.KeyChar);
            }
            else
            {
                if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                {
                    sb.Append("Shift+");
                }
                sb.Append(key.Key);
            }
#else
            char c = ConsoleKeyChordConverter.GetCharFromConsoleKey(key.Key,
                (mods & ConsoleModifiers.Shift) != 0 ? ConsoleModifiers.Shift : 0);
            if (char.IsControl(c) || char.IsWhiteSpace(c))
            {
                if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                {
                    if (sb.Length > 0)
                        sb.Append("+");
                    sb.Append("Shift");
                }
                if (sb.Length > 0)
                    sb.Append("+");
                sb.Append(key.Key);
            }
            else
            {
                if (sb.Length > 0)
                    sb.Append("+");
                sb.Append(c);
            }
#endif
            return sb.ToString();
        }
    }

    internal class ConhostConsole : IConsole
    {
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr _hwnd = (IntPtr)0;
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr _hDC = (IntPtr)0;
        private uint _codePage;
        private bool _istmInitialized = false;
        private TEXTMETRIC _tm = new TEXTMETRIC();
        private bool _trueTypeInUse = false;

        private readonly Lazy<SafeFileHandle> _outputHandle = new Lazy<SafeFileHandle>(() =>
        {
            // We use CreateFile here instead of GetStdWin32Handle, as GetStdWin32Handle will return redirected handles
            var handle = NativeMethods.CreateFile(
                "CONOUT$",
                (UInt32)(AccessQualifiers.GenericRead | AccessQualifiers.GenericWrite),
                (UInt32)ShareModes.ShareWrite,
                (IntPtr)0,
                (UInt32)CreationDisposition.OpenExisting,
                0,
                (IntPtr)0);

            if (handle == NativeMethods.INVALID_HANDLE_VALUE)
            {
                int err = Marshal.GetLastWin32Error();
                Win32Exception innerException = new Win32Exception(err);
                throw new Exception("Failed to retreive the input console handle.", innerException);
            }

            return new SafeFileHandle(handle, true);
        }
        );

        private readonly Lazy<SafeFileHandle> _inputHandle = new Lazy<SafeFileHandle>(() =>
        {
            // We use CreateFile here instead of GetStdWin32Handle, as GetStdWin32Handle will return redirected handles
            var handle = NativeMethods.CreateFile(
                "CONIN$",
                (UInt32)(AccessQualifiers.GenericRead | AccessQualifiers.GenericWrite),
                (UInt32)ShareModes.ShareWrite,
                (IntPtr)0,
                (UInt32)CreationDisposition.OpenExisting,
                0,
                (IntPtr)0);

            if (handle == NativeMethods.INVALID_HANDLE_VALUE)
            {
                int err = Marshal.GetLastWin32Error();
                Win32Exception innerException = new Win32Exception(err);
                throw new Exception("Failed to retreive the input console handle.", innerException);
            }

            return new SafeFileHandle(handle, true);
        });

        public uint GetConsoleInputMode()
        {
            var handle = _inputHandle.Value.DangerousGetHandle();
            uint result;
            NativeMethods.GetConsoleMode(handle, out result);
            return result;
        }

        public void SetConsoleInputMode(uint mode)
        {
            var handle = _inputHandle.Value.DangerousGetHandle();
            NativeMethods.SetConsoleMode(handle, mode);
        }

        public ConsoleKeyInfo ReadKey()
        {
            return Console.ReadKey(true);
        }

        public bool KeyAvailable
        {
            get { return Console.KeyAvailable; }
        }

        public int CursorLeft
        {
            get { return Console.CursorLeft; }
            set { Console.CursorLeft = value; }
        }

        public int CursorTop
        {
            get { return Console.CursorTop; }
            set { Console.CursorTop = value; }
        }

        public int CursorSize
        {
            get { return Console.CursorSize; }
            set { Console.CursorSize = value; }
        }

        public int BufferWidth
        {
            get { return Console.BufferWidth; }
            set { Console.BufferWidth = value; }
        }

        public int BufferHeight
        {
            get { return Console.BufferHeight; }
            set { Console.BufferHeight = value; }
        }

        public int WindowWidth
        {
            get { return Console.WindowWidth; }
            set { Console.WindowWidth = value; }
        }

        public int WindowHeight
        {
            get { return Console.WindowHeight; }
            set { Console.WindowHeight = value; }
        }

        public int WindowTop
        {
            get { return Console.WindowTop; }
            set { Console.WindowTop = value; }
        }

        public ConsoleColor BackgroundColor
        {
            get { return Console.BackgroundColor; }
            set { Console.BackgroundColor = value; }
        }

        public ConsoleColor ForegroundColor
        {
            get { return Console.ForegroundColor; }
            set { Console.ForegroundColor = value; }
        }

        public void SetWindowPosition(int left, int top)
        {
            Console.SetWindowPosition(left, top);
        }

        public void SetCursorPosition(int left, int top)
        {
            Console.SetCursorPosition(left, top);
        }

        public void Write(string value)
        {
            Console.Write(value);
        }

        public void WriteLine(string value)
        {
            Console.WriteLine(value);
        }

        public void WriteBufferLines(CHAR_INFO[] buffer, ref int top)
        {
            WriteBufferLines(buffer, ref top, true);
        }

        public void WriteBufferLines(CHAR_INFO[] buffer, ref int top, bool ensureBottomLineVisible)
        {
#if !CORECLR
            var handle = NativeMethods.GetStdHandle((uint) StandardHandleId.Output);
#endif
            int bufferWidth = Console.BufferWidth;
            int bufferLineCount = buffer.Length / bufferWidth;
            if ((top + bufferLineCount) > Console.BufferHeight)
            {
                var scrollCount = (top + bufferLineCount) - Console.BufferHeight;
                ScrollBuffer(scrollCount);
                top -= scrollCount;
            }
#if CORECLR
            ConsoleColor foregroundColor = Console.ForegroundColor;
            ConsoleColor backgroundColor = Console.BackgroundColor;

            Console.SetCursorPosition(0, (top>=0) ? top : 0);

            for (int i = 0; i < buffer.Length; ++i)
            {
                Console.ForegroundColor =  buffer[i].ForegroundColor;
                Console.BackgroundColor =  buffer[i].BackgroundColor;

                Console.Write((char)buffer[i].UnicodeChar);
            }

            Console.BackgroundColor = backgroundColor;
            Console.ForegroundColor = foregroundColor;
#else
            var bufferSize = new COORD
            {
                X = (short) bufferWidth,
                Y = (short) bufferLineCount
            };
            var bufferCoord = new COORD {X = 0, Y = 0};
            var bottom = top + bufferLineCount - 1;
            var writeRegion = new SMALL_RECT
            {
                Top = (short) top,
                Left = 0,
                Bottom = (short) bottom,
                Right = (short) (bufferWidth - 1)
            };
            NativeMethods.WriteConsoleOutput(handle, buffer,
                                             bufferSize, bufferCoord, ref writeRegion);

            // Now make sure the bottom line is visible
            if (ensureBottomLineVisible &&
                (bottom >= (Console.WindowTop + Console.WindowHeight)))
            {
                Console.CursorTop = bottom;
            }
#endif
        }

        public void ScrollBuffer(int lines)
        {
#if CORECLR
            for (int i=0; i<lines; ++i)
            {
                Console.SetCursorPosition(Console.BufferWidth, Console.BufferHeight - 1);
                Console.WriteLine();
            }
#else
            var handle = NativeMethods.GetStdHandle((uint) StandardHandleId.Output);

            var scrollRectangle = new SMALL_RECT
            {
                Top = (short) lines,
                Left = 0,
                Bottom = (short)(Console.BufferHeight - 1),
                Right = (short)Console.BufferWidth
            };
            var destinationOrigin = new COORD {X = 0, Y = 0};
            var fillChar = new CHAR_INFO(' ', Console.ForegroundColor, Console.BackgroundColor);
            NativeMethods.ScrollConsoleScreenBuffer(handle, ref scrollRectangle, IntPtr.Zero, destinationOrigin, ref fillChar);
#endif
        }

        public CHAR_INFO[] ReadBufferLines(int top, int count)
        {
            var result = new CHAR_INFO[BufferWidth * count];
#if CORECLR
            for (int i=0; i<BufferWidth*count; ++i)
            {
                result[i].UnicodeChar = ' ';
                result[i].ForegroundColor = Console.ForegroundColor;
                result[i].BackgroundColor = Console.BackgroundColor;
            }
#else
            var handle = NativeMethods.GetStdHandle((uint) StandardHandleId.Output);

            var readBufferSize = new COORD {
                X = (short)BufferWidth,
                Y = (short)count};
            var readBufferCoord = new COORD {X = 0, Y = 0};
            var readRegion = new SMALL_RECT
            {
                Top = (short)top,
                Left = 0,
                Bottom = (short)(top + count),
                Right = (short)(BufferWidth - 1)
            };
            NativeMethods.ReadConsoleOutput(handle, result,
                readBufferSize, readBufferCoord, ref readRegion);
#endif
            return result;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods",
            Justification = "Then the API we pass the handle to will return an error if it is invalid. They are not exposed.")]
        internal static CONSOLE_FONT_INFO_EX GetConsoleFontInfo(SafeFileHandle consoleHandle)
        {

            CONSOLE_FONT_INFO_EX fontInfo = new CONSOLE_FONT_INFO_EX();
            fontInfo.cbSize = Marshal.SizeOf(fontInfo);
            bool result = NativeMethods.GetCurrentConsoleFontEx(consoleHandle.DangerousGetHandle(), false, ref fontInfo);

            if (result == false)
            {
                int err = Marshal.GetLastWin32Error();
                Win32Exception innerException = new Win32Exception(err);
                throw new Exception("Failed to get console font information.", innerException);
            }
            return fontInfo;
        }

        public int LengthInBufferCells(char c)
        {
            if (!IsCJKOutputCodePage() || !_trueTypeInUse)
                return 1;

            return LengthInBufferCellsFE(c);
        }

        internal static bool IsAnyDBCSCharSet(uint charSet)
        {
            const uint SHIFTJIS_CHARSET = 128;
            const uint HANGEUL_CHARSET = 129;
            const uint CHINESEBIG5_CHARSET = 136;
            const uint GB2312_CHARSET = 134;
            return charSet == SHIFTJIS_CHARSET || charSet == HANGEUL_CHARSET ||
                   charSet == CHINESEBIG5_CHARSET || charSet == GB2312_CHARSET;
        }

        internal uint CodePageToCharSet()
        {
            CHARSETINFO csi;
            const uint TCI_SRCCODEPAGE = 2;
            const uint OEM_CHARSET = 255;
            if (!NativeMethods.TranslateCharsetInfo((IntPtr)_codePage, out csi, TCI_SRCCODEPAGE))
            {
                csi.ciCharset = OEM_CHARSET;
            }
            return csi.ciCharset;
        }

        /// <summary>
        /// Check if the output buffer code page is Japanese, Simplified Chinese, Korean, or Traditional Chinese
        /// </summary>
        /// <returns>true if it is CJK code page; otherwise, false.</returns>
        internal bool IsCJKOutputCodePage()
        {
            return _codePage == 932 || // Japanese
                   _codePage == 936 || // Simplified Chinese
                   _codePage == 949 || // Korean
                   _codePage == 950;  // Traditional Chinese
        }

        internal bool IsAvailableFarEastCodePage()
        {
            uint charSet = CodePageToCharSet();
            return IsAnyDBCSCharSet(charSet);
        }

        internal int LengthInBufferCellsFE(char c)
        {
            if (0x20 <= c && c <= 0x7e)
            {
                /* ASCII */
                return 1;
            }
            else if (0x3041 <= c && c <= 0x3094)
            {
                /* Hiragana */
                return 2;
            }
            else if (0x30a1 <= c && c <= 0x30f6)
            {
                /* Katakana */
                return 2;
            }
            else if (0x3105 <= c && c <= 0x312c)
            {
                /* Bopomofo */
                return 2;
            }
            else if (0x3131 <= c && c <= 0x318e)
            {
                /* Hangul Elements */
                return 2;
            }
            else if (0xac00 <= c && c <= 0xd7a3)
            {
                /* Korean Hangul Syllables */
                return 2;
            }
            else if (0xff01 <= c && c <= 0xff5e)
            {
                /* Fullwidth ASCII variants */
                return 2;
            }
            else if (0xff61 <= c && c <= 0xff9f)
            {
                /* Halfwidth Katakana variants */
                return 1;
            }
            else if ((0xffa0 <= c && c <= 0xffbe) ||
                     (0xffc2 <= c && c <= 0xffc7) ||
                     (0xffca <= c && c <= 0xffcf) ||
                     (0xffd2 <= c && c <= 0xffd7) ||
                     (0xffda <= c && c <= 0xffdc))
            {
                /* Halfwidth Hangule variants */
                return 1;
            }
            else if (0xffe0 <= c && c <= 0xffe6)
            {
                /* Fullwidth symbol variants */
                return 2;
            }
            else if (0x4e00 <= c && c <= 0x9fa5)
            {
                /* Han Ideographic */
                return 2;
            }
            else if (0xf900 <= c && c <= 0xfa2d)
            {
                /* Han Compatibility Ideographs */
                return 2;
            }
            else
            {
                /* Unknown character: need to use GDI*/
                if (_hDC == (IntPtr)0)
                {
                    _hwnd = NativeMethods.GetConsoleWindow();
                    if ((IntPtr)0 == _hwnd)
                    {
                        return 1;
                    }
                    _hDC = NativeMethods.GetDC(_hwnd);
                    if ((IntPtr)0 == _hDC)
                    {
                        //Don't throw exception so that output can continue
                        return 1;
                    }
                }
                bool result = true;
                if (!_istmInitialized)
                {
                    result = NativeMethods.GetTextMetrics(_hDC, out _tm);
                    if (!result)
                    {
                        return 1;
                    }
                    _istmInitialized = true;
                }
                int width;
                result = NativeMethods.GetCharWidth32(_hDC, (uint)c, (uint)c, out width);
                if (!result)
                {
                    return 1;
                }
                if (width >= _tm.tmMaxCharWidth)
                {
                    return 2;
                }
            }
            return 1;
        }

        public void StartRender()
        {
            _codePage = NativeMethods.GetConsoleOutputCP();
            _istmInitialized = false;
            var consoleHandle = _outputHandle.Value;
            CONSOLE_FONT_INFO_EX fontInfo = ConhostConsole.GetConsoleFontInfo(consoleHandle);
            int fontType = fontInfo.FontFamily & NativeMethods.FontTypeMask;
            _trueTypeInUse = (fontType & NativeMethods.TrueTypeFont) == NativeMethods.TrueTypeFont;

        }

        public void EndRender()
        {
            if (_hwnd != (IntPtr)0 && _hDC != (IntPtr)0)
            {
                NativeMethods.ReleaseDC(_hwnd, _hDC);
            }
        }

#if CORECLR
        public void Clear()
        {
            Console.Clear();
        }
#endif
    }

#pragma warning restore 1591
}
