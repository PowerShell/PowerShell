// Copyright (c) Microsoft Corporation. All rights reserved.
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
    /// This class implements New-PSBreakpoint command.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PSBreakpoint", DefaultParameterSetName = "Line", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113449")]
    [OutputType(typeof(VariableBreakpoint), typeof(CommandBreakpoint), typeof(LineBreakpoint))]
    public class NewPSBreakpointCommand : PSCmdlet
    {
        #region parameters

        /// <summary>
        /// The action to take when hitting this breakpoint.
        /// </summary>
        [Parameter(ParameterSetName = "Command")]
        [Parameter(ParameterSetName = "Line")]
        [Parameter(ParameterSetName = "Variable")]
        public ScriptBlock Action { get; set; } = null;

        /// <summary>
        /// The column to set the breakpoint on.
        /// </summary>
        [Parameter(Position = 2, ParameterSetName = "Line")]
        [ValidateRange(1, int.MaxValue)]
        public int Column
        {
            get
            {
                return _column ?? 0;
            }

            set
            {
                _column = value;
            }
        }

        private int? _column = null;

        /// <summary>
        /// The command(s) to set the breakpoint on.
        /// </summary>
        [Alias("C")]
        [Parameter(ParameterSetName = "Command", Mandatory = true)]
        [ValidateNotNull]
        public string[] Command { get; set; } = null;

        /// <summary>
        /// The line to set the breakpoint on.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = "Line", Mandatory = true)]
        [ValidateNotNull]
        public int[] Line { get; set; } = null;

        /// <summary>
        /// The script to set the breakpoint on.
        /// </summary>
        [Parameter(ParameterSetName = "Command", Position = 0)]
        [Parameter(ParameterSetName = "Line", Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "Variable", Position = 0)]
        [ValidateNotNull]
        public string[] Script { get; set; } = null;

        /// <summary>
        /// The variables to set the breakpoint(s) on.
        /// </summary>
        [Alias("V")]
        [Parameter(ParameterSetName = "Variable", Mandatory = true)]
        [ValidateNotNull]
        public string[] Variable { get; set; } = null;

        /// <summary>
        /// </summary>
        [Parameter(ParameterSetName = "Variable")]
        public VariableAccessMode Mode { get; set; } = VariableAccessMode.Write;

        #endregion parameters

        /// <summary>
        /// Create a new breakpoint.
        /// </summary>
        protected override void ProcessRecord()
        {
            // If there is a script, resolve its path
            Collection<string> scripts = new Collection<string>();

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

            //
            // If it is a command breakpoint...
            //
            if (ParameterSetName.Equals("Command", StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < Command.Length; i++)
                {
                    if (scripts.Count > 0)
                    {
                        foreach (string path in scripts)
                        {
                            WildcardPattern pattern = WildcardPattern.Get(Command[i], WildcardOptions.Compiled | WildcardOptions.IgnoreCase);
                            WriteObject(new CommandBreakpoint(path, pattern, Command[i], Action));
                        }
                    }
                    else
                    {
                        WildcardPattern pattern = WildcardPattern.Get(Command[i], WildcardOptions.Compiled | WildcardOptions.IgnoreCase);
                        WriteObject(new CommandBreakpoint(null, pattern, Command[i], Action));
                    }
                }
            }
            //
            // If it is a variable breakpoint...
            //
            else if (ParameterSetName.Equals("Variable", StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < Variable.Length; i++)
                {
                    if (scripts.Count > 0)
                    {
                        foreach (string path in scripts)
                        {
                            WriteObject(new VariableBreakpoint(path, Variable[i], Mode, Action));
                        }
                    }
                    else
                    {
                        WriteObject(new VariableBreakpoint(null, Variable[i], Mode, Action));
                    }
                }
            }
            //
            // Else it is the default parameter set (Line breakpoint)...
            //
            else
            {
                Debug.Assert(ParameterSetName.Equals("Line", StringComparison.OrdinalIgnoreCase));

                for (int i = 0; i < Line.Length; i++)
                {
                    if (Line[i] < 1)
                    {
                        WriteError(
                            new ErrorRecord(
                                new ArgumentException(Debugger.LineLessThanOne),
                                "NewPSBreakpoint:LineLessThanOne",
                                ErrorCategory.InvalidArgument,
                                null));

                        continue;
                    }

                    foreach (string path in scripts)
                    {
                        if (_column != null)
                        {
                            WriteObject(new LineBreakpoint(path, Line[i], Column, Action));
                        }
                        else
                        {
                            WriteObject(new LineBreakpoint(path, Line[i], Action));
                        }
                    }
                }
            }
        }
    }
}
