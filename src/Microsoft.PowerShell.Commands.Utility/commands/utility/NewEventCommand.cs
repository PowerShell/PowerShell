// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Generates a new event notification.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "Event", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135234")]
    [OutputType(typeof(PSEventArgs))]
    public class NewEventCommand : PSCmdlet
    {
        #region parameters

        /// <summary>
        /// Adds an event to the event queue.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string SourceIdentifier
        {
            get
            {
                return _sourceIdentifier;
            }

            set
            {
                _sourceIdentifier = value;
            }
        }

        private string _sourceIdentifier = null;

        /// <summary>
        /// Data relating to this event.
        /// </summary>
        [Parameter(Position = 1)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public PSObject Sender
        {
            get
            {
                return _sender;
            }

            set
            {
                _sender = value;
            }
        }

        private PSObject _sender = null;

        /// <summary>
        /// Data relating to this event.
        /// </summary>
        [Parameter(Position = 2)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public PSObject[] EventArguments
        {
            get
            {
                return _eventArguments;
            }

            set
            {
                if (_eventArguments != null)
                {
                    _eventArguments = value;
                }
            }
        }

        private PSObject[] _eventArguments = Array.Empty<PSObject>();

        /// <summary>
        /// Data relating to this event.
        /// </summary>
        [Parameter(Position = 3)]
        public PSObject MessageData
        {
            get
            {
                return _messageData;
            }

            set
            {
                _messageData = value;
            }
        }

        private PSObject _messageData = null;

        #endregion parameters

        /// <summary>
        /// Add the event to the event queue.
        /// </summary>
        protected override void EndProcessing()
        {
            object[] baseEventArgs = null;

            // Get the BaseObject from the event arguments
            if (_eventArguments != null)
            {
                baseEventArgs = new object[_eventArguments.Length];
                int loopCounter = 0;
                foreach (PSObject eventArg in _eventArguments)
                {
                    if (eventArg != null)
                        baseEventArgs[loopCounter] = eventArg.BaseObject;

                    loopCounter++;
                }
            }

            object messageSender = null;
            if (_sender != null) { messageSender = _sender.BaseObject; }

            // And then generate the event
            WriteObject(Events.GenerateEvent(_sourceIdentifier, messageSender, baseEventArgs, _messageData, true, false));
        }
    }
}
