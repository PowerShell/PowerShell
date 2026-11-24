// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation;

internal sealed partial class SessionStateScope
{
    private Stack<DottedScopedVariable> _dottedScopedVariables;

    // A dotted command shares the caller's scope. Its implicit preference is
    // active only while the command runs, just like its automatic variables.
    internal sealed class DottedScopedVariable(string name, MutableTuple locals)
    {
        private ScopedSettingVariable _variable;

        internal string Name { get; } = name;

        internal MutableTuple Locals { get; } = locals;

        internal ScopedSettingEntry Entry => _variable?.Entry;

        internal ScopedSettingVariable Variable
        {
            get => _variable;
            set
            {
                if (ReferenceEquals(_variable, value))
                {
                    return;
                }

                value?.Entry.AddBinding();
                Entry?.ReleaseBinding(markVariableRemoved: true);
                _variable = value;
            }
        }

        /// <summary>
        /// Ends the command's ownership of its binding while preserving the value of captured variable objects.
        /// </summary>
        internal void Release()
        {
            // A captured variable keeps its value when the dotted command exits.
            Entry?.ReleaseBinding(markVariableRemoved: false);
            _variable = null;
        }
    }

    /// <summary>
    /// Creates and activates a temporary binding frame for a dotted command.
    /// The locals tuple identifies the debugger frame; the preference value is stored separately.
    /// </summary>
    internal DottedScopedVariable PushDottedScopedVariable(string name, MutableTuple locals)
    {
        var frame = new DottedScopedVariable(name, locals);
        PushDottedScopedVariable(frame);
        return frame;
    }

    /// <summary>
    /// Activates an existing command frame for another invocation stage, retaining its current binding.
    /// </summary>
    internal void PushDottedScopedVariable(DottedScopedVariable frame)
        => (_dottedScopedVariables ??= new()).Push(frame);

    /// <summary>
    /// Deactivates the current command frame without releasing its binding, allowing later stages to reuse it.
    /// </summary>
    internal void PopDottedScopedVariable(DottedScopedVariable frame)
    {
        DottedScopedVariable popped = _dottedScopedVariables.Pop();
        Diagnostics.Assert(ReferenceEquals(popped, frame), "Dotted scoped variables must be popped in order.");
    }

    /// <summary>
    /// Finds the innermost active dotted binding for a read, skipping frames whose binding was reset.
    /// </summary>
    internal bool TryGetDottedScopedVariable(string name, out PSVariable variable)
    {
        if (_dottedScopedVariables is not null)
        {
            foreach (DottedScopedVariable frame in _dottedScopedVariables)
            {
                if (frame.Variable is { } candidate && string.Equals(frame.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    variable = candidate;
                    return true;
                }
            }
        }

        variable = null;
        return false;
    }

    /// <summary>
    /// Finds the innermost active dotted frame for a write or removal, even when its binding was reset.
    /// </summary>
    internal bool TryGetDottedScopedVariableTarget(string name, out DottedScopedVariable target)
    {
        if (_dottedScopedVariables is not null)
        {
            foreach (DottedScopedVariable frame in _dottedScopedVariables)
            {
                if (string.Equals(frame.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    target = frame;
                    return true;
                }
            }
        }

        target = null;
        return false;
    }

    /// <summary>
    /// Gets the binding belonging to a specific debugger frame without falling back to another frame's binding.
    /// </summary>
    internal ScopedSettingVariable GetDottedScopedVariableForFrame(string name, MutableTuple locals)
    {
        if (_dottedScopedVariables is not null)
        {
            foreach (DottedScopedVariable frame in _dottedScopedVariables)
            {
                if (ReferenceEquals(frame.Locals, locals) && string.Equals(frame.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return frame.Variable;
                }
            }
        }

        return null;
    }
}
