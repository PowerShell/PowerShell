// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Internal;

namespace System.Management.Automation;

/// <summary>Runtime property descriptors are separate from configuration settings.</summary>
internal static class ScopedRuntimeProperties
{
    internal static readonly IReadOnlyDictionary<string, ScopedRuntimePath> ByPath = CreateRegistry();

    private static IReadOnlyDictionary<string, ScopedRuntimePath> CreateRegistry()
    {
        var entries = new[]
        {
            Q(),
            A("Invocation.Arguments", "args", AutomaticVariable.Args),
            A("Invocation.Input", "input", AutomaticVariable.Input),
            A("Invocation.This", "this", AutomaticVariable.This),
            A("Invocation.Cmdlet", "PSCmdlet", AutomaticVariable.PSCmdlet),
            A("Invocation.BoundParameters", "PSBoundParameters", AutomaticVariable.PSBoundParameters),
            A("Invocation.Info", "MyInvocation", AutomaticVariable.MyInvocation),
            A("Invocation.ScriptRoot", "PSScriptRoot", AutomaticVariable.PSScriptRoot),
            A("Invocation.CommandPath", "PSCommandPath", AutomaticVariable.PSCommandPath),
            V("Invocation.ForEachEnumerator", "foreach"),
            V("Invocation.SwitchEnumerator", "switch"),
            V("Invocation.Matches", "Matches"),
            V("LastExitCode", "LASTEXITCODE"),
            V("Diagnostics.Errors", "Error"),
            V("Diagnostics.StackTrace", "StackTrace"),
            V("Diagnostics.DebugContext", "PSDebugContext"),
            V("History.FirstToken", "^"),
            V("History.LastToken", "$"),
            V("Session.Host", "Host"),
            V("Session.ExecutionContext", "ExecutionContext"),
            V("Session.Home", "HOME"),
            V("Session.CurrentLocation", "PWD"),
            V("Session.Profile", "PROFILE"),
            V("Session.NestedPromptLevel", "NestedPromptLevel"),
            V("Session.CurrentlyExecutingCommand", "CurrentlyExecutingCommand"),
            V("Runtime.ProcessId", "PID"),
            V("Runtime.InstallationDirectory", "PSHOME"),
            V("Runtime.ShellId", "ShellId"),
            V("Runtime.VersionTable", "PSVersionTable"),
            V("Runtime.Edition", "PSEdition"),
            V("Runtime.EnabledExperimentalFeatures", "EnabledExperimentalFeatures"),
            V("Runtime.Culture", "PSCulture"),
            V("Runtime.UICulture", "PSUICulture"),
            V("Runtime.IsWindows", "IsWindows"),
            V("Runtime.IsLinux", "IsLinux"),
            V("Runtime.IsMacOS", "IsMacOS"),
            V("Runtime.IsCoreCLR", "IsCoreCLR"),
            V("Event.Record", "Event"),
            V("Event.Subscriber", "EventSubscriber"),
            V("Event.Arguments", "EventArgs"),
            V("Event.Sender", "Sender"),
            V("Remoting.SenderInfo", "PSSenderInfo"),
            C("Constants.True", "true"),
            C("Constants.False", "false"),
            C("Constants.Null", "null"),
        };

        return new System.Collections.ObjectModel.ReadOnlyDictionary<string, ScopedRuntimePath>(
            entries.ToDictionary(static entry => entry.Path, StringComparer.OrdinalIgnoreCase));
    }

    private static ScopedRuntimePath A(string path, string name, AutomaticVariable variable)
        => new(path, name, name, variable, false);

    private static ScopedRuntimePath V(string path, string name, string variablePath = null)
        => new(path, name, variablePath ?? name, null, false);

    private static ScopedRuntimePath C(string path, string name)
        => new(path, name, name, null, true);

    private static ScopedRuntimePath Q()
        => new("LastStatementSuccessful", "?", "?", null, false, true);

    internal static ScopedRuntimePath Get(string path)
        => ByPath.TryGetValue(path, out var descriptor)
            ? descriptor
            : throw new ArgumentException(StringUtil.Format(SessionStateStrings.UnknownScopedSetting, path), nameof(path));
}

internal sealed record ScopedRuntimePath(string Path, string LegacyName, string VariablePath, AutomaticVariable? AutomaticVariable, bool IsLanguageConstant, bool IsQuestionMark = false);

