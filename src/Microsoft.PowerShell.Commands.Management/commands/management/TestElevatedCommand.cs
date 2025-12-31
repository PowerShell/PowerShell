// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command to determine if the current PowerShell session is running with elevated (administrator) privileges.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "Elevated", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097231")]
    [OutputType(typeof(bool))]
    public class TestElevatedCommand : PSCmdlet
    {
        /// <summary>
        /// Tests if the current session is running with elevated privileges.
        /// </summary>
        protected override void EndProcessing()
        {
            WriteObject(Platform.IsElevated);
        }
    }
}
