// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation;

internal sealed partial class SessionStateScope
{
    internal void InitializeErrorPreference(ActionPreference value, SessionStateInternal sessionState)
        => SetScopedSettingVariable(SpecialVariables.ErrorActionPreference, value, asValue: true, force: false,
            sessionState, CommandOrigin.Internal, replace: true, bypassProtection: true);

    internal void InitializeErrorPreferenceParameter(PSVariable variable, SessionStateInternal sessionState)
        => SetScopedSettingVariable(SpecialVariables.ErrorActionPreference, variable, asValue: false, force: false,
            sessionState, CommandOrigin.Internal, replace: true, bypassProtection: true);

    internal ScopedSettingVariable GetScopedSettingVariableForWrite(string name)
        => TryGetDottedScopedVariableTarget(name, out var frame)
            ? (ScopedSettingVariable)frame.Variable
            : GetScopedSetting(ScopedProperties.ErrorPreferenceSetting)?.Variable;

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