/// <summary>Provides live views of runtime variables and values.</summary>
public sealed partial class PSScopedPropertyFacade
{
    private readonly PSScopedInvocation _invocation;
    private readonly PSScopedDiagnostics _diagnostics;
    private readonly PSScopedRuntime _runtime;
    private readonly PSScopedSession _session;
    private readonly PSScopedEvent _event;
    private readonly PSScopedConstants _constants;

    /// <summary>Gets invocation state.</summary>
    public PSScopedInvocation Invocation => RuntimeGroup(_invocation);

    /// <summary>Gets diagnostic state.</summary>
    public PSScopedDiagnostics Diagnostics => RuntimeGroup(_diagnostics);

    /// <summary>Gets runtime identity and platform state.</summary>
    public PSScopedRuntime Runtime => RuntimeGroup(_runtime);

    /// <summary>Gets host and session state.</summary>
    public PSScopedSession Session => RuntimeGroup(_session);

    /// <summary>Gets event action state.</summary>
    public PSScopedEvent Event => RuntimeGroup(_event);

    /// <summary>Gets language constants.</summary>
    public PSScopedConstants Constants => RuntimeGroup(_constants);

    /// <summary>Gets the current pipeline success flag.</summary>
    public bool LastStatementSuccessful => (bool)ScopedRuntimeAccess.Read(this, "LastStatementSuccessful");

    /// <summary>Gets or sets the last native process exit code variable.</summary>
    public object LastExitCode
    {
        get => ScopedRuntimeAccess.Read(this, "LastExitCode");
        set => ScopedRuntimeAccess.Write(this, "LastExitCode", value);
    }

    private T RuntimeGroup<T>(T group) where T : IScopedPropertyContainer
    {
        Resolve();
        return group;
    }
}

/// <summary>Invocation-frame variables, resolved from active tuple frames.</summary>
public sealed class PSScopedInvocation : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedInvocation(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets the current invocation arguments.</summary>
    public object Arguments { get => Read("Invocation.Arguments"); set => Write("Invocation.Arguments", value); }

    /// <summary>Gets or sets the current input enumerator.</summary>
    public object Input { get => Read("Invocation.Input"); set => Write("Invocation.Input", value); }

    /// <summary>Gets or sets the current invocation's this value.</summary>
    public object This { get => Read("Invocation.This"); set => Write("Invocation.This", value); }

    /// <summary>Gets or sets the current script cmdlet.</summary>
    public object Cmdlet { get => Read("Invocation.Cmdlet"); set => Write("Invocation.Cmdlet", value); }

    /// <summary>Gets or sets the current bound parameters.</summary>
    public object BoundParameters { get => Read("Invocation.BoundParameters"); set => Write("Invocation.BoundParameters", value); }

    /// <summary>Gets or sets the current invocation metadata.</summary>
    public object Info { get => Read("Invocation.Info"); set => Write("Invocation.Info", value); }

    /// <summary>Gets or sets the current script root.</summary>
    public object ScriptRoot { get => Read("Invocation.ScriptRoot"); set => Write("Invocation.ScriptRoot", value); }

    /// <summary>Gets or sets the current script path.</summary>
    public object CommandPath { get => Read("Invocation.CommandPath"); set => Write("Invocation.CommandPath", value); }

    /// <summary>Gets or sets the current foreach enumerator variable.</summary>
    public object ForEachEnumerator { get => Read("Invocation.ForEachEnumerator"); set => Write("Invocation.ForEachEnumerator", value); }

    /// <summary>Gets or sets the current switch enumerator variable.</summary>
    public object SwitchEnumerator { get => Read("Invocation.SwitchEnumerator"); set => Write("Invocation.SwitchEnumerator", value); }

    /// <summary>Gets or sets the results of the last successful match.</summary>
    public object Matches { get => Read("Invocation.Matches"); set => Write("Invocation.Matches", value); }

    /// <summary>Returns a description of the invocation group.</summary>
    /// <returns>The invocation group description.</returns>
    public override string ToString() => SessionStateStrings.ScopedInvocationDescription;

    private object Read(string path) => ScopedRuntimeAccess.Read(_facade, path);

    private void Write(string path, object value) => ScopedRuntimeAccess.Write(_facade, path, value);
}

