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
    }
}
