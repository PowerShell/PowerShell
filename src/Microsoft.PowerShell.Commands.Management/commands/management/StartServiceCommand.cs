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
    /// This class implements the start-service command on Unix platforms.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "Service", DefaultParameterSetName = "InputObject", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097051")]
    [OutputType(typeof(UnixServiceController))]
    public sealed class StartServiceCommand : ServiceOperationBaseCommand
    {
        /// <summary>
        /// Start the services.
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

                if (DoStartService(serviceController) && PassThru)
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
