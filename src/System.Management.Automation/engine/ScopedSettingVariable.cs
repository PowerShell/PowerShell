// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation;

/// <summary>
/// A legacy variable whose value is held by a scoped setting entry.
/// </summary>
internal sealed class ScopedSettingVariable(string name, ScopedSettingEntry entry) : PSVariable(name, false)
{
    private ScopedSettingEntry _entry = entry;

    public override object Value
    {
        get
        {
            DebuggerCheckVariableRead();
            return GetValueRaw();
        }

        set => SetValue(value, force: false);
    }

    internal void SetValue(object value, bool force)
    {
        ThrowIfNotWritable(force);
        object prepared = NormalizeValue(TransformValueForAssignment(value));
        ValidateValueForAssignment(prepared);
        SetPreparedValue(prepared);
        DebuggerCheckVariableWrite();
    }

    internal void Replace(PSVariable source, bool force)
    {
        if (ReferenceEquals(this, source))
        {
            ThrowIfNotWritable(force);
            return;
        }

        ValidateOptions(source.Options, force);
        object prepared = NormalizeValue(source.Value);
        if (!IsValidValue(source.Attributes, prepared))
        {
            ThrowInvalidValue(prepared);
        }

        CopyMetadataFrom(source, initializing: false);
        SetPreparedValue(prepared);
        DebuggerCheckVariableWrite();
    }

    internal bool IsAttached => _entry is not null;

    internal ScopedSettingEntry Entry => _entry;

    internal override object GetValueRaw() => _entry is { } attached ? attached.GetValueRaw() : base.GetValueRaw();

    internal override void SetValueRaw(object newValue, bool preserveValueTypeSemantics)
    {
        if (_entry is { } attached)
        {
            attached.SetPreparedValue(attached.ConvertValue(newValue));
        }
        else
        {
            base.SetValueRaw(newValue, preserveValueTypeSemantics);
        }
    }

    internal object NormalizeValue(object value) => _entry is { } attached ? attached.ConvertValue(value) : value;

    internal void SetPreparedValue(object value)
    {
        if (_entry is { } attached)
        {
            attached.SetPreparedValue(value);
        }
        else
        {
            base.SetValueRaw(value, preserveValueTypeSemantics: true);
        }
    }

    internal void Detach()
    {
        _entry = null;
        WasRemoved = true;
    }
}
