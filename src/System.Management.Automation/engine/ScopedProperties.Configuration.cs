// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation.Runspaces;
using System.Text;

namespace System.Management.Automation;

public sealed partial class PSScopedPropertyFacade
{
    private readonly PSScopedFormatting _formatting;
    private readonly PSScopedNativeCommand _nativeCommand;
    private readonly PSScopedModule _module;
    private readonly PSScopedHistory _history;
    private readonly PSScopedTranscription _transcription;
    private readonly PSScopedMail _mail;
    private readonly PSScopedHelp _help;
    private readonly PSScopedLogging _logging;
    private readonly PSScopedRemoting _remoting;

    /// <summary>Gets formatting settings.</summary>
    public PSScopedFormatting Formatting => GetGroup(_formatting);

    /// <summary>Gets native command settings.</summary>
    public PSScopedNativeCommand NativeCommand => GetGroup(_nativeCommand);

    /// <summary>Gets module loading settings.</summary>
    public PSScopedModule Module => GetGroup(_module);

    /// <summary>Gets history settings.</summary>
    public PSScopedHistory History => GetGroup(_history);

    /// <summary>Gets transcription settings.</summary>
    public PSScopedTranscription Transcription => GetGroup(_transcription);

    /// <summary>Gets mail settings.</summary>
    public PSScopedMail Mail => GetGroup(_mail);

    /// <summary>Gets help settings.</summary>
    public PSScopedHelp Help => GetGroup(_help);

    /// <summary>Gets logging settings.</summary>
    public PSScopedLogging Logging => GetGroup(_logging);

    /// <summary>Gets remoting settings.</summary>
    public PSScopedRemoting Remoting => GetGroup(_remoting);

    internal T GetConfiguration<T>(ScopedSetting<T> setting)
    {
        var (context, scope) = Resolve();
        return context.GetScopedConfigurationPreference(setting, scope);
    }

    internal T GetScopedStyleSetting<T>(ScopedSetting<T> setting)
    {
        var (_, scope) = Resolve();
        return scope.GetScopedSettingValue(setting);
    }

    internal void SetScopedStyleSetting<T>(ScopedSetting<T> setting, T value)
    {
        var (context, scope) = Resolve();
        scope.SetScopedSettingValue(setting, value, context.EngineSessionState);
    }

    internal void SetConfiguration<T>(ScopedSetting<T> setting, T value)
        => SetConfiguration((ScopedSetting)setting, value);

    internal void SetConfiguration(ScopedSetting setting, object value)
    {
        var (context, scope) = Resolve();
        var state = context.EngineSessionState;
        CommandOrigin origin = state.CurrentScope.ScopeOrigin;
        if (ExperimentalFeature.PSScopedProperties)
        {
            scope.SetScopedSettingValue(setting, value, state, origin);
        }
        else
        {
            scope.SetVariable(setting.VariableName, value, asValue: true, force: false, state, origin);
        }
    }

    private TGroup GetGroup<TGroup>(TGroup group)
    {
        Resolve();
        return group;
    }
}

/// <summary>Provides scoped output formatting settings.</summary>
public sealed class PSScopedFormatting : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedFormatting(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets the error display view.</summary>
    public ErrorView ErrorView
    {
        get => _facade.GetConfiguration(ScopedProperties.ErrorViewSetting);
        set => _facade.SetConfiguration(ScopedProperties.ErrorViewSetting, value);
    }

    /// <summary>Gets or sets the maximum number of items shown during enumeration.</summary>
    public int EnumerationLimit
    {
        get => _facade.GetConfiguration(ScopedProperties.EnumerationLimitSetting);
        set => _facade.SetConfiguration(ScopedProperties.EnumerationLimitSetting, value);
    }

    /// <summary>Gets or sets the separator used when formatting values.</summary>
    public object FieldSeparator
    {
        get => _facade.GetConfiguration(ScopedProperties.FieldSeparatorSetting);
        set => _facade.SetConfiguration(ScopedProperties.FieldSeparatorSetting, value);
    }

    /// <summary>Returns a description of formatting settings.</summary>
    /// <returns>The formatting settings description.</returns>
    public override string ToString() => SessionStateStrings.ScopedFormattingDescription;
}

