// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Set-PSBreakpoint command.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PSBreakpoint", DefaultParameterSetName = LineParameterSetName, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096623")]
    [OutputType(typeof(CommandBreakpoint), ParameterSetName = new string[] { CommandParameterSetName })]
    [OutputType(typeof(LineBreakpoint), ParameterSetName = new string[] { LineParameterSetName })]
    [OutputType(typeof(VariableBreakpoint), ParameterSetName = new string[] { VariableParameterSetName })]
    public class SetPSBreakpointCommand : PSBreakpointAccessorCommandBase
    {
        #region parameters

        /// <summary>
        /// Gets or sets the action to take when hitting this breakpoint.
        /// </summary>
        [Parameter(ParameterSetName = CommandParameterSetName)]
        [Parameter(ParameterSetName = LineParameterSetName)]
        [Parameter(ParameterSetName = VariableParameterSetName)]
        public ScriptBlock Action { get; set; }

        /// <summary>
        /// Gets or sets the column to set the breakpoint on.
        /// </summary>
        [Parameter(Position = 2, ParameterSetName = LineParameterSetName)]
        [ValidateRange(1, int.MaxValue)]
        public int Column { get; set; }

        /// <summary>
        /// Gets or sets the command(s) to set the breakpoint on.
        /// </summary>
        [Alias("C")]
        [Parameter(ParameterSetName = CommandParameterSetName, Mandatory = true)]
        public string[] Command { get; set; }

        /// <summary>
        /// Gets or sets the line to set the breakpoint on.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = LineParameterSetName, Mandatory = true)]
        public int[] Line { get; set; }

        /// <summary>
        /// Gets or sets the script to set the breakpoint on.
        /// </summary>
        [Parameter(ParameterSetName = CommandParameterSetName, Position = 0)]
        [Parameter(ParameterSetName = LineParameterSetName, Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = VariableParameterSetName, Position = 0)]
        [ValidateNotNull]
        public string[] Script { get; set; }

        /// <summary>
        /// Gets or sets the variables to set the breakpoint(s) on.
        /// </summary>
        [Alias("V")]
        [Parameter(ParameterSetName = VariableParameterSetName, Mandatory = true)]
        public string[] Variable { get; set; }

        /// <summary>
        /// Gets or sets the access type for variable breakpoints to break on.
        /// </summary>
        [Parameter(ParameterSetName = VariableParameterSetName)]
        public VariableAccessMode Mode { get; set; } = VariableAccessMode.Write;

        #endregion parameters

        #region overrides

        /// <summary>
        /// Verifies that debugging is supported.
        /// </summary>
        protected override void BeginProcessing()
        {
            // Call the base method to ensure Runspace is initialized properly.
            base.BeginProcessing();

            // Check whether we are executing on a remote session and if so
            // whether the RemoteScript debug option is selected.
            if (this.Context.InternalHost.ExternalHost is System.Management.Automation.Remoting.ServerRemoteHost &&
                ((this.Context.CurrentRunspace == null) || (this.Context.CurrentRunspace.Debugger == null) ||
                 ((this.Context.CurrentRunspace.Debugger.DebugMode & DebugModes.RemoteScript) != DebugModes.RemoteScript) &&
                  (this.Context.CurrentRunspace.Debugger.DebugMode != DebugModes.None)))
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSNotSupportedException(Debugger.RemoteDebuggerNotSupportedInHost),
                        "SetPSBreakpoint:RemoteDebuggerNotSupported",
                        ErrorCategory.NotImplemented,
                        null));
            }

            // If we're in ConstrainedLanguage mode and the system is not in lockdown mode,
            // don't allow breakpoints as we can't protect that boundary.
            // This covers the case where the debugger could modify variables in a trusted
            // script block.  So debugging is supported in Constrained language mode only if
            // the system is also in lock down mode.
            if ((Context.LanguageMode == PSLanguageMode.ConstrainedLanguage) &&
                (System.Management.Automation.Security.SystemPolicy.GetSystemLockdownPolicy() !=
                 System.Management.Automation.Security.SystemEnforcementMode.Enforce))
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new PSNotSupportedException(Debugger.RemoteDebuggerNotSupported),
                            "CannotSetBreakpointInconsistentLanguageMode",
                            ErrorCategory.PermissionDenied,
                            Context.LanguageMode));
            }
        }

        /// <summary>
        /// Set a new breakpoint.
        /// </summary>
        protected override void ProcessRecord()
        {
            // If there is a script, resolve its path
            Collection<string> scripts = new();

            if (Script != null)
            {
                foreach (string script in Script)
                {
                    Collection<PathInfo> scriptPaths = SessionState.Path.GetResolvedPSPathFromPSPath(script);

                    for (int i = 0; i < scriptPaths.Count; i++)
                    {
                        string providerPath = scriptPaths[i].ProviderPath;

                        if (!File.Exists(providerPath))
                        {
                            WriteError(
                                new ErrorRecord(
                                    new ArgumentException(StringUtil.Format(Debugger.FileDoesNotExist, providerPath)),
                                    "NewPSBreakpoint:FileDoesNotExist",
                                    ErrorCategory.InvalidArgument,
                                    null));

                            continue;
                        }

                        string extension = Path.GetExtension(providerPath);

                        if (!extension.Equals(".ps1", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".psm1", StringComparison.OrdinalIgnoreCase))
                        {
                            WriteError(
                                new ErrorRecord(
                                    new ArgumentException(StringUtil.Format(Debugger.WrongExtension, providerPath)),
                                    "NewPSBreakpoint:WrongExtension",
                                    ErrorCategory.InvalidArgument,
                                    null));
                            continue;
                        }

                        scripts.Add(Path.GetFullPath(providerPath));
                    }
                }
            }

            // If it is a command breakpoint...
            if (ParameterSetName.Equals(CommandParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < Command.Length; i++)
                {
                    if (scripts.Count > 0)
                    {
                        foreach (string path in scripts)
                        {
                            ProcessBreakpoint(
                                Runspace.Debugger.SetCommandBreakpoint(Command[i], Action, path));
                        }
                    }
                    else
                    {
                        ProcessBreakpoint(
                            Runspace.Debugger.SetCommandBreakpoint(Command[i], Action, path: null));
                    }
                }
            }
            // If it is a variable breakpoint...
            else if (ParameterSetName.Equals(VariableParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < Variable.Length; i++)
                {
                    if (scripts.Count > 0)
                    {
                        foreach (string path in scripts)
                        {
                            ProcessBreakpoint(
                                Runspace.Debugger.SetVariableBreakpoint(Variable[i], Mode, Action, path));
                        }
                    }
                    else
                    {
                        ProcessBreakpoint(
                            Runspace.Debugger.SetVariableBreakpoint(Variable[i], Mode, Action, path: null));
                    }
                }
            }
            // Else it is the default parameter set (Line breakpoint)...
            else
            {
                Debug.Assert(ParameterSetName.Equals(LineParameterSetName, StringComparison.OrdinalIgnoreCase));

                for (int i = 0; i < Line.Length; i++)
                {
                    if (Line[i] < 1)
                    {
                        WriteError(
                            new ErrorRecord(
                                new ArgumentException(Debugger.LineLessThanOne),
                                "SetPSBreakpoint:LineLessThanOne",
                                ErrorCategory.InvalidArgument,
                                null));

                        continue;
                    }

                    foreach (string path in scripts)
                    {
                        ProcessBreakpoint(
                            Runspace.Debugger.SetLineBreakpoint(path, Line[i], Column, Action));
                    }
                }
            }
        }

        #endregion overrides
    }
}
