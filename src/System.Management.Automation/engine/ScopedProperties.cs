// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation;

/// <summary>Marks a scoped property facade or group whose getter errors must propagate through dynamic binding.</summary>
internal interface IScopedPropertyContainer
{
}

/// <summary>Provides typed access to scoped PowerShell engine settings.</summary>
public sealed partial class PSScopedPropertyFacade : IScopedPropertyContainer
{
    private readonly WeakReference<ExecutionContext> _owner;
    private readonly WeakReference<SessionStateScope> _scope;
    private readonly PSScopedActionPreference _actionPreference;
    private readonly PSScopedShouldProcess _shouldProcess;
    private readonly PSStyle _style;

    internal PSScopedPropertyFacade(ExecutionContext context = null, SessionStateScope scope = null)
    {
        if (scope is not null)
        {
            _owner = new(context);
            _scope = new(scope);
        }

        _actionPreference = new(this);
        _shouldProcess = new(this);
        _style = PSStyle.CreateScoped(this);
        _formatting = new(this);
        _nativeCommand = new(this);
        _module = new(this);
        _history = new(this);
        _transcription = new(this);
        _mail = new(this);
        _help = new(this);
        _logging = new(this);
        _remoting = new(this);
        _invocation = new(this);
        _diagnostics = new(this);
        _runtime = new(this);
        _session = new(this);
        _event = new(this);
        _constants = new(this);
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

    /// <summary>Gets the ShouldProcess settings.</summary>
    public PSScopedShouldProcess ShouldProcess
    {
        get
        {
            Resolve();
            return _shouldProcess;
        }
    }

    /// <summary>Gets the style settings.</summary>
    public PSStyle Style
    {
        get
        {
            Resolve();
            return _style;
        }
    }

    internal PSStyle GetStyleForRunspaceInitialization() => _style;

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
        if (!ScopedProperties.TryGetSetting(path, out var setting))
        {
            throw new ArgumentException(StringUtil.Format(SessionStateStrings.UnknownScopedSetting, path), nameof(path));
        }

        CommandOrigin origin = context.EngineSessionState.CurrentScope.ScopeOrigin;
        if (setting.VariableName is { } name && !ExperimentalFeature.PSScopedProperties)
        {
            if (scope.GetVariable(name, origin) is not null)
            {
                scope.RemoveVariable(name, force: false);
            }
        }
        else
        {
            scope.ResetScopedSetting(setting, origin);
        }
    }

