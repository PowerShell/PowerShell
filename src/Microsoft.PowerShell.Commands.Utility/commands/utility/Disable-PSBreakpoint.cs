// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Disable-PSBreakpoint.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Disable, "PSBreakpoint", SupportsShouldProcess = true, DefaultParameterSetName = "Breakpoint", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113294")]
    [OutputType(typeof(Breakpoint))]
    public class DisablePSBreakpointCommand : PSBreakpointCommandBase
    {
        /// <summary>
        /// Gets or sets the parameter -passThru which states whether the
        /// command should place the breakpoints it processes in the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get
            {
                return _passThru;
            }

            set
            {
                _passThru = value;
            }
        }

        private bool _passThru;

        /// <summary>
        /// Disables the given breakpoint.
        /// </summary>
        protected override void ProcessBreakpoint(Breakpoint breakpoint)
        {
            this.Context.Debugger.DisableBreakpoint(breakpoint);

            if (_passThru)
            {
                WriteObject(breakpoint);
            }
        }
    }
}
