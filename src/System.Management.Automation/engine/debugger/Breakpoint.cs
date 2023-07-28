// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.IO;
using System.Linq;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Threading;

namespace System.Management.Automation
{
    /// <summary>
    /// Holds the information for a given breakpoint.
    /// </summary>
    public abstract class Breakpoint
    {
        #region properties

        /// <summary>
        /// The action to take when the breakpoint is hit.
        /// </summary>
        public ScriptBlock Action { get; }

        /// <summary>
        /// Gets whether this breakpoint is enabled.
        /// </summary>
        public bool Enabled { get; private set; }

        internal void SetEnabled(bool value)
        {
            Enabled = value;
        }

        /// <summary>
        /// Records how many times this breakpoint has been triggered.
        /// </summary>
        public int HitCount { get; private set; }

        /// <summary>
        /// This breakpoint's Id.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// True if breakpoint is set on a script, false if the breakpoint is not scoped.
        /// </summary>
        internal bool IsScriptBreakpoint
        {
            get { return Script != null; }
        }

        /// <summary>
        /// The script this breakpoint is on, or null if the breakpoint is not scoped.
        /// </summary>
        public string Script { get; }

        #endregion properties

        #region constructors

        /// <summary>
        /// Creates a new instance of a <see cref="Breakpoint"/>
        /// </summary>
        protected Breakpoint(string script)
            : this(script, null)
        { }

        /// <summary>
        /// Creates a new instance of a <see cref="Breakpoint"/>
        /// </summary>
        protected Breakpoint(string script, ScriptBlock action)
        {
            Enabled = true;
            Script = string.IsNullOrEmpty(script) ? null : script;
            Id = Interlocked.Increment(ref s_lastID);
            Action = action;
            HitCount = 0;
        }

        /// <summary>
        /// Creates a new instance of a <see cref="Breakpoint"/>
        /// </summary>
        protected Breakpoint(string script, int id)
            : this(script, null, id)
        { }

        /// <summary>
        /// Creates a new instance of a <see cref="Breakpoint"/>
        /// </summary>
        protected Breakpoint(string script, ScriptBlock action, int id)
        {
            Enabled = true;
            Script = string.IsNullOrEmpty(script) ? null : script;
            Id = id;
            Action = action;
            HitCount = 0;
        }

        #endregion constructors

        #region methods

        internal BreakpointAction Trigger()
        {
            ++HitCount;
            if (Action == null)
            {
                return BreakpointAction.Break;
            }

            try
            {
                // Pass this to the action so the breakpoint.  This could be used to
                // implement a "trigger once" breakpoint that disables itself after first hit.
                // One could also share an action across many breakpoints - and hence needs
                // to know something about the breakpoint that is hit, e.g. in a poor mans code coverage tool.
                Action.DoInvoke(dollarUnder: this, input: null, args: Array.Empty<object>());
            }
            catch (BreakException)
            {
                return BreakpointAction.Break;
            }
            catch (Exception)
            {
            }

            return BreakpointAction.Continue;
        }

        internal virtual bool RemoveSelf(ScriptDebugger debugger) => false;

        #endregion methods

        #region enums

        internal enum BreakpointAction
        {
            Continue = 0x0,
            Break = 0x1
        }

        #endregion enums

        #region private members

        private static int s_lastID;

        #endregion private members
    }

    /// <summary>
    /// A breakpoint on a command.
    /// </summary>
    public class CommandBreakpoint : Breakpoint
    {
        /// <summary>
        /// Creates a new instance of a <see cref="CommandBreakpoint"/>
        /// </summary>
        public CommandBreakpoint(string script, WildcardPattern command, string commandString)
            : this(script, command, commandString, null)
        { }

        /// <summary>
        /// Creates a new instance of a <see cref="CommandBreakpoint"/>
        /// </summary>
        public CommandBreakpoint(string script, WildcardPattern command, string commandString, ScriptBlock action)
            : base(script, action)
        {
            CommandPattern = command;
            Command = commandString;
        }

        /// <summary>
        /// Creates a new instance of a <see cref="CommandBreakpoint"/>
        /// </summary>
        public CommandBreakpoint(string script, WildcardPattern command, string commandString, int id)
            : this(script, command, commandString, null, id)
        { }

        /// <summary>
        /// Creates a new instance of a <see cref="CommandBreakpoint"/>
        /// </summary>
        public CommandBreakpoint(string script, WildcardPattern command, string commandString, ScriptBlock action, int id)
            : base(script, action, id)
        {
            CommandPattern = command;
            Command = commandString;
        }

        /// <summary>
        /// Which command this breakpoint is on.
        /// </summary>
        public string Command { get; }

