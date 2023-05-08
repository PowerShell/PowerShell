// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Disable-PSBreakpoint.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Disable, "PSBreakpoint", SupportsShouldProcess = true, DefaultParameterSetName = BreakpointParameterSetName, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096498")]
    [OutputType(typeof(Breakpoint))]
    public class DisablePSBreakpointCommand : PSBreakpointUpdaterCommandBase
    {
        #region parameters

        /// <summary>
        /// Gets or sets the parameter -passThru which states whether the
        /// command should place the breakpoints it processes in the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        #endregion parameters

        #region overrides

        /// <summary>
        /// Disables the given breakpoint.
        /// </summary>
        protected override void ProcessBreakpoint(Breakpoint breakpoint)
        {
            breakpoint = Runspace.Debugger.DisableBreakpoint(breakpoint);

            if (PassThru)
            {
                base.ProcessBreakpoint(breakpoint);
            }
        }

        #endregion overrides
    }
}
