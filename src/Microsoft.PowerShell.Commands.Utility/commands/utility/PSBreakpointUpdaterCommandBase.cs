// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Base class for Enable/Disable/Remove-PSBreakpoint.
    /// </summary>
    public abstract class PSBreakpointUpdaterCommandBase : PSBreakpointCommandBase
    {
        #region strings

        internal const string BreakpointParameterSetName = "Breakpoint";
        internal const string IdParameterSetName = "Id";

        #endregion strings

        #region parameters

        /// <summary>
        /// Gets or sets the breakpoint to enable.
        /// </summary>
        [Parameter(ParameterSetName = BreakpointParameterSetName, ValueFromPipeline = true, Position = 0, Mandatory = true)]
        [ValidateNotNull]
        public Breakpoint[] Breakpoint { get; set; }

        /// <summary>
        /// Gets or sets the Id of the breakpoint to enable.
        /// </summary>
        [Parameter(ParameterSetName = IdParameterSetName, ValueFromPipelineByPropertyName = true, Position = 0, Mandatory = true)]
        [ValidateNotNull]
        public int[] Id { get; set; }

        /// <summary>
        /// Gets or sets the runspace where the breakpoints will be used.
        /// </summary>
        [Parameter(ParameterSetName = IdParameterSetName, ValueFromPipelineByPropertyName = true)]
        [Alias("RunspaceId")]
        [Runspace]
        public override Runspace Runspace { get; set; }

        #endregion parameters

        #region overrides

        /// <summary>
        /// Gathers the list of breakpoints to process and calls ProcessBreakpoints.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (ParameterSetName.Equals(BreakpointParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                foreach (Breakpoint breakpoint in Breakpoint)
                {
                    if (ShouldProcessInternal(breakpoint.ToString()) &&
                        TryGetRunspace(breakpoint))
                    {
                        ProcessBreakpoint(breakpoint);
                    }
                }
            }
            else
            {
                Debug.Assert(
                    ParameterSetName.Equals(IdParameterSetName, StringComparison.OrdinalIgnoreCase),
                    $"There should be no other parameter sets besides '{BreakpointParameterSetName}' and '{IdParameterSetName}'.");

                foreach (int id in Id)
                {
                    Breakpoint breakpoint;
                    if (TryGetBreakpoint(id, out breakpoint) &&
                        ShouldProcessInternal(breakpoint.ToString()))
                    {
                        ProcessBreakpoint(breakpoint);
                    }
                }
            }
        }

        #endregion overrides

        #region private data

        private readonly Dictionary<Guid, Runspace> runspaces = new();

        #endregion private data

        #region private methods

        private bool TryGetRunspace(Breakpoint breakpoint)
        {
            // Breakpoints retrieved from another runspace will have a RunspaceId note property of type Guid on them.
            var pso = new PSObject(breakpoint);
            var runspaceInstanceIdProperty = pso.Properties[RemotingConstants.RunspaceIdNoteProperty];
            if (runspaceInstanceIdProperty == null)
            {
                Runspace = Context.CurrentRunspace;
                return true;
            }

            Debug.Assert(runspaceInstanceIdProperty.TypeNameOfValue.Equals("System.Guid", StringComparison.OrdinalIgnoreCase), "Instance ids must be GUIDs.");

            var runspaceInstanceId = (Guid)runspaceInstanceIdProperty.Value;
            if (runspaces.TryGetValue(runspaceInstanceId, out Runspace runspace))
            {
                Runspace = runspace;
                return true;
            }

            var matchingRunspaces = GetRunspaceUtils.GetRunspacesByInstanceId(new[] { runspaceInstanceId });
            if (matchingRunspaces.Count != 1)
            {
                WriteError(
                    new ErrorRecord(
                        new ArgumentException(StringUtil.Format(Debugger.RunspaceInstanceIdNotFound, runspaceInstanceId)),
                        "PSBreakpoint:RunspaceInstanceIdNotFound",
                        ErrorCategory.InvalidArgument,
                        null));
                return false;
            }

            Runspace = runspaces[runspaceInstanceId] = matchingRunspaces[0];
            return true;
        }

        private bool TryGetBreakpoint(int id, out Breakpoint breakpoint)
        {
            breakpoint = Runspace.Debugger.GetBreakpoint(id);

            if (breakpoint == null)
            {
                WriteError(
                    new ErrorRecord(
                        new ArgumentException(StringUtil.Format(Debugger.BreakpointIdNotFound, id)),
                        "PSBreakpoint:BreakpointIdNotFound",
                        ErrorCategory.InvalidArgument,
                        null));
                return false;
            }

            return true;
        }

        private bool ShouldProcessInternal(string target)
        {
            // ShouldProcess should be called only if the WhatIf or Confirm parameters are passed in explicitly.
            // It should *not* be called if we are in a nested debug prompt and the current running command was
            // run with -WhatIf or -Confirm, because this prevents the user from adding/removing breakpoints inside
            // a debugger stop.
            if (MyInvocation.BoundParameters.ContainsKey("WhatIf") ||
                MyInvocation.BoundParameters.ContainsKey("Confirm"))
            {
                return ShouldProcess(target);
            }

            return true;
        }

        #endregion private methods
    }
}
