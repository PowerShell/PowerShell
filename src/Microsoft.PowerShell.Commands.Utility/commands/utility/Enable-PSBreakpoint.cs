// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Base class for Enable/Disable/Remove-PSBreakpoint.
    /// </summary>
    public abstract class PSBreakpointCommandBase : PSCmdlet
    {
        /// <summary>
        /// The breakpoint to enable.
        /// </summary>
        [Parameter(ParameterSetName = "Breakpoint", ValueFromPipeline = true, Position = 0, Mandatory = true)]
        [ValidateNotNull]
        public Breakpoint[] Breakpoint
        {
            get
            {
                return _breakpoints;
            }

            set
            {
                _breakpoints = value;
            }
        }

        private Breakpoint[] _breakpoints;

        /// <summary>
        /// The Id of the breakpoint to enable.
        /// </summary>
        [Parameter(ParameterSetName = "Id", ValueFromPipelineByPropertyName = true, Position = 0, Mandatory = true)]
        [ValidateNotNull]
        public int[] Id
        {
            get
            {
                return _ids;
            }

            set
            {
                _ids = value;
            }
        }

        private int[] _ids;

        /// <summary>
        /// Gathers the list of breakpoints to process and calls ProcessBreakpoints.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (ParameterSetName.Equals("Breakpoint", StringComparison.OrdinalIgnoreCase))
            {
                foreach (Breakpoint breakpoint in _breakpoints)
                {
                    if (ShouldProcessInternal(breakpoint.ToString()))
                    {
                        ProcessBreakpoint(breakpoint);
                    }
                }
            }
            else
            {
                Debug.Assert(ParameterSetName.Equals("Id", StringComparison.OrdinalIgnoreCase));

                foreach (int i in _ids)
                {
                    Breakpoint breakpoint = this.Context.Debugger.GetBreakpoint(i);

                    if (breakpoint == null)
                    {
                        WriteError(
                            new ErrorRecord(
                                new ArgumentException(StringUtil.Format(Debugger.BreakpointIdNotFound, i)),
                                "PSBreakpoint:BreakpointIdNotFound",
                                ErrorCategory.InvalidArgument,
                                null));
                        continue;
                    }

                    if (ShouldProcessInternal(breakpoint.ToString()))
                    {
                        ProcessBreakpoint(breakpoint);
                    }
                }
            }
        }

        /// <summary>
        /// Process the given breakpoint.
        /// </summary>
        protected abstract void ProcessBreakpoint(Breakpoint breakpoint);

        private bool ShouldProcessInternal(string target)
        {
            // ShouldProcess should be called only if the WhatIf or Confirm parameters are passed in explicitly.
            // It should *not* be called if we are in a nested debug prompt and the current running command was
            // run with -WhatIf or -Confirm, because this prevents the user from adding/removing breakpoints inside
            // a debugger stop.
            if (this.MyInvocation.BoundParameters.ContainsKey("WhatIf") || this.MyInvocation.BoundParameters.ContainsKey("Confirm"))
            {
                return ShouldProcess(target);
            }

            return true;
        }
    }

    /// <summary>
    /// This class implements Enable-PSBreakpoint.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Enable, "PSBreakpoint", SupportsShouldProcess = true, DefaultParameterSetName = "Id", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113295")]
    [OutputType(typeof(Breakpoint))]
    public class EnablePSBreakpointCommand : PSBreakpointCommandBase
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
        /// Enables the given breakpoint.
        /// </summary>
        protected override void ProcessBreakpoint(Breakpoint breakpoint)
        {
            this.Context.Debugger.EnableBreakpoint(breakpoint);

            if (_passThru)
            {
                WriteObject(breakpoint);
            }
        }
    }
}
