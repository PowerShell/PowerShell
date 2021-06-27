// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Writes the PSHost object to the success stream.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Host", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097110", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(System.Management.Automation.Host.PSHost))]
    public
    class GetHostCommand : PSCmdlet
    {
        /// <summary>
        /// See base class.
        /// </summary>
        protected override void BeginProcessing()
        {
            WriteObject(this.Host);
        }
    }
}
