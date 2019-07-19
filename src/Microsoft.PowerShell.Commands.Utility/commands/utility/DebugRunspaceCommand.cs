// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Remoting.Internal;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This cmdlet takes a Runspace object and checks to see if it is debuggable (i.e, if
    /// it is running a script or is currently stopped in the debugger.
    /// If it is debuggable then it breaks into the Runspace debugger in step mode.
    /// </summary>
    [SuppressMessage("Microsoft.PowerShell", "PS1012:CallShouldProcessOnlyIfDeclaringSupport")]
    [Cmdlet(VerbsDiagnostic.Debug, "Runspace", SupportsShouldProcess = true, DefaultParameterSetName = DefaultParameterSetName,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=403731")]
    public sealed class DebugRunspaceCommand : PSRemoteDebugCmdlet
    {
        #region Private members

        private Pipeline _runningPipeline;
        private System.Management.Automation.PowerShell _runningPowerShell;
        private RunspaceAvailability _previousRunspaceAvailability = RunspaceAvailability.None;

        #endregion Private members

        #region Parameters

        /// <summary>
        /// The Runspace to be debugged.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ValueFromPipeline = true,
                   ParameterSetName = DefaultParameterSetName)]
        public Runspace Runspace { get; set; }

        #endregion Parameters

        #region Overrides

        /// <summary>
        /// The message to display when the debugger is first attached to the job.
        /// </summary>
        protected override string DebuggingStartedMessage => string.Format(CultureInfo.InvariantCulture, global::Debugger.RunspaceDebuggingStarted, Runspace.Name);

        /// <summary>
        /// End processing. Do work.
        /// </summary>
        protected override void EndProcessing()
        {
            IReadOnlyList<Runspace> runspaces = null;

            switch (ParameterSetName)
            {
                case NameParameterSetName:
                    runspaces = GetRunspaceUtils.GetRunspacesByName(new string[] { Name });
                    break;

                case IdParameterSetName:
                    runspaces = GetRunspaceUtils.GetRunspacesById(new int[] { Id });
                    break;

                case InstanceIdParameterSetName:
                    runspaces = GetRunspaceUtils.GetRunspacesByInstanceId(new Guid[] { InstanceId });
                    break;
            }

            if (runspaces != null)
            {
                if (runspaces.Count > 1)
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            new PSArgumentException(global::Debugger.RunspaceDebuggingTooManyRunspacesFound),
                            "DebugRunspaceTooManyRunspaceFound",
                            ErrorCategory.InvalidOperation,
                            this)
                        );
                }

                if (runspaces.Count == 1)
                {
                    Runspace = runspaces[0];
                }
            }

            if (Runspace == null)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSArgumentNullException(global::Debugger.RunspaceDebuggingNoRunspaceFound),
                        "DebugRunspaceNoRunspaceFound",
                        ErrorCategory.InvalidOperation,
                        this)
                    );
            }

            if (Runspace == HostRunspace)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSInvalidOperationException(global::Debugger.RunspaceDebuggingCannotDebugDefaultRunspace),
                        "DebugRunspaceCannotDebugHostRunspace",
                        ErrorCategory.InvalidOperation,
                        this)
                    );
            }

            if (!ShouldProcess(Runspace.Name, VerbsDiagnostic.Debug))
            {
                return;
            }

            base.EndProcessing();
        }

        /// <summary>
        /// Starts the runspace debugging session.
        /// </summary>
        protected override void StartDebugging()
        {
            InitDebuggingSignal();

            SetLocalMode(Runspace.Debugger, true);
            EnableHostDebugger(Runspace, false);

            Runspace.AvailabilityChanged += Runspace_AvailabilityChanged;

            // Set up host script debugger to debug the runspace.
            Debugger.DebugRunspace(Runspace, disableBreakAll: NoInitialBreak);
        }

        /// <summary>
        /// Adds event handlers to the runspace output for output data processing.
        /// </summary>
        protected override void AddDataEventHandlers()
        {
            _runningPowerShell = Runspace.GetCurrentBasePowerShell();
            if (_runningPowerShell == null)
            {
                _runningPipeline = Runspace.GetCurrentlyRunningPipeline();
                _runningPowerShell = (_runningPipeline as RemotePipeline)?.PowerShell;
            }

            if (_runningPowerShell != null)
            {
                if (_runningPowerShell.OutputBuffer != null)
                {
                    _runningPowerShell.OutputBuffer.DataAdding += OutputBuffer_DataAdding;
                }

                if (_runningPowerShell.ErrorBuffer != null)
                {
                    _runningPowerShell.ErrorBuffer.DataAdding += ErrorBuffer_DataAdding;
                }

                if (_runningPowerShell.WarningBuffer != null)
                {
                    _runningPowerShell.WarningBuffer.DataAdding += WarningBuffer_DataAdding;
                }

                if (_runningPowerShell.VerboseBuffer != null)
                {
                    _runningPowerShell.VerboseBuffer.DataAdding += VerboseBuffer_DataAdding;
                }

                if (_runningPowerShell.DebugBuffer != null)
                {
                    _runningPowerShell.DebugBuffer.DataAdding += DebugBuffer_DataAdding;
                }

                if (_runningPowerShell.InformationBuffer != null)
                {
                    _runningPowerShell.InformationBuffer.DataAdding += InformationBuffer_DataAdding;
                }

                if (_runningPowerShell.ProgressBuffer != null)
                {
                    _runningPowerShell.ProgressBuffer.DataAdding += ProgressBuffer_DataAdding;
                }
            }
            else if (_runningPipeline != null)
            {
                if (_runningPipeline.Output != null)
                {
                    _runningPipeline.Output.DataReady += Output_DataReady;
                }

                if (_runningPipeline.Error != null)
                {
                    _runningPipeline.Error.DataReady += Error_DataReady;
                }
            }
        }

        /// <summary>
        /// Close the runspace on exception.
        /// </summary>
        /// <param name="_">Not used.</param>
        protected override void HandleDataProcessingException(Exception _)
        {
            // Close the runspace on exception.
            if (Runspace.RunspaceStateInfo.State != RunspaceState.Broken && Runspace.RunspaceStateInfo.State != RunspaceState.Closed)
            {
                Runspace.Close();
            }
        }

        /// <summary>
        /// Removes event handlers from the runspace output that were added for output data processing.
        /// </summary>
        protected override void RemoveDataEventHandlers()
        {
            if (_runningPowerShell != null)
            {
                if (_runningPowerShell.OutputBuffer != null)
                {
                    _runningPowerShell.OutputBuffer.DataAdding -= OutputBuffer_DataAdding;
                }

                if (_runningPowerShell.ErrorBuffer != null)
                {
                    _runningPowerShell.ErrorBuffer.DataAdding -= ErrorBuffer_DataAdding;
                }

                if (_runningPowerShell.WarningBuffer != null)
                {
                    _runningPowerShell.WarningBuffer.DataAdding -= WarningBuffer_DataAdding;
                }

                if (_runningPowerShell.VerboseBuffer != null)
                {
                    _runningPowerShell.VerboseBuffer.DataAdding -= VerboseBuffer_DataAdding;
                }

                if (_runningPowerShell.DebugBuffer != null)
                {
                    _runningPowerShell.DebugBuffer.DataAdding -= DebugBuffer_DataAdding;
                }

                if (_runningPowerShell.InformationBuffer != null)
                {
                    _runningPowerShell.InformationBuffer.DataAdding -= InformationBuffer_DataAdding;
                }

                if (_runningPowerShell.ProgressBuffer != null)
                {
                    _runningPowerShell.ProgressBuffer.DataAdding -= ProgressBuffer_DataAdding;
                }

                _runningPowerShell = null;
            }
            else if (_runningPipeline != null)
            {
                if (_runningPipeline.Output != null)
                {
                    _runningPipeline.Output.DataReady -= Output_DataReady;
                }

                if (_runningPipeline.Error != null)
                {
                    _runningPipeline.Error.DataReady -= Error_DataReady;
                }

                _runningPipeline = null;
            }
        }

        /// <summary>
        /// Stop the runspace debugging session.
        /// </summary>
        protected override void StopDebugging()
        {
            Runspace.AvailabilityChanged -= Runspace_AvailabilityChanged;
            Debugger.StopDebugRunspace(Runspace);
            SetLocalMode(Runspace.Debugger, false);
            EnableHostDebugger(Runspace, true);
        }

        #endregion

        #region Private methods

        private void Runspace_AvailabilityChanged(object sender, RunspaceAvailabilityEventArgs e)
        {
            // Ignore nested commands.
            LocalRunspace localRunspace = sender as LocalRunspace;
            if (localRunspace != null)
            {
                var basePowerShell = localRunspace.GetCurrentBasePowerShell();
                if (basePowerShell != null && basePowerShell.IsNested)
                {
                    return;
                }
            }

            RunspaceAvailability prevAvailability = _previousRunspaceAvailability;
            _previousRunspaceAvailability = e.RunspaceAvailability;

            if ((e.RunspaceAvailability == RunspaceAvailability.Available) || (e.RunspaceAvailability == RunspaceAvailability.None))
            {
                CloseOutput();
            }
            else if ((e.RunspaceAvailability == RunspaceAvailability.Busy) &&
                     ((prevAvailability == RunspaceAvailability.Available) || (prevAvailability == RunspaceAvailability.None)))
            {
                SetDebuggingSignal();
            }
        }

        private void OutputBuffer_DataAdding(object sender, DataAddingEventArgs e)
        {
            if (e.ItemAdded != null)
            {
                AddOutput(new PSStreamObject(PSStreamObjectType.Output, e.ItemAdded));
            }
        }

        private void ErrorBuffer_DataAdding(object sender, DataAddingEventArgs e)
        {
            if (e.ItemAdded != null)
            {
                AddOutput(new PSStreamObject(PSStreamObjectType.Error, e.ItemAdded));
            }
        }

        private void WarningBuffer_DataAdding(object sender, DataAddingEventArgs e)
        {
            if (e.ItemAdded != null)
            {
                AddOutput(new PSStreamObject(PSStreamObjectType.Warning, e.ItemAdded));
            }
        }

        private void VerboseBuffer_DataAdding(object sender, DataAddingEventArgs e)
        {
            if (e.ItemAdded != null)
            {
                AddOutput(new PSStreamObject(PSStreamObjectType.Verbose, e.ItemAdded));
            }
        }

        private void DebugBuffer_DataAdding(object sender, DataAddingEventArgs e)
        {
            if (e.ItemAdded != null)
            {
                AddOutput(new PSStreamObject(PSStreamObjectType.Debug, e.ItemAdded));
            }
        }

        private void InformationBuffer_DataAdding(object sender, DataAddingEventArgs e)
        {
            if (e.ItemAdded != null)
            {
                AddOutput(new PSStreamObject(PSStreamObjectType.Information, e.ItemAdded));
            }
        }

        private void ProgressBuffer_DataAdding(object sender, DataAddingEventArgs e)
        {
            if (e.ItemAdded != null && e.ItemAdded is ProgressRecord)
            {
                Host.UI.WriteProgress(0, e.ItemAdded as ProgressRecord);
            }
        }

        private void Output_DataReady(object sender, EventArgs e)
        {
            PipelineReader<PSObject> reader = sender as PipelineReader<PSObject>;
            WritePipelineCollection(reader, PSStreamObjectType.Output);
        }

        private void Error_DataReady(object sender, EventArgs e)
        {
            PipelineReader<object> reader = sender as PipelineReader<object>;
            WritePipelineCollection(reader, PSStreamObjectType.Error);
        }

        private void WritePipelineCollection<T>(PipelineReader<T> reader, PSStreamObjectType psStreamType)
        {
            if (reader == null || !reader.IsOpen)
            {
                return;
            }

            foreach (var item in reader.NonBlockingRead())
            {
                if (item != null)
                {
                    AddOutput(new PSStreamObject(psStreamType, item));
                }
            }
        }

        private void EnableHostDebugger(Runspace runspace, bool enabled)
        {
            // Only enable and disable the host's runspace if we are in process attach mode.
            if (Debugger is ServerRemoteDebugger)
            {
                LocalRunspace localRunspace = runspace as LocalRunspace;
                if ((localRunspace != null) && (localRunspace.ExecutionContext != null) && (localRunspace.ExecutionContext.EngineHostInterface != null))
                {
                    try
                    {
                        localRunspace.ExecutionContext.EngineHostInterface.DebuggerEnabled = enabled;
                    }
                    catch (PSNotImplementedException) { }
                }
            }
        }

        private void SetLocalMode(System.Management.Automation.Debugger debugger, bool localMode)
        {
            ServerRemoteDebugger remoteDebugger = debugger as ServerRemoteDebugger;
            if (remoteDebugger != null)
            {
                remoteDebugger.LocalDebugMode = localMode;
            }
        }

        #endregion
    }
}