    /// <summary>Returns a description of the scoped settings facade.</summary>
    /// <returns>The facade description.</returns>
    public override string ToString() => SessionStateStrings.ScopedPropertyFacadeDescription;

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
public sealed class PSScopedActionPreference : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedActionPreference(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets the error action preference.</summary>
    public ActionPreference Error
    {
        get => GetPreference(ScopedProperties.ErrorPreferenceSetting);
        set => SetPreference(ScopedProperties.ErrorPreferenceSetting, value);
    }

    /// <summary>Gets or sets the warning action preference.</summary>
    public ActionPreference Warning
    {
        get => GetPreference(ScopedProperties.WarningPreferenceSetting);
        set => SetPreference(ScopedProperties.WarningPreferenceSetting, value);
    }

    /// <summary>Gets or sets the information action preference.</summary>
    public ActionPreference Information
    {
        get => GetPreference(ScopedProperties.InformationPreferenceSetting);
        set => SetPreference(ScopedProperties.InformationPreferenceSetting, value);
    }

    /// <summary>Gets or sets the verbose action preference.</summary>
    public ActionPreference Verbose
    {
        get => GetPreference(ScopedProperties.VerbosePreferenceSetting);
        set => SetPreference(ScopedProperties.VerbosePreferenceSetting, value);
    }

    /// <summary>Gets or sets the debug action preference.</summary>
    public ActionPreference Debug
    {
        get => GetPreference(ScopedProperties.DebugPreferenceSetting);
        set => SetPreference(ScopedProperties.DebugPreferenceSetting, value);
    }

    /// <summary>Gets or sets the progress action preference.</summary>
    public ActionPreference Progress
    {
        get => GetPreference(ScopedProperties.ProgressPreferenceSetting);
        set => SetPreference(ScopedProperties.ProgressPreferenceSetting, value);
    }

    /// <summary>Returns a description of the scoped action preference settings.</summary>
    /// <returns>The action preference group description.</returns>
    public override string ToString() => SessionStateStrings.ScopedActionPreferenceDescription;

    private ActionPreference GetPreference(ScopedActionPreferenceSetting setting)
        => _facade.GetConfiguration(setting);

    private void SetPreference(ScopedActionPreferenceSetting setting, ActionPreference value)
        => _facade.SetConfiguration(setting, value);
}

/// <summary>Provides scoped ShouldProcess settings.</summary>
public sealed class PSScopedShouldProcess : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedShouldProcess(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets the confirmation impact threshold.</summary>
    public ConfirmImpact Confirm
    {
        get => GetSetting(ScopedProperties.ConfirmPreferenceSetting);
        set => SetSetting(ScopedProperties.ConfirmPreferenceSetting, value);
    }

    /// <summary>Gets or sets whether WhatIf is enabled.</summary>
    public bool WhatIf
    {
        get => GetSetting(ScopedProperties.WhatIfPreferenceSetting);
        set => SetSetting(ScopedProperties.WhatIfPreferenceSetting, value);
    }

    /// <summary>Returns a description of the scoped ShouldProcess settings.</summary>
    /// <returns>The ShouldProcess group description.</returns>
    public override string ToString() => SessionStateStrings.ScopedShouldProcessDescription;

    private T GetSetting<T>(ScopedSetting<T> setting)
        => _facade.GetConfiguration(setting);

    private void SetSetting<T>(ScopedSetting<T> setting, T value)
        => _facade.SetConfiguration(setting, value);
}

/// <summary>Provides scoped style settings.</summary>
public sealed class PSScopedStyle : IScopedPropertyContainer
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

    /// <summary>Returns a description of the scoped style settings.</summary>
    /// <returns>The style group description.</returns>
    public override string ToString() => SessionStateStrings.ScopedStyleDescription;
}

/// <summary>Provides scoped progress rendering settings.</summary>
public sealed class PSScopedProgressStyle : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedProgressStyle(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets the progress view.</summary>
    public ProgressView View
    {
        get
        {
            var (_, scope) = _facade.Resolve();
            return scope.GetScopedSettingValue(ScopedProperties.ProgressViewSetting);
        }

        set
        {
            var (_, scope) = _facade.Resolve();
            scope.SetScopedSettingValue(ScopedProperties.ProgressViewSetting, value);
        }
    }

    /// <summary>Returns a description of the scoped progress rendering settings.</summary>
    /// <returns>The progress style group description.</returns>
    public override string ToString() => SessionStateStrings.ScopedProgressStyleDescription;
}

/// <summary>
/// Identifies a setting independently of the entries holding its values in individual scopes.
/// </summary>
internal abstract class ScopedSetting(string path, string variableName, PreferenceVariable? index = null)
{
    internal string Path { get; } = path;

    internal string VariableName { get; } = variableName;

    internal PreferenceVariable? Index { get; } = index;

    internal abstract object GetDefaultValue();

    internal virtual object GetDefaultExposedValue() => GetDefaultValue();

    internal abstract ScopedSettingEntry CreateEntry(object value);

    internal abstract ScopedSettingEntry CreateEntryFromVariable(PSVariable source);
}

