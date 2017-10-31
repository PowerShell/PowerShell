/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/


using System.Management.Automation;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    ///
    /// Writes the PSHost object to the success stream
    ///
    /// </summary>

    [Cmdlet(VerbsCommon.Get, "Host", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113318", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(System.Management.Automation.Host.PSHost))]
    public
    class GetHostCommand : PSCmdlet
    {
        /// <summary>
        ///
        /// See base class
        ///
        /// </summary>
        protected override void BeginProcessing()
        {
            WriteObject(this.Host);
        }
    }
}