        internal WildcardPattern CommandPattern { get; }

        /// <summary>
        /// Gets a string representation of this breakpoint.
        /// </summary>
        /// <returns>A string representation of this breakpoint.</returns>
        public override string ToString()
        {
            return IsScriptBreakpoint
                       ? StringUtil.Format(DebuggerStrings.CommandScriptBreakpointString, Script, Command)
                       : StringUtil.Format(DebuggerStrings.CommandBreakpointString, Command);
        }

        internal override bool RemoveSelf(ScriptDebugger debugger) =>
            debugger.RemoveCommandBreakpoint(this);

        private bool CommandInfoMatches(CommandInfo commandInfo)
        {
            if (commandInfo == null)
                return false;

            if (CommandPattern.IsMatch(commandInfo.Name))
                return true;

            // If the breakpoint looks like it might have specified a module name and the command
            // we're checking is in a module, try matching the module\command against the pattern
            // in the breakpoint.
            if (!string.IsNullOrEmpty(commandInfo.ModuleName) && Command.Contains('\\'))
            {
                if (CommandPattern.IsMatch(commandInfo.ModuleName + "\\" + commandInfo.Name))
                    return true;
            }

            var externalScript = commandInfo as ExternalScriptInfo;
            if (externalScript != null)
            {
                if (externalScript.Path.Equals(Command, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (CommandPattern.IsMatch(Path.GetFileNameWithoutExtension(externalScript.Path)))
                    return true;
            }

            return false;
        }

        internal bool Trigger(InvocationInfo invocationInfo)
        {
            // invocationInfo.MyCommand can be null when invoked via ScriptBlock.Invoke()
            if (CommandPattern.IsMatch(invocationInfo.InvocationName) || CommandInfoMatches(invocationInfo.MyCommand))
            {
                return (Script == null || Script.Equals(invocationInfo.ScriptName, StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }
    }

    /// <summary>
    /// The access type for variable breakpoints to break on.
    /// </summary>
    public enum VariableAccessMode
    {
        /// <summary>
        /// Break on read access only.
        /// </summary>
        Read,
        /// <summary>
        /// Break on write access only (default).
        /// </summary>
        Write,
        /// <summary>
        /// Breakon read or write access.
        /// </summary>
        ReadWrite
    }

    /// <summary>
    /// A breakpoint on a variable.
    /// </summary>
    public class VariableBreakpoint : Breakpoint
    {
        /// <summary>
        /// Creates a new instance of a <see cref="VariableBreakpoint"/>.
        /// </summary>
        public VariableBreakpoint(string script, string variable, VariableAccessMode accessMode)
            : this(script, variable, accessMode, null)
        { }

        /// <summary>
        /// Creates a new instance of a <see cref="VariableBreakpoint"/>.
        /// </summary>
        public VariableBreakpoint(string script, string variable, VariableAccessMode accessMode, ScriptBlock action)
            : base(script, action)
        {
            Variable = variable;
            AccessMode = accessMode;
        }

        /// <summary>
        /// Creates a new instance of a <see cref="VariableBreakpoint"/>.
        /// </summary>
        public VariableBreakpoint(string script, string variable, VariableAccessMode accessMode, int id)
            : this(script, variable, accessMode, null, id)
        { }

        /// <summary>
        /// Creates a new instance of a <see cref="VariableBreakpoint"/>.
        /// </summary>
        public VariableBreakpoint(string script, string variable, VariableAccessMode accessMode, ScriptBlock action, int id)
            : base(script, action, id)
        {
            Variable = variable;
            AccessMode = accessMode;
        }

        /// <summary>
        /// The access mode to trigger this variable breakpoint on.
        /// </summary>
        public VariableAccessMode AccessMode { get; }

        /// <summary>
        /// Which variable this breakpoint is on.
        /// </summary>
        public string Variable { get; }

        /// <summary>
        /// Gets the string representation of this breakpoint.
        /// </summary>
        /// <returns>The string representation of this breakpoint.</returns>
        public override string ToString()
        {
            return IsScriptBreakpoint
                       ? StringUtil.Format(DebuggerStrings.VariableScriptBreakpointString, Script, Variable, AccessMode)
                       : StringUtil.Format(DebuggerStrings.VariableBreakpointString, Variable, AccessMode);
        }

        internal bool Trigger(string currentScriptFile, bool read)
        {
            if (!Enabled)
                return false;

            if (AccessMode != VariableAccessMode.ReadWrite && AccessMode != (read ? VariableAccessMode.Read : VariableAccessMode.Write))
                return false;

            if (Script == null || Script.Equals(currentScriptFile, StringComparison.OrdinalIgnoreCase))
            {
                return Trigger() == BreakpointAction.Break;
            }

            return false;
        }

        internal override bool RemoveSelf(ScriptDebugger debugger) =>
            debugger.RemoveVariableBreakpoint(this);
    }

    /// <summary>
    /// A breakpoint on a line or statement.
    /// </summary>
    public class LineBreakpoint : Breakpoint
    {
        /// <summary>
        /// Creates a new instance of a <see cref="LineBreakpoint"/>
        /// </summary>
        public LineBreakpoint(string script, int line)
            : this(script, line, null)
        { }

        /// <summary>
        /// Creates a new instance of a <see cref="LineBreakpoint"/>
        /// </summary>
        public LineBreakpoint(string script, int line, ScriptBlock action)
            : base(script, action)
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(script), "Caller to verify script parameter is not null or empty.");
            Line = line;
            Column = 0;
            SequencePointIndex = -1;
        }

        /// <summary>
        /// Creates a new instance of a <see cref="LineBreakpoint"/>
        /// </summary>
        public LineBreakpoint(string script, int line, int column)
            : this(script, line, column, null)
        { }

        /// <summary>
        /// Creates a new instance of a <see cref="LineBreakpoint"/>
        /// </summary>
        public LineBreakpoint(string script, int line, int column, ScriptBlock action)
            : base(script, action)
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(script), "Caller to verify script parameter is not null or empty.");
            Line = line;
            Column = column;
            SequencePointIndex = -1;
        }

        /// <summary>
        /// Creates a new instance of a <see cref="LineBreakpoint"/>
        /// </summary>
        public LineBreakpoint(string script, int line, int column, int id)
            : this(script, line, column, null, id)
        { }

        /// <summary>
        /// Creates a new instance of a <see cref="LineBreakpoint"/>
        /// </summary>
        public LineBreakpoint(string script, int line, int column, ScriptBlock action, int id)
            : base(script, action, id)
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(script), "Caller to verify script parameter is not null or empty.");
            Line = line;
            Column = column;
            SequencePointIndex = -1;
        }

