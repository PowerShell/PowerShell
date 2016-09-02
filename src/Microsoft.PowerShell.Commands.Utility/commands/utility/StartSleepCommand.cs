/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Threading;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Suspend shell, script, or runspace activity for the specified period of time.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "Sleep", DefaultParameterSetName = "Seconds", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113407")]
    public sealed class StartSleepCommand : PSCmdlet, IDisposable
    {
        private bool _disposed = false;

        #region IDisposable
        /// <summary>
        ///  Dispose method of IDisposable interface.
        /// </summary>
        public void Dispose()
        {
            if (_disposed == false)
            {
                if (_waitHandle != null)
                {
                    _waitHandle.Dispose();
                    _waitHandle = null;
                }
                _disposed = true;
            }
        }

        #endregion



        #region parameters

        /// <summary>
        /// Allows sleep time to be specified in seconds
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Seconds", ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateRangeAttribute(0, int.MaxValue / 1000)]
        public int Seconds { get; set; }


        /// <summary>
        /// Allows sleep time to be specified in milliseconds
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "Milliseconds", ValueFromPipelineByPropertyName = true)]
        [ValidateRangeAttribute(0, int.MaxValue)]
        public int Milliseconds { get; set; }

        #endregion

        #region methods

        //Wait handle which is used by thread to sleep.
        private ManualResetEvent _waitHandle;

        //object used for synchronizes pipeline thread and stop thread
        //access to waitHandle
        private object _syncObject = new object();

        //this is set to true by stopProcessing
        private bool _stopping = false;

        /// <summary>
        /// This method causes calling thread to sleep for 
        /// specified milliseconds
        /// </summary>
        private void Sleep(int milliSecondsToSleep)
        {
            lock (_syncObject)
            {
                if (_stopping == false)
                {
                    _waitHandle = new ManualResetEvent(false);
                }
            }
            if (_waitHandle != null)
            {
#if CORECLR //TODO:CORECLR bool WaitOne(int millisecondsTimeout,bool exitContext) is not available on CLR yet
                _waitHandle.WaitOne(new TimeSpan(0, 0, 0, 0, milliSecondsToSleep));
#else
                _waitHandle.WaitOne(new TimeSpan(0, 0, 0, 0, milliSecondsToSleep), true);
#endif
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void ProcessRecord()
        {
            int sleepTime = 0;

            switch (ParameterSetName)
            {
                case "Seconds":
                    sleepTime = Seconds * 1000;
                    break;

                case "Milliseconds":
                    sleepTime = Milliseconds;
                    break;

                default:
                    Dbg.Diagnostics.Assert(false, "Only one of the specified parameter sets should be called.");
                    break;
            }

            Sleep(sleepTime);
        } // EndProcessing

        /// <summary>
        /// stopprocessing override
        /// </summary>
        protected override
        void
        StopProcessing()
        {
            lock (_syncObject)
            {
                _stopping = true;
                if (_waitHandle != null)
                {
                    _waitHandle.Set();
                }
            }
        }

        #endregion
    } // StartSleepCommand
} // namespace Microsoft.PowerShell.Commands

