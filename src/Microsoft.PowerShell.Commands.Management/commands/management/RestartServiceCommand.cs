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
    /// This class implements the restart-service command on Unix platforms.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Restart, "Service", DefaultParameterSetName = "InputObject", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097059")]
    [OutputType(typeof(UnixServiceController))]
    public sealed class RestartServiceCommand : ServiceOperationBaseCommand
    {
        /// <summary>
        /// Gets or sets a value indicating whether to force the service to stop even if
        /// it has dependent services. The parameter is accepted for script compatibility
        /// with Windows; on Unix, the service manager itself handles service ordering.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Stop and restart the services.
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

                if (DoRestartService(serviceController) && PassThru)
                {
                    WriteObject(
                        UnixServiceCmdletHelper.AddDefaultDisplayProperties(
                            ApplyExpectedStatus(serviceController, ServiceControllerStatus.Running)));
                }
            }
        }
    }
}

#endif
