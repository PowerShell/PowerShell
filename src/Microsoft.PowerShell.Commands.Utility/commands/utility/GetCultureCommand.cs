/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Returns the thread's current culture.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Culture", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113312")]
    [OutputType(typeof(System.Globalization.CultureInfo))]
    public sealed class GetCultureCommand : PSCmdlet
    {
        /// <summary>
        /// Output the current Culture info object
        /// </summary>
        protected override void BeginProcessing()
        {
            WriteObject(Host.CurrentCulture);
        } // EndProcessing
    } // GetCultureCommand
} // Microsoft.PowerShell.Commands


