// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation;

internal interface IScopedMutableSettingEntry
{
    object RawValue { get; }
}

internal interface IScopedMutableSetting
{
    object GetVariableView(SessionStateInternal sessionState, ExecutionContext context, VariablePath path, CommandOrigin origin);
}

/// <summary>
/// A mutable setting whose value is exposed through a concrete, scope-aware object.
/// </summary>
internal sealed class ScopedMutableSetting<T>(
    string path,
    string variableName,
    Func<T> defaultFactory,
    Func<T, T> clone,
    Func<ScopedMutableBinding<T>, T, T> viewFactory,
    Func<T, T> materialize = null)
    : ScopedSetting<T>(path, defaultFactory(), variableName, defaultFactory: defaultFactory, preserveNull: true), IScopedMutableSetting
    where T : class
{
    private readonly Func<T> _defaultFactory = defaultFactory;
    private readonly Func<T, T> _clone = clone;
    private readonly Func<ScopedMutableBinding<T>, T, T> _viewFactory = viewFactory;
    private readonly Func<T, T> _materialize = materialize ?? (static value => value);

    internal override ScopedSettingEntry CreateEntry(object value)
        => new ScopedMutableSettingEntry<T>(this, ConvertValue(value));

    internal override T ConvertValue(object value)
    {
        T converted = base.ConvertValue(value);
        return converted is null ? null : _materialize(converted);
    }

    internal override ScopedSettingEntry CreateEntryFromVariable(PSVariable source)
    {
        object value = source is ScopedSettingVariable { Entry: ScopedMutableSettingEntry<T> mutable }
            ? mutable.Value
            : source.Value;
        var entry = (ScopedMutableSettingEntry<T>)CreateEntry(value);
        if (!PSVariable.IsValidValue(source.Attributes, entry.Value))
        {
            entry.Variable.ThrowInvalidValue(entry.Value);
        }

        entry.Variable.CopyMetadataFrom(source, initializing: true);
        return entry;
    }

    internal T CreateDefaultValue() => _defaultFactory();

    internal T CloneValue(T value) => _clone(value);

    internal T CreateView(ScopedMutableBinding<T> binding, T value) => _viewFactory(binding, value);

    internal ScopedMutableSettingEntry<T> CreateMutableEntry(T value)
        => new(this, value);

    internal override object GetDefaultValue() => CreateDefaultValue();

    internal override object GetDefaultExposedValue()
    {
        T value = CreateDefaultValue();
        if (value is null)
        {
            return null;
        }

        return CreateView(new ScopedMutableBinding<T>(this), value);
    }

    object IScopedMutableSetting.GetVariableView(SessionStateInternal sessionState, ExecutionContext context, VariablePath path, CommandOrigin origin)
    {
        if (path.IsUnscopedVariable)
        {
            SessionStateScope caller = sessionState.CurrentScope;
            T raw = caller.GetScopedMutableSettingValue(this, out _, origin);
            return raw is null ? null : CreateView(new ScopedMutableBinding<T>(this), raw);
        }

        SessionStateScope target = path.IsGlobal ? sessionState.GlobalScope
            : path.IsScript ? sessionState.ScriptScope
            : sessionState.CurrentScope;
        return target.GetScopedMutableSettingView(this, context, origin);
    }
}

/// <summary>A canonical mutable backing value and its exposed concrete object.</summary>
internal sealed class ScopedMutableSettingEntry<T> : ScopedSettingEntry, IScopedMutableSettingEntry where T : class
{
    private readonly ScopedMutableSetting<T> _setting;
    private T _value;

    internal ScopedMutableSettingEntry(ScopedMutableSetting<T> setting, T value)
    {
        _setting = setting;
        _value = value;
        Variable = new ScopedSettingVariable(setting.VariableName, this);
    }

    internal override ScopedSetting Setting => _setting;

    internal override object GetValueRaw() => _value;

    internal override object ConvertValue(object value) => _setting.ConvertValue(value);

    internal override void SetPreparedValue(object value) => _value = (T)value;

    internal T Value => _value;

    object IScopedMutableSettingEntry.RawValue => _value;

    internal T CloneValue(T value) => _setting.CloneValue(value);

}

/// <summary>
/// Resolves mutable-setting reads and writes against an unbound caller or an explicitly selected scope.
/// </summary>
internal sealed class ScopedMutableBinding<T> where T : class
{
    private readonly ScopedMutableSetting<T> _setting;
    private readonly bool _isBound;
    private WeakReference<ExecutionContext> _owner;
    private readonly WeakReference<SessionStateScope> _scope;
    private readonly PSScopedPropertyFacade _facade;

    internal ScopedMutableBinding(ScopedMutableSetting<T> setting)
    {
        _setting = setting;
    }

    internal ScopedMutableBinding(ScopedMutableSetting<T> setting, PSScopedPropertyFacade facade)
    {
        _setting = setting;
        _isBound = true;
        _facade = facade;
    }

