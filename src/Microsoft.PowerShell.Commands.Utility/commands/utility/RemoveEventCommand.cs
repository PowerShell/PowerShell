// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Removes an event from the event queue.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "Event", SupportsShouldProcess = true, DefaultParameterSetName = "BySource", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135247")]
    public class RemoveEventCommand : PSCmdlet
    {
        #region parameters

        /// <summary>
        /// A source identifier for this event subscription.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "BySource")]
        public string SourceIdentifier
        {
            get
            {
                return _sourceIdentifier;
            }

            set
            {
                _sourceIdentifier = value;

                if (value != null)
                {
                    _matchPattern = WildcardPattern.Get(value, WildcardOptions.IgnoreCase);
                }
            }
        }

        private string _sourceIdentifier = null;

        /// <summary>
        /// An identifier for this event subscription.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByIdentifier")]
        public int EventIdentifier
        {
            get
            {
                return _eventIdentifier;
            }

            set
            {
                _eventIdentifier = value;
            }
        }

        private int _eventIdentifier = -1;

        #endregion parameters

        private WildcardPattern _matchPattern;

        /// <summary>
        /// Remove the event from the queue.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Go through all the received events and write them to the output
            // pipeline
            bool foundMatch = false;

            lock (Events.ReceivedEvents.SyncRoot)
            {
                PSEventArgsCollection currentEvents = Events.ReceivedEvents;

                for (int eventCounter = currentEvents.Count; eventCounter > 0; eventCounter--)
                {
                    PSEventArgs currentEvent = currentEvents[eventCounter - 1];

                    // If they specified a event identifier and we don't match, continue
                    if ((_sourceIdentifier != null) &&
                       (!_matchPattern.IsMatch(currentEvent.SourceIdentifier)))
                    {
                        continue;
                    }

                    // If they specified a TimeGenerated and we don't match, continue
                    if ((_eventIdentifier >= 0) &&
                        (currentEvent.EventIdentifier != _eventIdentifier))
                    {
                        continue;
                    }

                    foundMatch = true;
                    if (ShouldProcess(
                        string.Format(
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
            if ((_sourceIdentifier != null) &&
               (!WildcardPattern.ContainsWildcardCharacters(_sourceIdentifier)) &&
               (!foundMatch))
            {
                ErrorRecord errorRecord = new ErrorRecord(
                    new ArgumentException(
                        string.Format(
                            System.Globalization.CultureInfo.CurrentCulture,
                            EventingStrings.SourceIdentifierNotFound, _sourceIdentifier)),
                    "INVALID_SOURCE_IDENTIFIER",
                    ErrorCategory.InvalidArgument,
                    null);

                WriteError(errorRecord);
            }
            else if ((_eventIdentifier >= 0) && (!foundMatch))
            {
                ErrorRecord errorRecord = new ErrorRecord(
                    new ArgumentException(
                        string.Format(
                            System.Globalization.CultureInfo.CurrentCulture,
                            EventingStrings.EventIdentifierNotFound, _eventIdentifier)),
                    "INVALID_EVENT_IDENTIFIER",
                    ErrorCategory.InvalidArgument,
                    null);

                WriteError(errorRecord);
            }
        }
    }
}
