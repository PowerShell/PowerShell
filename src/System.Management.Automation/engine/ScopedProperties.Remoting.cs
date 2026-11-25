// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;

namespace System.Management.Automation;

internal static partial class ScopedProperties
{
    internal static readonly ScopedMutableSetting<PSSessionOption> SessionOptionsSetting = new(
        "Remoting.SessionOptions",
        Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet.DEFAULT_SESSION_OPTION,
        static () => new PSSessionOption(),
        static value => value.CloneForScopedSetting(),
        static (binding, value) => PSSessionOption.CreateScopedView(binding),
        static value => value is null ? null : value.CloneForScopedSetting());

    internal static readonly ScopedMutableSetting<object> ApplicationPrivateDataSetting = new(
        "Remoting.ApplicationPrivateData",
        "PSApplicationPrivateData",
        static () => null,
        CloneApplicationPrivateData,
        CreateApplicationPrivateDataView,
        MaterializeApplicationPrivateData);

    internal static readonly ScopedSetting[] RemotingMutableSettings =
    [
        SessionOptionsSetting,
        ApplicationPrivateDataSetting,
    ];

    private static object CloneApplicationPrivateData(object value)
    {
        object baseValue = PSObject.Base(value);
        if (baseValue is PSPrimitiveDictionary primitiveDictionary)
        {
            PSPrimitiveDictionary raw = primitiveDictionary.GetScopedBackingValue();
            return raw?.ShallowCloneForScopedSetting();
        }

        if (baseValue is ScopedHashtableView scopedHashtable)
        {
            return scopedHashtable.ShallowCloneForScopedSetting();
        }

        return baseValue is Hashtable hashtable ? hashtable.Clone() : value;
    }

    private static object MaterializeApplicationPrivateData(object value)
        => CloneApplicationPrivateData(value);

    internal static object CreateApplicationPrivateDataView(ScopedMutableBinding<object> binding, object value)
    {
        if (PSObject.Base(value) is PSPrimitiveDictionary)
        {
            return PSPrimitiveDictionary.CreateScopedView(
                () => binding.ReadRaw() as PSPrimitiveDictionary,
                mutation => binding.Mutate(candidate =>
                {
                    if (candidate is not PSPrimitiveDictionary dictionary)
                    {
                        throw new InvalidOperationException(StringUtil.Format(SessionStateStrings.ScopedMutableValueUnavailable, "Remoting.ApplicationPrivateData"));
                    }

                    mutation(dictionary);
                    return 0;
                }));
        }

        if (PSObject.Base(value) is Hashtable)
        {
            return new ScopedHashtableView(
                () => binding.ReadRaw() as Hashtable,
                mutation => binding.Mutate(candidate =>
                {
                    if (candidate is not Hashtable dictionary)
                    {
                        throw new InvalidOperationException(StringUtil.Format(SessionStateStrings.ScopedMutableValueUnavailable, "Remoting.ApplicationPrivateData"));
                    }

                    mutation(dictionary);
                    return 0;
                }));
        }

        return value;
    }
}

/// <summary>Additional scope-aware mutable settings for remoting.</summary>
public sealed partial class PSScopedRemoting
{
    private ScopedMutableBinding<object> _applicationPrivateDataBinding;

    /// <summary>Gets remoting session options backed by the current or bound scope.</summary>
    public PSSessionOption SessionOptions
    {
        get
        {
            _facade.Resolve();
            return _sessionOptions.HasBackingValue ? _sessionOptions : null;
        }
        set => _facade.SetConfiguration(ScopedProperties.SessionOptionsSetting, value);
    }

    /// <summary>Gets or sets application private data for remoting startup.</summary>
    public object ApplicationPrivateData
    {
        get
        {
            _facade.Resolve();
            object value = _applicationPrivateDataBinding.ReadRaw();
            return ScopedProperties.CreateApplicationPrivateDataView(_applicationPrivateDataBinding, value);
        }
        set => _applicationPrivateDataBinding.SetValue(value);
    }
}

/// <summary>A hashtable view whose reads and writes follow its current scoped backing value.</summary>
internal sealed class ScopedHashtableView : Hashtable, IScopedPropertyContainer, IEnumerable, IDictionary, ICollection
{
    private readonly Func<Hashtable> _read;
    private readonly Action<Action<Hashtable>> _mutate;

    internal ScopedHashtableView(Func<Hashtable> read, Action<Action<Hashtable>> mutate)
        : base(StringComparer.OrdinalIgnoreCase)
    {
        _read = read;
        _mutate = mutate;
    }

    internal Hashtable ReadBacking()
        => _read() ?? throw new InvalidOperationException(StringUtil.Format(SessionStateStrings.ScopedMutableValueUnavailable, "Remoting.ApplicationPrivateData"));

    internal Hashtable ShallowCloneForScopedSetting()
    {
        Hashtable source = ReadBacking();
        return source is null ? null : (Hashtable)source.Clone();
    }

    public override object Clone() => ShallowCloneForScopedSetting();

    private void MutateBacking(Action<Hashtable> mutation, Action standaloneMutation)
    {
        if (_mutate is null)
        {
            standaloneMutation();
        }
        else
        {
            _mutate(mutation);
        }
    }

    public override object this[object key]
    {
        get => ReadBacking()?[key];
        set => MutateBacking(backing => backing[key] = value, () => base[key] = value);
    }

    public override int Count => ReadBacking()?.Count ?? 0;

    public override ICollection Keys => ReadBacking()?.Keys ?? Array.Empty<object>();

    public override ICollection Values => ReadBacking()?.Values ?? Array.Empty<object>();

    public override bool Contains(object key) => ReadBacking()?.Contains(key) ?? false;

    public override bool ContainsKey(object key) => ReadBacking()?.ContainsKey(key) ?? false;

    public override bool ContainsValue(object value) => ReadBacking()?.ContainsValue(value) ?? false;

    public override IDictionaryEnumerator GetEnumerator() => ReadBacking()?.GetEnumerator() ?? new Hashtable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    int ICollection.Count => Count;

    bool ICollection.IsSynchronized => ReadBacking() is ICollection collection && collection.IsSynchronized;

    object ICollection.SyncRoot => (ReadBacking() as ICollection)?.SyncRoot;

    void ICollection.CopyTo(Array array, int index) => CopyTo(array, index);

    object IDictionary.this[object key] { get => this[key]; set => this[key] = value; }

    ICollection IDictionary.Keys => Keys;

    ICollection IDictionary.Values => Values;

    bool IDictionary.IsReadOnly => ReadBacking() is IDictionary dictionary && dictionary.IsReadOnly;

    bool IDictionary.IsFixedSize => ReadBacking() is IDictionary dictionary && dictionary.IsFixedSize;

    void IDictionary.Add(object key, object value) => Add(key, value);

    void IDictionary.Clear() => Clear();

    bool IDictionary.Contains(object key) => Contains(key);

    IDictionaryEnumerator IDictionary.GetEnumerator() => GetEnumerator();

    void IDictionary.Remove(object key) => Remove(key);

    public override void Add(object key, object value) => MutateBacking(backing => backing.Add(key, value), () => base.Add(key, value));

    public override void Clear() => MutateBacking(static backing => backing.Clear(), base.Clear);

    public override void Remove(object key) => MutateBacking(backing => backing.Remove(key), () => base.Remove(key));

    public override void CopyTo(Array array, int index) => ReadBacking()?.CopyTo(array, index);
}