        /// <summary>
        /// Which column this breakpoint is on.
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// Which line this breakpoint is on.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Gets a string representation of this breakpoint.
        /// </summary>
        /// <returns>A string representation of this breakpoint.</returns>
        public override string ToString()
        {
            return Column == 0
                       ? StringUtil.Format(DebuggerStrings.LineBreakpointString, Script, Line)
                       : StringUtil.Format(DebuggerStrings.StatementBreakpointString, Script, Line, Column);
        }

        internal int SequencePointIndex { get; set; }

        internal IScriptExtent[] SequencePoints { get; set; }

        internal BitArray BreakpointBitArray { get; set; }

        private sealed class CheckBreakpointInScript : AstVisitor
        {
            public static bool IsInNestedScriptBlock(Ast ast, LineBreakpoint breakpoint)
            {
                var visitor = new CheckBreakpointInScript { _breakpoint = breakpoint };
                ast.InternalVisit(visitor);
                return visitor._result;
            }

            private LineBreakpoint _breakpoint;
            private bool _result;

            public override AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst)
            {
                if (functionDefinitionAst.Extent.ContainsLineAndColumn(_breakpoint.Line, _breakpoint.Column))
                {
                    _result = true;
                    return AstVisitAction.StopVisit;
                }

                // We don't need to visit the body, we're just checking extents of the topmost functions.
                // We'll visit the bodies eventually, but only when the nested function/script is executed.
                return AstVisitAction.SkipChildren;
            }