/// <summary>Diagnostic runtime state.</summary>
public sealed class PSScopedDiagnostics : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedDiagnostics(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets the error history.</summary>
    public object Errors { get => ScopedRuntimeAccess.Read(_facade, "Diagnostics.Errors"); set => ScopedRuntimeAccess.Write(_facade, "Diagnostics.Errors", value); }

    /// <summary>Gets or sets the stack trace variable.</summary>
    public object StackTrace { get => ScopedRuntimeAccess.Read(_facade, "Diagnostics.StackTrace"); set => ScopedRuntimeAccess.Write(_facade, "Diagnostics.StackTrace", value); }

    /// <summary>Gets or sets the debugger context.</summary>
    public object DebugContext { get => ScopedRuntimeAccess.Read(_facade, "Diagnostics.DebugContext"); set => ScopedRuntimeAccess.Write(_facade, "Diagnostics.DebugContext", value); }

    /// <summary>Returns a description of the diagnostics group.</summary>
    /// <returns>The diagnostics group description.</returns>
    public override string ToString() => SessionStateStrings.ScopedDiagnosticsDescription;
}

/// <summary>Runtime identity and live process/runspace state.</summary>
public sealed class PSScopedRuntime : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedRuntime(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets the process identifier variable.</summary>
    public object ProcessId { get => Read("Runtime.ProcessId"); set => Write("Runtime.ProcessId", value); }

    /// <summary>Gets or sets the installation directory variable.</summary>
    public object InstallationDirectory { get => Read("Runtime.InstallationDirectory"); set => Write("Runtime.InstallationDirectory", value); }

    /// <summary>Gets or sets the shell identifier variable.</summary>
    public object ShellId { get => Read("Runtime.ShellId"); set => Write("Runtime.ShellId", value); }

    /// <summary>Gets or sets the PowerShell version table.</summary>
    public object VersionTable { get => Read("Runtime.VersionTable"); set => Write("Runtime.VersionTable", value); }

    /// <summary>Gets or sets the PowerShell edition variable.</summary>
    public object Edition { get => Read("Runtime.Edition"); set => Write("Runtime.Edition", value); }

    /// <summary>Gets or sets the enabled experimental feature list.</summary>
    public object EnabledExperimentalFeatures { get => Read("Runtime.EnabledExperimentalFeatures"); set => Write("Runtime.EnabledExperimentalFeatures", value); }

    /// <summary>Gets or sets the current culture variable.</summary>
    public object Culture { get => Read("Runtime.Culture"); set => Write("Runtime.Culture", value); }

    /// <summary>Gets or sets the current UI culture variable.</summary>
    public object UICulture { get => Read("Runtime.UICulture"); set => Write("Runtime.UICulture", value); }

    /// <summary>Gets or sets the Windows platform variable.</summary>
    public object IsWindows { get => Read("Runtime.IsWindows"); set => Write("Runtime.IsWindows", value); }

    /// <summary>Gets or sets the Linux platform variable.</summary>
    public object IsLinux { get => Read("Runtime.IsLinux"); set => Write("Runtime.IsLinux", value); }

    /// <summary>Gets or sets the macOS platform variable.</summary>
    public object IsMacOS { get => Read("Runtime.IsMacOS"); set => Write("Runtime.IsMacOS", value); }

    /// <summary>Gets or sets the CoreCLR platform variable.</summary>
    public object IsCoreCLR { get => Read("Runtime.IsCoreCLR"); set => Write("Runtime.IsCoreCLR", value); }

    /// <summary>Returns a description of the runtime group.</summary>
    /// <returns>The runtime group description.</returns>
    public override string ToString() => SessionStateStrings.ScopedRuntimeDescription;

    private object Read(string path) => ScopedRuntimeAccess.Read(_facade, path);

    private void Write(string path, object value) => ScopedRuntimeAccess.Write(_facade, path, value);
}

