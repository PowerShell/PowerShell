// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Remove-PSBreakpoint.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PSBreakpoint", SupportsShouldProcess = true, DefaultParameterSetName = BreakpointParameterSetName,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113375")]
    public class RemovePSBreakpointCommand : PSBreakpointUpdaterCommandBase
    {
        #region overrides

        /// <summary>
        /// Removes the given breakpoint.
        /// </summary>
        protected override void ProcessBreakpoint(Breakpoint breakpoint)
        {
            Runspace.Debugger.RemoveBreakpoint(breakpoint);
        }

        #endregion overrides
    }
}
