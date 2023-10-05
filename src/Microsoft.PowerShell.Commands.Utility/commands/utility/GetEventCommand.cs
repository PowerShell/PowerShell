// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Gets events from the event queue.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Event", DefaultParameterSetName = "BySource", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097014")]    
    [OutputType(typeof(PSEventArgs))]
    public class GetEventCommand : PSCmdlet
    {
        #region parameters

        /// <summary>
        /// An identifier for this event subscription.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "BySource")]
        [ValidateNotNullOrEmpty]
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
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "ById")]
        [Alias("Id")]
        public int EventIdentifier
        {
            get
            {
                return _eventId;
            }

            set
            {
                _eventId = value;
            }
        }

        /// <summary>
        /// If set, will return results in descending order (most recent event first)
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter Descending { get; set; }

        /// <summary>
        /// If provided, will only return the first N results.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public long First { get; set; } = 0;

        /// <summary>
        /// If provided, will skip the first N results.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public long Skip { get; set; } = 0;

        private int _eventId = -1;

        #endregion parameters

        private WildcardPattern _matchPattern;

        /// <summary>
        /// Get the requested events.
        /// </summary>
        protected override void EndProcessing()
        {                        
            bool foundMatch = false;
            long outputCount = 0;
            long skipCount   = 0;
            // Go through all the received events and write them to the output
            // pipeline
            List<PSEventArgs> eventArgsCollection;
            lock (Events.ReceivedEvents.SyncRoot)
            {
                eventArgsCollection = new List<PSEventArgs>(Events.ReceivedEvents);
                if (this.Descending) {
                    eventArgsCollection.Reverse();
                }
            }

            foreach (PSEventArgs eventArg in eventArgsCollection)
            {
                // If they specified a event identifier and we don't match, continue
                if ((_sourceIdentifier != null) &&
                   (!_matchPattern.IsMatch(eventArg.SourceIdentifier)))
                {
                    continue;
                }

                // If they specified an event identifier and we don't match, continue
                if ((_eventId >= 0) &&
                    (eventArg.EventIdentifier != _eventId))
                {
                    continue;
                }

                if (Skip > 0 && skipCount < Skip) {
                    skipCount++;
                    continue;
                }
                
                if (First > 0 && outputCount >= First) {                    
                    break;
                }

                outputCount++;

                WriteObject(eventArg);
                foundMatch = true;
            }

            // Generate an error if we couldn't find the subscription identifier,
            // and no globbing was done.
            if (!foundMatch)
            {
                bool lookingForSource = (_sourceIdentifier != null) &&
                    (!WildcardPattern.ContainsWildcardCharacters(_sourceIdentifier));
                bool lookingForId = (_eventId >= 0);

                if (lookingForSource || lookingForId)
                {
                    object identifier = null;
                    string error = null;

                    if (lookingForSource)
                    {
                        identifier = _sourceIdentifier;
                        error = EventingStrings.SourceIdentifierNotFound;
                    }
                    else if (lookingForId)
                    {
                        identifier = _eventId;
                        error = EventingStrings.EventIdentifierNotFound;
                    }

                    ErrorRecord errorRecord = new(
                        new ArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture, error, identifier)),
                        "INVALID_SOURCE_IDENTIFIER",
                        ErrorCategory.InvalidArgument,
                        null);

                    WriteError(errorRecord);
                }
            }
        }
    }
}
