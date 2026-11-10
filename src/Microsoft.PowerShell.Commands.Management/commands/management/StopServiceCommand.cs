// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if UNIX

using System.Collections.Generic;
using System.Management.Automation;
using System.ServiceProcess;

namespace Microsoft.PowerShell.Commands
{

    /// <summary>
    /// This class implements the stop-service command on Unix platforms.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "Service", DefaultParameterSetName = "InputObject", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097052")]
    [OutputType(typeof(UnixServiceController))]
    public sealed class StopServiceCommand : ServiceOperationBaseCommand
    {
        /// <summary>
        /// Gets or sets a value indicating whether to force the service to stop even if
        /// it has dependent services. The parameter is accepted for script compatibility
        /// with Windows; on Unix, the service manager itself handles service ordering.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to return immediately instead of
        /// waiting for the service to reach the stopped state, when the service manager
        /// supports it.
        /// </summary>
        [Parameter]
        public SwitchParameter NoWait { get; set; }

        /// <summary>
        /// Stop the services.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (UnixServiceController serviceController in MatchingServices())
            {
                // confirm the operation first
                // this is always false if WhatIf is set
                if (!ShouldProcessServiceOperation(serviceController))
                {
                    continue;
                }

                if (DoStopService(serviceController, noWait: NoWait.IsPresent) && PassThru)
                {
                    WriteObject(
                        UnixServiceCmdletHelper.AddDefaultDisplayProperties(
                            ApplyExpectedStatus(serviceController, ServiceControllerStatus.Stopped)));
                }
            }
        }
    }
}

#endif
