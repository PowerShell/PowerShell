// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.Commands
{
    #region PSRunspaceDebug class

    /// <summary>
    /// Runspace Debug Options class.
    /// </summary>
    public sealed class PSRunspaceDebug
    {
        #region Properties

        /// <summary>
        /// When true this property will cause any breakpoints set in a Runspace to stop
        /// the running command or script when the breakpoint is hit, regardless of whether a
        /// debugger is currently attached.  The script or command will remain stopped until
        /// a debugger is attached to debug the breakpoint.
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// When true this property will cause any running command or script in the Runspace
        /// to stop in step mode, regardless of whether a debugger is currently attached.  The
        /// script or command will remain stopped until a debugger is attached to debug the
        /// current stop point.
        /// </summary>
        public bool BreakAll { get; }

        /// <summary>
        /// Name of runspace for which the options apply.
        /// </summary>
        public string RunspaceName { get; }

        /// <summary>
        /// Local Id of runspace for which the options apply.
        /// </summary>
        public int RunspaceId { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PSRunspaceDebug"/> class.
        /// </summary>
        /// <param name="enabled">Enable debugger option.</param>
        /// <param name="breakAll">BreakAll option.</param>
        /// <param name="runspaceName">Runspace name.</param>
        /// <param name="runspaceId">Runspace local Id.</param>
        public PSRunspaceDebug(bool enabled, bool breakAll, string runspaceName, int runspaceId)
        {
            if (string.IsNullOrEmpty(runspaceName)) { throw new PSArgumentNullException(nameof(runspaceName)); }

            this.Enabled = enabled;
            this.BreakAll = breakAll;
            this.RunspaceName = runspaceName;
            this.RunspaceId = runspaceId;
        }

        #endregion
    }

    #endregion

    #region CommonRunspaceCommandBase class

    /// <summary>
    /// Abstract class that defines common Runspace Command parameters.
    /// </summary>
    public abstract class CommonRunspaceCommandBase : PSCmdlet
    {
        #region Strings

        /// <summary>
        /// RunspaceParameterSet.
        /// </summary>
        protected const string RunspaceParameterSet = "RunspaceParameterSet";

        /// <summary>
        /// RunspaceNameParameterSet.
        /// </summary>
        protected const string RunspaceNameParameterSet = "RunspaceNameParameterSet";

        /// <summary>
        /// RunspaceIdParameterSet.
        /// </summary>
        protected const string RunspaceIdParameterSet = "RunspaceIdParameterSet";

        /// <summary>
        /// RunspaceInstanceIdParameterSet.
        /// </summary>
        protected const string RunspaceInstanceIdParameterSet = "RunspaceInstanceIdParameterSet";

        /// <summary>
        /// ProcessNameParameterSet.
        /// </summary>
        protected const string ProcessNameParameterSet = "ProcessNameParameterSet";

        #endregion

        #region Parameters

        /// <summary>
        /// Runspace Name.
        /// </summary>
        [Parameter(Position = 0,
                   ParameterSetName = CommonRunspaceCommandBase.RunspaceNameParameterSet)]
        [ValidateNotNullOrEmpty()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] RunspaceName
        {
            get;
            set;
        }

        /// <summary>
        /// Runspace.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ValueFromPipeline = true,
                   ParameterSetName = CommonRunspaceCommandBase.RunspaceParameterSet)]
        [ValidateNotNullOrEmpty()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Runspace[] Runspace
        {
            get;
            set;
        }

        /// <summary>
        /// Runspace Id.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ParameterSetName = CommonRunspaceCommandBase.RunspaceIdParameterSet)]
        [ValidateNotNullOrEmpty()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public int[] RunspaceId
        {
            get;
            set;
        }
        /// <summary>
        /// RunspaceInstanceId.
        /// </summary>
        [Parameter(Position = 0,
                   Mandatory = true,
                   ParameterSetName = CommonRunspaceCommandBase.RunspaceInstanceIdParameterSet)]
        [ValidateNotNullOrEmpty()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public System.Guid[] RunspaceInstanceId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or Sets the ProcessName for which runspace debugging has to be enabled or disabled.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = CommonRunspaceCommandBase.ProcessNameParameterSet)]
        [ValidateNotNullOrEmpty()]
        public string ProcessName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or Sets the AppDomain Names for which runspace debugging has to be enabled or disabled.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = CommonRunspaceCommandBase.ProcessNameParameterSet)]
        [ValidateNotNullOrEmpty()]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope = "member",
            Target = "Microsoft.PowerShell.Commands.CommonRunspaceCommandBase.#AppDomainName")]
        public string[] AppDomainName
        {
            get;
            set;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Returns a list of valid runspaces based on current parameter set.
        /// </summary>
        /// <returns>IReadOnlyList.</returns>
        protected IReadOnlyList<Runspace> GetRunspaces()
        {
            IReadOnlyList<Runspace> results = null;

            if ((ParameterSetName == CommonRunspaceCommandBase.RunspaceNameParameterSet) && ((RunspaceName == null) || RunspaceName.Length == 0))
            {
                results = GetRunspaceUtils.GetAllRunspaces();
            }
            else
            {
                switch (ParameterSetName)
                {
                    case CommonRunspaceCommandBase.RunspaceNameParameterSet:
                        results = GetRunspaceUtils.GetRunspacesByName(RunspaceName);
                        break;

                    case CommonRunspaceCommandBase.RunspaceIdParameterSet:
                        results = GetRunspaceUtils.GetRunspacesById(RunspaceId);
                        break;

                    case CommonRunspaceCommandBase.RunspaceParameterSet:
                        results = new ReadOnlyCollection<Runspace>(new List<Runspace>(Runspace));
                        break;

                    case CommonRunspaceCommandBase.RunspaceInstanceIdParameterSet:
                        results = GetRunspaceUtils.GetRunspacesByInstanceId(RunspaceInstanceId);
                        break;
                }
            }

            return results;
        }

        /// <summary>
        /// Returns Runspace Debugger.
        /// </summary>
        /// <param name="runspace">Runspace.</param>
        /// <returns>Debugger.</returns>
        protected System.Management.Automation.Debugger GetDebuggerFromRunspace(Runspace runspace)
        {
            System.Management.Automation.Debugger debugger = null;
            try
            {
                debugger = runspace.Debugger;
            }
            catch (PSInvalidOperationException) { }

            if (debugger == null)
            {
                WriteError(
                    new ErrorRecord(
                        new PSInvalidOperationException(string.Format(CultureInfo.InvariantCulture, Debugger.RunspaceOptionNoDebugger, runspace.Name)),
                        "RunspaceDebugOptionNoDebugger",
                        ErrorCategory.InvalidOperation,
                        this)
                    );
            }

            return debugger;
        }

        /// <summary>
        /// SetDebugPreferenceHelper is a helper method used to enable/disable debug preference.
        /// </summary>
        /// <param name="processName">Process Name.</param>
        /// <param name="appDomainName">App Domain Name.</param>
        /// <param name="enable">Indicates if debug preference has to be enabled or disabled.</param>
        /// <param name="fullyQualifiedErrorId">FullyQualifiedErrorId to be used on error.</param>
        protected void SetDebugPreferenceHelper(string processName, string[] appDomainName, bool enable, string fullyQualifiedErrorId)
        {
            List<string> appDomainNames = null;
            if (appDomainName != null)
            {
                foreach (string currentAppDomainName in appDomainName)
                {
                    if (!string.IsNullOrEmpty(currentAppDomainName))
                    {
                        appDomainNames ??= new List<string>();

                        appDomainNames.Add(currentAppDomainName.ToLowerInvariant());
                    }
                }
            }

            try
            {
                System.Management.Automation.Runspaces.LocalRunspace.SetDebugPreference(processName.ToLowerInvariant(), appDomainNames, enable);
            }
            catch (Exception ex)
            {
                ErrorRecord errorRecord = new(
                new PSInvalidOperationException(string.Format(CultureInfo.InvariantCulture, Debugger.PersistDebugPreferenceFailure, processName), ex),
                fullyQualifiedErrorId,
                ErrorCategory.InvalidOperation,
                this);
                WriteError(errorRecord);
            }
        }

        #endregion
    }

    #endregion

    #region EnableRunspaceDebugCommand Cmdlet

    /// <summary>
    /// This cmdlet enables debugging for selected runspaces in the current or specified process.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Enable, "RunspaceDebug", DefaultParameterSetName = CommonRunspaceCommandBase.RunspaceNameParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096831")]
    public sealed class EnableRunspaceDebugCommand : CommonRunspaceCommandBase
    {
        #region Parameters

        /// <summary>
        /// When true this property will cause any running command or script in the Runspace
        /// to stop in step mode, regardless of whether a debugger is currently attached.  The
        /// script or command will remain stopped until a debugger is attached to debug the
        /// current stop point.
        /// </summary>
        [Parameter(Position = 1,
                   ParameterSetName = CommonRunspaceCommandBase.RunspaceParameterSet)]
        [Parameter(Position = 1,
                   ParameterSetName = CommonRunspaceCommandBase.RunspaceNameParameterSet)]
        [Parameter(Position = 1,
                   ParameterSetName = CommonRunspaceCommandBase.RunspaceIdParameterSet)]
        public SwitchParameter BreakAll
        {
            get;
            set;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Process Record.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (this.ParameterSetName.Equals(CommonRunspaceCommandBase.ProcessNameParameterSet))
            {
                SetDebugPreferenceHelper(ProcessName, AppDomainName, true, "EnableRunspaceDebugCommandPersistDebugPreferenceFailure");
                return;
            }

            IReadOnlyList<Runspace> results = GetRunspaces();

            foreach (var runspace in results)
            {
                if (runspace.RunspaceStateInfo.State != RunspaceState.Opened)
                {
                    WriteError(
                        new ErrorRecord(new PSInvalidOperationException(string.Format(CultureInfo.InvariantCulture, Debugger.RunspaceOptionInvalidRunspaceState, runspace.Name)),
                        "SetRunspaceDebugOptionCommandInvalidRunspaceState",
                        ErrorCategory.InvalidOperation,
                        this));

                    continue;
                }

                System.Management.Automation.Debugger debugger = GetDebuggerFromRunspace(runspace);
                if (debugger == null)
                {
                    continue;
                }

                // Enable debugging by preserving debug stop events.
                debugger.UnhandledBreakpointMode = UnhandledBreakpointProcessingMode.Wait;

                if (this.MyInvocation.BoundParameters.ContainsKey(nameof(BreakAll)))
                {
                    if (BreakAll)
                    {
                        try
                        {
                            debugger.SetDebuggerStepMode(true);
                        }
                        catch (PSInvalidOperationException e)
                        {
                            WriteError(
                                new ErrorRecord(
                                e,
                                "SetRunspaceDebugOptionCommandCannotEnableDebuggerStepping",
                                ErrorCategory.InvalidOperation,
                                this));
                        }
                    }
                    else
                    {
                        debugger.SetDebuggerStepMode(false);
                    }
                }
            }
        }

        #endregion
    }

    #endregion

    #region DisableRunspaceDebugCommand Cmdlet

    /// <summary>
    /// This cmdlet disables Runspace debugging in selected Runspaces.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Disable, "RunspaceDebug", DefaultParameterSetName = CommonRunspaceCommandBase.RunspaceNameParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096924")]
    public sealed class DisableRunspaceDebugCommand : CommonRunspaceCommandBase
    {
        #region Overrides

        /// <summary>
        /// Process Record.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (this.ParameterSetName.Equals(CommonRunspaceCommandBase.ProcessNameParameterSet))
            {
                SetDebugPreferenceHelper(ProcessName.ToLowerInvariant(), AppDomainName, false, "DisableRunspaceDebugCommandPersistDebugPreferenceFailure");
            }
            else
            {
                IReadOnlyList<Runspace> results = GetRunspaces();

                foreach (var runspace in results)
                {
                    if (runspace.RunspaceStateInfo.State != RunspaceState.Opened)
                    {
                        WriteError(
                            new ErrorRecord(
                                new PSInvalidOperationException(string.Format(CultureInfo.InvariantCulture, Debugger.RunspaceOptionInvalidRunspaceState, runspace.Name)),
                                "SetRunspaceDebugOptionCommandInvalidRunspaceState",
                                ErrorCategory.InvalidOperation,
                                this)
                            );

                        continue;
                    }

                    System.Management.Automation.Debugger debugger = GetDebuggerFromRunspace(runspace);
                    if (debugger == null)
                    {
                        continue;
                    }

                    debugger.SetDebuggerStepMode(false);
                    debugger.UnhandledBreakpointMode = UnhandledBreakpointProcessingMode.Ignore;
                }
            }
        }

        #endregion
    }

    #endregion

    #region GetRunspaceDebugCommand Cmdlet

    /// <summary>
    /// This cmdlet returns a PSRunspaceDebug object for each found Runspace object.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "RunspaceDebug", DefaultParameterSetName = CommonRunspaceCommandBase.RunspaceNameParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2097015")]
    [OutputType(typeof(PSRunspaceDebug))]
    public sealed class GetRunspaceDebugCommand : CommonRunspaceCommandBase
    {
        #region Overrides

        /// <summary>
        /// Process Record.
        /// </summary>
        protected override void ProcessRecord()
        {
            IReadOnlyList<Runspace> results = GetRunspaces();

            foreach (var runspace in results)
            {
                System.Management.Automation.Debugger debugger = GetDebuggerFromRunspace(runspace);
                if (debugger != null)
                {
                    WriteObject(
                        new PSRunspaceDebug((debugger.UnhandledBreakpointMode == UnhandledBreakpointProcessingMode.Wait),
                            debugger.IsDebuggerSteppingEnabled,
                            runspace.Name,
                            runspace.Id)
                        );
                }
            }
        }

        #endregion
    }

    #endregion

    #region WaitDebuggerCommand Cmdlet

    /// <summary>
    /// This cmdlet causes a running script or command to stop in the debugger at the next execution point.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Wait, "Debugger",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2097035")]
    public sealed class WaitDebuggerCommand : PSCmdlet
    {
        #region Overrides

        /// <summary>
        /// EndProcessing.
        /// </summary>
        protected override void EndProcessing()
        {
            Runspace currentRunspace = this.Context.CurrentRunspace;

            if (currentRunspace != null && currentRunspace.Debugger != null)
            {
                WriteVerbose(string.Format(CultureInfo.InvariantCulture, Debugger.DebugBreakMessage, MyInvocation.ScriptLineNumber, MyInvocation.ScriptName));

                currentRunspace.Debugger.Break();
            }
        }

        #endregion
    }

    #endregion
}
