// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Threading;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Waits for a given event to arrive.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Wait, "Event", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097042")]
    [OutputType(typeof(PSEventArgs))]
    public class WaitEventCommand : PSCmdlet
    {
        #region parameters

        /// <summary>
        /// An identifier for this event subscription.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        public string SourceIdentifier
        {
            get
            {
                return _sourceIdentifier;
            }

            set
            {
                _sourceIdentifier = value;
                _matchPattern = WildcardPattern.Get(value, WildcardOptions.IgnoreCase);
            }
        }

        private string _sourceIdentifier = null;

        /// <summary>
        /// If timeout is specified, the cmdlet will only wait for this number of seconds.
        /// Value of -1 means never timeout.
        /// </summary>
        [Parameter]
        [Alias("TimeoutSec")]
        [ValidateRangeAttribute(-1, int.MaxValue)]
        public int Timeout
        {
            get
            {
                return (int)_timeoutTimespan.TotalSeconds;
            }

            set
            {
                _timeoutTimespan = TimeSpan.FromSeconds(value);
            }
        }

        /// <summary>
        /// If timespan is specified, the cmdlet will only wait for this timespan.
        /// Negative values mean never timeout.
        /// </summary>
        [Parameter]        
        public TimeSpan TimeSpan {
            get
            {
                return _timeoutTimespan;
            }

            set
            {
                _timeoutTimespan = value;
            }
        }

        private TimeSpan _timeoutTimespan = TimeSpan.FromSeconds(-1); // -1: infinite, this default is to wait for as long as it takes.

        #endregion parameters

        private readonly AutoResetEvent _eventArrived = new(false);
        private PSEventArgs _receivedEvent = null;
        private readonly object _receivedEventLock = new();
        private WildcardPattern _matchPattern;

        /// <summary>
        /// Wait for the event to arrive.
        /// </summary>
        protected override void ProcessRecord()
        {
            DateTime startTime = DateTime.UtcNow;

            // Subscribe to notification of events received
            Events.ReceivedEvents.PSEventReceived += ReceivedEvents_PSEventReceived;
            bool received = false;

            // Scan the queue to see if it's already arrived
            ScanEventQueue();

            // And wait for our event handler (or Control-C processor) to give us control
            PSLocalEventManager eventManager = (PSLocalEventManager)Events;

            while (!received)
            {
                if (_timeoutTimespan.TotalMilliseconds >= 0)
                {
                    if ((DateTime.UtcNow - startTime) > _timeoutTimespan)
                        break;
                }

                received = _eventArrived.WaitOne((int)(_timeoutTimespan.TotalMilliseconds / 100));

                eventManager.ProcessPendingActions();
            }

            // Unsubscribe, and write the event information we received
            Events.ReceivedEvents.PSEventReceived -= ReceivedEvents_PSEventReceived;

            if (_receivedEvent != null)
            {
                WriteObject(_receivedEvent);
            }
        }

        /// <summary>
        /// Handle Control-C.
        /// </summary>
        protected override void StopProcessing()
        {
            _eventArrived.Set();
        }

        private void ReceivedEvents_PSEventReceived(object sender, PSEventArgs e)
        {
            // If they want to wait on just any event
            if (_sourceIdentifier == null)
            {
                NotifyEvent(e);
            }
            // They are waiting on a specific one
            else
            {
                ScanEventQueue();
            }
        }

        // Go through all the received events. If one matches the subscription identifier,
        // break.
        private void ScanEventQueue()
        {
            lock (Events.ReceivedEvents.SyncRoot)
            {
                foreach (PSEventArgs eventArg in Events.ReceivedEvents)
                {
                    // If they specified a subscription identifier and we don't match, continue
                    if ((_matchPattern == null) || (_matchPattern.IsMatch(eventArg.SourceIdentifier)))
                    {
                        NotifyEvent(eventArg);
                        return;
                    }
                }
            }
        }

        // Notify that an event has arrived
        private void NotifyEvent(PSEventArgs e)
        {
            if (_receivedEvent == null)
            {
                lock (_receivedEventLock)
                {
                    if (_receivedEvent == null)
                    {
                        _receivedEvent = e;
                        _eventArrived.Set();
                    }
                }
            }
        }
    }
}
