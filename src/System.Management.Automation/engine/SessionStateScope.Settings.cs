// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation;

internal sealed partial class SessionStateScope
{
    /// <summary>
    /// Resolves a value from the nearest visible binding, including active dotted bindings.
    /// A private variable is inherited only when its binding is in the starting scope.
    /// </summary>
    internal T GetScopedSettingValue<T>(ScopedSetting<T> setting, out bool defaultUsed, CommandOrigin origin = CommandOrigin.Internal)
    {
        for (SessionStateScope scope = this; scope is not null; scope = scope.Parent)
        {
            if (setting.VariableName is null)
            {
                if (scope.GetScopedSetting(setting) is { } entry)
                {
                    defaultUsed = false;
                    return entry.Value;
                }
            }
            else if (scope.GetScopedSettingVariable(setting, origin) is { } variable && (scope == this || !variable.IsPrivate))
            {
                defaultUsed = false;
                return (T)variable.Value;
            }
        }

        defaultUsed = true;
        return setting.DefaultValue;
    }

    internal T GetScopedSettingValue<T>(ScopedSetting<T> setting) => GetScopedSettingValue(setting, out _);

    internal void SetScopedSettingValue(ScopedSetting setting, object value, SessionStateInternal sessionState = null, CommandOrigin origin = CommandOrigin.Internal)
    {
        if (setting.VariableName is not null)
        {
            SetScopedSettingVariable(setting, value, asValue: true, force: false, sessionState, origin);
        }
        else if (GetScopedSetting(setting) is { } entry)
        {
            entry.SetPreparedValue(entry.ConvertValue(value));
        }
        else
        {
            SetScopedSetting(setting.CreateEntry(value));
        }
    }

    internal void ResetScopedSetting(ScopedSetting setting, CommandOrigin origin = CommandOrigin.Internal)
    {
        if (setting.VariableName is null)
        {
            RemoveScopedSetting(setting);
        }
        else if (GetScopedSettingVariable(setting, origin) is not null)
        {
            RemoveScopedSettingVariable(setting, force: false);
        }
    }

    /// <summary>
    /// Installs an invocation-owned setting, replacing parameter metadata and shadowing inherited protections.
    /// For dotted commands, the preference belongs to the active temporary frame.
    /// </summary>
    internal void InitializeScopedSetting<T>(ScopedSetting<T> setting, T value, SessionStateInternal sessionState)
        => SetScopedSettingVariable(setting, value, asValue: true, force: false,
            sessionState, CommandOrigin.Internal, replace: true, bypassProtection: true);

    /// <summary>
    /// Installs a bound parameter and its metadata as an invocation-owned setting.
    /// Inherited variable protections do not prevent parameter initialization.
    /// </summary>
    internal void InitializeScopedSettingParameter(ScopedSetting setting, PSVariable variable, SessionStateInternal sessionState)
        => SetScopedSettingVariable(setting, variable, asValue: false, force: false,
            sessionState, CommandOrigin.Internal, replace: true, bypassProtection: true);

    /// <summary>
    /// Gets the binding targeted by a write in this scope. An empty active dotted frame returns null
    /// so the caller creates a temporary override instead of updating the underlying scope binding.
    /// </summary>
    internal ScopedSettingVariable GetScopedSettingVariableForWrite(ScopedSetting setting)
        => GetScopedSettingVariableForWrite(setting, out _);

    private ScopedSettingVariable GetScopedSettingVariableForWrite(ScopedSetting setting, out DottedScopedVariable frame)
        => TryGetDottedScopedVariableTarget(setting.VariableName, out frame)
            ? frame.Variable
            : GetScopedSetting(setting)?.Variable;

    /// <summary>
    /// Gets the readable binding in this scope, checking visibility and falling through empty dotted frames.
    /// Parent scopes are not searched.
    /// </summary>
    private PSVariable GetScopedSettingVariable(ScopedSetting setting, CommandOrigin origin)
    {
        PSVariable variable;
        if (!TryGetDottedScopedVariable(setting.VariableName, out variable))
        {
            variable = GetScopedSetting(setting)?.Variable;
        }

        if (variable is not null)
        {
            SessionState.ThrowIfNotVisible(origin, variable);
        }

        return variable;
    }

    private PSVariable SetScopedSettingVariable(
        ScopedSetting setting,
        object value,
        bool asValue,
        bool force,
        SessionStateInternal sessionState,
        CommandOrigin origin,
        bool replace = false,
        bool bypassProtection = false)
    {
        Diagnostics.Assert(setting.VariableName is not null, "A variable binding requires a legacy variable name.");
        var existing = GetScopedSettingVariableForWrite(setting, out var frame);
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
            ? setting.CreateEntry(value)
            : setting.CreateEntryFromVariable(source);
        entry.Variable.SessionState = sessionState;
        if (ExecutionContext.HasEverUsedConstrainedLanguage)
        {
            CheckVariableChangeInConstrainedLanguage(entry.Variable);
        }

        return BindScopedSettingVariable(entry, frame);
    }

    private PSVariable BindScopedSettingVariable(ScopedSettingEntry entry, DottedScopedVariable frame)
    {
        if (frame is not null)
        {
            frame.Variable = entry.Variable;
        }
        else
        {
            SetScopedSetting(entry);
        }

        return entry.Variable;
    }

    internal void RemoveScopedSettingVariable(ScopedSetting setting, bool force)
    {
        if (TryGetDottedScopedVariableTarget(setting.VariableName, out var frame))
        {
            frame.Variable?.ThrowIfNotRemovable(force);
            frame.Variable = null;
            return;
        }

        RemoveScopedSetting(setting, force);
    }
}