/// <summary>Runspace, host, and session state.</summary>
public sealed class PSScopedSession : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedSession(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets the host variable.</summary>
    public object Host { get => Read("Session.Host"); set => Write("Session.Host", value); }

    /// <summary>Gets or sets the EngineIntrinsics variable.</summary>
    public object ExecutionContext { get => Read("Session.ExecutionContext"); set => Write("Session.ExecutionContext", value); }

    /// <summary>Gets or sets the home directory variable.</summary>
    public object Home { get => Read("Session.Home"); set => Write("Session.Home", value); }

    /// <summary>Gets or sets the current location variable.</summary>
    public object CurrentLocation { get => Read("Session.CurrentLocation"); set => Write("Session.CurrentLocation", value); }

    /// <summary>Gets or sets the profile variable.</summary>
    public object Profile { get => Read("Session.Profile"); set => Write("Session.Profile", value); }

    /// <summary>Gets or sets the nested prompt level variable.</summary>
    public object NestedPromptLevel { get => Read("Session.NestedPromptLevel"); set => Write("Session.NestedPromptLevel", value); }

    /// <summary>Gets or sets the currently executing command variable.</summary>
    public object CurrentlyExecutingCommand { get => Read("Session.CurrentlyExecutingCommand"); set => Write("Session.CurrentlyExecutingCommand", value); }

    /// <summary>Returns a description of the session group.</summary>
    /// <returns>The session group description.</returns>
    public override string ToString() => SessionStateStrings.ScopedSessionDescription;

    private object Read(string path) => ScopedRuntimeAccess.Read(_facade, path);

    private void Write(string path, object value) => ScopedRuntimeAccess.Write(_facade, path, value);
}

/// <summary>Event action variables.</summary>
public sealed class PSScopedEvent : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedEvent(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets or sets the current event record.</summary>
    public object Record { get => Read("Event.Record"); set => Write("Event.Record", value); }

    /// <summary>Gets or sets the current event subscriber.</summary>
    public object Subscriber { get => Read("Event.Subscriber"); set => Write("Event.Subscriber", value); }

    /// <summary>Gets or sets the current event arguments.</summary>
    public object Arguments { get => Read("Event.Arguments"); set => Write("Event.Arguments", value); }

    /// <summary>Gets or sets the current event sender.</summary>
    public object Sender { get => Read("Event.Sender"); set => Write("Event.Sender", value); }

    /// <summary>Returns a description of the event group.</summary>
    /// <returns>The event group description.</returns>
    public override string ToString() => SessionStateStrings.ScopedEventDescription;

    private object Read(string path) => ScopedRuntimeAccess.Read(_facade, path);

    private void Write(string path, object value) => ScopedRuntimeAccess.Write(_facade, path, value);
}

/// <summary>History token variables, added to the existing history group.</summary>
public sealed partial class PSScopedHistory
{
    /// <summary>Gets or sets the first history token.</summary>
    public object FirstToken { get => ScopedRuntimeAccess.Read(_facade, "History.FirstToken"); set => ScopedRuntimeAccess.Write(_facade, "History.FirstToken", value); }

    /// <summary>Gets or sets the last history token.</summary>
    public object LastToken { get => ScopedRuntimeAccess.Read(_facade, "History.LastToken"); set => ScopedRuntimeAccess.Write(_facade, "History.LastToken", value); }
}

/// <summary>Remoting sender information, added to the existing remoting group.</summary>
public sealed partial class PSScopedRemoting : IScopedPropertyContainer
{
    /// <summary>Gets or sets the remoting sender information variable.</summary>
    public object SenderInfo { get => ScopedRuntimeAccess.Read(_facade, "Remoting.SenderInfo"); set => ScopedRuntimeAccess.Write(_facade, "Remoting.SenderInfo", value); }
}

/// <summary>Language constants as read-only runtime views.</summary>
public sealed class PSScopedConstants : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;

    internal PSScopedConstants(PSScopedPropertyFacade facade) => _facade = facade;

    /// <summary>Gets the true language constant.</summary>
    public bool True => (bool)ScopedRuntimeAccess.Read(_facade, "Constants.True");

    /// <summary>Gets the false language constant.</summary>
    public bool False => (bool)ScopedRuntimeAccess.Read(_facade, "Constants.False");

    /// <summary>Gets the null language constant.</summary>
    public object Null => ScopedRuntimeAccess.Read(_facade, "Constants.Null");

    /// <summary>Returns a description of the constants group.</summary>
    /// <returns>The constants group description.</returns>
    public override string ToString() => SessionStateStrings.ScopedConstantsDescription;
}

