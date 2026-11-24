// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Xunit;

namespace PSTests.Sequential
{
    public class ScopedPropertiesTests
    {
        [Fact]
        public void DescriptorLookupUsesCaseInsensitivePathsAndFeatureGatedVariableNames()
        {
            var error = ScopedProperties.ErrorPreferenceSetting;
            Assert.Contains(error, ScopedProperties.PreferenceSettings);
            Assert.Equal(PreferenceVariable.Error, error.Index);
            Assert.True(ScopedProperties.TryGetSetting("actionpreference.ERROR", out var setting));
            Assert.Same(error, setting);
            Assert.True(ScopedProperties.TryGetSetting("style.progress.VIEW", out setting));
            Assert.Same(ScopedProperties.ProgressViewSetting, setting);
            Assert.False(ScopedProperties.TryGetSetting("ActionPreference.Progress", out _));

            bool found = ScopedProperties.TryGetPreferenceSetting("erroractionPREFERENCE", out var preference);
            Assert.Equal(ExperimentalFeature.PSScopedProperties, found);
            Assert.Same(found ? error : null, preference);
            Assert.False(ScopedProperties.TryGetPreferenceSetting("ProgressPreference", out _));
        }

        [Fact]
        public void FacadeRequiresExecutionContextAndLegacyStyleKeepsItsContextFreeValue()
        {
            Runspace previousRunspace = Runspace.DefaultRunspace;
            Runspace.DefaultRunspace = null;
            try
            {
                var facade = new PSScopedPropertyFacade();
                Assert.Throws<InvalidOperationException>(() => facade.ActionPreference);
                Assert.Throws<InvalidOperationException>(() => facade.Style);
                Assert.Throws<InvalidOperationException>(() => facade.AtScope("Local"));
                Assert.Throws<InvalidOperationException>(() => facade.Reset("ActionPreference.Error"));

                var legacyProgress = PSStyle.Instance.Progress;
                ProgressView original = legacyProgress.View;
                try
                {
                    legacyProgress.View = ProgressView.Classic;
                    Assert.Equal(ProgressView.Classic, legacyProgress.View);

                    using var runspace = RunspaceFactory.CreateRunspace();
                    runspace.Open();
                    runspace.SessionStateProxy.SetVariable("facade", facade);
                    using var shell = PowerShell.Create();
                    shell.Runspace = runspace;
                    var result = Assert.Single(shell.AddScript("$facade.Style.Progress.View").Invoke<ProgressView>());
                    Assert.Equal(ProgressView.Minimal, result);
                }
                finally
                {
                    legacyProgress.View = original;
                }
            }
            finally
            {
                Runspace.DefaultRunspace = previousRunspace;
            }
        }

        [Fact]
        public void ScopeBoundFacadesRejectForeignRunspacesAndExitedScopes()
        {
            using var firstRunspace = RunspaceFactory.CreateRunspace();
            firstRunspace.Open();
            firstRunspace.SessionStateProxy.SetVariable("facade", new PSScopedPropertyFacade());
            using var shell = PowerShell.Create();
            shell.Runspace = firstRunspace;

            var globalView = Assert.Single(shell.AddScript("$facade.AtScope('Global')").Invoke<PSScopedPropertyFacade>());
            shell.Commands.Clear();
            var expiredView = Assert.Single(shell.AddScript("& { $facade.AtScope('Local') }").Invoke<PSScopedPropertyFacade>());
            firstRunspace.SessionStateProxy.SetVariable("view", expiredView);
            shell.Commands.Clear();
            var expired = Assert.Single(shell.AddScript("try { $view.Style } catch { $_.Exception.GetBaseException().Message }").Invoke<string>());
            Assert.Equal(SessionStateStrings.ExpiredScopedSettingScope, expired);

            using var secondRunspace = RunspaceFactory.CreateRunspace();
            secondRunspace.Open();
            secondRunspace.SessionStateProxy.SetVariable("view", globalView);
            using var foreignShell = PowerShell.Create();
            foreignShell.Runspace = secondRunspace;
            var foreign = Assert.Single(foreignShell.AddScript("try { $view.ActionPreference } catch { $_.Exception.GetBaseException().Message }").Invoke<string>());
            Assert.Equal(SessionStateStrings.ForeignScopedSettingScope, foreign);
        }

