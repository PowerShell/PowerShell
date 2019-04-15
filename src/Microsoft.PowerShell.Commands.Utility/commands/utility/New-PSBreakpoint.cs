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
    [Experimental("Microsoft.PowerShell.Utility.PSDebugRunspaceWithBreakpoints", ExperimentAction.Show)]
    [Cmdlet(VerbsCommon.New, "PSBreakpoint", DefaultParameterSetName = LineParameterSetName, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113449")]
    [OutputType(typeof(VariableBreakpoint), typeof(CommandBreakpoint), typeof(LineBreakpoint))]
    public class NewPSBreakpointCommand : PSBreakpointCreationBase
    {
        /// <summary>
        /// Create a new breakpoint.
        /// </summary>
        protected override void ProcessRecord()
        {
            // If there is a script, resolve its path
            Collection<string> scripts = ResolveScriptPaths();

            // If it is a command breakpoint...
            if (ParameterSetName.Equals(CommandParameterSetName, StringComparison.OrdinalIgnoreCase))
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
            else if (ParameterSetName.Equals(VariableParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                // If it is a variable breakpoint...
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
            else
            {
                // Else it is the default parameter set (Line breakpoint)...
                Debug.Assert(ParameterSetName.Equals(LineParameterSetName, StringComparison.OrdinalIgnoreCase));

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
                        if (Column != 0)
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