            public override AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
            {
                if (scriptBlockExpressionAst.Extent.ContainsLineAndColumn(_breakpoint.Line, _breakpoint.Column))
                {
                    _result = true;
                    return AstVisitAction.StopVisit;
                }

                // We don't need to visit the body, we're just checking extents of the topmost functions.
                // We'll visit the bodies eventually, but only when the nested function/script is executed.
                return AstVisitAction.SkipChildren;
            }
        }

        internal bool TrySetBreakpoint(string scriptFile, FunctionContext functionContext)
        {
            Diagnostics.Assert(SequencePointIndex == -1, "shouldn't be trying to set on a pending breakpoint");

            if (!scriptFile.Equals(this.Script, StringComparison.OrdinalIgnoreCase))
                return false;

            // A quick check to see if the breakpoint is within the scriptblock.
            bool couldBeInNestedScriptBlock;
            var scriptBlock = functionContext._scriptBlock;
            if (scriptBlock != null)
            {
                var ast = scriptBlock.Ast;
                if (!ast.Extent.ContainsLineAndColumn(Line, Column))
                    return false;

                var sequencePoints = functionContext._sequencePoints;
                if (sequencePoints.Length == 1 && sequencePoints[0] == scriptBlock.Ast.Extent)
                {
                    // If there was no real executable code in the function (e.g. only function definitions),
                    // we added the entire scriptblock as a sequence point, but it shouldn't be allowed as a breakpoint.
                    return false;
                }

                couldBeInNestedScriptBlock = CheckBreakpointInScript.IsInNestedScriptBlock(((IParameterMetadataProvider)ast).Body, this);
            }
            else
            {
                couldBeInNestedScriptBlock = false;
            }

            int sequencePointIndex;
            var sequencePoint = FindSequencePoint(functionContext, Line, Column, out sequencePointIndex);
            if (sequencePoint != null)
            {
                // If the bp could be in a nested script block, we want to be careful and get the bp in the correct script block.
                // If it's a simple line bp (no column specified), then the start line must match the bp line exactly, otherwise
                // we assume the bp is in the nested script block.
                if (!couldBeInNestedScriptBlock || (sequencePoint.StartLineNumber == Line && Column == 0))
                {
                    SetBreakpoint(functionContext, sequencePointIndex);
                    return true;
                }
            }

            // Before using heuristics, make sure the breakpoint isn't in a nested function/script block.
            if (couldBeInNestedScriptBlock)
            {
                return false;
            }

            // Not found.  First, we check if the line/column is before any real code.  If so, we'll
            // move the breakpoint to the first interesting sequence point (could be a dynamicparam,
            // begin, process, end, or clean block.)
            if (scriptBlock != null)
            {
                var ast = scriptBlock.Ast;
                var bodyAst = ((IParameterMetadataProvider)ast).Body;
                if ((bodyAst.DynamicParamBlock == null || bodyAst.DynamicParamBlock.Extent.IsAfter(Line, Column))
                    && (bodyAst.BeginBlock == null || bodyAst.BeginBlock.Extent.IsAfter(Line, Column))
                    && (bodyAst.ProcessBlock == null || bodyAst.ProcessBlock.Extent.IsAfter(Line, Column))
                    && (bodyAst.EndBlock == null || bodyAst.EndBlock.Extent.IsAfter(Line, Column))
                    && (bodyAst.CleanBlock == null || bodyAst.CleanBlock.Extent.IsAfter(Line, Column)))
                {
                    SetBreakpoint(functionContext, 0);
                    return true;
                }
            }

            // Still not found.  Try fudging a bit, but only if it's a simple line breakpoint.
            if (Column == 0 && FindSequencePoint(functionContext, Line + 1, 0, out sequencePointIndex) != null)
            {
                SetBreakpoint(functionContext, sequencePointIndex);
                return true;
            }

            return false;
        }

        private static IScriptExtent FindSequencePoint(FunctionContext functionContext, int line, int column, out int sequencePointIndex)
        {
            var sequencePoints = functionContext._sequencePoints;

            for (int i = 0; i < sequencePoints.Length; ++i)
            {
                var extent = sequencePoints[i];
                if (extent.ContainsLineAndColumn(line, column))
                {
                    sequencePointIndex = i;
                    return extent;
                }
            }

            sequencePointIndex = -1;
            return null;
        }

        private void SetBreakpoint(FunctionContext functionContext, int sequencePointIndex)
        {
            // Remember the bitarray so we when the last breakpoint is removed, we can avoid
            // stopping at the sequence point.
            this.BreakpointBitArray = functionContext._breakPoints;
            this.SequencePoints = functionContext._sequencePoints;

            SequencePointIndex = sequencePointIndex;
            this.BreakpointBitArray.Set(SequencePointIndex, true);
        }

        internal override bool RemoveSelf(ScriptDebugger debugger)
        {
            if (this.SequencePoints != null)
            {
                // Remove ourselves from the list of bound breakpoints in this script.  It's possible the breakpoint was never
                // bound, in which case there is nothing to do.
                var boundBreakPoints = debugger.GetBoundBreakpoints(this.SequencePoints);
                if (boundBreakPoints != null)
                {
                    Diagnostics.Assert(boundBreakPoints.Contains(this),
                                       "If we set _scriptBlock, we should have also added the breakpoint to the bound breakpoint list");
                    boundBreakPoints.Remove(this);

                    if (boundBreakPoints.All(breakpoint => breakpoint.SequencePointIndex != this.SequencePointIndex))
                    {
                        // No other line breakpoints are at the same sequence point, so disable the breakpoint so
                        // we don't go looking for breakpoints the next time we hit the sequence point.
                        // This isn't strictly necessary, but script execution will be faster.
                        this.BreakpointBitArray.Set(SequencePointIndex, false);
                    }
                }
            }

            return debugger.RemoveLineBreakpoint(this);
        }
    }
}