        [Fact]
        public void ProgressResetRemovesTheLocalEntryAndResumesInheritance()
        {
            var setting = ScopedProperties.ProgressViewSetting;
            var parent = new SessionStateScope(null);
            var child = new SessionStateScope(parent);

            Assert.Null(parent.GetScopedSetting(setting));
            Assert.Equal(ProgressView.Minimal, child.GetScopedSettingValue(setting));

            parent.SetScopedSettingValue(setting, 1);
            child.ResetScopedSetting(setting);
            Assert.Null(child.GetScopedSetting(setting));
            Assert.Equal(ProgressView.Classic, child.GetScopedSettingValue(setting));

            child.SetScopedSettingValue(setting, ProgressView.Classic);
            var entry = child.GetScopedSetting(setting);
            Assert.NotNull(entry);
            child.SetScopedSettingValue(setting, "minimal");
            Assert.Same(entry, child.GetScopedSetting(setting));
            Assert.Equal(ProgressView.Minimal, child.GetScopedSettingValue(setting));
            Assert.Equal(ProgressView.Classic, parent.GetScopedSettingValue(setting));

            child.ResetScopedSetting(setting);
            Assert.Null(child.GetScopedSetting(setting));
            Assert.Equal(ProgressView.Classic, child.GetScopedSettingValue(setting));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(500)]
        [InlineData("invalid view")]
        public void InvalidProgressAssignmentsPreserveExistingEntriesAndInheritance(object value)
        {
            var setting = ScopedProperties.ProgressViewSetting;
            var parent = new SessionStateScope(null);
            var child = new SessionStateScope(parent);
            parent.SetScopedSettingValue(setting, ProgressView.Classic);

            Assert.Throws<PSInvalidCastException>(() => child.SetScopedSettingValue(setting, value));
            Assert.Null(child.GetScopedSetting(setting));
            Assert.Equal(ProgressView.Classic, child.GetScopedSettingValue(setting));

            child.SetScopedSettingValue(setting, ProgressView.Minimal);
            var entry = child.GetScopedSetting(setting);
            Assert.Throws<PSInvalidCastException>(() => child.SetScopedSettingValue(setting, value));
            Assert.Same(entry, child.GetScopedSetting(setting));
            Assert.Equal(ProgressView.Minimal, child.GetScopedSettingValue(setting));
        }

        [Fact]
        public void ProgressFacadeTargetsLocalScriptAndGlobalScopes()
        {
            using var runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            runspace.SessionStateProxy.SetVariable("facade", new PSScopedPropertyFacade());

            using var shell = PowerShell.Create();
            shell.Runspace = runspace;
            var result = shell.AddScript("""
                $globalView = $facade.AtScope('Global')
                $globalView.Style.Progress.View = 'Classic'
                $module = New-Module {}
                & $module {
                    $scriptView = $facade.AtScope('Script')
                    $scriptView.Style.Progress.View = 'Minimal'
                    & {
                        $localView = $facade.AtScope('Local')
                        $localView.Style.Progress.View = 'Classic'
                        "$($localView.Style.Progress.View)/$($scriptView.Style.Progress.View)/$($globalView.Style.Progress.View)"
                        $localView.Reset('Style.Progress.View')
                        "$($localView.Style.Progress.View)/$($scriptView.Style.Progress.View)/$($globalView.Style.Progress.View)"
                        $scriptView.Reset('Style.Progress.View')
                        "$($localView.Style.Progress.View)/$($scriptView.Style.Progress.View)/$($globalView.Style.Progress.View)"
                    }
                }
                $globalView.Reset('Style.Progress.View')
                $globalView.Style.Progress.View.ToString()
                """).Invoke<string>();

            Assert.Empty(shell.Streams.Error);
            Assert.Equal(new[] { "Classic/Minimal/Classic", "Minimal/Minimal/Classic", "Classic/Classic/Classic", "Minimal" }, result);
        }
    }
}