/// <summary>Provides scoped native command settings.</summary>
public sealed class PSScopedNativeCommand : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;
    private readonly PSScopedNativeCommandEncoding _encoding;

    internal PSScopedNativeCommand(PSScopedPropertyFacade facade)
    {
        _facade = facade;
        _encoding = new(facade);
    }

    /// <summary>Gets native command text encoding settings.</summary>
    public PSScopedNativeCommandEncoding Encoding
    {
        get
        {
            _facade.Resolve();
            return _encoding;
        }
    }

    /// <summary>Gets or sets the native argument passing style.</summary>
    public NativeArgumentPassingStyle ArgumentPassing
    {
        get => _facade.GetConfiguration(ScopedProperties.NativeArgumentPassingSetting);
        set => _facade.SetConfiguration(ScopedProperties.NativeArgumentPassingSetting, value);
    }

    /// <summary>Gets or sets whether native command failures follow ErrorActionPreference.</summary>
    public bool UseErrorActionPreference
    {
        get => _facade.GetConfiguration(ScopedProperties.NativeCommandUseErrorActionPreferenceSetting);
        set => _facade.SetConfiguration(ScopedProperties.NativeCommandUseErrorActionPreferenceSetting, value);
    }

    /// <summary>Returns a description of native command settings.</summary>
    /// <returns>The native command settings description.</returns>
    public override string ToString() => SessionStateStrings.ScopedNativeCommandDescription;
}

/// <summary>Provides scoped native command text encodings.</summary>
public sealed class PSScopedNativeCommandEncoding : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedNativeCommandEncoding(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets the encoding used to send text to native process input.</summary>
    public Encoding ToProcess
    {
        get => _facade.GetConfiguration(ScopedProperties.OutputEncodingSetting);
        set => _facade.SetConfiguration(ScopedProperties.OutputEncodingSetting, value);
    }

    /// <summary>Gets or sets the encoding used to decode native process output.</summary>
    public Encoding FromProcess
    {
        get => _facade.GetConfiguration(ScopedProperties.ApplicationOutputEncodingSetting);
        set => _facade.SetConfiguration(ScopedProperties.ApplicationOutputEncodingSetting, value);
    }

    /// <summary>Returns a description of native process text encodings.</summary>
    /// <returns>The encoding settings description.</returns>
    public override string ToString() => SessionStateStrings.ScopedNativeCommandEncodingDescription;
}

/// <summary>Provides scoped module loading settings.</summary>
public sealed class PSScopedModule : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedModule(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets the module auto-loading preference.</summary>
    public PSModuleAutoLoadingPreference? AutoLoadingPreference
    {
        get => _facade.GetConfiguration(ScopedProperties.ModuleAutoLoadingPreferenceSetting);
        set => _facade.SetConfiguration(ScopedProperties.ModuleAutoLoadingPreferenceSetting, value);
    }

    /// <summary>Returns a description of module loading settings.</summary>
    /// <returns>The module settings description.</returns>
    public override string ToString() => SessionStateStrings.ScopedModuleDescription;
}

/// <summary>Provides scoped history settings.</summary>
public sealed partial class PSScopedHistory : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedHistory(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets the maximum number of history entries.</summary>
    public int MaximumCount
    {
        get => _facade.GetConfiguration(ScopedProperties.HistoryMaximumCountSetting);
        set => _facade.SetConfiguration(ScopedProperties.HistoryMaximumCountSetting, value);
    }

    /// <summary>Returns a description of history settings.</summary>
    /// <returns>The history settings description.</returns>
    public override string ToString() => SessionStateStrings.ScopedHistoryDescription;
}

/// <summary>Provides scoped transcription settings.</summary>
public sealed class PSScopedTranscription : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedTranscription(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets the transcript output path.</summary>
    public object Path
    {
        get => _facade.GetConfiguration(ScopedProperties.TranscriptionPathSetting);
        set => _facade.SetConfiguration(ScopedProperties.TranscriptionPathSetting, value);
    }

    /// <summary>Returns a description of transcription settings.</summary>
    /// <returns>The transcription settings description.</returns>
    public override string ToString() => SessionStateStrings.ScopedTranscriptionDescription;
}

