// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Base class for PSBreakpoint cmdlets.
    /// </summary>
    public abstract class PSBreakpointCommandBase : PSCmdlet
    {
        #region parameters

        /// <summary>
        /// Gets or sets the runspace where the breakpoints will be used.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        [Runspace]
        public virtual Runspace Runspace { get; set; }

        #endregion parameters

        #region overrides

        /// <summary>
        /// Identifies the default runspace.
        /// </summary>
        protected override void BeginProcessing()
        {
            Runspace ??= Context.CurrentRunspace;
        }

        #endregion overrides

        #region protected methods

        /// <summary>
        /// Write the given breakpoint out to the pipeline, decorated with the runspace instance id if appropriate.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to write to the pipeline.</param>
        protected virtual void ProcessBreakpoint(Breakpoint breakpoint)
        {
            if (Runspace != Context.CurrentRunspace)
            {
                var pso = new PSObject(breakpoint);
                pso.Properties.Add(new PSNoteProperty(RemotingConstants.RunspaceIdNoteProperty, Runspace.InstanceId));
                WriteObject(pso);
            }
            else
            {
                WriteObject(breakpoint);
            }
        }

        #endregion protected methods
    }
}
