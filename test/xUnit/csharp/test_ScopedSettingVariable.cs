// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Management.Automation.Language;
using Xunit;

namespace PSTests.Sequential;

public class ScopedSettingVariableTests
{
    private static readonly ScopedSetting<ActionPreference> s_setting = ScopedProperties.ErrorPreferenceSetting;

    [Fact]
    public void DottedFramesKeepIndependentBindingTargetsAcrossStages()
    {
        var warning = new ScopedActionPreferenceSetting(
            PreferenceVariable.Warning, ActionPreference.Continue, SpecialVariables.WarningPreferenceVarPath);
        var scope = new SessionStateScope(null);
        scope.SetScopedSettingValue(s_setting, ActionPreference.Continue);
        scope.SetScopedSettingValue(warning, ActionPreference.Continue);

        var outerLocals = MutableTuple.MakeTuple(Compiler.DottedLocalsTupleType, Compiler.DottedLocalsNameIndexMap);
        var outer = scope.ActivateDottedScopedSettings(outerLocals);
        scope.InitializeScopedSetting(warning, ActionPreference.Stop, sessionState: null, outer);
        scope.InitializeScopedSetting(s_setting, ActionPreference.Ignore, sessionState: null, outer);
        var outerError = scope.GetScopedSettingVariableForWrite(s_setting);
        var outerWarning = scope.GetScopedSettingVariableForWrite(warning);

        var innerLocals = MutableTuple.MakeTuple(Compiler.DottedLocalsTupleType, Compiler.DottedLocalsNameIndexMap);
        var inner = scope.ActivateDottedScopedSettings(innerLocals);
        Assert.Throws<PSInvalidCastException>(() => scope.InitializeScopedSetting(s_setting, ActionPreference.Suspend, sessionState: null, inner));
        Assert.Same(outerError, scope.GetScopedSettingVariableForWrite(s_setting));

        scope.InitializeScopedSetting(s_setting, ActionPreference.SilentlyContinue, sessionState: null, inner);
        var innerError = scope.GetScopedSettingVariableForWrite(s_setting);
        Assert.NotSame(outerError, innerError);
        Assert.Same(outerWarning, scope.GetScopedSettingVariableForWrite(warning));

        scope.ResetScopedSetting(s_setting);
        Assert.True(innerError.WasRemoved);
        Assert.Null(scope.GetScopedSettingVariableForWrite(s_setting));
        Assert.Equal(ActionPreference.Ignore, scope.GetScopedSettingValue(s_setting));
        Assert.Equal(ActionPreference.Stop, scope.GetScopedSettingValue(warning));

        scope.SetScopedSettingValue(s_setting, ActionPreference.Break);
        var replacement = scope.GetScopedSettingVariableForWrite(s_setting);
        scope.DeactivateDottedScopedSettings(inner);
        Assert.Same(outerError, scope.GetScopedSettingVariableForWrite(s_setting));
        scope.ActivateDottedScopedSettings(inner);
        Assert.Same(replacement, scope.GetScopedSettingVariableForWrite(s_setting));

        scope.DeactivateDottedScopedSettings(inner);
        inner.Release();
        scope.DeactivateDottedScopedSettings(outer);
        outer.Release();
        Assert.Equal(ActionPreference.Break, replacement.GetValueRaw());
        Assert.Equal(ActionPreference.Stop, outerWarning.GetValueRaw());
        Assert.Equal(ActionPreference.Continue, scope.GetScopedSettingValue(s_setting));
        Assert.Equal(ActionPreference.Continue, scope.GetScopedSettingValue(warning));
    }

    [Fact]
    public void VariableAndCanonicalWritesUseTheSameTypedEntry()
    {
        var scope = new SessionStateScope(null);
        scope.SetScopedSettingValue(s_setting, "cOnTiNuE");
        var variable = scope.GetScopedSetting(s_setting).Variable;
        Assert.Equal(ActionPreference.Continue, Assert.IsType<ActionPreference>(variable.Value));

        variable.Value = 1;
        Assert.Equal(ActionPreference.Stop, scope.GetScopedSettingValue(s_setting));
        scope.SetScopedSettingValue(s_setting, ActionPreference.SilentlyContinue);
        Assert.Equal(ActionPreference.SilentlyContinue, variable.GetValueRaw());

        variable.SetValueRaw("Stop", preserveValueTypeSemantics: true);
        Assert.Equal(ActionPreference.Stop, scope.GetScopedSettingValue(s_setting));
        Assert.Throws<PSInvalidCastException>(() => variable.SetValueRaw(ActionPreference.Suspend, preserveValueTypeSemantics: false));
        Assert.Throws<PSInvalidCastException>(() => variable.Value = "invalid");
        Assert.Throws<PSInvalidCastException>(() => scope.SetScopedSettingValue(s_setting, null));
        Assert.Throws<PSInvalidCastException>(() => scope.SetScopedSettingValue(s_setting, 500));
        Assert.Throws<PSInvalidCastException>(() => scope.SetScopedSettingValue(s_setting, (ActionPreference)500));
        Assert.Equal(ActionPreference.Stop, variable.Value);
    }

