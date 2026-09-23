// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System;
using System.ComponentModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConsoleHandle = Microsoft.Win32.SafeHandles.SafeFileHandle;
using Dbg = System.Management.Automation.Diagnostics;
using WORD = System.UInt16;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// Implementation of RawConsole for powershell.
    /// </summary>
    internal sealed
    class ConsoleHostRawUserInterface : System.Management.Automation.Host.PSHostRawUserInterface
    {
        /// <summary>
        /// </summary>
        /// <exception cref="HostException">
        /// If obtaining the buffer's foreground and background color failed
        /// </exception>
        internal
        ConsoleHostRawUserInterface(ConsoleHostUserInterface mshConsole) : base()
        {
            defaultForeground = ForegroundColor;
            defaultBackground = BackgroundColor;
            parent = mshConsole;
            // cacheKeyEvent is a value type and initialized automatically

            // add "Administrator: " prefix into the window title, but don't wait for it to finish
            //   (we may load resources which can take some time)
            Task.Run(() =>
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    // Check if the window already has the "Administrator: " prefix (i.e. from the parent console process).
                    ReadOnlySpan<char> prefix = ConsoleHostRawUserInterfaceStrings.WindowTitleElevatedPrefix;
                    ReadOnlySpan<char> windowTitle = WindowTitle;
                    if (!windowTitle.StartsWith(prefix))
                    {
                        WindowTitle = string.Concat(prefix, windowTitle);
                    }
                }
            });
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <value></value>
        /// <exception cref="ArgumentException">
        /// If set to an invalid ConsoleColor
        /// </exception>
        /// <exception cref="HostException">
        /// If obtaining information about the buffer failed
        ///    OR
        ///    Win32's SetConsoleTextAttribute
        /// </exception>
        public override
        ConsoleColor
        ForegroundColor
        {
            get
            {
                ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo;
                GetBufferInfo(out bufferInfo);

                ConsoleColor foreground;

                ConsoleControl.WORDToColor(bufferInfo.Attributes, out foreground, out _);
                return foreground;
            }

            set
            {
                if (ConsoleControl.IsConsoleColor(value))
                {
                    ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo;

                    ConsoleHandle handle = GetBufferInfo(out bufferInfo);

                    // mask in the foreground from the current color.
                    short a = (short)bufferInfo.Attributes;

                    a &= (short)~0x0f;
                    a = (short)((ushort)a | (ushort)value);
                    ConsoleControl.SetConsoleTextAttribute(handle, (WORD)a);
                }
                else
                {
                    throw PSTraceSource.NewArgumentException("value", ConsoleHostRawUserInterfaceStrings.InvalidConsoleColorError);
                }
            }
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <value></value>
        /// <exception cref="ArgumentException">
        /// If set to an invalid ConsoleColor
        /// </exception>
        /// <exception cref="HostException">
        /// If obtaining information about the buffer failed
        ///    OR
        ///    Win32's SetConsoleTextAttribute
        /// </exception>
        public override
        ConsoleColor
        BackgroundColor
        {
            get
            {
                ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo;
                GetBufferInfo(out bufferInfo);

                ConsoleColor background;

                ConsoleControl.WORDToColor(bufferInfo.Attributes, out _, out background);
                return background;
            }

            set
            {
                if (ConsoleControl.IsConsoleColor(value))
                {
                    ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo;

                    ConsoleHandle handle = GetBufferInfo(out bufferInfo);

                    // mask in the background from the current color.
                    short a = (short)bufferInfo.Attributes;

                    a &= (short)~0xf0;
                    a = (short)((ushort)a | (ushort)((uint)value << 4));
                    ConsoleControl.SetConsoleTextAttribute(handle, (WORD)a);
                }
                else
                {
                    throw PSTraceSource.NewArgumentException("value", ConsoleHostRawUserInterfaceStrings.InvalidConsoleColorError);
                }
            }
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <value></value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If set to outside of the buffer
        /// </exception>
        /// <exception cref="HostException">
        /// If obtaining information about the buffer failed
        ///    OR
        ///    Win32's SetConsoleCursorPosition failed
        /// </exception>
        public override
        Coordinates
        CursorPosition
        {
            get
            {
                ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo;
                GetBufferInfo(out bufferInfo);

                Coordinates c = new Coordinates(bufferInfo.CursorPosition.X, bufferInfo.CursorPosition.Y);
                return c;
            }

            set
            {
                try
                {
                    Console.SetCursorPosition(value.X, value.Y);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // if screen buffer has changed, we cannot set it anywhere reasonable as the screen buffer
                    // might change again, so we ignore this
                }
            }
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <value>
        /// Cursor size
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If set to under 0 or over 100
        /// </exception>
        /// <exception cref="HostException">
        /// If obtaining a handle to the active screen buffer failed
        ///    OR
        ///    Win32's GetConsoleCursorInfo failed
        ///    OR
        ///    Win32's SetConsoleCursorInfo failed
        /// </exception>
        public override
        int
        CursorSize
        {
            get
            {
                ConsoleHandle consoleHandle = ConsoleControl.GetActiveScreenBufferHandle();
                int size = (int)ConsoleControl.GetConsoleCursorInfo(consoleHandle).Size;

                return size;
            }

            set
            {
                const int MinCursorSize = 0;
                const int MaxCursorSize = 100;

                if (value >= MinCursorSize && value <= MaxCursorSize)
                {
                    ConsoleHandle consoleHandle = ConsoleControl.GetActiveScreenBufferHandle();
                    ConsoleControl.CONSOLE_CURSOR_INFO cursorInfo =
                        ConsoleControl.GetConsoleCursorInfo(consoleHandle);

                    if (value == 0)
                    {
                        cursorInfo.Visible = false;
                    }
                    else
                    {
                        cursorInfo.Size = (uint)value;
                        cursorInfo.Visible = true;
                    }

                    ConsoleControl.SetConsoleCursorInfo(consoleHandle, cursorInfo);
                }
                else
                {
                    throw PSTraceSource.NewArgumentOutOfRangeException("value", value,
                        ConsoleHostRawUserInterfaceStrings.InvalidCursorSizeError);
                }
            }
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <value></value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If set outside of the buffer
        /// </exception>
        /// <exception cref="HostException">
        /// If obtaining information about the buffer failed
        ///    OR
        ///    Win32's SetConsoleWindowInfo failed
        /// </exception>
        public override
        Coordinates
        WindowPosition
        {
            get
            {
                ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo;
                GetBufferInfo(out bufferInfo);

                Coordinates c = new Coordinates(bufferInfo.WindowRect.Left, bufferInfo.WindowRect.Top);

                return c;
            }

            set
            {
                ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo;

                ConsoleHandle handle = GetBufferInfo(out bufferInfo);
                ConsoleControl.SMALL_RECT r = bufferInfo.WindowRect;

                // the dimensions of the window can't extend past the dimensions of the screen buffer.  This means that the
                // X position of the window is limited to the buffer width minus one minus the window width, and the Y
                // position of the window is limited to the buffer height minus one minus the window height.
                int windowWidth = r.Right - r.Left + 1;
                int windowHeight = r.Bottom - r.Top + 1;

                if (value.X < 0 || value.X > bufferInfo.BufferSize.X - windowWidth)
                {
                    throw PSTraceSource.NewArgumentOutOfRangeException("value.X", value.X,
                        ConsoleHostRawUserInterfaceStrings.InvalidXWindowPositionError);
                }

                if (value.Y < 0 || value.Y > bufferInfo.BufferSize.Y - windowHeight)
                {
                    throw PSTraceSource.NewArgumentOutOfRangeException("value.Y", value.Y,
                        ConsoleHostRawUserInterfaceStrings.InvalidYWindowPositionError);
                }

                r.Left = (short)value.X;
                r.Top = (short)value.Y;

                // subtract 1 from each dimension because the semantics of the win32 api are not "number of characters in
                // span" but "starting and ending position"
                r.Right = (short)(r.Left + windowWidth - 1);
                r.Bottom = (short)(r.Top + windowHeight - 1);
                Dbg.Assert(r.Right >= r.Left, "Window size is too narrow");
                Dbg.Assert(r.Bottom >= r.Top, "Window size is too short");
                ConsoleControl.SetConsoleWindowInfo(handle, true, r);
            }
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <value></value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If setting to an invalid size
        /// </exception>
        /// <exception cref="HostException">
        /// If obtaining a handle to the active screen buffer failed
        ///    OR
        ///    obtaining information about the buffer failed
        ///    OR
        ///    Win32's SetConsoleScreenBufferSize failed
        /// </exception>
        public override
        Size
        BufferSize
        {
            get
            {
                ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo;
                GetBufferInfo(out bufferInfo);
                return new Size(bufferInfo.BufferSize.X, bufferInfo.BufferSize.Y);
            }

            set
            {
                // looking in windows/core/ntcon/server/output.c, it looks like the minimum size is 1 row X however many
                // characters will fit in the minimum window size system metric (SM_CXMIN).  Instead of going to the effort of
                // computing that minimum here, it is cleaner and cheaper to make the call to SetConsoleScreenBuffer and just
                // translate any exception that might get thrown.
                try
                {
                    ConsoleHandle handle = ConsoleControl.GetActiveScreenBufferHandle();
                    ConsoleControl.SetConsoleScreenBufferSize(handle, value);
                }
                catch (HostException e)
                {
                    if (e.InnerException is Win32Exception win32exception &&
                        win32exception.NativeErrorCode == 0x57)
                    {
                        throw PSTraceSource.NewArgumentOutOfRangeException("value", value,
                            ConsoleHostRawUserInterfaceStrings.InvalidBufferSizeError);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <value></value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If setting width or height to less than 1, larger than the screen buffer,
        ///  over the maximum window size allowed
        /// </exception>
        /// <exception cref="HostException">
        /// If obtaining information about the buffer failed
        ///    OR
        ///    Win32's SetConsoleWindowInfo failed
        /// </exception>
        public override
        Size
        WindowSize
        {
            get
            {
                ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo;
                GetBufferInfo(out bufferInfo);

                Size s =
                    new Size(
                        bufferInfo.WindowRect.Right - bufferInfo.WindowRect.Left + 1,
                        bufferInfo.WindowRect.Bottom - bufferInfo.WindowRect.Top + 1);

                return s;
            }

            set
            {
                ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo;

                ConsoleHandle handle = GetBufferInfo(out bufferInfo);

                // the dimensions of the window can't extend past the dimensions of the screen buffer.  This means that the
                // width of the window is limited to the buffer width minus one minus the window X position, and the height
                // of the window is limited to the buffer height minus one minus the window Y position.

                if (value.Width < 1)
                {
                    throw PSTraceSource.NewArgumentOutOfRangeException("value.Width", value.Width,
                        ConsoleHostRawUserInterfaceStrings.WindowWidthTooSmallError);
                }

                if (value.Height < 1)
                {
                    throw PSTraceSource.NewArgumentOutOfRangeException("value.Height", value.Height,
                        ConsoleHostRawUserInterfaceStrings.WindowHeightTooSmallError);
                }

                if (value.Width > bufferInfo.BufferSize.X)
                {
                    throw PSTraceSource.NewArgumentOutOfRangeException("value.Width", value.Width,
                        ConsoleHostRawUserInterfaceStrings.WindowWidthLargerThanBufferError);
                }

                if (value.Height > bufferInfo.BufferSize.Y)
                {
                    throw PSTraceSource.NewArgumentOutOfRangeException("value.Height", value.Height,
                        ConsoleHostRawUserInterfaceStrings.WindowHeightLargerThanBufferError);
                }

                if (value.Width > bufferInfo.MaxWindowSize.X)
                {
                    throw PSTraceSource.NewArgumentOutOfRangeException("value.Width", value.Width,
                        ConsoleHostRawUserInterfaceStrings.WindowWidthTooLargeErrorTemplate,
                        bufferInfo.MaxWindowSize.X);
                }

                if (value.Height > bufferInfo.MaxWindowSize.Y)
                {
                    throw PSTraceSource.NewArgumentOutOfRangeException("value.Height", value.Height,
                        ConsoleHostRawUserInterfaceStrings.WindowHeightTooLargeErrorTemplate,
                        bufferInfo.MaxWindowSize.Y);
                }

                // if the new size will extend past the edge of screen buffer, then move the window position to try to
                // accommodate that.

                ConsoleControl.SMALL_RECT r = bufferInfo.WindowRect;

                // subtract 1 from each dimension because the semantics of the win32 api are not "number of characters in
                // span" but "starting and ending position"

                r.Right = (short)(r.Left + value.Width - 1);
                r.Bottom = (short)(r.Top + value.Height - 1);

                // Now we check if the bottom right coordinate of our window went over the coordinate of the bottom
                // right of the buffer.  If it did then we need to adjust the window.

                // bufferInfo.BufferSize.X - 1 will give us the rightmost coordinate of the buffer.
                // r.Right - rightCoordinateOfBuffer will give us how much we need to adjust the window left and right coordinates.
                // Then we can do the same for top and bottom.
                short adjustLeft = (short)(r.Right - (bufferInfo.BufferSize.X - 1));
                short adjustTop = (short)(r.Bottom - (bufferInfo.BufferSize.Y - 1));

                if (adjustLeft > 0)
                {
                    r.Left -= adjustLeft;
                    r.Right -= adjustLeft;
                }

                if (adjustTop > 0)
                {
                    r.Top -= adjustTop;
                    r.Bottom -= adjustTop;
                }

                if (r.Right < r.Left)
                {
                    throw PSTraceSource.NewArgumentOutOfRangeException("value", value,
                        ConsoleHostRawUserInterfaceStrings.WindowTooNarrowError);
                }

                if (r.Bottom < r.Top)
                {
                    throw PSTraceSource.NewArgumentOutOfRangeException("value", value,
                        ConsoleHostRawUserInterfaceStrings.WindowTooShortError);
                }

                ConsoleControl.SetConsoleWindowInfo(handle, true, r);
            }
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <value></value>
        /// <exception cref="HostException">
        /// If obtaining information about the buffer failed
        /// </exception>
        public override
        Size
        MaxWindowSize
        {
            get
            {
                ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo;
                GetBufferInfo(out bufferInfo);
                Size s = new Size(bufferInfo.MaxWindowSize.X, bufferInfo.MaxWindowSize.Y);

                return s;
            }
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <value></value>
        /// <exception cref="HostException">
        /// If obtaining a handle to the active screen buffer failed
        ///    OR
        ///    Win32's GetLargestConsoleWindowSize failed
        /// </exception>
        public override
        Size
        MaxPhysicalWindowSize
        {
            get
            {
                ConsoleHandle handle = ConsoleControl.GetActiveScreenBufferHandle();
                return ConsoleControl.GetLargestConsoleWindowSize(handle);
            }
        }

        /// <summary>
        /// Helper method to create and trace PipelineStoppedException.
        /// </summary>
        /// <returns></returns>
        private static PipelineStoppedException NewPipelineStoppedException()
        {
            PipelineStoppedException e = new PipelineStoppedException();
            return e;
        }

        /// <summary>
        /// Used by ReadKey, cache KeyEvent based on if input.RepeatCount > 1.
        /// </summary>
        /// <param name="input">Input key event record.</param>
        /// <param name="cache">Cache key event.</param>
        private static void CacheKeyEvent(ConsoleControl.KEY_EVENT_RECORD input, ref ConsoleControl.KEY_EVENT_RECORD cache)
        {
            if (input.RepeatCount > 1)
            {
                cache = input;
                cache.RepeatCount--;
            }
        }

        /// <summary>
        /// See base class
        /// This method unwraps the repeat count in KEY_EVENT_RECORD by caching repeated keystrokes
        /// in a logical queue. The implications are:
        /// 1) Per discussion with Sburns on 2005/01/20, calling this method with allowCtrlC | includeKeyUp may not
        ///    return ctrl-c even it is pressed. This is because ctrl-c could generate the following sequence of
        ///    key events: {Ctrl, KeyDown}, {Ctrl-c KeyDown}, {Ctrl, KeyUp}, {c, KeyUp} if Ctrl is released before c.
        ///    In this case, {Ctrl, KeyUp}, {c, KeyUp} would be returned.
        /// 2) If the cache is non-empty, a call to ReadLine will not return the cached keys. This
        ///    behavior is the same as that of System.Console.ReadKey.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// If neither IncludeKeyDown or IncludeKeyUp is set in <paramref name="options"/>
        /// </exception>
        /// <exception cref="HostException">
        /// If obtaining a handle to the active screen buffer failed
        ///    OR
        ///    Win32's setting input buffer mode to disregard window and mouse input failed
        ///    OR
        ///    Win32's ReadConsoleInput failed
        /// </exception>
        public override
        KeyInfo
        ReadKey(ReadKeyOptions options)
        {
            if ((options & (ReadKeyOptions.IncludeKeyDown | ReadKeyOptions.IncludeKeyUp)) == 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(options), ConsoleHostRawUserInterfaceStrings.InvalidReadKeyOptionsError);
            }

            // keyInfo is initialized in the below if-else statement
            KeyInfo keyInfo;

            if (cachedKeyEvent.RepeatCount > 0)
            {
                //    Ctrl-C is not allowed and Ctrl-C is cached.
                if (((options & ReadKeyOptions.AllowCtrlC) == 0) && cachedKeyEvent.UnicodeChar == (char)3)
                {
                    // Ctrl-C is in the cache, stop pipeline immediately

                    cachedKeyEvent.RepeatCount--;
                    throw NewPipelineStoppedException();
                }
                // If IncludeKeyUp is not set and cached key events are KeyUp OR
                //    IncludeKeyDown is not set and cached key events are KeyDown, clear the cache
                if ((((options & ReadKeyOptions.IncludeKeyUp) == 0) && !cachedKeyEvent.KeyDown) ||
                     (((options & ReadKeyOptions.IncludeKeyDown) == 0) && cachedKeyEvent.KeyDown))
                {
                    cachedKeyEvent.RepeatCount = 0;
                }
            }

            if (cachedKeyEvent.RepeatCount > 0)
            {
                KEY_EVENT_RECORDToKeyInfo(cachedKeyEvent, out keyInfo);
                cachedKeyEvent.RepeatCount--;
            }
            else
            {
                ConsoleHandle handle = ConsoleControl.GetConioDeviceHandle();
                ConsoleControl.INPUT_RECORD[] inputRecords = new ConsoleControl.INPUT_RECORD[1];
                ConsoleControl.ConsoleModes originalMode = ConsoleControl.GetMode(handle);

                // set input mode to exclude mouse or window events
                // turn off ProcessedInput flag to handle ctrl-c
                ConsoleControl.ConsoleModes newMode = originalMode &
                                            ~ConsoleControl.ConsoleModes.WindowInput &
                                            ~ConsoleControl.ConsoleModes.MouseInput &
                                            ~ConsoleControl.ConsoleModes.ProcessedInput;

                try
                {
                    ConsoleControl.SetMode(handle, newMode);
                    while (true)
                    {
                        int actualNumberOfInput = ConsoleControl.ReadConsoleInput(handle, ref inputRecords);
                        Dbg.Assert(actualNumberOfInput == 1,
                            string.Create(CultureInfo.InvariantCulture, $"ReadConsoleInput returns {actualNumberOfInput} number of input event records"));
                        if (actualNumberOfInput == 1)
                        {
                            if (((ConsoleControl.InputRecordEventTypes)inputRecords[0].EventType) ==
                                ConsoleControl.InputRecordEventTypes.KEY_EVENT)
                            {
                                Dbg.Assert(!inputRecords[0].KeyEvent.KeyDown || inputRecords[0].KeyEvent.RepeatCount != 0,
                                    string.Format(CultureInfo.InvariantCulture, "ReadConsoleInput returns a KeyEvent that is KeyDown and RepeatCount 0"));
                                if (inputRecords[0].KeyEvent.RepeatCount == 0)
                                {
                                    // Sometimes Win32 ReadConsoleInput returns a KeyEvent record whose
                                    // RepeatCount is zero. This type of record does not
                                    // represent a keystroke.
                                    continue;
                                }
                                //    Ctrl-C is not allowed and Ctrl-C is input
                                if ((options & ReadKeyOptions.AllowCtrlC) == 0 &&
                                    inputRecords[0].KeyEvent.UnicodeChar == (char)3)
                                {
                                    CacheKeyEvent(inputRecords[0].KeyEvent, ref cachedKeyEvent);
                                    throw NewPipelineStoppedException();
                                }
                                // if KeyDown events are wanted and event is KeyDown OR
                                //    KeyUp events are wanted and event is KeyUp
                                if ((((options & ReadKeyOptions.IncludeKeyDown) != 0) &&
                                        inputRecords[0].KeyEvent.KeyDown) ||
                                    (((options & ReadKeyOptions.IncludeKeyUp) != 0) &&
                                        !inputRecords[0].KeyEvent.KeyDown))
                                {
                                    CacheKeyEvent(inputRecords[0].KeyEvent, ref cachedKeyEvent);
                                    KEY_EVENT_RECORDToKeyInfo(inputRecords[0].KeyEvent, out keyInfo);
                                    break;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    ConsoleControl.SetMode(handle, originalMode);
                }
            }

            if ((options & ReadKeyOptions.NoEcho) == 0)
            {
                parent.WriteToConsole(keyInfo.Character, transcribeResult: true);
            }

            return keyInfo;
        }

        private static
        void
        KEY_EVENT_RECORDToKeyInfo(ConsoleControl.KEY_EVENT_RECORD keyEventRecord, out KeyInfo keyInfo)
        {
            keyInfo = new KeyInfo(
                keyEventRecord.VirtualKeyCode,
                keyEventRecord.UnicodeChar,
                (ControlKeyStates)keyEventRecord.ControlKeyState,
                keyEventRecord.KeyDown);
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <exception cref="HostException">
        /// If obtaining a handle to the active screen buffer failed
        ///    OR
        ///    Win32's FlushConsoleInputBuffer failed
        /// </exception>
        public override
        void
        FlushInputBuffer()
        {
            ConsoleHandle handle = ConsoleControl.GetConioDeviceHandle();
            ConsoleControl.FlushConsoleInputBuffer(handle);

            cachedKeyEvent.RepeatCount = 0;
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="HostException">
        /// If obtaining a handle to the active screen buffer failed
        ///    OR
        ///    Win32's GetNumberOfConsoleInputEvents failed
        ///    OR
        ///    Win32's PeekConsoleInput failed
        /// </exception>
        public override
        bool
        KeyAvailable
        {
            get
            {
                if (cachedKeyEvent.RepeatCount > 0)
                {
                    return true;
                }

                ConsoleHandle handle = ConsoleControl.GetConioDeviceHandle();
                ConsoleControl.INPUT_RECORD[] inputRecords =
                    new ConsoleControl.INPUT_RECORD[ConsoleControl.GetNumberOfConsoleInputEvents(handle)];

                int actualNumberOfInputRecords = ConsoleControl.PeekConsoleInput(handle, ref inputRecords);

                for (int i = 0; i < actualNumberOfInputRecords; i++)
                {
                    if (((ConsoleControl.InputRecordEventTypes)inputRecords[i].EventType) ==
                            ConsoleControl.InputRecordEventTypes.KEY_EVENT)
                    {
                        Dbg.Assert(!inputRecords[i].KeyEvent.KeyDown || inputRecords[i].KeyEvent.RepeatCount != 0,
                            string.Format(CultureInfo.InvariantCulture, "PeekConsoleInput returns a KeyEvent that is KeyDown and RepeatCount 0"));

                        if (inputRecords[i].KeyEvent.KeyDown && inputRecords[i].KeyEvent.RepeatCount == 0)
                        {
                            // Sometimes Win32 ReadConsoleInput returns a KeyEvent record whose
                            // KeyDown is true and RepeatCount is zero. This type of record does not
                            // represent a keystroke.
                            continue;
                        }

                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <value></value>
        /// <exception cref="ArgumentNullException">
        /// If set to null
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If set to a string whose length is not between 1 to 1023
        /// </exception>
        /// <exception cref="HostException">
        /// If Win32's GetConsoleWindowTitle failed
        ///    OR
        ///    Win32's SetConsoleWindowTitle failed
        /// </exception>
        public override string WindowTitle
        {
            get
            {
                return ConsoleControl.GetConsoleWindowTitle();
            }

            set
            {
                const int MaxWindowTitleLength = 1023;
                const int MinWindowTitleLength = 0;

                if (value != null)
                {
                    if (
                        value.Length >= MinWindowTitleLength &&
                        value.Length <= MaxWindowTitleLength
                        )
                    {
                        ConsoleControl.SetConsoleWindowTitle(value);
                    }
                    else if (value.Length < MinWindowTitleLength)
                    {
                        throw PSTraceSource.NewArgumentException("value", ConsoleHostRawUserInterfaceStrings.WindowTitleTooShortError);
                    }
                    else
                    {
                        throw PSTraceSource.NewArgumentException("value",
                                                                 ConsoleHostRawUserInterfaceStrings
                                                                     .WindowTitleTooLongErrorTemplate,
                                                                 MaxWindowTitleLength);
                    }
                }
                else
                {
                    throw PSTraceSource.NewArgumentNullException("value");
                }
            }
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <param name="origin">
        /// location on screen buffer where contents will be written
        /// </param>
        /// <param name="contents">
        /// array of info to be written
        /// </param>
        /// <remarks></remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="origin"/> is outside of the screen buffer.
        ///    OR
        ///    <paramref name="contents"/> is an ill-formed BufferCell array
        ///    OR
        ///    it is illegal to write <paramref name="contents"/> at <paramref name="origin"/> in the buffer
        /// </exception>
        /// <exception cref="HostException">
        /// If obtaining a handle to the active screen buffer failed
        ///    OR
        ///    obtaining information about the buffer failed
        ///    OR
        ///    there is not enough memory to complete calls to Win32's WriteConsoleOutput
        /// </exception>
        public override
        void
        SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            if (contents == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(contents));
            }
            // the origin must be within the window.

            ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo;

            ConsoleHandle handle = GetBufferInfo(out bufferInfo);
            CheckCoordinateWithinBuffer(ref origin, ref bufferInfo, nameof(origin));

            // The output is clipped by the console subsystem, so we don't have to check that the array exceeds the buffer
            // boundaries.

            ConsoleControl.WriteConsoleOutput(handle, origin, contents);
        }

        /// <summary>
        /// If <paramref name="region"/> is completely outside of the screen buffer, it's a no-op.
        /// </summary>
        /// <param name="region">
        /// region with all elements = -1 means "entire screen buffer"
        /// </param>
        /// <param name="fill">
        /// character and attribute to fill the screen buffer
        /// </param>
        /// <remarks>
        /// Provided for clearing regions -- less chatty than passing an array of cells.
        /// Clear screen is:
        ///    SetBufferContents(new Rectangle(-1, -1, -1, -1), ' ', ForegroundColor, BackgroundColor);
        ///    CursorPosition = new Coordinates(0, 0);
        ///
        /// fill.Type is ignored
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// If <paramref name="region"/>'s Left exceeds Right or Bottom exceeds Top
        ///    OR
        ///    it is illegal to set <paramref name="region"/> in the buffer with <paramref name="fill"/>
        /// </exception>
        /// <exception cref="HostException">
        /// If Win32's CreateFile fails
        ///    OR
        ///    Win32's GetConsoleScreenBufferInfo fails
        ///    OR
        ///    there is not enough memory to complete calls to Win32's WriteConsoleOutput
        /// </exception>
        public override
        void
        SetBufferContents(Rectangle region, BufferCell fill)
        {
            // make sure the rect is valid
            if (region.Right < region.Left)
            {
                throw PSTraceSource.NewArgumentException(nameof(region),
                    ConsoleHostRawUserInterfaceStrings.InvalidRegionErrorTemplate,
                    "region.Right", "region.Left");
            }

            if (region.Bottom < region.Top)
            {
                throw PSTraceSource.NewArgumentException(nameof(region),
                    ConsoleHostRawUserInterfaceStrings.InvalidRegionErrorTemplate,
                    "region.Bottom", "region.Top");
            }

            ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo;

            ConsoleHandle handle = GetBufferInfo(out bufferInfo);

            int bufferWidth = bufferInfo.BufferSize.X;
            int bufferHeight = bufferInfo.BufferSize.Y;
            WORD attribute = ConsoleControl.ColorToWORD(fill.ForegroundColor, fill.BackgroundColor);
            Coordinates origin = new Coordinates(0, 0);
            uint codePage;

            // region == {-1, -1, -1, -1} is a special case meaning "the whole screen buffer"
            if (region.Left == -1 && region.Right == -1 && region.Top == -1 && region.Bottom == -1)
            {
                if (bufferWidth % 2 == 1 &&
                    ConsoleControl.IsCJKOutputCodePage(out codePage) &&
                    LengthInBufferCells(fill.Character) == 2)
                {
                    throw PSTraceSource.NewArgumentException(nameof(fill));
                }

                int cells = bufferWidth * bufferHeight;

                ConsoleControl.FillConsoleOutputCharacter(handle, fill.Character, cells, origin);
                ConsoleControl.FillConsoleOutputAttribute(handle, attribute, cells, origin);
                return;
            }

            // The FillConsoleOutputXxx functions wrap at the end of a line.  So we need to convert our rectangular region
            // into line segments that don't extend past the end of a line.  We will also clip the rectangle so that the semantics
            // are the same as SetBufferContents(Coordinates, BufferCell[,]), which clips if the rectangle extends past the
            // screen buffer boundaries.
            if (region.Left >= bufferWidth || region.Top >= bufferHeight || region.Right < 0 || region.Bottom < 0)
            {
                // region is entirely outside the buffer boundaries
                tracer.WriteLine("region outside boundaries");
                return;
            }

            int lineStart = Math.Max(0, region.Left);
            int lineEnd = Math.Min(bufferWidth - 1, region.Right);
            int lineLength = lineEnd - lineStart + 1;

            origin.X = lineStart;

            int firstRow = Math.Max(0, region.Top);
            int lastRow = Math.Min(bufferHeight - 1, region.Bottom);
            origin.Y = firstRow;

            if (ConsoleControl.IsCJKOutputCodePage(out codePage))
            {
                Rectangle existingRegion = new Rectangle(0, 0, 1, lastRow - firstRow);
                int charLength = LengthInBufferCells(fill.Character);
                // Check left edge
                if (origin.X > 0)
                {
                    BufferCell[,] leftExisting = new BufferCell[existingRegion.Bottom + 1, 2];
                    ConsoleControl.ReadConsoleOutputCJK(handle, codePage,
                        new Coordinates(origin.X - 1, origin.Y), existingRegion, ref leftExisting);
                    for (int r = 0; r <= existingRegion.Bottom; r++)
                    {
                        if (leftExisting[r, 0].BufferCellType == BufferCellType.Leading)
                        {
                            throw PSTraceSource.NewArgumentException(nameof(fill));
                        }
                    }
                }
                // Check right edge
                if (lineEnd == bufferWidth - 1)
                {
                    if (charLength == 2)
                    {
                        throw PSTraceSource.NewArgumentException(nameof(fill));
                    }
                }
                else
                {
                    // use ReadConsoleOutputCJK because checking the left and right edges of the existing output
                    // is NOT needed
                    BufferCell[,] rightExisting = new BufferCell[existingRegion.Bottom + 1, 2];
                    ConsoleControl.ReadConsoleOutputCJK(handle, codePage,
                        new Coordinates(lineEnd, origin.Y), existingRegion, ref rightExisting);
                    if (lineLength % 2 == 0)
                    {
                        for (int r = 0; r <= existingRegion.Bottom; r++)
                        {
                            if (rightExisting[r, 0].BufferCellType == BufferCellType.Leading)
                            {
                                throw PSTraceSource.NewArgumentException(nameof(fill));
                            }
                        }
                    }
                    else
                    {
                        for (int r = 0; r <= existingRegion.Bottom; r++)
                        {
                            if (rightExisting[r, 0].BufferCellType == BufferCellType.Leading ^ charLength == 2)
                            {
                                throw PSTraceSource.NewArgumentException(nameof(fill));
                            }
                        }
                    }
                }

                if (lineLength % 2 == 1)
                {
                    lineLength++;
                }
            }

            for (int row = firstRow; row <= lastRow; ++row)
            {
                origin.Y = row;

                // we know that lineStart and lineEnd will always be within the buffer area because of previous boundary
                // checks already done.

                ConsoleControl.FillConsoleOutputCharacter(handle, fill.Character, lineLength, origin);
                ConsoleControl.FillConsoleOutputAttribute(handle, attribute, lineLength, origin);
            }
        }

        /// <summary>
        /// See base class.
        /// If the rectangle is invalid, ie, Right exceeds Left OR Bottom exceeds Top,
        /// </summary>
        /// <param name="region">
        /// area on screen buffer to be read
        /// </param>
        /// <returns>
        /// an array of BufferCell containing screen buffer contents
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="region"/>'s Left exceeds Right or Bottom exceeds Top.
        /// </exception>
        /// <exception cref="HostException">
        /// If obtaining a handle to the active screen buffer failed
        ///    OR
        ///    obtaining information about the buffer failed
        ///    OR
        ///    there is not enough memory to complete calls to Win32's ReadConsoleOutput
        /// </exception>
        public override
        BufferCell[,] GetBufferContents(Rectangle region)
        {
            // make sure the rect is valid
            if (region.Right < region.Left)
            {
                throw PSTraceSource.NewArgumentException(nameof(region),
                    ConsoleHostRawUserInterfaceStrings.InvalidRegionErrorTemplate,
                    "region.Right", "region.Left");
            }

            if (region.Bottom < region.Top)
            {
                throw PSTraceSource.NewArgumentException(nameof(region),
                    ConsoleHostRawUserInterfaceStrings.InvalidRegionErrorTemplate,
                    "region.Bottom", "region.Top");
            }

            ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo;

            ConsoleHandle handle = GetBufferInfo(out bufferInfo);
            int bufferWidth = bufferInfo.BufferSize.X;
            int bufferHeight = bufferInfo.BufferSize.Y;

            if (region.Left >= bufferWidth || region.Top >= bufferHeight || region.Right < 0 || region.Bottom < 0)
            {
                // region is entirely outside the buffer boundaries
                tracer.WriteLine("region outside boundaries");
                return new BufferCell[0, 0];
            }

            int colStart = Math.Max(0, region.Left);
            int colEnd = Math.Min(bufferWidth - 1, region.Right);
            int rowStart = Math.Max(0, region.Top);
            int rowEnd = Math.Min(bufferHeight - 1, region.Bottom);
            Coordinates origin = new Coordinates(colStart, rowStart);

            // contentsRegion indicates the area in contents (declared below) in which
            // the data read from ReadConsoleOutput is stored.
            Rectangle contentsRegion = new Rectangle();
            contentsRegion.Left = Math.Max(0, 0 - region.Left);
            contentsRegion.Top = Math.Max(0, 0 - region.Top);
            contentsRegion.Right = contentsRegion.Left + (colEnd - colStart);
            contentsRegion.Bottom = contentsRegion.Top + (rowEnd - rowStart);

            BufferCell[,] contents = new BufferCell[region.Bottom - region.Top + 1,
                                                    region.Right - region.Left + 1];

            ConsoleControl.ReadConsoleOutput(handle, origin, contentsRegion, ref contents);

            return contents;
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <param name="source">
        /// area to be moved
        /// </param>
        /// <param name="destination">
        /// top left corner to which source to be moved
        /// </param>
        /// <param name="clip">
        /// area to be updated caused by the move
        /// </param>
        /// <param name="fill">
        /// character and attribute to fill the area vacated by the move
        /// </param>
        /// <exception cref="HostException">
        /// If obtaining the active screen buffer failed
        ///    OR
        ///    Call to Win32's ScrollConsoleScreenBuffer failed
        /// </exception>
        public override
        void
        ScrollBufferContents
        (
            Rectangle source,
            Coordinates destination,
            Rectangle clip,
            BufferCell fill
        )
        {
            ConsoleControl.SMALL_RECT scrollRectangle;
            scrollRectangle.Left = (short)source.Left;
            scrollRectangle.Right = (short)source.Right;
            scrollRectangle.Top = (short)source.Top;
            scrollRectangle.Bottom = (short)source.Bottom;

            ConsoleControl.SMALL_RECT clipRectangle;
            clipRectangle.Left = (short)clip.Left;
            clipRectangle.Right = (short)clip.Right;
            clipRectangle.Top = (short)clip.Top;
            clipRectangle.Bottom = (short)clip.Bottom;

            ConsoleControl.COORD origin;
            origin.X = (short)destination.X;
            origin.Y = (short)destination.Y;

            ConsoleControl.CHAR_INFO fillChar;

            fillChar.UnicodeChar = fill.Character;
            fillChar.Attributes = ConsoleControl.ColorToWORD(fill.ForegroundColor, fill.BackgroundColor);

            ConsoleHandle consoleHandle = ConsoleControl.GetActiveScreenBufferHandle();
            ConsoleControl.ScrollConsoleScreenBuffer
            (
                consoleHandle,
                scrollRectangle,
                clipRectangle,
                origin,
                fillChar
            );
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        /// <exception cref="HostException">
        /// If Win32's WideCharToMultiByte fails
        /// </exception>
        public override
        int LengthInBufferCells(string s)
        {
            return this.LengthInBufferCells(s, 0);
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        /// <exception cref="HostException">
        /// If Win32's WideCharToMultiByte fails
        /// </exception>
        public override
        int LengthInBufferCells(string s, int offset)
        {
            if (s == null)
            {
                throw PSTraceSource.NewArgumentNullException("str");
            }

            return ConsoleControl.LengthInBufferCells(s, offset, parent.SupportsVirtualTerminal);
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        /// <exception cref="HostException">
        /// If Win32's WideCharToMultiByte fails
        /// </exception>
        public override
        int LengthInBufferCells(char c)
        {
            return ConsoleControl.LengthInBufferCells(c);
        }

        #region internal

        /// <summary>
        /// Clear the ReadKey cache.
        /// </summary>
        /// <exception/>
        internal void ClearKeyCache()
        {
            cachedKeyEvent.RepeatCount = 0;
        }

        #endregion internal

        #region helpers

        // pass-by-ref for speed.
        /// <summary>
        /// </summary>
        /// <param name="c"></param>
        /// <param name="bufferInfo"></param>
        /// <param name="paramName"></param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="c"/> is outside of the output buffer area
        /// </exception>
        private static
        void
        CheckCoordinateWithinBuffer(ref Coordinates c, ref ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo, string paramName)
        {
            if (c.X < 0 || c.X > bufferInfo.BufferSize.X)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException(
                    paramName + ".X",
                    c.X,
                    ConsoleHostRawUserInterfaceStrings.CoordinateOutOfBufferErrorTemplate, bufferInfo.BufferSize);
            }

            if (c.Y < 0 || c.Y > bufferInfo.BufferSize.Y)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException(
                    paramName + ".Y",
                    c.Y,
                    ConsoleHostRawUserInterfaceStrings.CoordinateOutOfBufferErrorTemplate, bufferInfo.BufferSize);
            }
        }

        /// <summary>
        /// Get output buffer info.
        /// </summary>
        /// <param name="bufferInfo"></param>
        /// <returns></returns>
        /// <exception cref="HostException">
        /// If Win32's CreateFile fails
        ///    OR
        ///    Win32's GetConsoleScreenBufferInfo fails
        /// </exception>
        private static
        ConsoleHandle
        GetBufferInfo(out ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo)
        {
            ConsoleHandle result = ConsoleControl.GetActiveScreenBufferHandle();
            bufferInfo = ConsoleControl.GetConsoleScreenBufferInfo(result);
            return result;
        }

        #endregion helpers

        private readonly ConsoleColor defaultForeground = ConsoleColor.Gray;

        private readonly ConsoleColor defaultBackground = ConsoleColor.Black;

        private readonly ConsoleHostUserInterface parent = null;

        private ConsoleControl.KEY_EVENT_RECORD cachedKeyEvent;

        [TraceSourceAttribute("ConsoleHostRawUserInterface", "Console host's subclass of S.M.A.Host.RawConsole")]
        private static readonly PSTraceSource tracer = PSTraceSource.GetTracer("ConsoleHostRawUserInterface", "Console host's subclass of S.M.A.Host.RawConsole");
    }
}   // namespace

#else

// Managed code only implementation for portability

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Host;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell
{
    // this is all originally from https://msdn.microsoft.com/library/ee706570%28v=vs.85%29.aspx

    internal sealed class ConsoleHostRawUserInterface : PSHostRawUserInterface
    {

        private readonly ConsoleHostUserInterface _parent = null;

        internal ConsoleHostRawUserInterface(ConsoleHostUserInterface mshConsole) : base()
        {
            _parent = mshConsole;
        }

        /// <summary>
        /// Gets or sets the background color of the displayed text.
        /// This maps to the corresponding Console.Background property.
        /// </summary>
        public override ConsoleColor BackgroundColor
        {
            get { return Console.BackgroundColor; }

            set { Console.BackgroundColor = value; }
        }

        // TODO: Make wrap width user-customizable.
        private static Size s_wrapSize = new Size(80, 40);

        /// <summary>
        /// Gets or sets the size of the host buffer.
        /// </summary>
        public override Size BufferSize
        {
            get
            {
                // Console can return zero when a pseudo-TTY is allocated, which
                // is useless for us. Instead, map to the wrap size.
                return Console.BufferWidth == 0 || Console.BufferHeight == 0
                    ? s_wrapSize
                    : new Size(Console.BufferWidth, Console.BufferHeight);
            }

            set
            {
                Console.SetBufferSize(value.Width, value.Height);
            }
        }

        /// <summary>
        /// Gets or sets the cursor position.
        /// </summary>
        public override Coordinates CursorPosition
        {
            get
            {
                return new Coordinates(Console.CursorLeft, Console.CursorTop);
            }

            set
            {
                Console.SetCursorPosition(value.X < 0 ? 0 : value.X,
                                          value.Y < 0 ? 0 : value.Y);
            }
        }

        /// <summary>
        /// Gets or sets the size of the displayed cursor.
        /// This maps to the corresponding Console.CursorSize property.
        /// </summary>
        public override int CursorSize
        {
            // Future porting note: this API throws on Windows when output is
            // redirected, but never throws on Unix because it's fake.
            get { return Console.CursorSize; }

            set { Console.CursorSize = value; }
        }

        /// <summary>
        /// Gets or sets the foreground color of the displayed text.
        /// This maps to the corresponding Console.ForegroundColor property.
        /// </summary>
        public override ConsoleColor ForegroundColor
        {
            get { return Console.ForegroundColor; }

            set { Console.ForegroundColor = value; }
        }

        /// <summary>
        /// Gets a value indicating whether the user has pressed a key. This maps
        /// to the corresponding Console.KeyAvailable property.
        /// </summary>
        public override bool KeyAvailable
        {
            get { return Console.KeyAvailable; }
        }

        /// <summary>
        /// Gets the dimensions of the largest window that could be rendered in
        /// the current display, if the buffer was at the least that large.
        /// This maps to the MaxWindowSize.
        /// </summary>
        public override Size MaxPhysicalWindowSize
        {
            get { return MaxWindowSize; }
        }

        /// <summary>
        /// Gets the dimensions of the largest window size that can be
        /// displayed. This maps to the Console.LargestWindowWidth and
        /// Console.LargestWindowHeight properties to determine the returned
        /// value of this property.
        /// </summary>
        public override Size MaxWindowSize
        {
            get
            {
                // Console can return zero when a pseudo-TTY is allocated, which
                // is useless for us. Instead, map to the wrap size.
                return Console.LargestWindowWidth == 0 || Console.LargestWindowHeight == 0
                    ? s_wrapSize
                    : new Size(Console.LargestWindowWidth, Console.LargestWindowHeight);
            }
        }

        /// <summary>
        /// Gets or sets the position of the displayed window. This maps to the
        /// Console window position APIs to determine the returned value of this
        /// property.
        /// </summary>
        public override Coordinates WindowPosition
        {
            get { return new Coordinates(Console.WindowLeft, Console.WindowTop); }

            set { Console.SetWindowPosition(value.X, value.Y); }
        }

        /// <summary>
        /// Gets or sets the size of the displayed window. This example
        /// uses the corresponding Console window size APIs to determine the
        /// returned value of this property.
        /// </summary>
        public override Size WindowSize
        {
            get
            {
                // Console can return zero when a pseudo-TTY is allocated, which
                // is useless for us. Instead, map to the wrap size.
                return Console.WindowWidth == 0 || Console.WindowHeight == 0
                    ? s_wrapSize
                    : new Size(Console.WindowWidth, Console.WindowHeight);
            }

            set
            {
                Console.SetWindowSize(value.Width, value.Height);
            }
        }

        /// <summary>
        /// Cached Window Title, for systems that needs it.
        /// </summary>
        private string _title = string.Empty;

        /// <summary>
        /// Gets or sets the title of the displayed window. The example
        /// maps the Console.Title property to the value of this property.
        /// </summary>
        public override string WindowTitle
        {
            get
            {
                // Console throws an exception on Unix platforms, so we handle
                // caching and returning the Window title ourselves.
                return Platform.IsWindows ? Console.Title : _title;
            }

            set
            {
                Console.Title = value;
                _title = value;
            }
        }

        /// <summary>
        /// This API resets the input buffer.
        /// </summary>
        public override void FlushInputBuffer()
        {
            if (!Console.IsInputRedirected)
            {
                Console.OpenStandardInput().Flush();
            }
        }

        public void ScrollBuffer(int lines)
        {
            for (int i = 0; i < lines; ++i)
            {
                Console.Out.Write('\n');

            }
        }

        internal struct COORD
        {
            internal short X;

            internal short Y;

            public override string ToString()
            {
                return string.Create(CultureInfo.InvariantCulture, $"{X},{Y}");
            }
        }

        internal struct SMALL_RECT
        {
            internal short Left;

            internal short Top;

            internal short Right;

            internal short Bottom;

            public override string ToString()
            {
                return string.Create(CultureInfo.InvariantCulture, $"{Left},{Top},{Right},{Bottom}");
            }
        }

        /// <summary>
        /// This API returns a rectangular region of the screen buffer. In
        /// this example this functionality is not needed so the method throws
        /// a NotImplementException exception.
        /// </summary>
        /// <param name="rectangle">Defines the size of the rectangle.</param>
        /// <returns>Throws a NotImplementedException exception.</returns>
        public override BufferCell[,] GetBufferContents(Rectangle rectangle)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        /// <summary>
        /// This API reads a pressed, released, or pressed and released keystroke
        /// from the keyboard device, blocking processing until a keystroke is
        /// typed that matches the specified keystroke options.
        /// </summary>
        /// <param name="options">Only NoEcho is supported.</param>
        public override KeyInfo ReadKey(ReadKeyOptions options)
        {
            ConsoleKeyInfo key = Console.ReadKey((options & ReadKeyOptions.NoEcho) != 0);
            return new KeyInfo((int)key.Key, key.KeyChar, new ControlKeyStates(), true);
        }

        /// <summary>
        /// This API crops a region of the screen buffer. In this example
        /// this functionality is not needed so the method throws a
        /// NotImplementException exception.
        /// </summary>
        /// <param name="source">The region of the screen to be scrolled.</param>
        /// <param name="destination">The region of the screen to receive the
        /// source region contents.</param>
        /// <param name="clip">The region of the screen to include in the operation.</param>
        /// <param name="fill">The character and attributes to be used to fill all cell.</param>
        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        /// <summary>
        /// This method copies an array of buffer cells into the screen buffer
        /// at a specified location.
        /// </summary>
        /// <param name="origin">The parameter used to set the origin where the buffer where begin writing to.</param>
        /// <param name="contents">The parameter used to contain the contents to be written to the buffer.</param>
        public override void SetBufferContents(Coordinates origin,
                                               BufferCell[,] contents)
        {
            // if there are no contents, there is nothing to set the buffer to
            if (contents == null)
            {
                PSTraceSource.NewArgumentNullException("contents");
            }
            // if the cursor is on the last line, we need to make more space to print the specified buffer
            if (origin.Y == BufferSize.Height - 1 && origin.X >= BufferSize.Width)
            {
                // for each row in the buffer, create a new line
                int rows = contents.GetLength(0);
                ScrollBuffer(rows);
                // for each row in the buffer, move the cursor y up to the beginning of the created blank space
                // but not above zero
                if (origin.Y >= rows)
                {
                    origin.Y -= rows;
                }
            }

#if UNIX
            // Make sure that the physical cursor position matches where we think it is.
            // This is a problem on *nix, because input that the user types is echoed
            // and that moves the cursor. As a consequence, the cursor needs to be repositioned
            // before we update the screen.
            CursorPosition = origin;
#endif

            // iterate through the buffer to set
            foreach (var charitem in contents)
            {
                // set the cursor to false to prevent cursor flicker
                Console.CursorVisible = false;

                // if x is exceeding buffer width, reset to the next line
                if (origin.X >= BufferSize.Width)
                {
                    origin.X = 0;
                }

                // write the character from contents
                Console.Out.Write(charitem.Character);
            }

            // reset the cursor to the original position
            CursorPosition = origin;
            // reset the cursor to visible
            Console.CursorVisible = true;
        }

        /// <summary>
        /// This method copies a given character, foreground color, and background
        /// color to a region of the screen buffer. In this example this
        /// functionality is not needed so the method throws a
        /// NotImplementException exception./// </summary>
        /// <param name="rectangle">Defines the area to be filled.</param>
        /// <param name="fill">Defines the fill character.</param>
        public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>

        public override
        int LengthInBufferCells(string s)
        {
            return this.LengthInBufferCells(s, 0);
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="offset"></param>
        /// <returns></returns>

        public override
        int LengthInBufferCells(string s, int offset)
        {
            if (s == null)
            {
                throw PSTraceSource.NewArgumentNullException("str");
            }

            return ConsoleControl.LengthInBufferCells(s, offset, _parent.SupportsVirtualTerminal);
        }
    }
}
#endif