/// <summary>Provides scoped mail settings.</summary>
public sealed class PSScopedMail : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedMail(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets the default mail server.</summary>
    public object Server
    {
        get => _facade.GetConfiguration(ScopedProperties.MailServerSetting);
        set => _facade.SetConfiguration(ScopedProperties.MailServerSetting, value);
    }

    /// <summary>Returns a description of mail settings.</summary>
    /// <returns>The mail settings description.</returns>
    public override string ToString() => SessionStateStrings.ScopedMailDescription;
}

/// <summary>Provides scoped help settings.</summary>
public sealed class PSScopedHelp : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedHelp(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets whether help errors include verbose details.</summary>
    public bool VerboseErrors
    {
        get => _facade.GetConfiguration(ScopedProperties.VerboseHelpErrorsSetting);
        set => _facade.SetConfiguration(ScopedProperties.VerboseHelpErrorsSetting, value);
    }

    /// <summary>Returns a description of help settings.</summary>
    /// <returns>The help settings description.</returns>
    public override string ToString() => SessionStateStrings.ScopedHelpDescription;
}

/// <summary>Provides scoped logging settings.</summary>
public sealed class PSScopedLogging : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedLogging(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets engine health event logging.</summary>
    public bool? EngineHealthEvent
    {
        get => _facade.GetConfiguration(ScopedProperties.LogEngineHealthEventSetting);
        set => _facade.SetConfiguration(ScopedProperties.LogEngineHealthEventSetting, value);
    }

    /// <summary>Gets or sets engine lifecycle event logging.</summary>
    public bool? EngineLifecycleEvent
    {
        get => _facade.GetConfiguration(ScopedProperties.LogEngineLifecycleEventSetting);
        set => _facade.SetConfiguration(ScopedProperties.LogEngineLifecycleEventSetting, value);
    }

    /// <summary>Gets or sets command health event logging.</summary>
    public bool? CommandHealthEvent
    {
        get => _facade.GetConfiguration(ScopedProperties.LogCommandHealthEventSetting);
        set => _facade.SetConfiguration(ScopedProperties.LogCommandHealthEventSetting, value);
    }

    /// <summary>Gets or sets command lifecycle event logging.</summary>
    public bool? CommandLifecycleEvent
    {
        get => _facade.GetConfiguration(ScopedProperties.LogCommandLifecycleEventSetting);
        set => _facade.SetConfiguration(ScopedProperties.LogCommandLifecycleEventSetting, value);
    }

    /// <summary>Gets or sets provider health event logging.</summary>
    public bool? ProviderHealthEvent
    {
        get => _facade.GetConfiguration(ScopedProperties.LogProviderHealthEventSetting);
        set => _facade.SetConfiguration(ScopedProperties.LogProviderHealthEventSetting, value);
    }

    /// <summary>Gets or sets provider lifecycle event logging.</summary>
    public bool? ProviderLifecycleEvent
    {
        get => _facade.GetConfiguration(ScopedProperties.LogProviderLifecycleEventSetting);
        set => _facade.SetConfiguration(ScopedProperties.LogProviderLifecycleEventSetting, value);
    }

    /// <summary>Gets or sets settings event logging.</summary>
    public bool? SettingsEvent
    {
        get => _facade.GetConfiguration(ScopedProperties.LogSettingsEventSetting);
        set => _facade.SetConfiguration(ScopedProperties.LogSettingsEventSetting, value);
    }

    /// <summary>Gets or sets arbitrary user data attached to logging events.</summary>
    public object UserData
    {
        get => _facade.GetConfiguration(ScopedProperties.LogUserDataSetting);
        set => _facade.SetConfiguration(ScopedProperties.LogUserDataSetting, value);
    }

    /// <summary>Returns a description of logging settings.</summary>
    /// <returns>The logging settings description.</returns>
    public override string ToString() => SessionStateStrings.ScopedLoggingDescription;
}

