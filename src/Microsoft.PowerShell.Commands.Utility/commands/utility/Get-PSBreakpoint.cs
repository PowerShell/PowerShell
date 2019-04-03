// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Types of breakpoints.
    /// </summary>
    public enum BreakpointType
    {
        /// <summary>Breakpoint on a line within a script</summary>
        Line,

        /// <summary>
        /// Breakpoint on a variable</summary>
        Variable,

        /// <summary>Breakpoint on a command</summary>
        Command
    };

    /// <summary>
    /// This class implements Remove-PSBreakpoint.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PSBreakpoint", DefaultParameterSetName = "Script", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113325")]
    [OutputType(typeof(Breakpoint))]
    public class GetPSBreakpointCommand : PSCmdlet
    {
        #region parameters
        /// <summary>
        /// Scripts of the breakpoints to output.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "It's OK to use arrays for cmdlet parameters")]
        [Parameter(ParameterSetName = "Script", Position = 0, ValueFromPipeline = true)]
        [Parameter(ParameterSetName = "Variable")]
        [Parameter(ParameterSetName = "Command")]
        [Parameter(ParameterSetName = "Type")]
        [ValidateNotNullOrEmpty()]
        public string[] Script
        {
            get
            {
                return _script;
            }

            set
            {
                _script = value;
            }
        }

        private string[] _script;

        /// <summary>
        /// IDs of the breakpoints to output.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "It's OK to use arrays for cmdlet parameters")]
        [Parameter(ParameterSetName = "Id", Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [ValidateNotNull]
        public int[] Id
        {
            get
            {
                return _id;
            }

            set
            {
                _id = value;
            }
        }

        private int[] _id;

        /// <summary>
        /// Variables of the breakpoints to output.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "It's OK to use arrays for cmdlet parameters")]
        [Parameter(ParameterSetName = "Variable", Mandatory = true)]
        [ValidateNotNull]
        public string[] Variable
        {
            get
            {
                return _variable;
            }

            set
            {
                _variable = value;
            }
        }

        private string[] _variable;

        /// <summary>
        /// Commands of the breakpoints to output.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "It's OK to use arrays for cmdlet parameters")]
        [Parameter(ParameterSetName = "Command", Mandatory = true)]
        [ValidateNotNull]
        public string[] Command
        {
            get
            {
                return _command;
            }

            set
            {
                _command = value;
            }
        }

        private string[] _command;

        /// <summary>
        /// Commands of the breakpoints to output.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "It's OK to use arrays for cmdlet parameters")]
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "Type is OK for a cmdlet parameter")]
        [Parameter(ParameterSetName = "Type", Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [ValidateNotNull]
        public BreakpointType[] Type
        {
            get
            {
                return _type;
            }

            set
            {
                _type = value;
            }
        }

        private BreakpointType[] _type;

        #endregion parameters

        /// <summary>
        /// Remove breakpoints.
        /// </summary>
        protected override void ProcessRecord()
        {
            List<Breakpoint> breakpoints = Context.Debugger.GetBreakpoints();

            //
            // Filter by parameter set
            //
            if (this.ParameterSetName.Equals("Script", StringComparison.OrdinalIgnoreCase))
            {
                // no filter
            }
            else if (this.ParameterSetName.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                breakpoints = Filter(
                    breakpoints,
                    _id,
                    delegate (Breakpoint breakpoint, int id)
                    {
                        return breakpoint.Id == id;
                    }
                );
            }
            else if (this.ParameterSetName.Equals("Command", StringComparison.OrdinalIgnoreCase))
            {
                breakpoints = Filter(
                    breakpoints,
                    _command,
                    delegate (Breakpoint breakpoint, string command)
                    {
                        CommandBreakpoint commandBreakpoint = breakpoint as CommandBreakpoint;

                        if (commandBreakpoint == null)
                        {
                            return false;
                        }

                        return commandBreakpoint.Command.Equals(command, StringComparison.OrdinalIgnoreCase);
                    });
            }
            else if (this.ParameterSetName.Equals("Variable", StringComparison.OrdinalIgnoreCase))
            {
                breakpoints = Filter(
                    breakpoints,
                    _variable,
                    delegate (Breakpoint breakpoint, string variable)
                    {
                        VariableBreakpoint variableBreakpoint = breakpoint as VariableBreakpoint;

                        if (variableBreakpoint == null)
                        {
                            return false;
                        }

                        return variableBreakpoint.Variable.Equals(variable, StringComparison.OrdinalIgnoreCase);
                    });
            }
            else if (this.ParameterSetName.Equals("Type", StringComparison.OrdinalIgnoreCase))
            {
                breakpoints = Filter(
                    breakpoints,
                    _type,
                    delegate (Breakpoint breakpoint, BreakpointType type)
                    {
                        switch (type)
                        {
                            case BreakpointType.Line:
                                if (breakpoint is LineBreakpoint)
                                {
                                    return true;
                                }

                                break;

                            case BreakpointType.Command:
                                if (breakpoint is CommandBreakpoint)
                                {
                                    return true;
                                }

                                break;

                            case BreakpointType.Variable:
                                if (breakpoint is VariableBreakpoint)
                                {
                                    return true;
                                }

                                break;
                        }

                        return false;
                    });
            }
            else
            {
                Diagnostics.Assert(false, "Invalid parameter set: {0}", this.ParameterSetName);
            }

            //
            // Filter by script
            //
            if (_script != null)
            {
                breakpoints = Filter(
                    breakpoints,
                    _script,
                    delegate (Breakpoint breakpoint, string script)
                    {
                        if (breakpoint.Script == null)
                        {
                            return false;
                        }

                        return string.Compare(
                            SessionState.Path.GetUnresolvedProviderPathFromPSPath(breakpoint.Script),
                            SessionState.Path.GetUnresolvedProviderPathFromPSPath(script),
                            StringComparison.OrdinalIgnoreCase
                        ) == 0;
                    });
            }

            //
            // Output results
            //
            foreach (Breakpoint b in breakpoints)
            {
                WriteObject(b);
            }
        }

        /// <summary>
        /// Gives the criteria to filter breakpoints.
        /// </summary>
        private delegate bool FilterSelector<T>(Breakpoint breakpoint, T target);

        /// <summary>
        /// Returns the items in the input list that match an item in the filter array according to
        /// the given selection criterion.
        /// </summary>
        private List<Breakpoint> Filter<T>(List<Breakpoint> input, T[] filter, FilterSelector<T> selector)
        {
            List<Breakpoint> output = new List<Breakpoint>();

            for (int i = 0; i < input.Count; i++)
            {
                for (int j = 0; j < filter.Length; j++)
                {
                    if (selector(input[i], filter[j]))
                    {
                        output.Add(input[i]);
                        break;
                    }
                }
            }

            return output;
        }
    }
}
