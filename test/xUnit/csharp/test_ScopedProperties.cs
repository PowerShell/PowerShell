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
            Assert.Equal(ProgressView.Minimal, setting.GetValue(child));

            setting.SetValue(parent, 1);
            setting.Reset(child);
            Assert.Null(child.GetScopedSetting(setting));
            Assert.Equal(ProgressView.Classic, setting.GetValue(child));

            setting.SetValue(child, ProgressView.Classic);
            var entry = child.GetScopedSetting(setting);
            Assert.NotNull(entry);
            setting.SetValue(child, "minimal");
            Assert.Same(entry, child.GetScopedSetting(setting));
            Assert.Equal(ProgressView.Minimal, setting.GetValue(child));
            Assert.Equal(ProgressView.Classic, setting.GetValue(parent));

            setting.Reset(child);
            Assert.Null(child.GetScopedSetting(setting));
            Assert.Equal(ProgressView.Classic, setting.GetValue(child));
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
            setting.SetValue(parent, ProgressView.Classic);

            Assert.Throws<PSInvalidCastException>(() => setting.SetValue(child, value));
            Assert.Null(child.GetScopedSetting(setting));
            Assert.Equal(ProgressView.Classic, setting.GetValue(child));

            setting.SetValue(child, ProgressView.Minimal);
            var entry = child.GetScopedSetting(setting);
            Assert.Throws<PSInvalidCastException>(() => setting.SetValue(child, value));
            Assert.Same(entry, child.GetScopedSetting(setting));
            Assert.Equal(ProgressView.Minimal, setting.GetValue(child));
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
