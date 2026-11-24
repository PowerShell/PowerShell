// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using Xunit;

namespace PSTests.Sequential;

public class ScopedSettingVariableTests
{
    public static IEnumerable<object[]> PreferencePaths()
    {
        foreach (var setting in ScopedProperties.PreferenceSettings)
        {
            yield return new object[] { setting.Path };
        }
    }

    private static ScopedActionPreferenceSetting GetPreferenceSetting(string path)
    {
        Assert.True(ScopedProperties.TryGetSetting(path, out var setting));
        return Assert.IsType<ScopedActionPreferenceSetting>(setting);
    }

    [Fact]
    public void DottedFramesKeepIndependentBindingTargetsAcrossStages()
    {
        var error = ScopedProperties.ErrorPreferenceSetting;
        var warning = new ScopedActionPreferenceSetting(
            PreferenceVariable.Warning, ActionPreference.Continue, SpecialVariables.WarningPreferenceVarPath);
        var scope = new SessionStateScope(null);
        scope.SetScopedSettingValue(error, ActionPreference.Continue);
        scope.SetScopedSettingValue(warning, ActionPreference.Continue);

        var outerLocals = MutableTuple.MakeTuple(Compiler.DottedLocalsTupleType, Compiler.DottedLocalsNameIndexMap);
        var outer = scope.ActivateDottedScopedSettings(outerLocals);
        scope.InitializeScopedSetting(warning, ActionPreference.Stop, sessionState: null, outer);
        scope.InitializeScopedSetting(error, ActionPreference.Ignore, sessionState: null, outer);
        var outerError = scope.GetScopedSettingVariableForWrite(error);
        var outerWarning = scope.GetScopedSettingVariableForWrite(warning);

        var innerLocals = MutableTuple.MakeTuple(Compiler.DottedLocalsTupleType, Compiler.DottedLocalsNameIndexMap);
        var inner = scope.ActivateDottedScopedSettings(innerLocals);
        Assert.Throws<PSInvalidCastException>(() => scope.InitializeScopedSetting(error, ActionPreference.Suspend, sessionState: null, inner));
        Assert.Same(outerError, scope.GetScopedSettingVariableForWrite(error));

        scope.InitializeScopedSetting(error, ActionPreference.SilentlyContinue, sessionState: null, inner);
        var innerError = scope.GetScopedSettingVariableForWrite(error);
        Assert.NotSame(outerError, innerError);
        Assert.Same(outerWarning, scope.GetScopedSettingVariableForWrite(warning));

        scope.ResetScopedSetting(error);
        Assert.True(innerError.WasRemoved);
        Assert.Null(scope.GetScopedSettingVariableForWrite(error));
        Assert.Equal(ActionPreference.Ignore, scope.GetScopedSettingValue(error));
        Assert.Equal(ActionPreference.Stop, scope.GetScopedSettingValue(warning));

        scope.SetScopedSettingValue(error, ActionPreference.Break);
        var replacement = scope.GetScopedSettingVariableForWrite(error);
        scope.DeactivateDottedScopedSettings(inner);
        Assert.Same(outerError, scope.GetScopedSettingVariableForWrite(error));
        scope.ActivateDottedScopedSettings(inner);
        Assert.Same(replacement, scope.GetScopedSettingVariableForWrite(error));

        scope.DeactivateDottedScopedSettings(inner);
        inner.Release();
        scope.DeactivateDottedScopedSettings(outer);
        outer.Release();
        Assert.Equal(ActionPreference.Break, replacement.GetValueRaw());
        Assert.Equal(ActionPreference.Stop, outerWarning.GetValueRaw());
        Assert.Equal(ActionPreference.Continue, scope.GetScopedSettingValue(error));
        Assert.Equal(ActionPreference.Continue, scope.GetScopedSettingValue(warning));
    }

