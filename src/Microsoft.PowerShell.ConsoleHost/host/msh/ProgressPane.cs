/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/



using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Host;

using Dbg = System.Management.Automation.Diagnostics;


namespace Microsoft.PowerShell
{
    /// <summary>
    /// 
    /// ProgressPane is a class that represents the "window" in which outstanding activities for which the host has recevied 
    /// progress updates are shown. 
    /// 
    ///</summary>
    
    internal
    class ProgressPane
    {
        /// <summary>
        /// 
        /// Constructs a new instance.
        /// 
        /// </summary>
        /// <param name="ui">
        /// 
        /// An implementation of the PSHostRawUserInterface with which the pane will be shown and hidden.
        /// 
        /// </param>
        
        internal
        ProgressPane(ConsoleHostUserInterface ui)
        {
            if (ui == null) throw new ArgumentNullException("ui");
            this.ui = ui;
            this.rawui = ui.RawUI;
        }

        
        
        /// <summary>
        /// 
        /// Indicates whether the pane is visible on the screen buffer or not.
        /// 
        /// </summary>
        /// <value>
        ///
        /// true if the pane is visible, false if not.
        /// 
        ///</value>
        
        internal
        bool
        IsShowing
        {
            get 
            {
                return (savedRegion != null);
            }
        }



        /// <summary>
        /// 
        /// Shows the pane in the screen buffer.  Saves off the content of the region of the buffer that will be overwritten so 
        /// that it can be restored again.
        /// 
        /// </summary>
        
        internal
        void
        Show()
        {
            if (!IsShowing)
            {
                // Get temporary reference to the progress region since it can be 
                // changed at any time by a call to WriteProgress.
                BufferCell[,] tempProgressRegion = progressRegion;
                if (tempProgressRegion == null)
                {
                    return;
                }

                // The location where we show ourselves is always relative to the screen buffer's current window position.

                int rows = tempProgressRegion.GetLength(0);
                int cols = tempProgressRegion.GetLength(1);
                location = rawui.WindowPosition;

                // We have to show the progress pane in the first column, as the screen buffer at any point might contain
                // a CJK double-cell characters, which makes it impractical to try to find a position where the pane would
                // not slice a character.  Column 0 is the only place where we know for sure we can place the pane.

                location.X = 0;
                location.Y = Math.Min(location.Y + 2, bufSize.Height);

#if UNIX
                // replace the saved region in the screen buffer with our progress display
                location = rawui.CursorPosition;

                //set the cursor position back to the beginning of the region to overwrite write-progress
                //if the cursor is at the bottom, back it up to overwrite the previous write progress
                if (location.Y >= rawui.BufferSize.Height - rows)
                {
                    Console.Out.Write('\n');
                    if (location.Y >= rows)
                    {
                        location.Y -= rows;
                    }
                }

                rawui.CursorPosition = location;
#else
                // Save off the current contents of the screen buffer in the region that we will occupy
                savedRegion =
                    rawui.GetBufferContents(
                        new Rectangle(location.X, location.Y, location.X + cols - 1, location.Y + rows - 1));
#endif

                // replace the saved region in the screen buffer with our progress display
                rawui.SetBufferContents(location, tempProgressRegion);
            }
        }



        /// <summary>
        /// 
        /// Hides the pane by restoring the saved contents of the region of the buffer that the pane occupies.  If the pane is 
        /// not showing, then does nothing.
        /// 
        /// </summary>

        internal
        void
        Hide()
        {
            if (IsShowing)
            {
                // It would be nice if we knew that the saved region could be kept for the next time Show is called, but alas, 
                // we have no way of knowing if the screen buffer has changed since we were hidden.  By "no good way" I mean that
                // detecting a change would be at least as expensive as chucking the savedRegion and rebuilding it.  And it would
                // be very complicated.

                rawui.SetBufferContents(location, savedRegion);
                savedRegion = null;
            }
        }



        /// <summary>
        /// 
        /// Updates the pane with the rendering of the supplied PendingProgress, and shows it.
        /// 
        /// </summary>
        /// <param name="pendingProgress">
        /// 
        /// A PendingProgress instance that represents the outstanding activities that should be shown.
        /// 
        /// </param>
        
        internal
        void
        Show(PendingProgress pendingProgress)
        {
            Dbg.Assert(pendingProgress != null, "pendingProgress may not be null");

            bufSize = rawui.BufferSize;

            // In order to keep from slicing any CJK double-cell characters that might be present in the screen buffer, 
            // we use the full width of the buffer.

            int maxWidth = bufSize.Width;
            int maxHeight = Math.Max(5, rawui.WindowSize.Height / 3); 

            string[] contents = pendingProgress.Render(maxWidth, maxHeight, rawui);
            if (contents == null)
            {
                // There's nothing to show.

                Hide();
                progressRegion = null;
                return;
            }

            // NTRAID#Windows OS Bugs-1061752-2004/12/15-sburns should read a skin setting here...

            BufferCell[,] newRegion = rawui.NewBufferCellArray(contents, ui.ProgressForegroundColor, ui.ProgressBackgroundColor);
            Dbg.Assert(newRegion != null, "NewBufferCellArray has failed!");

            if (progressRegion == null)
            {
                // we've never shown this pane before.

                progressRegion = newRegion;
                Show();
            }
            else
            {
                // We have shown the pane before. We have to be smart about when we restore the saved region to minimize
                // flicker. We need to decide if the new contents will change the dimmensions of the progress pane
                // currently being shown.  If it will, then restore the saved region, and show the new one.  Otherwise,
                // just blast the new one on top of the last one shown.

                // We're only checking size, not content, as we assume that the content will always change upon receipt
                // of a new ProgressRecord.  That's not guaranteed, of course, but it's a good bet.  So checking content
                // would usually result in detection of a change, so why bother?

                bool sizeChanged =
                        (newRegion.GetLength(0) != progressRegion.GetLength(0))
                    ||  (newRegion.GetLength(1) != progressRegion.GetLength(1))
                    ? true : false;

                progressRegion = newRegion;

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
                    rawui.SetBufferContents(location, progressRegion);
                }
            }
        }



        private Coordinates location = new Coordinates(0, 0);
        private Size bufSize;
        private BufferCell[,] savedRegion;
        private BufferCell[,] progressRegion;
        private PSHostRawUserInterface rawui;
        private ConsoleHostUserInterface ui;
    }
}   // namespace 




