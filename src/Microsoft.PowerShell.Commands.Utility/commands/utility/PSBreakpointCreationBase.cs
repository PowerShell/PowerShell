// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Base class for Set/New-PSBreakpoint.
    /// </summary>
    public class PSBreakpointCreationBase : PSCmdlet
    {
        internal const string CommandParameterSetName = "Command";
        internal const string LineParameterSetName = "Line";
        internal const string VariableParameterSetName = "Variable";

        #region parameters

        /// <summary>
        /// The action to take when hitting this breakpoint.
        /// </summary>
        [Parameter(ParameterSetName = CommandParameterSetName)]
        [Parameter(ParameterSetName = LineParameterSetName)]
        [Parameter(ParameterSetName = VariableParameterSetName)]
        public ScriptBlock Action { get; set; }

        /// <summary>
        /// The column to set the breakpoint on.
        /// </summary>
        [Parameter(Position = 2, ParameterSetName = LineParameterSetName)]
        [ValidateRange(1, int.MaxValue)]
        public int Column { get; set; }

        /// <summary>
        /// The command(s) to set the breakpoint on.
        /// </summary>
        [Alias("C")]
        [Parameter(ParameterSetName = CommandParameterSetName, Mandatory = true)]
        public string[] Command { get; set; }

        /// <summary>
        /// The line to set the breakpoint on.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = LineParameterSetName, Mandatory = true)]
        public int[] Line { get; set; }

        /// <summary>
        /// The script to set the breakpoint on.
        /// </summary>
        [Parameter(ParameterSetName = CommandParameterSetName, Position = 0)]
        [Parameter(ParameterSetName = LineParameterSetName, Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = VariableParameterSetName, Position = 0)]
        [ValidateNotNull]
        public string[] Script { get; set; }

        /// <summary>
        /// The variables to set the breakpoint(s) on.
        /// </summary>
        [Alias("V")]
        [Parameter(ParameterSetName = VariableParameterSetName, Mandatory = true)]
        public string[] Variable { get; set; }

        /// <summary>
        /// The access type for variable breakpoints to break on.
        /// </summary>
        [Parameter(ParameterSetName = VariableParameterSetName)]
        public VariableAccessMode Mode { get; set; } = VariableAccessMode.Write;

        #endregion parameters

        internal Collection<string> ResolveScriptPaths()
        {
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

            return scripts;
        }
    }
}
