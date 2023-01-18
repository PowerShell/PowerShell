// Copyright (c) Microsoft Corporation.
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

        /// <summary>Breakpoint on a variable</summary>
        Variable,

        /// <summary>Breakpoint on a command</summary>
        Command
    }

    /// <summary>
    /// This class implements Get-PSBreakpoint.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PSBreakpoint", DefaultParameterSetName = LineParameterSetName, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097108")]
    [OutputType(typeof(CommandBreakpoint), ParameterSetName = new[] { CommandParameterSetName })]
    [OutputType(typeof(LineBreakpoint), ParameterSetName = new[] { LineParameterSetName })]
    [OutputType(typeof(VariableBreakpoint), ParameterSetName = new[] { VariableParameterSetName })]
    [OutputType(typeof(Breakpoint), ParameterSetName = new[] { TypeParameterSetName, IdParameterSetName })]
    public class GetPSBreakpointCommand : PSBreakpointAccessorCommandBase
    {
        #region strings

        internal const string TypeParameterSetName = "Type";
        internal const string IdParameterSetName = "Id";

        #endregion strings

        #region parameters
        /// <summary>
        /// Scripts of the breakpoints to output.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "It's OK to use arrays for cmdlet parameters")]
        [Parameter(ParameterSetName = LineParameterSetName, Position = 0, ValueFromPipeline = true)]
        [Parameter(ParameterSetName = CommandParameterSetName)]
        [Parameter(ParameterSetName = VariableParameterSetName)]
        [Parameter(ParameterSetName = TypeParameterSetName)]
        [ValidateNotNullOrEmpty()]
        public string[] Script { get; set; }

        /// <summary>
        /// IDs of the breakpoints to output.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "It's OK to use arrays for cmdlet parameters")]
        [Parameter(ParameterSetName = IdParameterSetName, Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [ValidateNotNull]
        public int[] Id { get; set; }

        /// <summary>
        /// Variables of the breakpoints to output.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "It's OK to use arrays for cmdlet parameters")]
        [Parameter(ParameterSetName = VariableParameterSetName, Mandatory = true)]
        [ValidateNotNull]
        public string[] Variable { get; set; }

        /// <summary>
        /// Commands of the breakpoints to output.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "It's OK to use arrays for cmdlet parameters")]
        [Parameter(ParameterSetName = CommandParameterSetName, Mandatory = true)]
        [ValidateNotNull]
        public string[] Command { get; set; }

        /// <summary>
        /// Commands of the breakpoints to output.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "It's OK to use arrays for cmdlet parameters")]
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "Type is OK for a cmdlet parameter")]
        [Parameter(ParameterSetName = TypeParameterSetName, Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [ValidateNotNull]
        public BreakpointType[] Type { get; set; }

        #endregion parameters

        #region overrides

        /// <summary>
        /// Remove breakpoints.
        /// </summary>
        protected override void ProcessRecord()
        {
            List<Breakpoint> breakpoints = Runspace.Debugger.GetBreakpoints();

            // Filter by parameter set
            if (ParameterSetName.Equals(LineParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                // no filter
            }
            else if (ParameterSetName.Equals(IdParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                breakpoints = Filter(
                    breakpoints,
                    Id,
                    static (Breakpoint breakpoint, int id) => breakpoint.Id == id);
            }
            else if (ParameterSetName.Equals(CommandParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                breakpoints = Filter(
                    breakpoints,
                    Command,
                    (Breakpoint breakpoint, string command) =>
                    {
                        if (!(breakpoint is CommandBreakpoint commandBreakpoint))
                        {
                            return false;
                        }

                        return commandBreakpoint.Command.Equals(command, StringComparison.OrdinalIgnoreCase);
                    });
            }
            else if (ParameterSetName.Equals(VariableParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                breakpoints = Filter(
                    breakpoints,
                    Variable,
                    (Breakpoint breakpoint, string variable) =>
                    {
                        if (!(breakpoint is VariableBreakpoint variableBreakpoint))
                        {
                            return false;
                        }

                        return variableBreakpoint.Variable.Equals(variable, StringComparison.OrdinalIgnoreCase);
                    });
            }
            else if (ParameterSetName.Equals(TypeParameterSetName, StringComparison.OrdinalIgnoreCase))
            {
                breakpoints = Filter(
                    breakpoints,
                    Type,
                    (Breakpoint breakpoint, BreakpointType type) =>
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
                Diagnostics.Assert(false, $"Invalid parameter set: {this.ParameterSetName}");
            }

            // Filter by script
            if (Script != null)
            {
                breakpoints = Filter(
                    breakpoints,
                    Script,
                    (Breakpoint breakpoint, string script) =>
                    {
                        if (breakpoint.Script == null)
                        {
                            return false;
                        }

                        return string.Equals(
                            SessionState.Path.GetUnresolvedProviderPathFromPSPath(breakpoint.Script),
                            SessionState.Path.GetUnresolvedProviderPathFromPSPath(script),
                            StringComparison.OrdinalIgnoreCase
                        );
                    });
            }

            // Output results
            foreach (Breakpoint b in breakpoints)
            {
                ProcessBreakpoint(b);
            }
        }

        #endregion overrides

        #region private methods

        /// <summary>
        /// Gives the criteria to filter breakpoints.
        /// </summary>
        private delegate bool FilterSelector<T>(Breakpoint breakpoint, T target);

        /// <summary>
        /// Returns the items in the input list that match an item in the filter array according to
        /// the given selection criterion.
        /// </summary>
        private static List<Breakpoint> Filter<T>(List<Breakpoint> input, T[] filter, FilterSelector<T> selector)
        {
            List<Breakpoint> output = new();

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

        #endregion private methods
    }
}
