/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/



using System;
using System.Management.Automation;
using Dbg = System.Management.Automation.Diagnostics;
using System.Threading;


namespace Microsoft.PowerShell
{
    internal partial
    class ConsoleHostUserInterface : System.Management.Automation.Host.PSHostUserInterface
    {
        /// <summary>
        /// 
        /// Called at the end of a prompt loop to take down any progress display that might have appeared and purge any 
        /// outstanding progress activity state.
        /// 
        /// </summary>

        internal
        void
        ResetProgress()
        {
            // destroy the data structures representing outstanding progress records
            // take down and destroy the progress display

            if (_progPaneUpdateTimer != null)
            {
                // Stop update 'ProgressPane' and destroy timer
                _progPaneUpdateTimer.Dispose();
                _progPaneUpdateTimer = null;
            }
            if (_progPane != null)
            {
                Dbg.Assert(_pendingProgress != null, "How can you have a progress pane and no backing data structure?");

                _progPane.Hide();
                _progPane = null;
            }
            _pendingProgress = null;
        }



        /// <summary>
        ///
        /// Invoked by ConsoleHostUserInterface.WriteProgress to update the set of outstanding activities for which 
        /// ProgressRecords have been received.
        ///
        /// </summary>

        private
        void
        HandleIncomingProgressRecord(Int64 sourceId, ProgressRecord record)
        {
            Dbg.Assert(record != null, "record should not be null");

            if (_pendingProgress == null)
            {
                Dbg.Assert(_progPane == null, "If there is no data struct, there shouldn't be a pane, either.");

                _pendingProgress = new PendingProgress();
            }

            _pendingProgress.Update(sourceId, record);

            if (_progPane == null)
            {
                // This is the first time we've received a progress record.
                // Create a progress pane,
                // then show it,
                // then create and start timer to update it.

                _progPane = new ProgressPane(this);

                if (_progPaneUpdateTimer == null && _progPane != null)
                {
                    _progPane.Show(_pendingProgress);
                    _progPaneUpdateTimer = new Timer( new TimerCallback(ProgressPaneUpdateTimerElapsed), null, UpdateTimerThreshold, Timeout.Infinite);
                }
            }
        }



        /// <summary>
        ///
        /// TimerCallback for _progPaneUpdateTimer to update 'ProgressPane' and restart the timer.
        ///
        /// </summary>

        private
        void
        ProgressPaneUpdateTimerElapsed(object sender)
        {
            if (_progPane != null)
            {
                _progPane.Show(_pendingProgress);
            }
            if (_progPaneUpdateTimer != null)
            {
                _progPaneUpdateTimer.Change(UpdateTimerThreshold, Timeout.Infinite);
            }
        }



        private
        void
        PreWrite()
        {
            if (_progPane != null)
            {
                _progPane.Hide();
            }
        }



        private
        void
        PostWrite()
        {
            if (_progPane != null)
            {
                _progPane.Show();
            }
        }



        private
        void
        PostWrite(string value)
        {
            PostWrite();

            if (_parent.IsTranscribing)
            {
                try
                {
                    _parent.WriteToTranscript(value);
                }
                catch (Exception e)
                {
                    ConsoleHost.CheckForSevereException(e);
                    _parent.IsTranscribing = false;
                }
            }
        }



        private
        void
        PreRead()
        {
            if (_progPane != null)
            {
                _progPane.Hide();
            }
        }



        private
        void
        PostRead()
        {
            if (_progPane != null)
            {
                _progPane.Show();
            }
        }



        private
        void
        PostRead(string value)
        {
            PostRead();

            if (_parent.IsTranscribing)
            {
                try
                {
                    // Reads always terminate with the enter key, so add that.
                    _parent.WriteToTranscript(value + Crlf);
                }
                catch (Exception e)
                {
                    ConsoleHost.CheckForSevereException(e);
                    _parent.IsTranscribing = false;
                }
            }
        }



        private ProgressPane _progPane = null;
        private PendingProgress _pendingProgress = null;
        // The timer update 'ProgressPane' every 'UpdateTimerThreshold' milliseconds
        private Timer _progPaneUpdateTimer;
        private const int UpdateTimerThreshold = 100;
    }
}   // namespace 