    internal ScopedMutableBinding(ScopedMutableSetting<T> setting, ExecutionContext owner, SessionStateScope scope)
    {
        _setting = setting;
        _isBound = true;
        _owner = new(owner ?? throw new ArgumentNullException(nameof(owner)));
        _scope = new(scope ?? throw new ArgumentNullException(nameof(scope)));
    }

    internal T ReadRaw()
    {
        var (context, scope) = Resolve();
        return scope.GetScopedMutableSettingValue(_setting, out _, context.EngineSessionState.CurrentScope.ScopeOrigin);
    }

    internal TResult Mutate<TResult>(Func<T, TResult> mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        var (context, scope) = Resolve();
        T current = scope.GetScopedMutableSettingValue(_setting, out _, context.EngineSessionState.CurrentScope.ScopeOrigin);
        if (current is null)
        {
            throw new InvalidOperationException(StringUtil.Format(SessionStateStrings.ScopedMutableValueUnavailable, _setting.Path));
        }

        T candidate = _setting.CloneValue(current);

        // Do not publish until the mutator returns successfully. The mutator is invoked exactly once.
        TResult result = mutation(candidate);
        scope.PublishScopedMutableSetting(_setting, candidate, context.EngineSessionState, context.EngineSessionState.CurrentScope.ScopeOrigin);
        return result;
    }

    internal void SetValue(T value)
    {
        var (context, scope) = Resolve();
        scope.SetScopedSettingValue(_setting, value, context.EngineSessionState, context.EngineSessionState.CurrentScope.ScopeOrigin);
    }

    internal T CreateBoundView(SessionStateScope scope)
    {
        var (context, _) = Resolve();
        T raw = scope.GetScopedMutableSettingValue(_setting, out _, context.EngineSessionState.CurrentScope.ScopeOrigin);
        return raw is null ? null : _setting.CreateView(new ScopedMutableBinding<T>(_setting, context, scope), raw);
    }

    private (ExecutionContext Context, SessionStateScope Scope) Resolve()
    {
        if (!_isBound)
        {
            ExecutionContext current = LocalPipeline.GetExecutionContextFromTLS();
            if (current?.EngineSessionState is null)
            {
                throw new InvalidOperationException(SessionStateStrings.NoScopedSettingExecutionContext);
            }

            return (current, current.EngineSessionState.CurrentScope);
        }

        if (_facade is not null)
        {
            return _facade.Resolve();
        }

        ExecutionContext owner = LocalPipeline.GetExecutionContextFromTLS();
        if (owner?.EngineSessionState is null)
        {
            throw new InvalidOperationException(SessionStateStrings.NoScopedSettingExecutionContext);
        }

        if (!_owner.TryGetTarget(out var expectedBoundOwner) || expectedBoundOwner != owner)
        {
            throw new InvalidOperationException(SessionStateStrings.ForeignScopedSettingScope);
        }

        if (!_scope.TryGetTarget(out var scope) || scope.IsRemoved)
        {
            throw new InvalidOperationException(SessionStateStrings.ExpiredScopedSettingScope);
        }

        return (owner, scope);
    }

}

internal static partial class ScopedProperties
{
    internal static readonly ScopedMutableSetting<DefaultParameterDictionary> DefaultParameterValuesSetting = new(
        "Command.DefaultParameterValues",
        SpecialVariables.PSDefaultParameterValues,
        static () => new DefaultParameterDictionary(),
        static value => value.CloneForScopedSetting(),
        static (binding, value) => DefaultParameterDictionary.CreateScopedView(binding),
        static value => value.MaterializeForScopedSetting());

    internal static readonly ScopedSetting[] ConfigurationSettingsMutable =
    [
        DefaultParameterValuesSetting,
    ];
}

public sealed partial class PSScopedPropertyFacade
{
    private PSScopedCommand _command;

    /// <summary>Gets command settings.</summary>
    public PSScopedCommand Command
    {
        get
        {
            Resolve();
            return _command ??= new PSScopedCommand(this);
        }
    }
}

/// <summary>Provides scoped command parameter defaults.</summary>
public sealed class PSScopedCommand : IScopedPropertyContainer
{
    private readonly PSScopedPropertyFacade _facade;
    private readonly DefaultParameterDictionary _defaultParameterValues;

    internal PSScopedCommand(PSScopedPropertyFacade facade)
    {
        _facade = facade;
        _defaultParameterValues = DefaultParameterDictionary.CreateScopedView(
            new ScopedMutableBinding<DefaultParameterDictionary>(ScopedProperties.DefaultParameterValuesSetting, facade));
    }

    /// <summary>Gets the scoped default parameter dictionary.</summary>
    public DefaultParameterDictionary DefaultParameterValues
    {
        get
        {
            _facade.Resolve();
            return _defaultParameterValues.HasBackingValue ? _defaultParameterValues : null;
        }

        set => _facade.SetConfiguration(ScopedProperties.DefaultParameterValuesSetting, value);
    }

    /// <summary>Returns a description of command settings.</summary>
    /// <returns>The command settings description.</returns>
    public override string ToString() => SessionStateStrings.ScopedCommandDescription;
}
