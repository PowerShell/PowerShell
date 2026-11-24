// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation;

internal sealed partial class SessionStateScope
{
    private Stack<DottedScopedSettings> _dottedScopedSettings;

    /// <summary>
    /// Owns a dotted invocation's temporary bindings. The scope activates the whole frame together,
    /// so named parameters and common parameters can add bindings in any order.
    /// </summary>
    internal sealed class DottedScopedSettings(MutableTuple locals)
    {
        private readonly Dictionary<ScopedSetting, ScopedSettingVariable> _variables = new();

        internal MutableTuple Locals { get; } = locals;

        /// <summary>
        /// Reports whether a setting belongs to this frame, including an empty binding after reset.
        /// </summary>
        internal bool TryGetVariable(ScopedSetting setting, out ScopedSettingVariable variable)
            => _variables.TryGetValue(setting, out variable);

        internal void SetVariable(ScopedSetting setting, ScopedSettingVariable variable)
        {
            if (_variables.TryGetValue(setting, out var previous) && ReferenceEquals(previous, variable))
            {
                return;
            }

            variable?.Entry.AddBinding();
            previous?.Entry.ReleaseBinding(markVariableRemoved: true);
            _variables[setting] = variable;
        }

        /// <summary>
        /// Ends the invocation's ownership of its bindings, preserving captured variable values.
        /// </summary>
        internal void Release()
        {
            foreach (ScopedSettingVariable variable in _variables.Values)
            {
                variable?.Entry.ReleaseBinding(markVariableRemoved: false);
            }

            _variables.Clear();
        }
    }

    internal DottedScopedSettings ActivateDottedScopedSettings(MutableTuple locals)
    {
        var frame = new DottedScopedSettings(locals);
        ActivateDottedScopedSettings(frame);
        return frame;
    }

    /// <summary>
    /// Activates an existing frame with the bindings retained from the previous command stage.
    /// </summary>
    internal void ActivateDottedScopedSettings(DottedScopedSettings frame)
        => (_dottedScopedSettings ??= new()).Push(frame);

    /// <summary>
    /// Deactivates a frame in reverse activation order, retaining its bindings for the next stage.
    /// </summary>
    internal void DeactivateDottedScopedSettings(DottedScopedSettings frame)
    {
        DottedScopedSettings popped = _dottedScopedSettings.Pop();
        Diagnostics.Assert(ReferenceEquals(popped, frame), "Dotted scoped settings must be popped in order.");
    }

    /// <summary>
    /// Finds the innermost active dotted binding for a read, skipping empty bindings.
    /// </summary>
    private ScopedSettingVariable GetDottedScopedSettingVariable(ScopedSetting setting)
    {
        if (_dottedScopedSettings is not null)
        {
            foreach (DottedScopedSettings frame in _dottedScopedSettings)
            {
                if (frame.TryGetVariable(setting, out var variable) && variable is not null)
                {
                    return variable;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the innermost target for a write, including a dotted frame whose binding was reset.
    /// </summary>
    private ScopedSettingVariable GetScopedSettingVariableForWrite(
        ScopedSetting setting,
        out DottedScopedSettings target,
        DottedScopedSettings invocationFrame = null)
    {
        if (invocationFrame is not null)
        {
            // A new invocation binding belongs to its own frame, even if the caller has a binding.
            target = invocationFrame;
            target.TryGetVariable(setting, out var variable);
            return variable;
        }

        if (_dottedScopedSettings is not null)
        {
            foreach (DottedScopedSettings frame in _dottedScopedSettings)
            {
                if (frame.TryGetVariable(setting, out var variable))
                {
                    target = frame;
                    return variable;
                }
            }
        }

        target = null;
        return GetScopedSetting(setting)?.Variable;
    }

    private DottedScopedSettings GetDottedScopedSettings(MutableTuple locals)
    {
        if (_dottedScopedSettings is not null)
        {
            foreach (DottedScopedSettings frame in _dottedScopedSettings)
            {
                if (ReferenceEquals(frame.Locals, locals))
                {
                    return frame;
                }
            }
        }

        return null;
    }

    internal void AddScopedSettingVariables(Dictionary<string, PSVariable> result, bool includePrivate)
    {
        if (!ExperimentalFeature.PSScopedProperties)
        {
            return;
        }

        foreach (var setting in ScopedProperties.PreferenceSettings)
        {
            var variable = GetLocalScopedSettingVariable(setting);
            if (variable is not null && (includePrivate || !variable.IsPrivate))
            {
                result.TryAdd(setting.VariableName, variable);
            }
        }
    }

    /// <summary>
    /// Enumerates bindings owned by the specified debugger frame, without falling back to another frame.
    /// </summary>
    internal void AddScopedSettingVariablesForFrame(Dictionary<string, PSVariable> result, MutableTuple locals)
    {
        if (!ExperimentalFeature.PSScopedProperties)
        {
            return;
        }

        bool isScopeFrame = ReferenceEquals(LocalsTuple, locals);
        var dottedFrame = isScopeFrame ? null : GetDottedScopedSettings(locals);
        if (!isScopeFrame && dottedFrame is null)
        {
            return;
        }

        foreach (var setting in ScopedProperties.PreferenceSettings)
        {
            ScopedSettingVariable variable;
            if (isScopeFrame)
            {
                variable = GetScopedSetting(setting)?.Variable;
            }
            else
            {
                dottedFrame.TryGetVariable(setting, out variable);
            }

            if (variable is not null)
            {
                result.TryAdd(setting.VariableName, variable);
            }
        }
    }
}