/// <summary>Provides scoped scalar remoting settings.</summary>
public sealed partial class PSScopedRemoting : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;
    private readonly System.Management.Automation.Remoting.PSSessionOption _sessionOptions;

    internal PSScopedRemoting(PSScopedPropertyFacade facade)
    {
        _facade = facade;
        _sessionOptions = System.Management.Automation.Remoting.PSSessionOption.CreateScopedView(
            new ScopedMutableBinding<System.Management.Automation.Remoting.PSSessionOption>(ScopedProperties.SessionOptionsSetting, facade));
        _applicationPrivateDataBinding = new ScopedMutableBinding<object>(ScopedProperties.ApplicationPrivateDataSetting, facade);
    }

    /// <summary>Gets or sets the default remoting configuration name.</summary>
    public object ConfigurationName
    {
        get => _facade.GetConfiguration(ScopedProperties.RemotingConfigurationNameSetting);
        set => _facade.SetConfiguration(ScopedProperties.RemotingConfigurationNameSetting, value);
    }

    /// <summary>Gets or sets the default remoting application name.</summary>
    public object ApplicationName
    {
        get => _facade.GetConfiguration(ScopedProperties.RemotingApplicationNameSetting);
        set => _facade.SetConfiguration(ScopedProperties.RemotingApplicationNameSetting, value);
    }

    /// <summary>Returns a description of scalar remoting settings.</summary>
    /// <returns>The remoting settings description.</returns>
    public override string ToString() => SessionStateStrings.ScopedRemotingDescription;
}

internal static partial class ScopedProperties
{
    private const string TranscriptVariable = "TRANSCRIPT";

    internal static readonly ScopedSetting<ErrorView> ErrorViewSetting = new(
        "Formatting.ErrorView", InitialSessionState.DefaultErrorView, SpecialVariables.ErrorView,
        value => ValidateDefinedEnum(value));

    internal static readonly ScopedSetting<int> EnumerationLimitSetting = new(
        "Formatting.EnumerationLimit", InitialSessionState.DefaultFormatEnumerationLimit, InitialSessionState.FormatEnumerationLimit);

    internal static readonly ScopedSetting<object> FieldSeparatorSetting = new(
        "Formatting.FieldSeparator", " ", SpecialVariables.OFS);

    internal static readonly ScopedSetting<Encoding> OutputEncodingSetting = new(
        "NativeCommand.Encoding.ToProcess", Encoding.Default, SpecialVariables.OutputEncoding,
        index: null);

    internal static readonly ScopedSetting<Encoding> ApplicationOutputEncodingSetting = new(
        "NativeCommand.Encoding.FromProcess", null, SpecialVariables.PSApplicationOutputEncoding,
        preserveNull: true);

    internal static readonly ScopedSetting<NativeArgumentPassingStyle> NativeArgumentPassingSetting = new(
        "NativeCommand.ArgumentPassing", GetNativeArgumentPassingStyleDefault(), SpecialVariables.NativeArgumentPassing,
        value => ValidateDefinedEnum(value));

    internal static readonly ScopedSetting<bool> NativeCommandUseErrorActionPreferenceSetting = new(
        "NativeCommand.UseErrorActionPreference", false, SpecialVariables.PSNativeCommandUseErrorActionPreference);

    internal static readonly ScopedSetting<PSModuleAutoLoadingPreference?> ModuleAutoLoadingPreferenceSetting = new(
        "Module.AutoLoadingPreference", PSModuleAutoLoadingPreference.All, SpecialVariables.PSModuleAutoLoading,
        value =>
        {
            if (value is { } preference)
            {
                ValidateDefinedEnum(preference);
            }
        },
        defaultFactory: GetModuleAutoLoadingPreferenceDefault, preserveNull: true);

    internal static readonly ScopedSetting<int> HistoryMaximumCountSetting = new(
        "History.MaximumCount", Microsoft.PowerShell.Commands.History.DefaultHistorySize, SpecialVariables.HistorySize,
        ValidateHistoryMaximumCount);

    internal static readonly ScopedSetting<string> TranscriptionPathSetting = new(
        "Transcription.Path", null, TranscriptVariable, preserveNull: true);

    internal static readonly ScopedSetting<string> MailServerSetting = new(
        "Mail.Server", string.Empty, SpecialVariables.PSEmailServer, preserveNull: true);

    internal static readonly ScopedSetting<bool> VerboseHelpErrorsSetting = new(
        "Help.VerboseErrors", false, SpecialVariables.VerboseHelpErrors);

    internal static readonly ScopedSetting<bool?> LogEngineHealthEventSetting = new(
        "Logging.EngineHealthEvent", true, SpecialVariables.LogEngineHealthEvent, preserveNull: true);

