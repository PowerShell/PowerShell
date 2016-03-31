//
//    Copyright (C) Microsoft.  All rights reserved.
//
using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Removes an event from the event queue.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "Event", SupportsShouldProcess = true, DefaultParameterSetName = "BySource", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=135247")]
    public class RemoveEventCommand : PSCmdlet
    {
        #region parameters

        /// <summary>
        /// A source identifier for this event subscription
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName="BySource")]
        public string SourceIdentifier
        {
            get
            {
                return sourceIdentifier;
            }
            set
            {
                sourceIdentifier = value;

                if(value != null)
                {
                    matchPattern = WildcardPattern.Get(value, WildcardOptions.IgnoreCase);
                }
            }
        }
        private string sourceIdentifier = null;

        /// <summary>
        /// An identifier for this event subscription
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName="ByIdentifier")]
        public int EventIdentifier
        {
            get
            {
                return eventIdentifier;
            }
            set
            {
                eventIdentifier = value;
            }
        }
        private int eventIdentifier = -1;

        #endregion parameters

        WildcardPattern matchPattern;

        /// <summary>
        /// Remove the event from the queue
        /// </summary>
        protected override void ProcessRecord()
        {
            // Go through all the received events and write them to the output
            // pipeline
            bool foundMatch = false;

            lock(Events.ReceivedEvents.SyncRoot)
            {
                PSEventArgsCollection currentEvents = Events.ReceivedEvents;

                for(int eventCounter = currentEvents.Count; eventCounter > 0; eventCounter--)
                {
                    PSEventArgs currentEvent = currentEvents[eventCounter - 1];

                    // If they specified a event identifier and we don't match, continue
                    if((sourceIdentifier != null) &&
                       (! matchPattern.IsMatch(currentEvent.SourceIdentifier)))
                    {
                        continue;
                    }

                    // If they specified a TimeGenerated and we don't match, continue
                    if ((eventIdentifier >= 0) &&
                        (currentEvent.EventIdentifier != eventIdentifier))
                    {
                        continue;
                    }

                    foundMatch = true;
                    if(ShouldProcess(
                        String.Format(
                            System.Globalization.CultureInfo.CurrentCulture,                    
                            EventingStrings.EventResource,
                            currentEvent.SourceIdentifier),
                        EventingStrings.Remove))
                    {
                        currentEvents.RemoveAt(eventCounter - 1);
                    }
                }
            }

            // Generate an error if we couldn't find the subscription identifier,
            // and no globbing was done.
            if((sourceIdentifier != null) &&
               (! WildcardPattern.ContainsWildcardCharacters(sourceIdentifier)) &&
               (! foundMatch))
            {
                ErrorRecord errorRecord = new ErrorRecord(
                    new ArgumentException(
                        String.Format(
                            System.Globalization.CultureInfo.CurrentCulture,
                            EventingStrings.SourceIdentifierNotFound, sourceIdentifier)),
                    "INVALID_SOURCE_IDENTIFIER",
                    ErrorCategory.InvalidArgument,
                    null);

                WriteError(errorRecord);
            }
            else if((eventIdentifier >= 0) && (! foundMatch))
            {
                ErrorRecord errorRecord = new ErrorRecord(
                    new ArgumentException(
                        String.Format(
                            System.Globalization.CultureInfo.CurrentCulture,
                            EventingStrings.EventIdentifierNotFound, eventIdentifier)),
                    "INVALID_EVENT_IDENTIFIER",
                    ErrorCategory.InvalidArgument,
                    null);

                WriteError(errorRecord);
            }
        }
    }
}