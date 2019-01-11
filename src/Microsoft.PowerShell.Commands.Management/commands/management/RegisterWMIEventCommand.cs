// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Management;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Registers for an event on an object.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Register, "WmiEvent", DefaultParameterSetName = "class",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135245", RemotingCapability = RemotingCapability.OwnedByCommand)]
    public class RegisterWmiEventCommand : ObjectEventRegistrationBase
    {
        #region parameters

        /// <summary>
        /// The WMI namespace to use.
        /// </summary>
        [Parameter]
        [Alias("NS")]
        public string Namespace { get; set; } = "root\\cimv2";

        /// <summary>
        /// The credential to use.
        /// </summary>
        [Parameter]
        [Credential()]
        public PSCredential Credential { get; set; }

        /// <summary>
        /// The ComputerName in which to query.
        /// </summary>
        [Parameter]
        [Alias("Cn")]
        [ValidateNotNullOrEmpty]
        public string ComputerName { get; set; } = "localhost";

        /// <summary>
        /// The WMI class to use.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "class")]
        public string Class { get; set; } = null;

        /// <summary>
        /// The query string to search for objects.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "query")]
        public string Query { get; set; } = null;

        /// <summary>
        /// Timeout in milliseconds.
        /// </summary>
        [Parameter]
        [Alias("TimeoutMSec")]
        public Int64 Timeout
        {
            get
            {
                return _timeOut;
            }

            set
            {
                _timeOut = value;
                _timeoutSpecified = true;
            }
        }

        private Int64 _timeOut = 0;
        private bool _timeoutSpecified = false;

        #endregion parameters
        #region helper functions
        private string BuildEventQuery(string objectName)
        {
            StringBuilder returnValue = new StringBuilder("select * from ");
            returnValue.Append(objectName);
            return returnValue.ToString();
        }

        private string GetScopeString(string computer, string namespaceParameter)
        {
            StringBuilder returnValue = new StringBuilder("\\\\");
            returnValue.Append(computer);
            returnValue.Append("\\");
            returnValue.Append(namespaceParameter);
            return returnValue.ToString();
        }
        #endregion helper functions

        /// <summary>
        /// Returns the object that generates events to be monitored.
        /// </summary>
        protected override object GetSourceObject()
        {
            string wmiQuery = this.Query;
            if (this.Class != null)
            {
                // Validate class format
                for (int i = 0; i < this.Class.Length; i++)
                {
                    if (char.IsLetterOrDigit(this.Class[i]) || this.Class[i].Equals('_'))
                    {
                        continue;
                    }

                    ErrorRecord errorRecord = new ErrorRecord(
                        new ArgumentException(
                            string.Format(
                                Thread.CurrentThread.CurrentCulture,
                                "Class", this.Class)),
                        "INVALID_QUERY_IDENTIFIER",
                        ErrorCategory.InvalidArgument,
                        null);
                    errorRecord.ErrorDetails = new ErrorDetails(this, "WmiResources", "WmiInvalidClass");

                    ThrowTerminatingError(errorRecord);
                    return null;
                }

                wmiQuery = BuildEventQuery(this.Class);
            }

            ConnectionOptions conOptions = new ConnectionOptions();
            if (this.Credential != null)
            {
                System.Net.NetworkCredential cred = this.Credential.GetNetworkCredential();
                if (string.IsNullOrEmpty(cred.Domain))
                {
                    conOptions.Username = cred.UserName;
                }
                else
                {
                    conOptions.Username = cred.Domain + "\\" + cred.UserName;
                }

                conOptions.Password = cred.Password;
            }

            ManagementScope scope = new ManagementScope(GetScopeString(ComputerName, this.Namespace), conOptions);
            EventWatcherOptions evtOptions = new EventWatcherOptions();

            if (_timeoutSpecified)
            {
                evtOptions.Timeout = new TimeSpan(_timeOut * 10000);
            }

            ManagementEventWatcher watcher = new ManagementEventWatcher(scope, new EventQuery(wmiQuery), evtOptions);
            return watcher;
        }

        /// <summary>
        /// Returns the event name to be monitored on the input object.
        /// </summary>
        protected override string GetSourceObjectEventName()
        {
            return "EventArrived";
        }

        /// <summary>
        /// Processes the event subscriber after the base class has registered.
        /// </summary>
        protected override void EndProcessing()
        {
            base.EndProcessing();

            // Register for the "Unsubscribed" event so that we can stop the
            // event watcher.
            PSEventSubscriber newSubscriber = NewSubscriber;
            if (newSubscriber != null)
            {
                newSubscriber.Unsubscribed += new PSEventUnsubscribedEventHandler(newSubscriber_Unsubscribed);
            }
        }

        private void newSubscriber_Unsubscribed(object sender, PSEventUnsubscribedEventArgs e)
        {
            ManagementEventWatcher watcher = sender as ManagementEventWatcher;
            if (watcher != null)
            {
                watcher.Stop();
            }
        }
    }
}
