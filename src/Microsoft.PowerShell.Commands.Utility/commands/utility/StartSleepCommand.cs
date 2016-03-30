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
    [Cmdlet(VerbsLifecycle.Start, "Sleep", DefaultParameterSetName = "Seconds", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113407" )]
    public sealed class StartSleepCommand : PSCmdlet, IDisposable
    {
        private bool disposed = false;

        #region IDisposable
        /// <summary>
        ///  Dispose method of IDisposable interface.
        /// </summary>
        public void Dispose()
        {
            if (disposed == false)
            {
                if (waitHandle != null)
                {
                    waitHandle.Dispose();
                    waitHandle = null;
                }
                disposed = true;
            }
        }

        #endregion



        #region parameters

        /// <summary>
        /// Allows sleep time to be specified in seconds
        /// </summary>
        [Parameter(Position = 0,Mandatory = true,ParameterSetName = "Seconds",ValueFromPipeline=true,
                   ValueFromPipelineByPropertyName = true )]
        [ValidateRangeAttribute( 0, int.MaxValue/1000 )]
        public int Seconds
        {
            get
            {
                return seconds;
            }
            set
            {
                seconds = value;
            }
        }
        private int seconds;


        /// <summary>
        /// Allows sleep time to be specified in milliseconds
        /// </summary>
        [Parameter(Mandatory=true,ParameterSetName = "Milliseconds", ValueFromPipelineByPropertyName=true)]
        [ValidateRangeAttribute( 0, int.MaxValue )]
        public int Milliseconds
        {
            get
            {
                return milliseconds;
            }
            set
            {
                milliseconds = value;
            }
        }
        private int milliseconds;

        #endregion

        #region methods

        //Wait handle which is used by thread to sleep.
        ManualResetEvent waitHandle;

        //object used for synchornizes pipeline thread and stop thread
        //access to waitHandle
        object syncObject = new object();

        //this is set to true by stopProcessing
        bool stopping = false;

        /// <summary>
        /// This method causes calling thread to sleep for 
        /// specified milliseconds
        /// </summary>
        void Sleep(int milliSecondsToSleep)
        {
            lock (syncObject)
            {
                if (stopping == false)
                {
                    waitHandle = new ManualResetEvent(false);
                }
            }
            if (waitHandle != null)
            {
#if CORECLR //TODO:CORECLR bool WaitOne(int millisecondsTimeout,bool exitContext) is not available on CLR yet
                waitHandle.WaitOne(new TimeSpan(0, 0, 0, 0, milliSecondsToSleep));
#else
                waitHandle.WaitOne(new TimeSpan(0, 0, 0, 0, milliSecondsToSleep), true);
#endif
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void ProcessRecord()
        {
            int sleepTime = 0;

            switch ( ParameterSetName )
            {
                case "Seconds":
                    sleepTime = Seconds * 1000;
                    break;

                case "Milliseconds":
                    sleepTime = Milliseconds;
                    break;

                default:
                    Dbg.Diagnostics.Assert(false,"Only one of the specified parameter sets should be called." );
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
            lock (syncObject)
            {
                stopping = true;
                if (waitHandle != null)
                {
                    waitHandle.Set();
                }
            }
        }


        #endregion
    } // StartSleepCommand
} // namespace Microsoft.PowerShell.Commands

