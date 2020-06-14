// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Returns the thread's current UI culture.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "UICulture", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096613")]
    [OutputType(typeof(System.Globalization.CultureInfo))]
    public sealed class GetUICultureCommand : PSCmdlet
    {
        /// <summary>
        /// Output the current UI Culture info object.
        /// </summary>
        protected override void BeginProcessing()
        {
            WriteObject(Host.CurrentUICulture);
        }
    }
}
