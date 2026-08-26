// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Threading;

using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Suspend shell, script, or runspace activity for the specified period of time.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "Sleep", DefaultParameterSetName = "Seconds", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097041")]
    public sealed class StartSleepCommand : PSCmdlet, IDisposable
    {
        #region IDisposable
        
        /// <summary>
        /// Release all resources.
        /// </summary>
        public void Dispose()
        {
            Interlocked.Exchange(ref _wakeEvent, null)?.Dispose();
        }

        #endregion

        #region parameters

        /// <summary>
        /// Allows sleep time to be specified in seconds.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Seconds", ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateRange(0.0, (double)(int.MaxValue / 1000))]
        public double Seconds { get; set; }

        /// <summary>
        /// Allows sleep time to be specified in milliseconds.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "Milliseconds", ValueFromPipelineByPropertyName = true)]
        [ValidateRange(0, int.MaxValue)]
        [Alias("ms")]
        public int Milliseconds { get; set; }

        /// <summary>
        /// Allows sleep time to be specified as a TimeSpan.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "FromTimeSpan", ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateRange(ValidateRangeKind.NonNegative)]
        [Alias("ts")]
        public TimeSpan Duration { get; set; }

        #endregion

        #region methods

        // Event used to wake up early from sleep when StopProcessing is called.
        private ManualResetEventSlim _wakeEvent = new(false);

        /// <summary>
        /// This method causes calling thread to sleep for specified milliseconds.
        /// </summary>
        private void Sleep(int milliSecondsToSleep)
        {
            _wakeEvent?.Wait(milliSecondsToSleep);
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            int sleepTime = 0;

            switch (ParameterSetName)
            {
                case "Seconds":
                    sleepTime = (int)(Seconds * 1000);
                    break;

                case "Milliseconds":
                    sleepTime = Milliseconds;
                    break;

                case "FromTimeSpan":
                    if (Duration.TotalMilliseconds > int.MaxValue)
                    {
                        PSArgumentException argumentException = PSTraceSource.NewArgumentException(
                            nameof(Duration),
                            StartSleepStrings.MaximumDurationExceeded,
                            TimeSpan.FromMilliseconds(int.MaxValue),
                            Duration);

                        ThrowTerminatingError(
                            new ErrorRecord(
                                argumentException,
                                "MaximumDurationExceeded",
                                ErrorCategory.InvalidArgument,
                                targetObject: null));
                    }

                    sleepTime = (int)Math.Floor(Duration.TotalMilliseconds);
                    break;

                default:
                    Dbg.Diagnostics.Assert(false, "Only one of the specified parameter sets should be called.");
                    break;
            }

            Sleep(sleepTime);
        }

        /// <summary>
        /// StopProcessing override.
        /// </summary>
        protected override void StopProcessing()
        {
            _wakeEvent?.Set();
        }

        #endregion
    }
}
