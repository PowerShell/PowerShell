// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell
{
    internal partial
    class ConsoleHostUserInterface : System.Management.Automation.Host.PSHostUserInterface
    {
        /// <summary>
        /// Called at the end of a prompt loop to take down any progress display that might have appeared and purge any
        /// outstanding progress activity state.
        /// </summary>
        internal
        void
        ResetProgress()
        {
            // destroy the data structures representing outstanding progress records
            // take down and destroy the progress display

            // If we have multiple runspaces on the host then any finished pipeline in any runspace will lead to call 'ResetProgress'
            // so we need the lock
            lock (_instanceLock)
            {
                if (_progPaneUpdateTimer != null)
                {
                    // Stop update a progress pane and destroy timer
                    _progPaneUpdateTimer.Dispose();
                    _progPaneUpdateTimer = null;
                }
                // We don't set 'progPaneUpdateFlag = 0' here, because:
                // 1. According to MSDN, the timer callback can occur after the Dispose() method has been called.
                //    So we cannot guarantee the flag is truly set to 0.
                // 2. When creating a new timer in 'HandleIncomingProgressRecord', we will set the flag to 1 anyway

                if (_progPane != null)
                {
                    Dbg.Assert(_pendingProgress != null, "How can you have a progress pane and no backing data structure?");

                    _progPane.Hide();
                    _progPane = null;
                }

                _pendingProgress = null;

                if (SupportsVirtualTerminal && ExperimentalFeature.IsEnabled(ExperimentalFeature.PSAnsiProgressFeatureName) && PSStyle.Instance.Progress.UseOSCIndicator)
                {
                    // OSC sequence to turn off progress indicator
                    // https://github.com/microsoft/terminal/issues/6700
                    Console.Write("\x1b]9;4;0\x1b\\");
                }
            }
        }

        /// <summary>
        /// Invoked by ConsoleHostUserInterface.WriteProgress to update the set of outstanding activities for which
        /// ProgressRecords have been received.
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

                    // The timer will be auto restarted every 'UpdateTimerThreshold' ms
                    _progPaneUpdateTimer = new Timer(new TimerCallback(ProgressPaneUpdateTimerElapsed), null, UpdateTimerThreshold, UpdateTimerThreshold);
                }
            }

            if (Interlocked.CompareExchange(ref progPaneUpdateFlag, 0, 1) == 1 || record.RecordType == ProgressRecordType.Completed)
            {
                // Update the progress pane only when the timer set up the update flag or WriteProgress is completed.
                // As a result, we do not block WriteProgress and whole script and eliminate unnecessary console locks and updates.
                if (SupportsVirtualTerminal && ExperimentalFeature.IsEnabled(ExperimentalFeature.PSAnsiProgressFeatureName) && PSStyle.Instance.Progress.UseOSCIndicator)
                {
                    int percentComplete = record.PercentComplete;
                    if (percentComplete < 0)
                    {
                        // Write-Progress allows for negative percent complete, but not greater than 100
                        // but OSC sequence is limited from 0 to 100.
                        percentComplete = 0;
                    }

                    // OSC sequence to turn on progress indicator
                    // https://github.com/microsoft/terminal/issues/6700
                    Console.Write($"\x1b]9;4;1;{percentComplete}\x1b\\");
                }

                // If VT is not supported, we change ProgressView to classic
                if (!SupportsVirtualTerminal && ExperimentalFeature.IsEnabled(ExperimentalFeature.PSAnsiProgressFeatureName))
                {
                    PSStyle.Instance.Progress.View = ProgressView.Classic;
                }

                _progPane.Show(_pendingProgress);
            }
        }

        /// <summary>
        /// TimerCallback for '_progPaneUpdateTimer' to update 'progPaneUpdateFlag'
        /// </summary>
        private
        void
        ProgressPaneUpdateTimerElapsed(object sender)
        {
            Interlocked.CompareExchange(ref progPaneUpdateFlag, 1, 0);
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
        PostWrite(ReadOnlySpan<char> value, bool newLine)
        {
            PostWrite();

            if (_parent.IsTranscribing)
            {
                try
                {
                    _parent.WriteToTranscript(value, newLine);
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
                    _parent.WriteLineToTranscript(value);
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
        private Timer _progPaneUpdateTimer = null;

        private const int UpdateTimerThreshold = 200;

        private int progPaneUpdateFlag = 0;
    }
}   // namespace