    [Theory]
    [MemberData(nameof(PreferencePaths))]
    public void VariableAndCanonicalWritesUseTheSameTypedEntry(string path)
    {
        var setting = GetPreferenceSetting(path);
        var scope = new SessionStateScope(null);
        scope.SetScopedSettingValue(setting, "cOnTiNuE");
        var variable = scope.GetScopedSetting(setting).Variable;
        Assert.Equal(ActionPreference.Continue, Assert.IsType<ActionPreference>(variable.Value));

        variable.Value = 1;
        Assert.Equal(ActionPreference.Stop, scope.GetScopedSettingValue(setting));
        scope.SetScopedSettingValue(setting, ActionPreference.SilentlyContinue);
        Assert.Equal(ActionPreference.SilentlyContinue, variable.GetValueRaw());

        variable.SetValueRaw("Stop", preserveValueTypeSemantics: true);
        Assert.Equal(ActionPreference.Stop, scope.GetScopedSettingValue(setting));
        Assert.Throws<PSInvalidCastException>(() => variable.SetValueRaw(ActionPreference.Suspend, preserveValueTypeSemantics: false));
        Assert.Throws<PSInvalidCastException>(() => variable.Value = "invalid");
        Assert.Throws<PSInvalidCastException>(() => scope.SetScopedSettingValue(setting, null));
        Assert.Throws<PSInvalidCastException>(() => scope.SetScopedSettingValue(setting, 500));
        Assert.Throws<PSInvalidCastException>(() => scope.SetScopedSettingValue(setting, (ActionPreference)500));
        Assert.Equal(ActionPreference.Stop, variable.Value);
    }

    [Theory]
    [MemberData(nameof(PreferencePaths))]
    public void AdapterAppliesAttributesAndValidatesAttributeEditsBeforePublication(string path)
    {
        var setting = GetPreferenceSetting(path);
        var scope = new SessionStateScope(null);
        scope.SetScopedSettingValue(setting, ActionPreference.Continue);
        var variable = scope.GetScopedSetting(setting).Variable;
        var allowed = new ValidateSetAttribute("Continue", "Stop");
        var transform = new TranslatePreferenceAttribute();
        variable.Attributes.Add(allowed);
        variable.Attributes.Add(transform);

        variable.Value = "halt";
        Assert.Equal(ActionPreference.Stop, scope.GetScopedSettingValue(setting));
        Assert.Throws<ValidationMetadataException>(() => scope.SetScopedSettingValue(setting, ActionPreference.Ignore));
        Assert.Throws<PSInvalidCastException>(() => variable.Attributes.Add(new ReplacePreferenceAttribute("invalid")));
        Assert.Throws<ValidationMetadataException>(() => variable.Attributes[1] = new ReplacePreferenceAttribute("Ignore"));
        Assert.Same(allowed, variable.Attributes[0]);
        Assert.Same(transform, variable.Attributes[1]);
        Assert.Equal(2, variable.Attributes.Count);
        Assert.Equal(ActionPreference.Stop, variable.Value);
    }

    [Theory]
    [MemberData(nameof(PreferencePaths))]
    public void AdapterPreservesProtectionAndAppliesForceWithoutChangingOptions(string path)
    {
        var setting = GetPreferenceSetting(path);
        var scope = new SessionStateScope(null);
        var source = new PSVariable(setting.VariableName, "Continue", ScopedItemOptions.ReadOnly);
        scope.InitializeScopedSettingParameter(setting, source, sessionState: null);
        var entry = scope.GetScopedSetting(setting);

        Assert.Throws<SessionStateUnauthorizedAccessException>(() => scope.SetScopedSettingValue(setting, ActionPreference.Stop));
        Assert.Throws<SessionStateUnauthorizedAccessException>(() => scope.ResetScopedSetting(setting));
        Assert.Throws<PSInvalidCastException>(() => entry.Variable.SetValue(null, force: true));
        Assert.Equal(ScopedItemOptions.ReadOnly, entry.Variable.Options);
        Assert.Equal(ActionPreference.Continue, entry.Value);

        entry.Variable.SetValue("Stop", force: true);
        Assert.Equal(ActionPreference.Stop, scope.GetScopedSettingValue(setting));
        scope.RemoveScopedSettingVariable(setting, force: true);
        Assert.True(entry.Variable.WasRemoved);
        Assert.Null(entry.Variable.Value);
        Assert.Equal(ScopedItemOptions.None, entry.Variable.Options);
        entry.Variable.Value = "detached";
        Assert.Equal("detached", entry.Variable.Value);
        Assert.Equal(setting.DefaultValue, scope.GetScopedSettingValue(setting));

        source = new PSVariable(setting.VariableName, "Continue", ScopedItemOptions.Constant);
        scope.InitializeScopedSettingParameter(setting, source, sessionState: null);
        entry = scope.GetScopedSetting(setting);
        Assert.Throws<SessionStateUnauthorizedAccessException>(() => entry.Variable.SetValue("Stop", force: true));
        Assert.Throws<SessionStateUnauthorizedAccessException>(() => scope.RemoveScopedSettingVariable(setting, force: true));
        Assert.Equal(ActionPreference.Continue, entry.Value);
    }

