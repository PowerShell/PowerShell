// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation;

internal sealed partial class SessionStateScope
{
    /// <summary>
    /// Resolves a value from the nearest visible binding, including active dotted bindings.
    /// Private variables in ancestor scopes are skipped. The descriptor default applies when no binding is found.
    /// </summary>
    internal T GetScopedSettingValue<T>(ScopedSetting<T> setting, out bool defaultUsed, CommandOrigin origin = CommandOrigin.Internal)
        => (T)GetScopedSettingValue((ScopedSetting)setting, out defaultUsed, origin);

    internal object GetScopedSettingValue(ScopedSetting setting, out bool defaultUsed, CommandOrigin origin = CommandOrigin.Internal)
    {
        for (SessionStateScope scope = this; scope is not null; scope = scope.Parent)
        {
            if (setting.VariableName is null)
            {
                if (scope.GetScopedSetting(setting) is { } entry)
                {
                    defaultUsed = false;
                    return entry.GetValueRaw();
                }
            }
            else if (scope.GetScopedSettingVariable(setting, origin) is { } variable && (scope == this || !variable.IsPrivate))
            {
                defaultUsed = false;
                return variable.Value;
            }
        }

        defaultUsed = true;
        return setting.GetDefaultExposedValue();
    }

    internal T GetScopedSettingValue<T>(ScopedSetting<T> setting) => GetScopedSettingValue(setting, out _);

    /// <summary>Resolves the raw mutable backing value without exposing its scope-aware view.</summary>
    internal T GetScopedMutableSettingValue<T>(ScopedMutableSetting<T> setting, out bool defaultUsed, CommandOrigin origin = CommandOrigin.Internal)
        where T : class
    {
        for (SessionStateScope scope = this; scope is not null; scope = scope.Parent)
        {
            if (scope.GetScopedSettingVariable(setting, origin) is { } variable
                && (scope == this || !variable.IsPrivate))
            {
                defaultUsed = false;
                return (T)(variable is ScopedSettingVariable scopedVariable
                    ? scopedVariable.GetValueBackingRaw()
                    : variable.Value);
            }
        }

        defaultUsed = true;
        return (T)setting.GetDefaultValue();
    }

    /// <summary>Creates an engine-owned view anchored to this caller scope.</summary>
    internal T GetScopedMutableSettingView<T>(ScopedMutableSetting<T> setting, ExecutionContext context, CommandOrigin origin = CommandOrigin.Internal)
        where T : class
    {
        T raw = GetScopedMutableSettingValue(setting, out _, origin);
        return raw is null ? null : setting.CreateView(new ScopedMutableBinding<T>(setting, context, this), raw);
    }

    /// <summary>Publishes a candidate created by a successful scope-aware mutation.</summary>
    internal void PublishScopedMutableSetting<T>(ScopedMutableSetting<T> setting, T value, SessionStateInternal sessionState, CommandOrigin origin)
        where T : class
    {
        ScopedSettingVariable existing = GetScopedSettingVariableForWrite(setting, out var frame);
        if (existing is not null && existing.Entry is ScopedMutableSettingEntry<T> localEntry)
        {
            SessionState.ThrowIfNotVisible(origin, existing);
            // Object member mutations are permitted even when the variable binding is read-only or constant.
            localEntry.SetPreparedValue(value);
            return;
        }

        var entry = setting.CreateMutableEntry(value);
        entry.Variable.SessionState = sessionState;

        // A copy-on-write child retains the inherited variable's metadata and attributes.
        for (SessionStateScope parent = this; parent is not null; parent = parent.Parent)
        {
            if (parent.GetScopedSettingVariable(setting, origin) is { } parentVariable
                && (parent == this || !parentVariable.IsPrivate))
            {
                entry.Variable.CopyMetadataFrom(parentVariable, initializing: true);
                break;
            }
        }

        if (ExecutionContext.HasEverUsedConstrainedLanguage)
        {
            CheckVariableChangeInConstrainedLanguage(entry.Variable);
        }

        if (frame is not null)
        {
            frame.SetVariable(setting, entry.Variable);
        }
        else
        {
            SetScopedSetting(entry);
        }
    }

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
    internal void InitializeScopedSetting<T>(ScopedSetting<T> setting, T value, SessionStateInternal sessionState, DottedScopedSettings invocationFrame = null)
        => SetScopedSettingVariable(setting, value, asValue: true, force: false,
            sessionState, CommandOrigin.Internal, replace: true, bypassProtection: true, invocationFrame);

    /// <summary>
    /// Installs a bound parameter and its metadata as an invocation-owned setting.
    /// Inherited variable protections do not prevent parameter initialization.
    /// </summary>
    internal void InitializeScopedSettingParameter(ScopedSetting setting, PSVariable variable, SessionStateInternal sessionState, DottedScopedSettings invocationFrame = null)
        => SetScopedSettingVariable(setting, variable, asValue: false, force: false,
            sessionState, CommandOrigin.Internal, replace: true, bypassProtection: true, invocationFrame);

    /// <summary>
    /// Gets the binding targeted by a write in this scope. An empty active dotted frame returns null
    /// so the caller creates a temporary override instead of updating the underlying scope binding.
    /// </summary>
    internal ScopedSettingVariable GetScopedSettingVariableForWrite(ScopedSetting setting)
        => GetScopedSettingVariableForWrite(setting, out _);

    private ScopedSettingVariable GetLocalScopedSettingVariable(ScopedSetting setting)
        => GetDottedScopedSettingVariable(setting) ?? GetScopedSetting(setting)?.Variable;

    /// <summary>
    /// Gets the readable binding in this scope, checking visibility and falling through empty dotted frames.
    /// Parent scopes are not searched.
    /// </summary>
    private PSVariable GetScopedSettingVariable(ScopedSetting setting, CommandOrigin origin)
    {
        var variable = GetLocalScopedSettingVariable(setting);
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
        bool bypassProtection = false,
        DottedScopedSettings invocationFrame = null)
    {
        Diagnostics.Assert(setting.VariableName is not null, "A variable binding requires a legacy variable name.");
        var existing = GetScopedSettingVariableForWrite(setting, out var frame, invocationFrame);
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

    private PSVariable BindScopedSettingVariable(ScopedSettingEntry entry, DottedScopedSettings frame)
    {
        if (frame is not null)
        {
            frame.SetVariable(entry.Setting, entry.Variable);
        }
        else
        {
            SetScopedSetting(entry);
        }

        return entry.Variable;
    }

    internal void RemoveScopedSettingVariable(ScopedSetting setting, bool force)
    {
        var variable = GetScopedSettingVariableForWrite(setting, out var frame);
        if (frame is not null)
        {
            variable?.ThrowIfNotRemovable(force);
            frame.SetVariable(setting, null);
            return;
        }

        RemoveScopedSetting(setting, force);
    }
}
