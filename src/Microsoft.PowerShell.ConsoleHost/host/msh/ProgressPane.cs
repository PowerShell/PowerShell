// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Management.Automation.Host;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// ProgressPane is a class that represents the "window" in which outstanding activities for which the host has received
    /// progress updates are shown.
    ///
    /// </summary>
    internal
    class ProgressPane
    {
        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="ui">
        /// An implementation of the PSHostRawUserInterface with which the pane will be shown and hidden.
        /// </param>
        internal
        ProgressPane(ConsoleHostUserInterface ui)
        {
            ArgumentNullException.ThrowIfNull(ui);
            _ui = ui;
            _rawui = ui.RawUI;
        }

        /// <summary>
        /// Indicates whether the pane is visible on the screen buffer or not.
        /// </summary>
        /// <value>
        /// true if the pane is visible, false if not.
        ///
        /// </value>
        internal
        bool
        IsShowing
        {
            get
            {
                return (_savedRegion != null);
            }
        }

        /// <summary>
        /// Shows the pane in the screen buffer.  Saves off the content of the region of the buffer that will be overwritten so
        /// that it can be restored again.
        /// </summary>
        internal
        void
        Show()
        {
            lock (_lock)
            {
                if (!IsShowing)
                {
                    // Get temporary reference to the progress region since it can be
                    // changed at any time by a call to WriteProgress.
                    BufferCell[,] tempProgressRegion = _progressRegion;
                    if (tempProgressRegion == null)
                    {
                        return;
                    }

                    // The location where we show ourselves is always relative to the screen buffer's current window position.

                    int rows = tempProgressRegion.GetLength(0);
                    int cols = tempProgressRegion.GetLength(1);

                    if (ProgressNode.IsMinimalProgressRenderingEnabled())
                    {
                        rows = _content.Length;
                        cols = PSStyle.Instance.Progress.MaxWidth;
                        if (cols > _bufSize.Width)
                        {
                            cols = _bufSize.Width;
                        }
                    }

                    _savedCursor = _rawui.CursorPosition;
                    _location.X = 0;

                    if (!Platform.IsWindows || ProgressNode.IsMinimalProgressRenderingEnabled())
                    {
                        _location.Y = _rawui.CursorPosition.Y;

                        // if cursor is not on left edge already move down one line
                        if (_rawui.CursorPosition.X != 0)
                        {
                            _location.Y++;
                            _rawui.CursorPosition = _location;
                        }

                        // if the cursor is at the bottom, create screen buffer space by scrolling
                        int scrollRows = rows - ((_rawui.BufferSize.Height - 1) - _location.Y);
                        if (scrollRows > 0)
                        {
                            // Scroll the console screen up by 'scrollRows'
                            var bottomLocation = _location;
                            bottomLocation.Y = _rawui.BufferSize.Height - 1;

                            _rawui.CursorPosition = bottomLocation;
                            for (int i = 0; i < scrollRows; i++)
                            {
                                Console.Out.Write('\n');
                            }

                            _location.Y -= scrollRows;
                            _savedCursor.Y -= scrollRows;
                        }

                        // create cleared region to clear progress bar later
                        _savedRegion = tempProgressRegion;
                        if (PSStyle.Instance.Progress.View != ProgressView.Minimal)
                        {
                            for (int row = 0; row < rows; row++)
                            {
                                for (int col = 0; col < cols; col++)
                                {
                                    _savedRegion[row, col].Character = ' ';
                                }
                            }
                        }

                        // put cursor back to where output should be
                        _rawui.CursorPosition = _location;
                    }
                    else
                    {
                        _location = _rawui.WindowPosition;

                        // We have to show the progress pane in the first column, as the screen buffer at any point might contain
                        // a CJK double-cell characters, which makes it impractical to try to find a position where the pane would
                        // not slice a character.  Column 0 is the only place where we know for sure we can place the pane.

                        _location.Y = Math.Min(_location.Y + 2, _bufSize.Height);

                        // Save off the current contents of the screen buffer in the region that we will occupy
                        _savedRegion =
                            _rawui.GetBufferContents(
                                new Rectangle(_location.X, _location.Y, _location.X + cols - 1, _location.Y + rows - 1));
                    }

                    if (ProgressNode.IsMinimalProgressRenderingEnabled())
                    {
                        WriteContent();
                    }
                    else
                    {
                        // replace the saved region in the screen buffer with our progress display
                        _rawui.SetBufferContents(_location, tempProgressRegion);
                    }
                }
            }
        }

        /// <summary>
        /// Hides the pane by restoring the saved contents of the region of the buffer that the pane occupies.  If the pane is
        /// not showing, then does nothing.
        /// </summary>
        internal
        void
        Hide()
        {
            lock (_lock)
            {
                if (IsShowing)
                {
                    if (ProgressNode.IsMinimalProgressRenderingEnabled())
                    {
                        _rawui.CursorPosition = _location;
                        int maxWidth = PSStyle.Instance.Progress.MaxWidth;
                        if (maxWidth > _bufSize.Width)
                        {
                            maxWidth = _bufSize.Width;
                        }
                        
                        for (int i = 0; i < _savedRegion.GetLength(1); i++)
                        {
                            if (i < _savedRegion.GetLength(1) - 1)
                            {
                                Console.Out.WriteLine(string.Empty.PadRight(maxWidth));
                            }
                            else
                            {
                                Console.Out.Write(string.Empty.PadRight(maxWidth));
                            }
                        }
                    }
                    else
                    {
                        // It would be nice if we knew that the saved region could be kept for the next time Show is called, but alas,
                        // we have no way of knowing if the screen buffer has changed since we were hidden.  By "no good way" I mean that
                        // detecting a change would be at least as expensive as chucking the savedRegion and rebuilding it.  And it would
                        // be very complicated.

                        _rawui.SetBufferContents(_location, _savedRegion);
                    }

                    _savedRegion = null;
                    _rawui.CursorPosition = _savedCursor;
                }
            }
        }

        /// <summary>
        /// Updates the pane with the rendering of the supplied PendingProgress, and shows it.
        /// </summary>
        /// <param name="pendingProgress">
        /// A PendingProgress instance that represents the outstanding activities that should be shown.
        /// </param>
        internal
        void
        Show(PendingProgress pendingProgress)
        {
            Dbg.Assert(pendingProgress != null, "pendingProgress may not be null");

            _bufSize = _rawui.BufferSize;

            // In order to keep from slicing any CJK double-cell characters that might be present in the screen buffer,
            // we use the full width of the buffer.

            int maxWidth = _bufSize.Width;
            int maxHeight = Math.Max(5, _rawui.WindowSize.Height / 3);

            _content = pendingProgress.Render(maxWidth, maxHeight, _rawui);
            if (_content == null)
            {
                // There's nothing to show.

                Hide();
                _progressRegion = null;
                return;
            }

            BufferCell[,] newRegion;
            if (ProgressNode.IsMinimalProgressRenderingEnabled())
            {
                // Legacy progress rendering relies on a BufferCell which defines a character, foreground color, and background color
                // per cell.  This model doesn't work with ANSI escape sequences.  However, there is existing logic on rendering that
                // relies on the existence of the BufferCell to know if something has been rendered previously.  Here we are creating
                // an empty BufferCell, but using the second dimension to capture the number of rows so that we can clear that many
                // elsewhere in Hide().
                newRegion = new BufferCell[0, _content.Length];
            }
            else
            {
                newRegion = _rawui.NewBufferCellArray(_content, _ui.ProgressForegroundColor, _ui.ProgressBackgroundColor);
            }

            Dbg.Assert(newRegion != null, "NewBufferCellArray has failed!");

            if (_progressRegion == null)
            {
                // we've never shown this pane before.

                _progressRegion = newRegion;
                Show();
            }
            else
            {
                // We have shown the pane before. We have to be smart about when we restore the saved region to minimize
                // flicker. We need to decide if the new contents will change the dimensions of the progress pane
                // currently being shown.  If it will, then restore the saved region, and show the new one.  Otherwise,
                // just blast the new one on top of the last one shown.

                // We're only checking size, not content, as we assume that the content will always change upon receipt
                // of a new ProgressRecord.  That's not guaranteed, of course, but it's a good bet.  So checking content
                // would usually result in detection of a change, so why bother?

                bool sizeChanged =
                        (newRegion.GetLength(0) != _progressRegion.GetLength(0))
                    || (newRegion.GetLength(1) != _progressRegion.GetLength(1));

                _progressRegion = newRegion;

                if (sizeChanged)
                {
                    if (IsShowing)
                    {
                        Hide();
                    }

                    Show();
                }
                else
                {
                    if (ProgressNode.IsMinimalProgressRenderingEnabled())
                    {
                        WriteContent();
                    }
                    else
                    {
                        _rawui.SetBufferContents(_location, _progressRegion);
                    }
                }
            }
        }

        private void WriteContent()
        {
            if (_content is not null)
            {
                // On Windows, we can check if the cursor is currently visible and not change it to visible
                // if it is intentionally hidden.  On Unix, it is not currently supported to read the cursor visibility.
#if UNIX
                Console.CursorVisible = false;
#else
                bool currentCursorVisible = Console.CursorVisible;
                if (currentCursorVisible)
                {
                    Console.CursorVisible = false;
                }
#endif

                var currentPosition = _rawui.CursorPosition;
                _rawui.CursorPosition = _location;

                for (int i = 0; i < _content.Length; i++)
                {
                    if (i < _content.Length - 1)
                    {
                        Console.Out.WriteLine(_content[i]);
                    }
                    else
                    {
                        Console.Out.Write(_content[i]);
                    }
                }

                _rawui.CursorPosition = currentPosition;
#if UNIX
                Console.CursorVisible = true;
#else
                Console.CursorVisible = currentCursorVisible;
#endif
            }
        }

        private Coordinates _location = new Coordinates(0, 0);
        private Coordinates _savedCursor;
        private Size _bufSize;
        private BufferCell[,] _savedRegion;
        private readonly object _lock = new();
        private BufferCell[,] _progressRegion;
        private string[] _content;
        private readonly PSHostRawUserInterface _rawui;
        private readonly ConsoleHostUserInterface _ui;
    }
}   // namespace
