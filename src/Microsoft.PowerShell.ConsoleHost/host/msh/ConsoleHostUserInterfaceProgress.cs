/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/



using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Host;

using Dbg = System.Management.Automation.Diagnostics;


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

            if (progPane != null)
            {
                Dbg.Assert(pendingProgress != null, "How can you have a progress pane and no backing data structure?");

                progPane.Hide();
                progPane = null;
            }
            pendingProgress = null;
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

            if (pendingProgress == null)
            {
                Dbg.Assert(progPane == null, "If there is no data struct, there shouldn't be a pane, either.");

                pendingProgress = new PendingProgress();
            }

            pendingProgress.Update(sourceId, record);

            if (progPane == null)
            {
                // This is the first time we've received a progress record.  Create a pane to show it, and 
                // then show it.

                progPane = new ProgressPane(this);
            }
            progPane.Show(pendingProgress);
        }


        

        private
        void
        PreWrite()
        {
            if (progPane != null)
            {
                progPane.Hide();
            }
        }



        private
        void
        PostWrite()
        {
            if (progPane != null)
            {
                progPane.Show();
            }
        }

        
        
        private
        void
        PostWrite(string value)
        {
            PostWrite();

            if (parent.IsTranscribing)
            {
                try
                {
                    parent.WriteToTranscript(value);
                }
                catch (Exception e)
                {
                    ConsoleHost.CheckForSevereException(e);
                    parent.IsTranscribing = false;
                }

            }
        }



        private
        void
        PreRead()
        {
            if (progPane != null)
            {
                progPane.Hide();
            }
        }



        private
        void
        PostRead()
        {
            if (progPane != null)
            {
                progPane.Show();
            }
        }



        private
        void
        PostRead(string value)
        {
            PostRead();

            if (parent.IsTranscribing)
            {
                try
                {
                    // Reads always terminate with the enter key, so add that.
                	parent.WriteToTranscript(value + Crlf);
                }
                catch (Exception e)
                {
                    ConsoleHost.CheckForSevereException(e);
                    parent.IsTranscribing = false;
                }
            }
        }

       
        
        private ProgressPane progPane = null;
        private PendingProgress pendingProgress = null;
    }



}   // namespace 



