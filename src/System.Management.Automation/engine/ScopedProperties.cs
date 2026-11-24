// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation;

/// <summary>Provides typed access to scoped PowerShell engine settings.</summary>
public sealed class PSScopedPropertyFacade
{
    private readonly WeakReference<ExecutionContext> _owner;
    private readonly WeakReference<SessionStateScope> _scope;
    private readonly PSScopedActionPreference _actionPreference;
    private readonly PSScopedStyle _style;

    internal PSScopedPropertyFacade(ExecutionContext context = null, SessionStateScope scope = null)
    {
        if (scope is not null)
        {
            _owner = new(context);
            _scope = new(scope);
        }

        _actionPreference = new(this);
        _style = new(this);
    }

    /// <summary>Gets the action preference settings.</summary>
    public PSScopedActionPreference ActionPreference
    {
        get
        {
            Resolve();
            return _actionPreference;
        }
    }

    /// <summary>Gets the style settings.</summary>
    public PSScopedStyle Style
    {
        get
        {
            Resolve();
            return _style;
        }
    }

    /// <summary>Gets a view bound to the current local, script, or global scope.</summary>
    /// <param name="scopeName">The name of the scope to bind.</param>
    /// <returns>The settings view for the named scope.</returns>
    public PSScopedPropertyFacade AtScope(string scopeName)
    {
        var (context, _) = Resolve();
        var state = context.EngineSessionState;
        var scope = scopeName?.ToUpperInvariant() switch
        {
            "LOCAL" => state.CurrentScope,
            "SCRIPT" => state.ScriptScope,
            "GLOBAL" => state.GlobalScope,
            _ => throw new ArgumentException(StringUtil.Format(SessionStateStrings.InvalidScopedSettingScope, scopeName), nameof(scopeName)),
        };

        return new PSScopedPropertyFacade(context, scope);
    }

    /// <summary>Removes an override from the local or bound scope.</summary>
    /// <param name="path">The path of the setting to reset.</param>
    public void Reset(string path)
    {
        var (context, scope) = Resolve();
        if (string.Equals(path, ScopedProperties.ErrorPath, StringComparison.OrdinalIgnoreCase))
        {
            ScopedProperties.ResetErrorActionPreference(context, scope);
        }
        else if (string.Equals(path, ScopedProperties.ProgressViewSetting.Path, StringComparison.OrdinalIgnoreCase))
        {
            ScopedProperties.ProgressViewSetting.Reset(scope);
        }
        else
        {
            throw new ArgumentException(StringUtil.Format(SessionStateStrings.UnknownScopedSetting, path), nameof(path));
        }
    }

    internal (ExecutionContext Context, SessionStateScope Scope) Resolve()
    {
        ExecutionContext context = LocalPipeline.GetExecutionContextFromTLS();
        if (context?.EngineSessionState is null)
        {
            throw new InvalidOperationException(SessionStateStrings.NoScopedSettingExecutionContext);
        }

        if (_scope is null)
        {
            return (context, context.EngineSessionState.CurrentScope);
        }

        if (!_owner.TryGetTarget(out var owner) || owner != context)
        {
            throw new InvalidOperationException(SessionStateStrings.ForeignScopedSettingScope);
        }

        if (!_scope.TryGetTarget(out var scope) || scope.IsRemoved)
        {
            throw new InvalidOperationException(SessionStateStrings.ExpiredScopedSettingScope);
        }

        return (context, scope);
    }
}

/// <summary>Provides scoped action preference settings.</summary>
public sealed class PSScopedActionPreference
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedActionPreference(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets the error action preference.</summary>
    public ActionPreference Error
    {
        get
        {
            var (context, scope) = _facade.Resolve();
            return ScopedProperties.GetErrorActionPreference(context, scope);
        }

        set
        {
            var (context, scope) = _facade.Resolve();
            ScopedProperties.SetErrorActionPreference(context, scope, value);
        }
    }
}

/// <summary>Provides scoped style settings.</summary>
public sealed class PSScopedStyle
{
    private readonly PSScopedPropertyFacade _facade;
    private readonly PSScopedProgressStyle _progress;

    internal PSScopedStyle(PSScopedPropertyFacade facade)
    {
        _facade = facade;
        _progress = new(facade);
    }

