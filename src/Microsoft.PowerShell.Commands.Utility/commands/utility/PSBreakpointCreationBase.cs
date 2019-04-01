// Copyright (c) Microsoft Corporation. All rights reserved.
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
        #region parameters

        /// <summary>
        /// The action to take when hitting this breakpoint.
        /// </summary>
        [Parameter(ParameterSetName = "Command")]
        [Parameter(ParameterSetName = "Line")]
        [Parameter(ParameterSetName = "Variable")]
        public ScriptBlock Action { get; set; }

        /// <summary>
        /// The column to set the breakpoint on.
        /// </summary>
        [Parameter(Position = 2, ParameterSetName = "Line")]
        [ValidateRange(1, int.MaxValue)]
        public int Column { get; set; }

        /// <summary>
        /// The command(s) to set the breakpoint on.
        /// </summary>
        [Alias("C")]
        [Parameter(ParameterSetName = "Command", Mandatory = true)]
        public string[] Command { get; set; }

        /// <summary>
        /// The line to set the breakpoint on.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = "Line", Mandatory = true)]
        public int[] Line { get; set; }

        /// <summary>
        /// The script to set the breakpoint on.
        /// </summary>
        [Parameter(ParameterSetName = "Command", Position = 0)]
        [Parameter(ParameterSetName = "Line", Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "Variable", Position = 0)]
        [ValidateNotNull]
        public string[] Script { get; set; }

        /// <summary>
        /// The variables to set the breakpoint(s) on.
        /// </summary>
        [Alias("V")]
        [Parameter(ParameterSetName = "Variable", Mandatory = true)]
        public string[] Variable { get; set; }

        /// <summary>
        /// </summary>
        [Parameter(ParameterSetName = "Variable")]
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