    [Theory]
    [MemberData(nameof(PreferencePaths))]
    public void ImportAndReplacementPreserveMetadataAndRejectInvalidUpdatesAtomically(string path)
    {
        var setting = GetPreferenceSetting(path);
        var source = new PSVariable(setting.VariableName, "Continue", ScopedItemOptions.Private)
        {
            Description = "original",
            Visibility = SessionStateEntryVisibility.Private,
        };
        var originalAttribute = new ValidateSetAttribute("Continue");
        source.Attributes.Add(originalAttribute);
        var entry = ScopedSettingEntry<ActionPreference>.Create(setting, source);
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

    [Theory]
    [MemberData(nameof(PreferencePaths))]
    public void SettingLookupSkipsPrivateAncestors(string path)
    {
        var setting = GetPreferenceSetting(path);
        var parent = new SessionStateScope(null);
        var child = new SessionStateScope(parent);
        var grandchild = new SessionStateScope(child);
        parent.SetScopedSettingValue(setting, ActionPreference.Stop);
        var source = new PSVariable(setting.VariableName, "SilentlyContinue", ScopedItemOptions.Private);
        child.InitializeScopedSettingParameter(setting, source, sessionState: null);

        Assert.Equal(ActionPreference.SilentlyContinue, child.GetScopedSettingValue(setting));
        Assert.Equal(ActionPreference.Stop, grandchild.GetScopedSettingValue(setting));
    }

    [Theory]
    [MemberData(nameof(PreferencePaths))]
    public void AllScopeBindingsShareValueAndMetadataUntilTheLastRemoval(string path)
    {
        var setting = GetPreferenceSetting(path);
        var parent = new SessionStateScope(null);
        var child = new SessionStateScope(parent);
        var scopeThatExits = new SessionStateScope(parent);
        var source = new PSVariable(setting.VariableName, "Stop", ScopedItemOptions.AllScope);
        parent.InitializeScopedSettingParameter(setting, source, sessionState: null);
        var entry = parent.GetScopedSetting(setting);

        child.SetScopedSettingValue(setting, "SilentlyContinue");
        Assert.Same(entry, child.GetScopedSetting(setting));
        Assert.Equal(ActionPreference.SilentlyContinue, parent.GetScopedSettingValue(setting));
        child.GetScopedSetting(setting).Variable.Description = "shared";
        Assert.Equal("shared", entry.Variable.Description);

        child.ResetScopedSetting(setting);
        Assert.Null(child.GetScopedSetting(setting));
        Assert.False(entry.Variable.WasRemoved);
        Assert.Equal(ActionPreference.SilentlyContinue, child.GetScopedSettingValue(setting));

        Assert.Same(entry, scopeThatExits.GetScopedSetting(setting));
        scopeThatExits.MarkRemoved();
        Assert.False(entry.Variable.WasRemoved);
        Assert.Equal(ActionPreference.SilentlyContinue, entry.Variable.Value);

        parent.ResetScopedSetting(setting);
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