    /// <summary>Gets the progress style settings.</summary>
    public PSScopedProgressStyle Progress
    {
        get
        {
            _facade.Resolve();
            return _progress;
        }
    }
}

/// <summary>Provides scoped progress rendering settings.</summary>
public sealed class PSScopedProgressStyle
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedProgressStyle(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets the progress view.</summary>
    public ProgressView View
    {
        get
        {
            var (_, scope) = _facade.Resolve();
            return ScopedProperties.ProgressViewSetting.GetValue(scope);
        }

        set
        {
            var (_, scope) = _facade.Resolve();
            ScopedProperties.ProgressViewSetting.SetValue(scope, value);
        }
    }
}

/// <summary>
/// Identifies a setting independently of the entries holding its values in individual scopes.
/// </summary>
internal abstract class ScopedSetting(string path)
{
    internal string Path { get; } = path;
}

internal sealed class ScopedSetting<T>(string path, T defaultValue, string variableName = null, Action<T> validate = null) : ScopedSetting(path)
{
    internal T DefaultValue { get; } = defaultValue;

    internal string VariableName { get; } = variableName;

    /// <summary>
    /// Resolves the nearest scope entry, excluding private ancestor variables, or returns the setting default.
    /// Temporary dotted-command bindings are resolved separately by the preference-specific reader.
    /// </summary>
    internal T GetValue(SessionStateScope scope)
    {
        SessionStateScope target = scope;
        for (; scope is not null; scope = scope.Parent)
        {
            if (scope.GetScopedSetting(this) is ScopedSettingEntry<T> entry
                && (scope == target || entry.Variable?.IsPrivate != true))
            {
                return entry.Variable is { } variable && variable.Value is T value ? value : entry.Value;
            }
        }

        return DefaultValue;
    }

    internal void SetValue(SessionStateScope scope, object value)
    {
        if (scope.GetScopedSetting(this) is ScopedSettingEntry<T> entry)
        {
            entry.SetValue(value);
        }
        else
        {
            scope.SetScopedSetting(new ScopedSettingEntry<T>(this, value));
        }
    }

    /// <summary>
    /// Converts and validates a candidate value without changing any scope binding.
    /// </summary>
    internal T ConvertValue(object value)
    {
        T converted = LanguagePrimitives.ConvertTo<T>(value);
        validate?.Invoke(converted);
        return converted;
    }

    /// <summary>
    /// Removes the target scope's override so subsequent reads inherit a value or use the setting default.
    /// </summary>
    internal void Reset(SessionStateScope scope)
    {
        if (scope.GetScopedSetting(this) is not null)
        {
            scope.RemoveScopedSetting(this);
        }
    }
}

/// <summary>
/// Holds a setting's canonical value and optional variable adapter, shared by one or more scope bindings.
/// </summary>
internal abstract class ScopedSettingEntry
{
    private int _bindings;

    internal abstract ScopedSetting Setting { get; }

    internal ScopedSettingVariable Variable { get; init; }

    internal abstract object GetValueRaw();

    internal abstract object ConvertValue(object value);

    /// <summary>
    /// Stores a value already converted and validated by the caller.
    /// </summary>
    internal abstract void SetPreparedValue(object value);

    internal void AddBinding() => _bindings++;

    /// <summary>
    /// Releases one scope or dotted-frame binding, detaching the adapter only on explicit removal of the last binding.
    /// Scope exit passes false to preserve the value of captured variable objects.
    /// </summary>
    internal void ReleaseBinding(bool markVariableRemoved)
    {
        Diagnostics.Assert(_bindings > 0, "A scoped setting entry must be bound before release.");
        if (--_bindings == 0 && markVariableRemoved)
        {
            Variable?.Detach();
        }
    }
}

internal sealed class ScopedSettingEntry<T> : ScopedSettingEntry
{
    private readonly ScopedSetting<T> _setting;

    private ScopedSettingEntry(ScopedSetting<T> setting, object value, string variableName)
    {
        _setting = setting;
        Value = setting.ConvertValue(value);
        if (variableName is not null)
        {
            Variable = new ScopedSettingVariable(variableName, this);
        }
    }

    internal ScopedSettingEntry(ScopedSetting<T> setting, object value) : this(setting, value, setting.VariableName)
    {
    }

