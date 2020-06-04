// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements Set-PSBreakpoint command.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PSBreakpoint", DefaultParameterSetName = LineParameterSetName, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096623")]
    [OutputType(typeof(VariableBreakpoint), typeof(CommandBreakpoint), typeof(LineBreakpoint))]
    public class SetPSBreakpointCommand : PSBreakpointCreationBase
    {
        /// <summary>
        /// Verifies that debugging is supported.
        /// </summary>
        protected override void BeginProcessing()
        {
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
            Collection<string> scripts = ResolveScriptPaths();

            //
            // If it is a command breakpoint...
            //
            if (ParameterSetName.Equals(CommandParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < Command.Length; i++)
                {
                    if (scripts.Count > 0)
                    {
                        foreach (string path in scripts)
                        {
                            WriteObject(
                                Context.Debugger.SetCommandBreakpoint(Command[i], Action, path));
                        }
                    }
                    else
                    {
                        WriteObject(
                            Context.Debugger.SetCommandBreakpoint(Command[i], Action, path: null));
                    }
                }
            }
            //
            // If it is a variable breakpoint...
            //
            else if (ParameterSetName.Equals(VariableParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < Variable.Length; i++)
                {
                    if (scripts.Count > 0)
                    {
                        foreach (string path in scripts)
                        {
                            WriteObject(
                                Context.Debugger.SetVariableBreakpoint(Variable[i], Mode, Action, path));
                        }
                    }
                    else
                    {
                        WriteObject(
                            Context.Debugger.SetVariableBreakpoint(Variable[i], Mode, Action, path: null));
                    }
                }
            }
            //
            // Else it is the default parameter set (Line breakpoint)...
            //
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
                        WriteObject(
                            Context.Debugger.SetLineBreakpoint(path, Line[i], Column, Action));
                    }
                }
            }
        }
    }
}
