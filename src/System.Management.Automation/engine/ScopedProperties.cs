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
        if (string.Equals(path, ScopedProperties.Error.Path, StringComparison.OrdinalIgnoreCase))
        {
            ScopedProperties.Error.Reset(context, scope);
        }
        else if (string.Equals(path, ScopedProperties.ProgressViewSetting.Path, StringComparison.OrdinalIgnoreCase))
        {
            ScopedProperties.ProgressViewSetting.Reset(context, scope);
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
            return ScopedProperties.Error.GetValue(context, scope);
        }

        set
        {
            var (context, scope) = _facade.Resolve();
            ScopedProperties.Error.Write(context, scope, value);
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
            var (context, scope) = _facade.Resolve();
            return ScopedProperties.ProgressViewSetting.GetValue(context, scope);
        }

        set
        {
            var (context, scope) = _facade.Resolve();
            ScopedProperties.ProgressViewSetting.Write(context, scope, value);
        }
    }
}

internal sealed record ScopedSetting<T>(
    string Path,
    T DefaultValue,
    Func<ExecutionContext, SessionStateScope, ScopedSetting<T>, T> Read,
    Action<ExecutionContext, SessionStateScope, T> Write,
    Action<ExecutionContext, SessionStateScope> Reset)
{
    internal T GetValue(ExecutionContext context, SessionStateScope scope) => Read(context, scope, this);
}

internal static class ScopedProperties
{
    internal const string ErrorPath = "ActionPreference.Error";
    private const string ProgressViewPath = "Style.Progress.View";

    internal static readonly ScopedSetting<ActionPreference> Error = new(
        ErrorPath,
        InitialSessionState.DefaultErrorActionPreference,
        static (context, scope, setting) => context.GetEnumPreference(SpecialVariables.ErrorActionPreferenceVarPath, setting.DefaultValue, out _, scope),
        static (context, scope, value) =>
        {
            var state = context.EngineSessionState;
            scope.SetVariable(SpecialVariables.ErrorActionPreference, value, asValue: true, force: false, state, state.CurrentScope.ScopeOrigin);
        },
        static (context, scope) =>
        {
            if (scope.GetVariable(SpecialVariables.ErrorActionPreference, context.EngineSessionState.CurrentScope.ScopeOrigin) is not null)
            {
                scope.RemoveVariable(SpecialVariables.ErrorActionPreference, force: false);
            }
        });

    internal static readonly ScopedSetting<ProgressView> ProgressViewSetting = new(
        ProgressViewPath,
        ProgressView.Minimal,
        static (_, scope, setting) =>
        {
            for (; scope is not null; scope = scope.Parent)
            {
                if (scope.TryGetScopedSetting(setting.Path, out var value))
                {
                    return (ProgressView)value;
                }
            }

            return setting.DefaultValue;
        },
        static (_, scope, value) => scope.SetScopedSetting(ProgressViewPath, value),
        static (_, scope) => scope.RemoveScopedSetting(ProgressViewPath));

    internal static ProgressView GetProgressView(ExecutionContext context)
        => ProgressViewSetting.GetValue(context, context.EngineSessionState.CurrentScope);

    internal static void SetProgressView(ExecutionContext context, ProgressView value)
        => ProgressViewSetting.Write(context, context.EngineSessionState.CurrentScope, value);
}
