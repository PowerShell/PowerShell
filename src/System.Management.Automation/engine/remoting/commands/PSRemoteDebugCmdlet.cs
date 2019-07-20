// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Management.Automation.Remoting.Internal;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Base class for cmdlets that attach a debugger to a "remote" job or runspace.
    /// </summary>
    public abstract class PSRemoteDebugCmdlet : PSCmdlet
    {
        #region strings

        /// <summary>
        /// The default parameter set name.
        /// </summary>
        protected const string DefaultParameterSetName = "Default";

        /// <summary>
        /// The "Name" parameter set name.
        /// </summary>
        protected const string NameParameterSetName = "Name";

        /// <summary>
        /// The "Id" parameter set name.
        /// </summary>
        protected const string IdParameterSetName = "Id";

        /// <summary>
        /// The "InstanceId" parameter set name.
        /// </summary>
        protected const string InstanceIdParameterSetName = "InstanceId";

        #endregion strings

        #region parameters

        /// <summary>
        /// Gets or sets the name of the remote target to be debugged.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ParameterSetName = NameParameterSetName)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Id of the remote target to be debugged.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ParameterSetName = IdParameterSetName)]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the InstanceId of the remote target to be debugged.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ParameterSetName = InstanceIdParameterSetName)]
        public Guid InstanceId { get; set; }

        /// <summary>
        /// Gets or sets a flag that prevents PowerShell from automatically performing a BreakAll when the debugger is attached to the remote target.
        /// </summary>
        [Experimental("Microsoft.PowerShell.Utility.PSManageBreakpointsInRunspace", ExperimentAction.Show)]
        [Parameter]
        public SwitchParameter NoInitialBreak { get; set; }

        #endregion parameters

        #region private members

        private bool _debugging;
        private ManualResetEventSlim _debuggingSignal = null;
        private PSDataCollection<PSStreamObject> _output;
        private PSDataCollection<PSStreamObject> _outputAccumulator;

        #endregion private members

        #region protected members

        /// <summary>
        /// Gets the host runspace from which the user will debug.
        /// </summary>
        protected Runspace HostRunspace { get; private set; }

        /// <summary>
        /// Gets the host debugger.
        /// </summary>
        protected Debugger Debugger { get; private set; }

        /// <summary>
        /// Gets the message to display when the debugger is first attached.
        /// </summary>
        protected abstract string DebuggingStartedMessage { get; }

        #endregion protected members

        #region overrides

        /// <summary>
        /// Capture the host runspace and debugger.
        /// </summary>
        protected override void BeginProcessing()
        {
            HostRunspace = Runspace.DefaultRunspace;
            Debugger = HostRunspace?.Debugger;
        }

        /// <summary>
        /// Enter the debugger until the user detaches or the script or command completes.
        /// </summary>
        protected override void EndProcessing()
        {
            if (Debugger == null)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSInvalidOperationException(DebuggerStrings.DebuggingNoDebugger),
                        "DebugRunspaceNoHostDebugger",
                        ErrorCategory.InvalidOperation,
                        this));
            }

            if (Host?.UI == null)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSInvalidOperationException(DebuggerStrings.DebuggingNoHost),
                        "DebugRunspaceNoHostAvailable",
                        ErrorCategory.InvalidOperation,
                        this));
            }

            // Make sure host debugger has debugging turned on.
            Debugger.SetDebugMode(DebugModes.LocalScript | DebugModes.RemoteScript);

            _debugging = true;

            try
            {
                ShowDebuggerEntryMessage();
                StartDebugging();

                Debugger.NestedDebuggingCancelledEvent += Debugger_NestedDebuggingCancelledEvent;

                while (_debugging)
                {
                    _debuggingSignal?.Wait();
                    if (!_debugging)
                    {
                        return;
                    }

                    try
                    {
                        InitDataCollections();

                        AddDataEventHandlers();

                        FlushOutput();
                    }
                    catch (Exception exception)
                    {
                        HandleDataProcessingException(exception);
                        throw;
                    }
                    finally
                    {
                        RemoveDataEventHandlers();
                    }

                    if (_debugging && !HostRunspace.InNestedPrompt)
                    {
                        ShowScriptCompletedMessage();
                    }

                    _debuggingSignal?.Reset();
                }
            }
            finally
            {
                Debugger.NestedDebuggingCancelledEvent -= Debugger_NestedDebuggingCancelledEvent;

                StopDebugging();

                _debuggingSignal?.Dispose();
            }
        }

        /// <summary>
        /// Stop processing.
        /// </summary>
        protected override void StopProcessing()
        {
            _debugging = false;

            StopDebugging();

            // Unblock the data collection.
            CloseOutput();

            SetDebuggingSignal();
        }

        #endregion overrides

        #region protected methods

        /// <summary>
        /// Initializes the debugging signal.
        /// </summary>
        protected void InitDebuggingSignal()
        {
            if (_debuggingSignal == null)
            {
                _debuggingSignal = new ManualResetEventSlim(true);
            }
        }

        /// <summary>
        /// Signals PowerShell that it can enter a debugger prompt.
        /// </summary>
        protected void SetDebuggingSignal()
        {
            _debuggingSignal?.Set();
        }

        /// <summary>
        /// Performs preparation work before debugging starts.
        /// </summary>
        protected abstract void StartDebugging();

        /// <summary>
        /// Prepares for output data processing.
        /// </summary>
        protected abstract void AddDataEventHandlers();

        /// <summary>
        /// Handle any unexpected exceptions that occur during data processing.
        /// </summary>
        /// <param name="_">Not used.</param>
        protected virtual void HandleDataProcessingException(Exception _) { }

        /// <summary>
        /// Cleans up after output data processing.
        /// </summary>
        protected abstract void RemoveDataEventHandlers();

        /// <summary>
        /// Stop the debugging session.
        /// </summary>
        protected abstract void StopDebugging();

        /// <summary>
        /// Adds an object to the output collection.
        /// </summary>
        /// <param name="psStreamObject">The object to add.</param>
        protected virtual void AddOutput(PSStreamObject psStreamObject)
        {
            if (psStreamObject != null)
            {
                if (Debugger.InBreakpoint)
                {
                    // Add to accumulator if debugger is stopped in breakpoint.
                    if (_outputAccumulator != null && _outputAccumulator.IsOpen)
                    {
                        try
                        {
                            _outputAccumulator.Add(psStreamObject);
                        }
                        catch (PSInvalidOperationException)
                        {
                        }
                    }
                }
                else if (_output != null && _output.IsOpen)
                {
                    // Make sure the debugger gives us enough time to show output
                    _debuggingSignal?.Reset();

                    // First write and clear any accumulated items.
                    if (_outputAccumulator != null)
                    {
                        try
                        {
                            _output.AddRange(_outputAccumulator.ReadAll());
                        }
                        catch (PSInvalidOperationException)
                        {
                        }
                    }

                    // Then handle the new item.
                    try
                    {
                        _output.Add(psStreamObject);
                    }
                    catch (PSInvalidOperationException)
                    {
                    }

                    // Allow the debugger to enter a breakpoint
                    _debuggingSignal?.Set();
                }
            }
        }

        /// <summary>
        /// Write any output in the buffer to the host.
        /// </summary>
        protected void FlushOutput()
        {
            // Block cmdlet during debugging until either the command finishes
            // or the user terminates the debugging session.
            foreach (PSStreamObject streamItem in _output)
            {
                streamItem.WriteStreamObject(this);
            }
        }

        /// <summary>
        /// Unblock the data collection.
        /// </summary>
        protected void CloseOutput()
        {
            _output?.Complete();
        }

        #endregion protected methods

        #region private methods

        private void ShowDebuggerEntryMessage()
        {
            HostWriteLine(DebuggingStartedMessage);
            HostWriteLine(DebuggerStrings.BreakInstruction);
            HostWriteLine(DebuggerStrings.DetachInstruction);
            HostWriteLine(string.Empty);
        }

        private void ShowScriptCompletedMessage()
        {
            HostWriteLine(string.Empty);
            HostWriteLine(DebuggerStrings.ScriptCompletedMessage);
            HostWriteLine(DebuggerStrings.ExitInstruction);
            HostWriteLine(string.Empty);
        }

        private void HostWriteLine(string line)
        {
            if (Host?.UI != null)
            {
                try
                {
                    if (Host.UI.RawUI != null)
                    {
                        Host.UI.WriteLine(ConsoleColor.Yellow, Host.UI.RawUI.BackgroundColor, line);
                    }
                    else
                    {
                        Host.UI.WriteLine(line);
                    }
                }
                catch (System.Management.Automation.Host.HostException)
                {
                }
            }
        }

        private void InitDataCollections()
        {
            if (_output != null)
            {
                _output.Dispose();
            }

            _output = new PSDataCollection<PSStreamObject>
            {
                BlockingEnumerator = true
            };

            if (_outputAccumulator != null)
            {
                _outputAccumulator.Dispose();
            }

            _outputAccumulator = new PSDataCollection<PSStreamObject>();
        }

        private void Debugger_NestedDebuggingCancelledEvent(object sender, EventArgs e)
        {
            StopProcessing();
        }

        #endregion privatemethods
    }
}