internal class ScopedSetting<T>(string path, T defaultValue, string variableName = null, Action<T> validate = null, PreferenceVariable? index = null, Func<T> defaultFactory = null, bool preserveNull = false)
    : ScopedSetting(path, variableName, index)
{
    internal T DefaultValue { get; } = defaultValue;

    internal override object GetDefaultValue() => GetDefaultValueTyped();

    internal T GetDefaultValueTyped() => defaultFactory is null ? DefaultValue : defaultFactory();

    internal bool PreserveNull { get; } = preserveNull;

    internal override ScopedSettingEntry CreateEntry(object value) => new ScopedSettingEntry<T>(this, value);

    internal override ScopedSettingEntry CreateEntryFromVariable(PSVariable source) => ScopedSettingEntry<T>.Create(this, source);

    /// <summary>
    /// Converts and validates a candidate value without changing any scope binding.
    /// </summary>
    internal virtual T ConvertValue(object value)
    {
        if (value is null && PreserveNull)
        {
            return default;
        }

        T converted = LanguagePrimitives.ConvertTo<T>(value);
        validate?.Invoke(converted);
        return converted;
    }
}

internal sealed class ScopedActionPreferenceSetting(PreferenceVariable index, ActionPreference defaultValue, VariablePath variablePath)
    : ScopedSetting<ActionPreference>($"ActionPreference.{index}", defaultValue, variablePath.UnqualifiedPath, Validate, index)
{
    internal VariablePath VariablePath { get; } = variablePath;

    private static void Validate(ActionPreference value)
    {
        if (value == ActionPreference.Suspend)
        {
            throw new PSInvalidCastException("InvalidActionPreference", null, ErrorPackage.ActionPreferenceReservedForFutureUseError, value);
        }

        // Conversion can return an already typed enum value without checking its underlying number.
        if (!Enum.IsDefined(value))
        {
            LanguagePrimitives.EnumSingleTypeConverter.ThrowForUndefinedEnum("UndefinedIntegerToEnum", value, typeof(ActionPreference));
        }
    }
}

