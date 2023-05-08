// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Lists all event subscribers.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "EventSubscriber", DefaultParameterSetName = "BySource", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096607")]
    [OutputType(typeof(PSEventSubscriber))]
    public class GetEventSubscriberCommand : PSCmdlet
    {
        #region parameters

        /// <summary>
        /// An identifier for this event subscription.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "BySource")]
        [ValidateNotNullOrEmpty()]
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
        public int SubscriptionId { get; set; } = -1;

        /// <summary>
        /// Also show supporting events.
        /// </summary>
        [Parameter(Position = 1)]
        public SwitchParameter Force { get; set; }

        #endregion parameters

        private WildcardPattern _matchPattern;

        /// <summary>
        /// Get the subscribers.
        /// </summary>
        protected override void ProcessRecord()
        {
            bool foundMatch = false;

            // Go through all the received events and write them to the output
            // pipeline
            List<PSEventSubscriber> subscribers = new(Events.Subscribers);
            foreach (PSEventSubscriber subscriber in subscribers)
            {
                // If they specified a event identifier and we don't match, continue
                if ((_sourceIdentifier != null) &&
                   (!_matchPattern.IsMatch(subscriber.SourceIdentifier)))
                {
                    continue;
                }

                // If they specified a subscription identifier and we don't match, continue
                if ((SubscriptionId >= 0) &&
                    (subscriber.SubscriptionId != SubscriptionId))
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
            if (!foundMatch)
            {
                bool lookingForSource = (_sourceIdentifier != null) &&
                    (!WildcardPattern.ContainsWildcardCharacters(_sourceIdentifier));
                bool lookingForId = (SubscriptionId >= 0);

                if (lookingForSource || lookingForId)
                {
                    object identifier = null;
                    string error = null;

                    if (lookingForSource)
                    {
                        identifier = _sourceIdentifier;
                        error = EventingStrings.EventSubscriptionSourceNotFound;
                    }
                    else if (lookingForId)
                    {
                        identifier = SubscriptionId;
                        error = EventingStrings.EventSubscriptionNotFound;
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
