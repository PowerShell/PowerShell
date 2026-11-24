// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using Xunit;

namespace PSTests.Sequential;

public class ScopedSettingVariableTests
{
    private static readonly ScopedSetting<ActionPreference> s_setting = ScopedProperties.ErrorPreferenceSetting;

    [Fact]
    public void VariableAndCanonicalWritesUseTheSameTypedEntry()
    {
        var scope = new SessionStateScope(null);
        s_setting.SetValue(scope, "cOnTiNuE");
        var variable = scope.GetScopedSetting(s_setting).Variable;
        Assert.Equal(ActionPreference.Continue, Assert.IsType<ActionPreference>(variable.Value));

        variable.Value = 1;
        Assert.Equal(ActionPreference.Stop, s_setting.GetValue(scope));
        s_setting.SetValue(scope, ActionPreference.SilentlyContinue);
        Assert.Equal(ActionPreference.SilentlyContinue, variable.GetValueRaw());

        variable.SetValueRaw("Stop", preserveValueTypeSemantics: true);
        Assert.Equal(ActionPreference.Stop, s_setting.GetValue(scope));
        Assert.Throws<PSInvalidCastException>(() => variable.SetValueRaw(ActionPreference.Suspend, preserveValueTypeSemantics: false));
        Assert.Throws<PSInvalidCastException>(() => variable.Value = "invalid");
        Assert.Throws<PSInvalidCastException>(() => s_setting.SetValue(scope, null));
        Assert.Throws<PSInvalidCastException>(() => s_setting.SetValue(scope, 500));
        Assert.Equal(ActionPreference.Stop, variable.Value);
    }

    [Fact]
    public void AdapterAppliesAttributesAndValidatesAttributeEditsBeforePublication()
    {
        var scope = new SessionStateScope(null);
        s_setting.SetValue(scope, ActionPreference.Continue);
        var variable = scope.GetScopedSetting(s_setting).Variable;
        var allowed = new ValidateSetAttribute("Continue", "Stop");
        var transform = new TranslatePreferenceAttribute();
        variable.Attributes.Add(allowed);
        variable.Attributes.Add(transform);

        variable.Value = "halt";
        Assert.Equal(ActionPreference.Stop, s_setting.GetValue(scope));
        Assert.Throws<ValidationMetadataException>(() => s_setting.SetValue(scope, ActionPreference.Ignore));
        Assert.Throws<PSInvalidCastException>(() => variable.Attributes.Add(new ReplacePreferenceAttribute("invalid")));
        Assert.Throws<ValidationMetadataException>(() => variable.Attributes[1] = new ReplacePreferenceAttribute("Ignore"));
        Assert.Same(allowed, variable.Attributes[0]);
        Assert.Same(transform, variable.Attributes[1]);
        Assert.Equal(2, variable.Attributes.Count);
        Assert.Equal(ActionPreference.Stop, variable.Value);
    }

    [Fact]
    public void AdapterPreservesProtectionAndAppliesForceWithoutChangingOptions()
    {
        var scope = new SessionStateScope(null);
        var source = new PSVariable(SpecialVariables.ErrorActionPreference, "Continue", ScopedItemOptions.ReadOnly);
        var entry = ScopedSettingEntry<ActionPreference>.Create(s_setting, source);
        scope.SetScopedSetting(entry);

        Assert.Throws<SessionStateUnauthorizedAccessException>(() => s_setting.SetValue(scope, ActionPreference.Stop));
        Assert.Throws<SessionStateUnauthorizedAccessException>(() => s_setting.Reset(scope));
        Assert.Throws<PSInvalidCastException>(() => entry.Variable.SetValue(null, force: true));
        Assert.Equal(ScopedItemOptions.ReadOnly, entry.Variable.Options);
        Assert.Equal(ActionPreference.Continue, entry.Value);

        entry.Variable.SetValue("Stop", force: true);
        Assert.Equal(ActionPreference.Stop, s_setting.GetValue(scope));
        scope.RemoveScopedSetting(s_setting, force: true);
        Assert.True(entry.Variable.WasRemoved);
        Assert.Null(entry.Variable.Value);
        Assert.Equal(ScopedItemOptions.None, entry.Variable.Options);
        entry.Variable.Value = "detached";
        Assert.Equal("detached", entry.Variable.Value);
        Assert.Equal(ActionPreference.Continue, s_setting.GetValue(scope));

        source = new PSVariable(SpecialVariables.ErrorActionPreference, "Continue", ScopedItemOptions.Constant);
        entry = ScopedSettingEntry<ActionPreference>.Create(s_setting, source);
        scope.SetScopedSetting(entry);
        Assert.Throws<SessionStateUnauthorizedAccessException>(() => entry.Variable.SetValue("Stop", force: true));
        Assert.Throws<SessionStateUnauthorizedAccessException>(() => scope.RemoveScopedSetting(s_setting, force: true));
        Assert.Equal(ActionPreference.Continue, entry.Value);
    }