    /// <summary>
    /// Prepares an independent entry and adapter from a supplied variable, validating its converted value
    /// against the source attributes before copying metadata. The source variable is not attached to the entry.
    /// </summary>
    internal static ScopedSettingEntry<T> Create(ScopedSetting<T> setting, PSVariable source)
    {
        var entry = new ScopedSettingEntry<T>(setting, source.Value, source.Name);
        if (!PSVariable.IsValidValue(source.Attributes, entry.Value))
        {
            entry.Variable.ThrowInvalidValue(entry.Value);
        }

        entry.Variable.CopyMetadataFrom(source, initializing: true);
        return entry;
    }

    internal override ScopedSetting Setting => _setting;

    internal T Value { get; private set; }

    internal void SetValue(object value)
    {
        if (Variable is { } variable)
        {
            variable.Value = value;
        }
        else
        {
            Value = _setting.ConvertValue(value);
        }
    }

    internal override object GetValueRaw() => Value;

    internal override object ConvertValue(object value) => _setting.ConvertValue(value);

    internal override void SetPreparedValue(object value) => Value = (T)value;
}

internal static class ScopedProperties
{
    internal const string ErrorPath = "ActionPreference.Error";
    private const string ProgressViewPath = "Style.Progress.View";

    internal static bool IsLegacySetting(string name)
        => ExperimentalFeature.PSScopedProperties && name.Equals(SpecialVariables.ErrorActionPreference, StringComparison.OrdinalIgnoreCase);

    internal static readonly ScopedSetting<ProgressView> ProgressViewSetting = new(
        ProgressViewPath,
        ProgressView.Minimal);

    internal static readonly ScopedSetting<ActionPreference> ErrorPreferenceSetting = new(
        ErrorPath,
        InitialSessionState.DefaultErrorActionPreference,
        SpecialVariables.ErrorActionPreference,
        static value =>
        {
            if (value == ActionPreference.Suspend)
            {
                throw new PSInvalidCastException("InvalidActionPreference", null, ErrorPackage.ActionPreferenceReservedForFutureUseError, value);
            }
        });

    internal static ActionPreference GetErrorActionPreference(ExecutionContext context, SessionStateScope scope)
        => ExperimentalFeature.PSScopedProperties
            ? GetErrorActionPreference(context, scope, out _)
            : context.GetEnumPreference(SpecialVariables.ErrorActionPreferenceVarPath, InitialSessionState.DefaultErrorActionPreference, out _, scope);

    /// <summary>
    /// Resolves the scoped error preference, including active dotted bindings and private-variable inheritance rules.
    /// Callers must ensure scoped properties are enabled; defaultUsed indicates that no binding was found.
    /// </summary>
    internal static ActionPreference GetErrorActionPreference(ExecutionContext context, SessionStateScope scope, out bool defaultUsed)
    {
        SessionStateScope target = scope;
        for (; scope is not null; scope = scope.Parent)
        {
            var variable = scope.GetScopedSettingVariable(SpecialVariables.ErrorActionPreference, context.EngineSessionState.CurrentScope.ScopeOrigin);
            if (variable is not null && (scope == target || !variable.IsPrivate))
            {
                defaultUsed = false;
                return (ActionPreference)variable.Value;
            }
        }

        defaultUsed = true;
        return ErrorPreferenceSetting.DefaultValue;
    }

    internal static void SetErrorActionPreference(ExecutionContext context, SessionStateScope scope, ActionPreference value)
    {
        var state = context.EngineSessionState;
        scope.SetVariable(SpecialVariables.ErrorActionPreference, value, asValue: true, force: false, state, state.CurrentScope.ScopeOrigin);
    }

    internal static void ResetErrorActionPreference(ExecutionContext context, SessionStateScope scope)
    {
        if (scope.GetVariable(SpecialVariables.ErrorActionPreference, context.EngineSessionState.CurrentScope.ScopeOrigin) is not null)
        {
            scope.RemoveVariable(SpecialVariables.ErrorActionPreference, force: false);
        }
    }

    internal static ProgressView GetProgressView(ExecutionContext context)
        => ProgressViewSetting.GetValue(context.EngineSessionState.CurrentScope);

    internal static void SetProgressView(ExecutionContext context, ProgressView value)
        => ProgressViewSetting.SetValue(context.EngineSessionState.CurrentScope, value);
}
