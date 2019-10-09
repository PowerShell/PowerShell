// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Registers for an event on an object.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Register, "ObjectEvent", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135244")]
    [OutputType(typeof(PSEventJob))]
    public class RegisterObjectEventCommand : ObjectEventRegistrationBase
    {
        #region parameters

        /// <summary>
        /// The object on which to subscribe.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public PSObject InputObject
        {
            get
            {
                return _inputObject;
            }

            set
            {
                _inputObject = value;
            }
        }

        private PSObject _inputObject = null;

        /// <summary>
        /// The event name to subscribe.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        public string EventName
        {
            get
            {
                return _eventName;
            }

            set
            {
                _eventName = value;
            }
        }

        private string _eventName = null;

        #endregion parameters

        /// <summary>
        /// Returns the object that generates events to be monitored.
        /// </summary>
        protected override object GetSourceObject()
        {
            return _inputObject;
        }

        /// <summary>
        /// Returns the event name to be monitored on the input object.
        /// </summary>
        protected override string GetSourceObjectEventName()
        {
            return _eventName;
        }
    }
}