    [Fact]
    public void ImportAndReplacementPreserveMetadataAndRejectInvalidUpdatesAtomically()
    {
        var source = new PSVariable(SpecialVariables.ErrorActionPreference, "Continue", ScopedItemOptions.Private)
        {
            Description = "original",
            Visibility = SessionStateEntryVisibility.Private,
        };
        var originalAttribute = new ValidateSetAttribute("Continue");
        source.Attributes.Add(originalAttribute);
        var entry = ScopedSettingEntry<ActionPreference>.Create(s_setting, source);
        var variable = entry.Variable;
        Assert.Equal(source.Name, variable.Name);
        Assert.Equal(source.Description, variable.Description);
        Assert.Equal(source.Visibility, variable.Visibility);
        Assert.Equal(source.Options, variable.Options);
        Assert.Same(originalAttribute, Assert.Single(variable.Attributes));

        var replacement = new PSVariable(source.Name, "invalid") { Description = "changed" };
        Assert.Throws<PSInvalidCastException>(() => variable.Replace(replacement, force: false));
        Assert.Equal(ActionPreference.Continue, entry.Value);
        Assert.Equal("original", variable.Description);
        Assert.Equal(ScopedItemOptions.Private, variable.Options);
        Assert.Same(originalAttribute, Assert.Single(variable.Attributes));

        replacement.Value = "Stop";
        replacement.Attributes.Add(new ValidateSetAttribute("Stop"));
        variable.Replace(replacement, force: false);
        Assert.Same(variable, entry.Variable);
        Assert.Equal("changed", variable.Description);
        Assert.Equal(ActionPreference.Stop, entry.Value);
        Assert.Equal("Continue", source.Value);
    }

    [Fact]
    public void SettingLookupSkipsPrivateAncestors()
    {
        var parent = new SessionStateScope(null);
        var child = new SessionStateScope(parent);
        var grandchild = new SessionStateScope(child);
        s_setting.SetValue(parent, ActionPreference.Stop);
        var source = new PSVariable(SpecialVariables.ErrorActionPreference, "SilentlyContinue", ScopedItemOptions.Private);
        child.SetScopedSetting(ScopedSettingEntry<ActionPreference>.Create(s_setting, source));

        Assert.Equal(ActionPreference.SilentlyContinue, s_setting.GetValue(child));
        Assert.Equal(ActionPreference.Stop, s_setting.GetValue(grandchild));
    }

    [Fact]
    public void AllScopeBindingsShareValueAndMetadataUntilTheLastRemoval()
    {
        var parent = new SessionStateScope(null);
        var child = new SessionStateScope(parent);
        var scopeThatExits = new SessionStateScope(parent);
        var source = new PSVariable("AdapterAllScopeProbe", "Stop", ScopedItemOptions.AllScope);
        var entry = ScopedSettingEntry<ActionPreference>.Create(s_setting, source);
        parent.SetScopedSetting(entry);

        s_setting.SetValue(child, "SilentlyContinue");
        Assert.Same(entry, child.GetScopedSetting(s_setting));
        Assert.Equal(ActionPreference.SilentlyContinue, s_setting.GetValue(parent));
        child.GetScopedSetting(s_setting).Variable.Description = "shared";
        Assert.Equal("shared", entry.Variable.Description);

        s_setting.Reset(child);
        Assert.Null(child.GetScopedSetting(s_setting));
        Assert.False(entry.Variable.WasRemoved);
        Assert.Equal(ActionPreference.SilentlyContinue, s_setting.GetValue(child));

        Assert.Same(entry, scopeThatExits.GetScopedSetting(s_setting));
        scopeThatExits.MarkRemoved();
        Assert.False(entry.Variable.WasRemoved);
        Assert.Equal(ActionPreference.SilentlyContinue, entry.Variable.Value);

        s_setting.Reset(parent);
        Assert.True(entry.Variable.WasRemoved);
        Assert.Null(entry.Variable.Value);
    }

    private sealed class TranslatePreferenceAttribute : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
            => inputData is "halt" ? "Stop" : inputData;
    }

    private sealed class ReplacePreferenceAttribute(string value) : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData) => value;
    }
}
