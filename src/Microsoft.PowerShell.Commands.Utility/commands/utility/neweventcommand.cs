//
//    Copyright (C) Microsoft.  All rights reserved.
//
using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Generates a new event notification.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "Event", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=135234")]
    [OutputType(typeof(PSEventArgs))]
    public class NewEventCommand : PSCmdlet
    {
        #region parameters

        /// <summary>
        /// Adds an event to the event queue
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string SourceIdentifier
        {
            get
            {
                return sourceIdentifier;
            }
            set
            {
                sourceIdentifier = value;
            }
        }
        private string sourceIdentifier = null;

        /// <summary>
        /// Data relating to this event
        /// </summary>
        [Parameter(Position = 1)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public PSObject Sender
        {
            get
            {
                return sender;
            }
            set
            {
                sender = value;
            }
        }
        private PSObject sender = null;

        /// <summary>
        /// Data relating to this event
        /// </summary>
        [Parameter(Position = 2)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public PSObject[] EventArguments
        {
            get
            {
                return eventArguments;
            }
            set
            {
                if (eventArguments != null)
                {
                    eventArguments = value;
                }
            }
        }
        private PSObject[] eventArguments = new PSObject[0];

        /// <summary>
        /// Data relating to this event
        /// </summary>
        [Parameter(Position = 3)]
        public PSObject MessageData
        {
            get
            {
                return messageData;
            }
            set
            {
                messageData = value;
            }
        }
        private PSObject messageData = null;

        #endregion parameters


        /// <summary>
        /// Add the event to the event queue
        /// </summary>
        protected override void EndProcessing()
        {
            object[] baseEventArgs = null;

            // Get the BaseObject from the event arguments
            if (eventArguments != null)
            {
                baseEventArgs = new object[eventArguments.Length];
                int loopCounter = 0;
                foreach (PSObject eventArg in eventArguments)
                {
                    if (eventArg != null)
                        baseEventArgs[loopCounter] = eventArg.BaseObject;

                    loopCounter++;
                }
            }

            Object messageSender = null;
            if (sender != null) { messageSender = sender.BaseObject; }

            // And then generate the event
            WriteObject(Events.GenerateEvent(sourceIdentifier, messageSender, baseEventArgs, messageData, true, false));
        }
    }
}