internal sealed class ScopedConfirmPreferenceSetting(PreferenceVariable index, ConfirmImpact defaultValue, VariablePath variablePath)
    : ScopedSetting<ConfirmImpact>("ShouldProcess.Confirm", defaultValue, variablePath.UnqualifiedPath, Validate, index)
{
    private static void Validate(ConfirmImpact value)
    {
        if (!Enum.IsDefined(value))
        {
            LanguagePrimitives.EnumSingleTypeConverter.ThrowForUndefinedEnum("UndefinedIntegerToEnum", value, typeof(ConfirmImpact));
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

    internal override object GetValueRaw() => Value;

    internal override object ConvertValue(object value) => _setting.ConvertValue(value);

    internal override void SetPreparedValue(object value) => Value = (T)value;
}

internal static partial class ScopedProperties
{
    internal static readonly ScopedSetting<ProgressView> ProgressViewSetting = new(
        "Style.Progress.View",
        ProgressView.Minimal);

    internal static readonly ScopedActionPreferenceSetting DebugPreferenceSetting = new(
        PreferenceVariable.Debug,
        InitialSessionState.DefaultDebugPreference,
        SpecialVariables.DebugPreferenceVarPath);

    internal static readonly ScopedActionPreferenceSetting VerbosePreferenceSetting = new(
        PreferenceVariable.Verbose,
        InitialSessionState.DefaultVerbosePreference,
        SpecialVariables.VerbosePreferenceVarPath);

    internal static readonly ScopedActionPreferenceSetting ErrorPreferenceSetting = new(
        PreferenceVariable.Error,
        InitialSessionState.DefaultErrorActionPreference,
        SpecialVariables.ErrorActionPreferenceVarPath);

    internal static readonly ScopedActionPreferenceSetting WarningPreferenceSetting = new(
        PreferenceVariable.Warning,
        InitialSessionState.DefaultWarningPreference,
        SpecialVariables.WarningPreferenceVarPath);

    internal static readonly ScopedActionPreferenceSetting InformationPreferenceSetting = new(
        PreferenceVariable.Information,
        InitialSessionState.DefaultInformationPreference,
        SpecialVariables.InformationPreferenceVarPath);

    internal static readonly ScopedActionPreferenceSetting ProgressPreferenceSetting = new(
        PreferenceVariable.Progress,
        InitialSessionState.DefaultProgressPreference,
        SpecialVariables.ProgressPreferenceVarPath);

    internal static readonly ScopedConfirmPreferenceSetting ConfirmPreferenceSetting = new(
        PreferenceVariable.Confirm,
        InitialSessionState.DefaultConfirmPreference,
        SpecialVariables.ConfirmPreferenceVarPath);

    internal static readonly ScopedSetting<bool> WhatIfPreferenceSetting = new(
        "ShouldProcess.WhatIf",
        InitialSessionState.DefaultWhatIfPreference,
        SpecialVariables.WhatIfPreferenceVarPath.UnqualifiedPath,
        index: PreferenceVariable.WhatIf);

    internal static IReadOnlyList<ScopedActionPreferenceSetting> PreferenceSettings { get; }

    internal static IReadOnlyList<ScopedSetting> ConfigurationSettings { get; }

    internal static IReadOnlyList<ScopedSetting> TuplePreferenceSettings { get; }

    private static readonly Dictionary<string, ScopedSetting> s_preferencesByName;

    private static readonly Dictionary<string, ScopedSetting> s_settingsByPath;

    static ScopedProperties()
    {
        PreferenceSettings = Array.AsReadOnly<ScopedActionPreferenceSetting>(
            [DebugPreferenceSetting, VerbosePreferenceSetting, ErrorPreferenceSetting, WarningPreferenceSetting, InformationPreferenceSetting, ProgressPreferenceSetting]);
        ConfigurationSettings = Array.AsReadOnly<ScopedSetting>(
            PreferenceSettings.Concat<ScopedSetting>([ConfirmPreferenceSetting, WhatIfPreferenceSetting]).Concat(ConfigurationSettingsCore).Concat(ConfigurationSettingsMutable).Concat(RemotingMutableSettings).ToArray());
        TuplePreferenceSettings = Array.AsReadOnly<ScopedSetting>(ConfigurationSettings.Where(static setting => setting.Index.HasValue).ToArray());
        s_preferencesByName = ConfigurationSettings.ToDictionary(static setting => setting.VariableName, StringComparer.OrdinalIgnoreCase);
        s_settingsByPath = ConfigurationSettings.Concat(StyleSettings).Append(ProgressViewSetting).ToDictionary(static setting => setting.Path, StringComparer.OrdinalIgnoreCase);
    }

    internal static bool IsLegacySetting(string name) => TryGetConfigurationSetting(name, out _);

    internal static bool TryGetPreferenceSetting(string name, out ScopedActionPreferenceSetting setting)
    {
        setting = null;
        return ExperimentalFeature.PSScopedProperties && name is not null && s_preferencesByName.TryGetValue(name, out var descriptor) && (setting = descriptor as ScopedActionPreferenceSetting) is not null;
    }

    internal static bool TryGetConfigurationSetting(string name, out ScopedSetting setting)
    {
        setting = null;
        return ExperimentalFeature.PSScopedProperties && name is not null && s_preferencesByName.TryGetValue(name, out setting);
    }

    internal static bool TryGetSetting(string path, out ScopedSetting setting)
    {
        setting = null;
        return path is not null && s_settingsByPath.TryGetValue(path, out setting);
    }

    internal static ProgressView GetProgressView(ExecutionContext context)
        => context.EngineSessionState.CurrentScope.GetScopedSettingValue(ProgressViewSetting);

    internal static void SetProgressView(ExecutionContext context, ProgressView value)
        => context.EngineSessionState.CurrentScope.SetScopedSettingValue(ProgressViewSetting, value);
}
