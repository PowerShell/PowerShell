//
//    Copyright (C) Microsoft.  All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Gets events from the event queue.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Event", DefaultParameterSetName = "BySource", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113453")]
    [OutputType(typeof(PSEventArgs))]
    public class GetEventCommand : PSCmdlet
    {
        #region parameters

        /// <summary>
        /// An identifier for this event subscription
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "BySource")]
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
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "ById")]
        [Alias("Id")]
        public int EventIdentifier
        {
            get
            {
                return eventId;
            }
            set
            {
                eventId = value;
            }
        }
        private int eventId = -1;
       

        #endregion parameters

        WildcardPattern matchPattern;

        /// <summary>
        /// Get the requested events
        /// </summary>
        protected override void EndProcessing()
        {
            bool foundMatch = false;

            // Go through all the received events and write them to the output
            // pipeline
            List<PSEventArgs> eventArgsCollection;
            lock (Events.ReceivedEvents.SyncRoot)
            {
                eventArgsCollection = new List<PSEventArgs>(Events.ReceivedEvents);
            }

            foreach(PSEventArgs eventArg in eventArgsCollection)
            {
                // If they specified a event identifier and we don't match, continue
                if((sourceIdentifier != null) &&
                   (! matchPattern.IsMatch(eventArg.SourceIdentifier)))
                {
                    continue;
                }

                // If they specified an event identifier and we don't match, continue
                if((eventId >= 0) &&
                    (eventArg.EventIdentifier != eventId))
                {
                    continue;
                }
               
                WriteObject(eventArg);
                foundMatch = true;
            }

            // Generate an error if we couldn't find the subscription identifier,
            // and no globbing was done.
            if(! foundMatch)
            {
                bool lookingForSource = (sourceIdentifier != null) &&
                    (! WildcardPattern.ContainsWildcardCharacters(sourceIdentifier));
                bool lookingForId = (eventId >= 0);
                
                if(lookingForSource || lookingForId)
                {
                    object identifier = null;
                    string error = null;

                    if(lookingForSource)
                    {
                        identifier = sourceIdentifier;
                        error = EventingStrings.SourceIdentifierNotFound;
                    }
                    else if(lookingForId)
                    {
                        identifier = eventId;
                        error = EventingStrings.EventIdentifierNotFound;
                    }

                    ErrorRecord errorRecord = new ErrorRecord(
                        new ArgumentException(String.Format(System.Globalization.CultureInfo.CurrentCulture, error, identifier)),
                        "INVALID_SOURCE_IDENTIFIER",
                        ErrorCategory.InvalidArgument,
                        null);

                    WriteError(errorRecord);
                }
             }
        }
    }
}