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
                // Stop update a progress pane and destroy timer
                _progPaneUpdateTimer.Dispose();
                _progPaneUpdateTimer = null;
            }
            // We don't reset 'progPaneUpdateFlag = false' here
            // because `HandleIncomingProgressRecord` will be init 'progPaneUpdateFlag' in any way.
            // (Also the timer callback can still set it to 'true' accidentally)


            if (_progPane != null)
            {
                Dbg.Assert(_pendingProgress != null, "How can you have a progress pane and no backing data structure?");

                // lock (_instanceLock) is not needed here because lock is done at global level
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
                // This is the first time we've received a progress record
                // Create a progress pane
                // Set up a update flag
                // Create a timer for updating the flag

                _progPane = new ProgressPane(this);

                if (_progPaneUpdateTimer == null)
                {
                    // Show a progress pane at the first time we've received a progress record
                    progPaneUpdateFlag = 1;

                    // Invoke the timer only once to exclude overlaps
                    // The timer will be restarted in 'ProgressPaneUpdateTimerElapsed'
                    _progPaneUpdateTimer = new Timer( new TimerCallback(ProgressPaneUpdateTimerElapsed), null, UpdateTimerThreshold, Timeout.Infinite);
                }
            }

            if (Interlocked.CompareExchange(ref progPaneUpdateFlag, 0, 1) == 1 || record.RecordType == ProgressRecordType.Completed)
            {
                // Update the progress pane only when the timer set up the update flag or WriteProgress is completed.
                // As a result, we do not block WriteProgress and whole script and eliminate unnecessary console locks and updates.
                _progPane.Show(_pendingProgress);
            }
        }



        /// <summary>
        ///
        /// TimerCallback for _progPaneUpdateTimer to update 'progPaneUpdateFlag' and restart the timer
        ///
        /// </summary>

        private
        void
        ProgressPaneUpdateTimerElapsed(object sender)
        {
            Interlocked.CompareExchange(ref progPaneUpdateFlag, 1, 0);

            _progPaneUpdateTimer?.Change(UpdateTimerThreshold, Timeout.Infinite);
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
                catch (Exception)
                {
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
                catch (Exception)
                {
                    _parent.IsTranscribing = false;
                }
            }
        }



        private ProgressPane _progPane = null;
        private PendingProgress _pendingProgress = null;
        // The timer set up 'progPaneUpdateFlag' every 'UpdateTimerThreshold' milliseconds to update 'ProgressPane'
        private Timer _progPaneUpdateTimer;
        private const int UpdateTimerThreshold = 200;
        private int progPaneUpdateFlag;
    }
}   // namespace 