    [Fact]
    public void AdapterAppliesAttributesAndValidatesAttributeEditsBeforePublication()
    {
        var scope = new SessionStateScope(null);
        scope.SetScopedSettingValue(s_setting, ActionPreference.Continue);
        var variable = scope.GetScopedSetting(s_setting).Variable;
        var allowed = new ValidateSetAttribute("Continue", "Stop");
        var transform = new TranslatePreferenceAttribute();
        variable.Attributes.Add(allowed);
        variable.Attributes.Add(transform);

        variable.Value = "halt";
        Assert.Equal(ActionPreference.Stop, scope.GetScopedSettingValue(s_setting));
        Assert.Throws<ValidationMetadataException>(() => scope.SetScopedSettingValue(s_setting, ActionPreference.Ignore));
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
        scope.InitializeScopedSettingParameter(s_setting, source, sessionState: null);
        var entry = scope.GetScopedSetting(s_setting);

        Assert.Throws<SessionStateUnauthorizedAccessException>(() => scope.SetScopedSettingValue(s_setting, ActionPreference.Stop));
        Assert.Throws<SessionStateUnauthorizedAccessException>(() => scope.ResetScopedSetting(s_setting));
        Assert.Throws<PSInvalidCastException>(() => entry.Variable.SetValue(null, force: true));
        Assert.Equal(ScopedItemOptions.ReadOnly, entry.Variable.Options);
        Assert.Equal(ActionPreference.Continue, entry.Value);

        entry.Variable.SetValue("Stop", force: true);
        Assert.Equal(ActionPreference.Stop, scope.GetScopedSettingValue(s_setting));
        scope.RemoveScopedSettingVariable(s_setting, force: true);
        Assert.True(entry.Variable.WasRemoved);
        Assert.Null(entry.Variable.Value);
        Assert.Equal(ScopedItemOptions.None, entry.Variable.Options);
        entry.Variable.Value = "detached";
        Assert.Equal("detached", entry.Variable.Value);
        Assert.Equal(ActionPreference.Continue, scope.GetScopedSettingValue(s_setting));

        source = new PSVariable(SpecialVariables.ErrorActionPreference, "Continue", ScopedItemOptions.Constant);
        scope.InitializeScopedSettingParameter(s_setting, source, sessionState: null);
        entry = scope.GetScopedSetting(s_setting);
        Assert.Throws<SessionStateUnauthorizedAccessException>(() => entry.Variable.SetValue("Stop", force: true));
        Assert.Throws<SessionStateUnauthorizedAccessException>(() => scope.RemoveScopedSettingVariable(s_setting, force: true));
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
        parent.SetScopedSettingValue(s_setting, ActionPreference.Stop);
        var source = new PSVariable(SpecialVariables.ErrorActionPreference, "SilentlyContinue", ScopedItemOptions.Private);
        child.InitializeScopedSettingParameter(s_setting, source, sessionState: null);

        Assert.Equal(ActionPreference.SilentlyContinue, child.GetScopedSettingValue(s_setting));
        Assert.Equal(ActionPreference.Stop, grandchild.GetScopedSettingValue(s_setting));
    }

    [Fact]
    public void AllScopeBindingsShareValueAndMetadataUntilTheLastRemoval()
    {
        var parent = new SessionStateScope(null);
        var child = new SessionStateScope(parent);
        var scopeThatExits = new SessionStateScope(parent);
        var source = new PSVariable(SpecialVariables.ErrorActionPreference, "Stop", ScopedItemOptions.AllScope);
        parent.InitializeScopedSettingParameter(s_setting, source, sessionState: null);
        var entry = parent.GetScopedSetting(s_setting);

        child.SetScopedSettingValue(s_setting, "SilentlyContinue");
        Assert.Same(entry, child.GetScopedSetting(s_setting));
        Assert.Equal(ActionPreference.SilentlyContinue, parent.GetScopedSettingValue(s_setting));
        child.GetScopedSetting(s_setting).Variable.Description = "shared";
        Assert.Equal("shared", entry.Variable.Description);

        child.ResetScopedSetting(s_setting);
        Assert.Null(child.GetScopedSetting(s_setting));
        Assert.False(entry.Variable.WasRemoved);
        Assert.Equal(ActionPreference.SilentlyContinue, child.GetScopedSettingValue(s_setting));

        Assert.Same(entry, scopeThatExits.GetScopedSetting(s_setting));
        scopeThatExits.MarkRemoved();
        Assert.False(entry.Variable.WasRemoved);
        Assert.Equal(ActionPreference.SilentlyContinue, entry.Variable.Value);

        parent.ResetScopedSetting(s_setting);
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
