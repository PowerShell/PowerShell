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

    /// <summary>
    /// Transforms, converts, and validates an assignment before updating the setting.
    /// Force permits a read-only write without changing the variable's options.
    /// </summary>
    internal void SetValue(object value, bool force)
    {
        ThrowIfNotWritable(force);
        object prepared = NormalizeValue(TransformValueForAssignment(value));
        ValidateValueForAssignment(prepared);
        SetPreparedValue(prepared);
        DebuggerCheckVariableWrite();
    }

    /// <summary>
    /// Applies a supplied variable's value and assignment metadata while retaining this adapter's identity.
    /// The source value is already transformed; conversion and validation precede mutation.
    /// </summary>
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

    internal PSVariable ModuleImportSource { get; set; }

    internal override object GetValueRaw() => _entry is { } attached ? attached.GetValueRaw() : base.GetValueRaw();

    /// <summary>
    /// Writes through the trusted internal path, retaining setting conversion and validation while attached.
    /// Variable protection checks and attribute processing are the caller's responsibility.
    /// </summary>
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

    /// <summary>
    /// Publishes a value that the caller has already converted and validated, without repeating assignment processing.
    /// </summary>
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

    /// <summary>
    /// Disconnects a removed variable from its setting and clears its variable state.
    /// Subsequent writes to a captured adapter cannot recreate the removed binding.
    /// </summary>
    internal void Detach()
    {
        _entry = null;
        ModuleImportSource = null;
        WasRemoved = true;
    }
}
