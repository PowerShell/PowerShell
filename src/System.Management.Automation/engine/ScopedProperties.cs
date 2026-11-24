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

internal abstract class ScopedSetting(string path)
{
    internal string Path { get; } = path;
}

internal sealed class ScopedSetting<T>(string path, T defaultValue) : ScopedSetting(path)
{
    internal T DefaultValue { get; } = defaultValue;

    internal T GetValue(SessionStateScope scope)
    {
        for (; scope is not null; scope = scope.Parent)
        {
            if (scope.GetScopedSetting(this) is ScopedSettingEntry<T> entry)
            {
                return entry.Value;
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

    internal T ConvertValue(object value) => LanguagePrimitives.ConvertTo<T>(value);

    internal void Reset(SessionStateScope scope) => scope.RemoveScopedSetting(this);
}

internal abstract class ScopedSettingEntry
{
    internal abstract ScopedSetting Setting { get; }
}

internal sealed class ScopedSettingEntry<T>(ScopedSetting<T> setting, object value) : ScopedSettingEntry
{
    internal override ScopedSetting Setting => setting;

    internal T Value { get; private set; } = setting.ConvertValue(value);

    internal void SetValue(object value) => Value = setting.ConvertValue(value);
}

internal static class ScopedProperties
{
    internal const string ErrorPath = "ActionPreference.Error";
    private const string ProgressViewPath = "Style.Progress.View";

    internal static readonly ScopedSetting<ProgressView> ProgressViewSetting = new(
        ProgressViewPath,
        ProgressView.Minimal);

    internal static ActionPreference GetErrorActionPreference(ExecutionContext context, SessionStateScope scope)
        => context.GetEnumPreference(SpecialVariables.ErrorActionPreferenceVarPath, InitialSessionState.DefaultErrorActionPreference, out _, scope);

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
