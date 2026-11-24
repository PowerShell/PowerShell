// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation;

internal sealed partial class SessionStateScope
{
    /// <summary>
    /// Installs an invocation-owned error preference, replacing parameter metadata and shadowing inherited protections.
    /// For dotted commands, the preference belongs to the active temporary frame.
    /// </summary>
    internal void InitializeErrorPreference(ActionPreference value, SessionStateInternal sessionState)
        => SetScopedSettingVariable(SpecialVariables.ErrorActionPreference, value, asValue: true, force: false,
            sessionState, CommandOrigin.Internal, replace: true, bypassProtection: true);

    /// <summary>
    /// Installs a bound ErrorActionPreference parameter and its metadata as an invocation-owned setting.
    /// Inherited variable protections do not prevent parameter initialization.
    /// </summary>
    internal void InitializeErrorPreferenceParameter(PSVariable variable, SessionStateInternal sessionState)
        => SetScopedSettingVariable(SpecialVariables.ErrorActionPreference, variable, asValue: false, force: false,
            sessionState, CommandOrigin.Internal, replace: true, bypassProtection: true);

    /// <summary>
    /// Gets the binding targeted by a write in this scope. An empty active dotted frame returns null
    /// so the caller creates a temporary override instead of updating the underlying scope binding.
    /// </summary>
    internal ScopedSettingVariable GetScopedSettingVariableForWrite(string name)
        => TryGetDottedScopedVariableTarget(name, out var frame)
            ? (ScopedSettingVariable)frame.Variable
            : GetScopedSetting(ScopedProperties.ErrorPreferenceSetting)?.Variable;

    /// <summary>
    /// Gets the readable binding in this scope, checking visibility and falling through empty dotted frames.
    /// Parent scopes are not searched.
    /// </summary>
    internal PSVariable GetScopedSettingVariable(string name, CommandOrigin origin)
    {
        PSVariable variable;
        if (!TryGetDottedScopedVariable(name, out variable))
        {
            variable = GetScopedSetting(ScopedProperties.ErrorPreferenceSetting)?.Variable;
        }

        if (variable is not null)
        {
            SessionState.ThrowIfNotVisible(origin, variable);
        }

        return variable;
    }

    private PSVariable SetScopedSettingVariable(
        string name,
        object value,
        bool asValue,
        bool force,
        SessionStateInternal sessionState,
        CommandOrigin origin,
        bool replace = false,
        bool bypassProtection = false)
    {
        var setting = ScopedProperties.ErrorPreferenceSetting;
        bool dotted = TryGetDottedScopedVariableTarget(name, out var frame);
        var existing = dotted ? (ScopedSettingVariable)frame.Variable : GetScopedSetting(setting)?.Variable;
        if (existing is not null && !bypassProtection)
        {
            SessionState.ThrowIfNotVisible(origin, existing);
            existing.ThrowIfNotWritable(force);
        }

        PSVariable source = asValue ? null : (PSVariable)value;
        if (ExecutionContext.HasEverUsedConstrainedLanguage && source is not null)
        {
            CheckVariableChangeInConstrainedLanguage(source);
        }

        if (existing is not null && !replace && !(existing.IsReadOnly && force && !asValue))
        {
            if (asValue)
            {
                existing.SetValue(value, force);
            }
            else
            {
                existing.Replace(source, force);
            }

            return existing;
        }

        if (existing is not null && ReferenceEquals(existing, source))
        {
            return existing;
        }

        // Prepare the entire value and its metadata before replacing a live binding.
        var entry = asValue
            ? new ScopedSettingEntry<ActionPreference>(setting, value)
            : ScopedSettingEntry<ActionPreference>.Create(setting, source);
        entry.Variable.SessionState = sessionState;
        if (ExecutionContext.HasEverUsedConstrainedLanguage)
        {
            CheckVariableChangeInConstrainedLanguage(entry.Variable);
        }

        return BindScopedSettingVariable(name, entry, frame);
    }

    private PSVariable BindScopedSettingVariable(string name, ScopedSettingEntry entry, DottedScopedVariable frame)
    {
        if (frame is not null)
        {
            frame.Variable = entry.Variable;
        }
        else
        {
            GetPrivateVariables();
            SetScopedSetting(entry);
            _variables[name] = entry.Variable;
        }

        return entry.Variable;
    }

    private void RemoveScopedSettingVariable(string name, bool force)
    {
        if (TryGetDottedScopedVariableTarget(name, out var frame))
        {
            frame.Variable?.ThrowIfNotRemovable(force);
            frame.Variable = null;
            return;
        }

        // Initialize lazy AllScope bindings before removing the target binding.
        GetScopedSetting(ScopedProperties.ErrorPreferenceSetting);
        RemoveScopedSetting(ScopedProperties.ErrorPreferenceSetting, force);
        _variables?.Remove(name);
    }
}
