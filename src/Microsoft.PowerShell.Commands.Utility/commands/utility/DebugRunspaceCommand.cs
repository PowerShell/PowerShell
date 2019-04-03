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
using System.Threading;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This cmdlet takes a Runspace object and checks to see if it is debuggable (i.e, if
    /// it is running a script or is currently stopped in the debugger.
    /// If it is debuggable then it breaks into the Runspace debugger in step mode.
    /// </summary>
    [SuppressMessage("Microsoft.PowerShell", "PS1012:CallShouldProcessOnlyIfDeclaringSupport")]
    [Cmdlet(VerbsDiagnostic.Debug, "Runspace", SupportsShouldProcess = true, DefaultParameterSetName = DebugRunspaceCommand.RunspaceParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=403731")]
    public sealed class DebugRunspaceCommand : PSCmdlet
    {
        #region Strings

        private const string RunspaceParameterSet = "RunspaceParameterSet";
        private const string NameParameterSet = "NameParameterSet";
        private const string IdParameterSet = "IdParameterSet";
        private const string InstanceIdParameterSet = "InstanceIdParameterSet";

        #endregion

        #region Private members

        private Runspace _runspace;
        private System.Management.Automation.Debugger _debugger;
        private PSDataCollection<PSStreamObject> _debugBlockingCollection;
        private PSDataCollection<PSStreamObject> _debugAccumulateCollection;
        private Pipeline _runningPipeline;
        private System.Management.Automation.PowerShell _runningPowerShell;

        // Debugging to persist until Ctrl+C or Debugger 'Exit' stops cmdlet.
        private bool _debugging;
        private ManualResetEventSlim _newRunningScriptEvent = new ManualResetEventSlim(true);
        private RunspaceAvailability _previousRunspaceAvailability = RunspaceAvailability.None;

        #endregion

        #region Parameters

        /// <summary>
        /// The Runspace to be debugged.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ValueFromPipeline = true,
                   ParameterSetName = DebugRunspaceCommand.RunspaceParameterSet)]
        public Runspace Runspace
        {
            get;
            set;
        }

        /// <summary>
        /// The name of a Runspace to be debugged.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ParameterSetName = DebugRunspaceCommand.NameParameterSet)]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// The Id of a Runspace to be debugged.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ParameterSetName = DebugRunspaceCommand.IdParameterSet)]
        public int Id
        {
            get;
            set;
        }

        /// <summary>
        /// The InstanceId of a Runspace to be debugged.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ParameterSetName = DebugRunspaceCommand.InstanceIdParameterSet)]
        public Guid InstanceId
        {
            get;
            set;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// End processing.  Do work.
        /// </summary>
        protected override void EndProcessing()
        {
            if (ParameterSetName == DebugRunspaceCommand.RunspaceParameterSet)
            {
                _runspace = Runspace;
            }
            else
            {
                IReadOnlyList<Runspace> runspaces = null;

                switch (ParameterSetName)
                {
                    case DebugRunspaceCommand.NameParameterSet:
                        runspaces = GetRunspaceUtils.GetRunspacesByName(new string[] { Name });
                        break;

                    case DebugRunspaceCommand.IdParameterSet:
                        runspaces = GetRunspaceUtils.GetRunspacesById(new int[] { Id });
                        break;

                    case DebugRunspaceCommand.InstanceIdParameterSet:
                        runspaces = GetRunspaceUtils.GetRunspacesByInstanceId(new Guid[] { InstanceId });
                        break;
                }

                if (runspaces.Count > 1)
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            new PSArgumentException(Debugger.RunspaceDebuggingTooManyRunspacesFound),
                            "DebugRunspaceTooManyRunspaceFound",
                            ErrorCategory.InvalidOperation,
                            this)
                        );
                }

                if (runspaces.Count == 1)
                {
                    _runspace = runspaces[0];
                }
            }

            if (_runspace == null)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSArgumentNullException(Debugger.RunspaceDebuggingNoRunspaceFound),
                        "DebugRunspaceNoRunspaceFound",
                        ErrorCategory.InvalidOperation,
                        this)
                    );
            }

            Runspace defaultRunspace = LocalRunspace.DefaultRunspace;
            if (defaultRunspace == null || defaultRunspace.Debugger == null)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSInvalidOperationException(Debugger.RunspaceDebuggingNoHostRunspaceOrDebugger),
                        "DebugRunspaceNoHostDebugger",
                        ErrorCategory.InvalidOperation,
                        this)
                    );
            }

            if (_runspace == defaultRunspace)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSInvalidOperationException(Debugger.RunspaceDebuggingCannotDebugDefaultRunspace),
                        "DebugRunspaceCannotDebugHostRunspace",
                        ErrorCategory.InvalidOperation,
                        this)
                    );
            }

            if (this.Host == null || this.Host.UI == null)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSInvalidOperationException(Debugger.RunspaceDebuggingNoHost),
                        "DebugRunspaceNoHostAvailable",
                        ErrorCategory.InvalidOperation,
                        this)
                    );
            }

            if (!ShouldProcess(_runspace.Name, VerbsDiagnostic.Debug))
            {
                return;
            }

            _debugger = defaultRunspace.Debugger;

            try
            {
                PrepareRunspace(_runspace);

                // Blocking call.  Send runspace/command output to host UI while debugging and wait for runspace/command completion.
                WaitAndReceiveRunspaceOutput();
            }
            finally
            {
                RestoreRunspace(_runspace);
            }
        }

        /// <summary>
        /// Stop processing.
        /// </summary>
        protected override void StopProcessing()
        {
            _debugging = false;

            // Cancel runspace debugging.
            System.Management.Automation.Debugger debugger = _debugger;
            if ((debugger != null) && (_runspace != null))
            {
                debugger.StopDebugRunspace(_runspace);
            }

            // Unblock the data collection.
            PSDataCollection<PSStreamObject> debugCollection = _debugBlockingCollection;
            if (debugCollection != null)
            {
                debugCollection.Complete();
            }

            // Unblock any new command wait.
            _newRunningScriptEvent.Set();
        }

        #endregion

        #region Private methods

        private void WaitAndReceiveRunspaceOutput()
        {
            _debugging = true;

            try
            {
                HostWriteLine(string.Format(CultureInfo.InvariantCulture, Debugger.RunspaceDebuggingStarted, _runspace.Name));
                HostWriteLine(Debugger.RunspaceDebuggingEndSession);
                HostWriteLine(string.Empty);

                _runspace.AvailabilityChanged += HandleRunspaceAvailabilityChanged;
                _debugger.NestedDebuggingCancelledEvent += HandleDebuggerNestedDebuggingCancelledEvent;

                // Make sure host debugger has debugging turned on.
                _debugger.SetDebugMode(DebugModes.LocalScript | DebugModes.RemoteScript);

                // Set up host script debugger to debug the runspace.
                _debugger.DebugRunspace(_runspace);

                while (_debugging)
                {
                    // Wait for running script.
                    _newRunningScriptEvent.Wait();

                    if (!_debugging) { return; }

                    AddDataEventHandlers();

                    try
                    {
                        // Block cmdlet during debugging until either the command finishes
                        // or the user terminates the debugging session.
                        foreach (var streamItem in _debugBlockingCollection)
                        {
                            streamItem.WriteStreamObject(this);
                        }
                    }
                    finally
                    {
                        RemoveDataEventHandlers();
                    }

                    if (_debugging &&
                        (!_runspace.InNestedPrompt))
                    {
                        HostWriteLine(string.Empty);
                        HostWriteLine(Debugger.RunspaceDebuggingScriptCompleted);
                        HostWriteLine(Debugger.RunspaceDebuggingEndSession);
                        HostWriteLine(string.Empty);
                    }

                    _newRunningScriptEvent.Reset();
                }
            }
            finally
            {
                _runspace.AvailabilityChanged -= HandleRunspaceAvailabilityChanged;
                _debugger.NestedDebuggingCancelledEvent -= HandleDebuggerNestedDebuggingCancelledEvent;
                _debugger.StopDebugRunspace(_runspace);
                _newRunningScriptEvent.Dispose();
            }
        }

        private void HostWriteLine(string line)
        {
            if ((this.Host != null) && (this.Host.UI != null))
            {
                try
                {
                    if (this.Host.UI.RawUI != null)
                    {
                        this.Host.UI.WriteLine(ConsoleColor.Yellow, this.Host.UI.RawUI.BackgroundColor, line);
                    }
                    else
                    {
                        this.Host.UI.WriteLine(line);
                    }
                }
                catch (System.Management.Automation.Host.HostException) { }
            }
        }

        private void AddDataEventHandlers()
        {
            // Create new collection objects.
            if (_debugBlockingCollection != null) { _debugBlockingCollection.Dispose(); }

            if (_debugAccumulateCollection != null) { _debugAccumulateCollection.Dispose(); }

            _debugBlockingCollection = new PSDataCollection<PSStreamObject>();
            _debugBlockingCollection.BlockingEnumerator = true;
            _debugAccumulateCollection = new PSDataCollection<PSStreamObject>();

            _runningPowerShell = _runspace.GetCurrentBasePowerShell();
            if (_runningPowerShell != null)
            {
                if (_runningPowerShell.OutputBuffer != null)
                {
                    _runningPowerShell.OutputBuffer.DataAdding += HandlePowerShellOutputBufferDataAdding;
                }

                if (_runningPowerShell.ErrorBuffer != null)
                {
                    _runningPowerShell.ErrorBuffer.DataAdding += HandlePowerShellErrorBufferDataAdding;
                }
            }
            else
            {
                _runningPipeline = _runspace.GetCurrentlyRunningPipeline();
                if (_runningPipeline != null)
                {
                    if (_runningPipeline.Output != null)
                    {
                        _runningPipeline.Output.DataReady += HandlePipelineOutputDataReady;
                    }

                    if (_runningPipeline.Error != null)
                    {
                        _runningPipeline.Error.DataReady += HandlePipelineErrorDataReady;
                    }
                }
            }
        }

        private void RemoveDataEventHandlers()
        {
            if (_runningPowerShell != null)
            {
                if (_runningPowerShell.OutputBuffer != null)
                {
                    _runningPowerShell.OutputBuffer.DataAdding -= HandlePowerShellOutputBufferDataAdding;
                }

                if (_runningPowerShell.ErrorBuffer != null)
                {
                    _runningPowerShell.ErrorBuffer.DataAdding -= HandlePowerShellErrorBufferDataAdding;
                }

                _runningPowerShell = null;
            }
            else if (_runningPipeline != null)
            {
                if (_runningPipeline.Output != null)
                {
                    _runningPipeline.Output.DataReady -= HandlePipelineOutputDataReady;
                }

                if (_runningPipeline.Error != null)
                {
                    _runningPipeline.Error.DataReady -= HandlePipelineErrorDataReady;
                }

                _runningPipeline = null;
            }
        }

        private void HandleRunspaceAvailabilityChanged(object sender, RunspaceAvailabilityEventArgs e)
        {
            // Ignore nested commands.
            LocalRunspace localRunspace = sender as LocalRunspace;
            if (localRunspace != null)
            {
                var basePowerShell = localRunspace.GetCurrentBasePowerShell();
                if ((basePowerShell != null) && (basePowerShell.IsNested))
                {
                    return;
                }
            }

            RunspaceAvailability prevAvailability = _previousRunspaceAvailability;
            _previousRunspaceAvailability = e.RunspaceAvailability;

            if ((e.RunspaceAvailability == RunspaceAvailability.Available) || (e.RunspaceAvailability == RunspaceAvailability.None))
            {
                _debugBlockingCollection.Complete();
            }
            else if ((e.RunspaceAvailability == RunspaceAvailability.Busy) &&
                     ((prevAvailability == RunspaceAvailability.Available) || (prevAvailability == RunspaceAvailability.None)))
            {
                _newRunningScriptEvent.Set();
            }
        }

        private void HandleDebuggerNestedDebuggingCancelledEvent(object sender, EventArgs e)
        {
            StopProcessing();
        }

        private void HandlePipelineOutputDataReady(object sender, EventArgs e)
        {
            PipelineReader<PSObject> reader = sender as PipelineReader<PSObject>;
            if (reader != null && reader.IsOpen)
            {
                WritePipelineCollection(reader.NonBlockingRead(), PSStreamObjectType.Output);
            }
        }

        private void HandlePipelineErrorDataReady(object sender, EventArgs e)
        {
            PipelineReader<object> reader = sender as PipelineReader<object>;
            if (reader != null && reader.IsOpen)
            {
                WritePipelineCollection(reader.NonBlockingRead(), PSStreamObjectType.Error);
            }
        }

        private void WritePipelineCollection<T>(Collection<T> collection, PSStreamObjectType psStreamType)
        {
            foreach (var item in collection)
            {
                if (item != null)
                {
                    AddToDebugBlockingCollection(new PSStreamObject(psStreamType, item));
                }
            }
        }

        private void HandlePowerShellOutputBufferDataAdding(object sender, DataAddingEventArgs e)
        {
            if (e.ItemAdded != null)
            {
                HandlePowerShellPStreamItem(new PSStreamObject(PSStreamObjectType.Output, e.ItemAdded));
            }
        }

        private void HandlePowerShellErrorBufferDataAdding(object sender, DataAddingEventArgs e)
        {
            if (e.ItemAdded != null)
            {
                HandlePowerShellPStreamItem(new PSStreamObject(PSStreamObjectType.Error, e.ItemAdded));
            }
        }

        private void HandlePowerShellPStreamItem(PSStreamObject streamItem)
        {
            if (!_debugger.InBreakpoint)
            {
                // First write any accumulated items.
                foreach (var item in _debugAccumulateCollection.ReadAll())
                {
                    AddToDebugBlockingCollection(item);
                }

                // Handle new item.
                if ((_debugBlockingCollection != null) && (_debugBlockingCollection.IsOpen))
                {
                    AddToDebugBlockingCollection(streamItem);
                }
            }
            else if (_debugAccumulateCollection.IsOpen)
            {
                // Add to accumulator if debugger is stopped in breakpoint.
                _debugAccumulateCollection.Add(streamItem);
            }
        }

        private void AddToDebugBlockingCollection(PSStreamObject streamItem)
        {
            if (!_debugBlockingCollection.IsOpen) { return; }

            if (streamItem != null)
            {
                try
                {
                    _debugBlockingCollection.Add(streamItem);
                }
                catch (PSInvalidOperationException) { }
            }
        }

        private void PrepareRunspace(Runspace runspace)
        {
            SetLocalMode(runspace.Debugger, true);
            EnableHostDebugger(runspace, false);
        }

        private void RestoreRunspace(Runspace runspace)
        {
            SetLocalMode(runspace.Debugger, false);
            EnableHostDebugger(runspace, true);
        }

        private void EnableHostDebugger(Runspace runspace, bool enabled)
        {
            // Only enable and disable the host's runspace if we are in process attach mode.
            if (_debugger is ServerRemoteDebugger)
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
