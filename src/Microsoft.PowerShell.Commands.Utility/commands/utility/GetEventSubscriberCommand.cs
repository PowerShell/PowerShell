//
//    Copyright (C) Microsoft.  All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Lists all event subscribers.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "EventSubscriber", DefaultParameterSetName="BySource", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=135155")]
    [OutputType(typeof(PSEventSubscriber))]
    public class GetEventSubscriberCommand : PSCmdlet
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
        public int SubscriptionId
        {
            get
            {
                return subscriptionId;
            }
            set
            {
                subscriptionId = value;
            }
        }
        private int subscriptionId = -1;


        /// <summary>
        /// Also show supporting events
        /// </summary>
        [Parameter(Position = 1)]
        public SwitchParameter Force
        {
            get
            {
                return force;
            }
            set
            {
                force = value;
            }
        }
        private SwitchParameter force;

        #endregion parameters

        WildcardPattern matchPattern;

        /// <summary>
        /// Get the subscribers
        /// </summary>
        protected override void ProcessRecord()
        {
            bool foundMatch = false;

            // Go through all the received events and write them to the output
            // pipeline
            List<PSEventSubscriber> subscribers = new List<PSEventSubscriber>(Events.Subscribers);
            foreach(PSEventSubscriber subscriber in subscribers)
            {
                // If they specified a event identifier and we don't match, continue
                if((sourceIdentifier != null) &&
                   (! matchPattern.IsMatch(subscriber.SourceIdentifier)))
                {
                    continue;
                }

                // If they specified a subscription identifier and we don't match, continue
                if((subscriptionId >= 0) &&
                    (subscriber.SubscriptionId != subscriptionId))
                {
                    continue;
                }

                // Don't display support events by default
                if (subscriber.SupportEvent && (!Force))
                {
                    continue;
                }
               
                WriteObject(subscriber);
                foundMatch = true;
            }

            // Generate an error if we couldn't find the subscription identifier,
            // and no globbing was done.
            if(! foundMatch)
            {
                bool lookingForSource = (sourceIdentifier != null) &&
                    (! WildcardPattern.ContainsWildcardCharacters(sourceIdentifier));
                bool lookingForId = (subscriptionId >= 0);
                
                if(lookingForSource || lookingForId)
                {
                    object identifier = null;
                    string error = null;

                    if(lookingForSource)
                    {
                        identifier = sourceIdentifier;
                        error = EventingStrings.EventSubscriptionSourceNotFound;
                    }
                    else if(lookingForId)
                    {
                        identifier = subscriptionId;
                        error = EventingStrings.EventSubscriptionNotFound;
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