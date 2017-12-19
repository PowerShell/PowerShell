/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Get-PSCallStack.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PSCallStack", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113326")]
    [OutputType(typeof(CallStackFrame))]
    public class GetPSCallStackCommand : PSCmdlet
    {
        /// <summary>
        /// Get the call stack
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (CallStackFrame frame in Context.Debugger.GetCallStack())
            {
                WriteObject(frame);
            }
        }
    }
}