    internal static readonly ScopedSetting<bool?> LogEngineLifecycleEventSetting = new(
        "Logging.EngineLifecycleEvent", true, SpecialVariables.LogEngineLifecycleEvent, preserveNull: true);

    internal static readonly ScopedSetting<bool?> LogCommandHealthEventSetting = new(
        "Logging.CommandHealthEvent", false, SpecialVariables.LogCommandHealthEvent, preserveNull: true);

    internal static readonly ScopedSetting<bool?> LogCommandLifecycleEventSetting = new(
        "Logging.CommandLifecycleEvent", false, SpecialVariables.LogCommandLifecycleEvent, preserveNull: true);

    internal static readonly ScopedSetting<bool?> LogProviderHealthEventSetting = new(
        "Logging.ProviderHealthEvent", true, SpecialVariables.LogProviderHealthEvent, preserveNull: true);

    internal static readonly ScopedSetting<bool?> LogProviderLifecycleEventSetting = new(
        "Logging.ProviderLifecycleEvent", true, SpecialVariables.LogProviderLifecycleEvent, preserveNull: true);

    internal static readonly ScopedSetting<bool?> LogSettingsEventSetting = new(
        "Logging.SettingsEvent", true, SpecialVariables.LogSettingsEvent, preserveNull: true);

    internal static readonly ScopedSetting<object> LogUserDataSetting = new(
        "Logging.UserData", null, SpecialVariables.PSLogUserData, preserveNull: true);

    internal static readonly ScopedSetting<string> RemotingConfigurationNameSetting = new(
        "Remoting.ConfigurationName", "http://schemas.microsoft.com/powershell/Microsoft.PowerShell", SpecialVariables.PSSessionConfigurationName,
        preserveNull: true);

    internal static readonly ScopedSetting<string> RemotingApplicationNameSetting = new(
        "Remoting.ApplicationName", "wsman", SpecialVariables.PSSessionApplicationName, preserveNull: true);

    private static readonly ScopedSetting[] ConfigurationSettingsCore =
    [
        ErrorViewSetting, EnumerationLimitSetting, FieldSeparatorSetting,
        OutputEncodingSetting, ApplicationOutputEncodingSetting, NativeArgumentPassingSetting, NativeCommandUseErrorActionPreferenceSetting,
        ModuleAutoLoadingPreferenceSetting, HistoryMaximumCountSetting, TranscriptionPathSetting, MailServerSetting, VerboseHelpErrorsSetting,
        LogEngineHealthEventSetting, LogEngineLifecycleEventSetting, LogCommandHealthEventSetting, LogCommandLifecycleEventSetting,
        LogProviderHealthEventSetting, LogProviderLifecycleEventSetting, LogSettingsEventSetting, LogUserDataSetting,
        RemotingConfigurationNameSetting, RemotingApplicationNameSetting,
    ];

    private static readonly ValidateRangeAttribute s_historyMaximumCountRange = new(1, (int)short.MaxValue);

    private static void ValidateDefinedEnum<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            LanguagePrimitives.EnumSingleTypeConverter.ThrowForUndefinedEnum("UndefinedIntegerToEnum", value, typeof(TEnum));
        }
    }

    private static void ValidateHistoryMaximumCount(int value)
    {
        s_historyMaximumCountRange.InternalValidate(value, engineIntrinsics: null);
    }

    private static NativeArgumentPassingStyle GetNativeArgumentPassingStyleDefault()
    {
#if UNIX
        return NativeArgumentPassingStyle.Standard;
#else
        return NativeArgumentPassingStyle.Windows;
#endif
    }

    private static PSModuleAutoLoadingPreference? GetModuleAutoLoadingPreferenceDefault()
    {
        string environmentValue = Environment.GetEnvironmentVariable("PSModuleAutoLoadingPreference");
        if (!string.IsNullOrEmpty(environmentValue))
        {
            try
            {
                PSModuleAutoLoadingPreference preference = LanguagePrimitives.ConvertTo<PSModuleAutoLoadingPreference>(environmentValue);
                ValidateDefinedEnum(preference);
                return preference;
            }
            catch (Exception)
            {
            }
        }

        return PSModuleAutoLoadingPreference.All;
    }
}