/// <summary>Resolves live runtime property values from the active execution context.</summary>
internal static class ScopedRuntimeAccess
{
    internal static object Read(PSScopedPropertyFacade facade, string path)
    {
        var (context, startScope) = facade.Resolve();
        ScopedRuntimePath descriptor = ScopedRuntimeProperties.Get(path);
        var result = RuntimePropertyResolvers.Read(context, startScope, descriptor);
        if (result.IsPresent)
        {
            return result.Value;
        }

        if (context.IsStrictVersion(1))
        {
            throw new ScopedRuntimeUndefinedVariableException(descriptor.LegacyName);
        }

        return null;
    }

    internal static void Write(PSScopedPropertyFacade facade, string path, object value)
    {
        var (context, startScope) = facade.Resolve();
        ScopedRuntimePath descriptor = ScopedRuntimeProperties.Get(path);
        if (descriptor.IsQuestionMark || descriptor.IsLanguageConstant)
        {
            throw new SessionStateUnauthorizedAccessException(descriptor.LegacyName, SessionStateCategory.Variable, "VariableNotWritable", SessionStateStrings.VariableNotWritable);
        }

        RuntimePropertyResolvers.Write(context, startScope, descriptor, value);
    }
}

internal readonly record struct RuntimePropertyResult(bool IsPresent, object Value);

/// <summary>Runtime resolver that never supplies configuration defaults.</summary>
internal static class RuntimePropertyResolvers
{
    internal static RuntimePropertyResult Read(ExecutionContext context, SessionStateScope startScope, ScopedRuntimePath descriptor)
    {
        if (descriptor.IsQuestionMark)
        {
            if (context._debuggingMode > 0)
            {
                context.Debugger.CheckVariableRead(SpecialVariables.Question);
            }

            return new(true, context.QuestionMarkVariableValue);
        }

        if (descriptor.IsLanguageConstant)
        {
            if (descriptor.LegacyName == SpecialVariables.Null)
            {
                return new(true, null);
            }

            VariablePath constantPath = new(descriptor.VariablePath);
            PSVariable constant = context.EngineSessionState.GetVariableItem(
                constantPath,
                out _,
                context.EngineSessionState.CurrentScope.ScopeOrigin,
                startScope);
            return constant is null ? new(false, null) : new(true, constant.Value);
        }

        if (descriptor.AutomaticVariable is { } automatic)
        {
            if (context._debuggingMode > 0)
            {
                context.Debugger.CheckVariableRead(descriptor.LegacyName);
            }

            for (SessionStateScope scope = startScope; scope is not null; scope = scope.Parent)
            {
                object value = scope.GetAutomaticVariableValue(automatic);
                if (!ReferenceEquals(value, AutomationNull.Value))
                {
                    return new(true, value);
                }
            }

            return new(false, null);
        }

        VariablePath variablePath = new(descriptor.VariablePath);
        PSVariable variable = context.EngineSessionState.GetVariableItem(
            variablePath,
            out _,
            context.EngineSessionState.CurrentScope.ScopeOrigin,
            startScope);
        if (variable is null && context._debuggingMode > 0)
        {
            context.Debugger.CheckVariableRead(variablePath.UnqualifiedPath);
        }

        return variable is null ? new(false, null) : new(true, variable.Value);
    }

    internal static void Write(ExecutionContext context, SessionStateScope startScope, ScopedRuntimePath descriptor, object value)
    {
        if (context.PSDebugTraceLevel > 1)
        {
            context.Debugger.TraceVariableSet(descriptor.LegacyName, value);
        }

        VariablePath path = new(descriptor.VariablePath);
        SessionStateScope targetScope = startScope;
        bool existed = targetScope.TryGetVariable(path.UnqualifiedPath, context.EngineSessionState.CurrentScope.ScopeOrigin, fromNewOrSet: true, out _);
        PSVariable variable = targetScope.SetVariable(
            path.UnqualifiedPath,
            value,
            asValue: true,
            force: false,
            context.EngineSessionState,
            context.EngineSessionState.CurrentScope.ScopeOrigin);

        if (!existed && context._debuggingMode > 0)
        {
            context.Debugger.CheckVariableWrite(path.UnqualifiedPath);
        }

        if (context.LanguageMode == PSLanguageMode.ConstrainedLanguage)
        {
            ExecutionContext.MarkObjectAsUntrustedForVariableAssignment(variable, startScope, context.EngineSessionState);
        }
    }

}
