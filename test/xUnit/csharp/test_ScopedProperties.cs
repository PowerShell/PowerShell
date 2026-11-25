// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using Xunit;

namespace PSTests.Sequential
{
    [Cmdlet(VerbsCommon.Get, "ScopedRemotingProbe")]
    public sealed class ScopedRemotingProbeCmdlet : Microsoft.PowerShell.Commands.PSRemotingCmdlet
    {
        protected override void BeginProcessing()
        {
            // This probe exercises preference resolution without starting a remoting transport.
        }

        protected override void ProcessRecord()
        {
            WriteObject(string.Join("|",
                ResolveShell(null),
                ResolveAppName(null),
                ResolveShell("ExplicitShell"),
                ResolveAppName("ExplicitApp")));
        }
    }

    public class ScopedPropertiesTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CmdletTupleRetainsFixedPreferenceIndices(bool disableOptimizations)
        {
            var ast = Parser.ParseInput("[CmdletBinding()] param() $localValue = 123; $localValue", out _, out var errors);
            Assert.Empty(errors);

            var (tupleType, nameToIndex) = VariableAnalysis.Analyze(ast, disableOptimizations, scriptCmdlet: true);
            var locals = MutableTuple.MakeTuple(tupleType, nameToIndex);
            for (int i = 0; i < SpecialVariables.PreferenceVariables.Length; i++)
            {
                string name = SpecialVariables.PreferenceVariables[i];
                int index = i + (int)AutomaticVariable.NumberOfAutomaticVariables;
                if (ScopedProperties.TryGetConfigurationSetting(name, out var setting))
                {
                    Assert.Equal(index, (int)setting.Index.Value);
                    Assert.False(nameToIndex.ContainsKey(name));
                    Assert.Null(locals.GetValue(index));
                }
                else
                {
                    Assert.Equal(index, nameToIndex[name]);
                }
            }

            if (!disableOptimizations)
            {
                Assert.Equal((int)PreferenceVariable.Progress + 1, nameToIndex["localValue"]);
            }
        }

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
            Assert.True(ScopedProperties.TryGetSetting("ActionPreference.Progress", out setting));
            Assert.Same(ScopedProperties.ProgressPreferenceSetting, setting);
            Assert.True(ScopedProperties.TryGetSetting("ShouldProcess.Confirm", out setting));
            Assert.Same(ScopedProperties.ConfirmPreferenceSetting, setting);
            Assert.True(ScopedProperties.TryGetSetting("ShouldProcess.WhatIf", out setting));
            Assert.Same(ScopedProperties.WhatIfPreferenceSetting, setting);

            bool found = ScopedProperties.TryGetPreferenceSetting("erroractionPREFERENCE", out var preference);
            Assert.Equal(ExperimentalFeature.PSScopedProperties, found);
            Assert.Same(found ? error : null, preference);
            bool progressFound = ScopedProperties.TryGetPreferenceSetting("ProgressPreference", out var progress);
            Assert.Equal(ExperimentalFeature.PSScopedProperties, progressFound);
            Assert.Same(progressFound ? ScopedProperties.ProgressPreferenceSetting : null, progress);
            bool confirmFound = ScopedProperties.TryGetConfigurationSetting("ConfirmPreference", out setting);
            Assert.Equal(ExperimentalFeature.PSScopedProperties, confirmFound);
            Assert.Same(confirmFound ? ScopedProperties.ConfirmPreferenceSetting : null, setting);
            bool whatIfFound = ScopedProperties.TryGetConfigurationSetting("WhatIfPreference", out setting);
            Assert.Equal(ExperimentalFeature.PSScopedProperties, whatIfFound);
            Assert.Same(whatIfFound ? ScopedProperties.WhatIfPreferenceSetting : null, setting);
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
                Assert.Throws<InvalidOperationException>(() => facade.ShouldProcess);
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
            var globalShouldProcess = Assert.Single(shell.AddScript("$facade.AtScope('Global').ShouldProcess").Invoke<PSScopedShouldProcess>());
            shell.Commands.Clear();
            var expiredView = Assert.Single(shell.AddScript("& { $facade.AtScope('Local') }").Invoke<PSScopedPropertyFacade>());
            firstRunspace.SessionStateProxy.SetVariable("view", expiredView);
            shell.Commands.Clear();
            var expired = Assert.Single(shell.AddScript("try { $view.Style } catch { $_.Exception.GetBaseException().Message }").Invoke<string>());
            Assert.Equal(SessionStateStrings.ExpiredScopedSettingScope, expired);
            shell.Commands.Clear();
            var expiredShouldProcess = Assert.Single(shell.AddScript("& { $facade.AtScope('Local').ShouldProcess }").Invoke<PSScopedShouldProcess>());
            firstRunspace.SessionStateProxy.SetVariable("group", expiredShouldProcess);
            shell.Commands.Clear();
            var expiredGroupMember = Assert.Single(shell.AddScript("try { $group.WhatIf } catch { $_.Exception.GetBaseException().Message }").Invoke<string>());
            Assert.Equal(SessionStateStrings.ExpiredScopedSettingScope, expiredGroupMember);

            using var secondRunspace = RunspaceFactory.CreateRunspace();
            secondRunspace.Open();
            secondRunspace.SessionStateProxy.SetVariable("view", globalView);
            secondRunspace.SessionStateProxy.SetVariable("group", globalShouldProcess);
            using var foreignShell = PowerShell.Create();
            foreignShell.Runspace = secondRunspace;
            var foreign = Assert.Single(foreignShell.AddScript("try { $view.ActionPreference } catch { $_.Exception.GetBaseException().Message }").Invoke<string>());
            Assert.Equal(SessionStateStrings.ForeignScopedSettingScope, foreign);
            foreignShell.Commands.Clear();
            var foreignGroupMember = Assert.Single(foreignShell.AddScript("try { $group.WhatIf } catch { $_.Exception.GetBaseException().Message }").Invoke<string>());
            Assert.Equal(SessionStateStrings.ForeignScopedSettingScope, foreignGroupMember);
        }

        [Fact]
        public void NullableLoggingSettingUsesConsumerFallbackAndQualifiedGlobalLookup()
        {
            if (!ExperimentalFeature.PSScopedProperties)
            {
                return;
            }

            using var runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            runspace.SessionStateProxy.SetVariable(
                "getSettingWithFallback",
                (Func<string, object, object>)((path, fallback) =>
                    LocalPipeline.GetExecutionContextFromTLS().GetVariableValue(new VariablePath(path), fallback)));

            using var shell = PowerShell.Create();
            shell.Runspace = runspace;
            shell.AddScript("""
                $global:LogEngineHealthEvent = $false
                & {
                    $PS.Logging.EngineHealthEvent = $null
                    [pscustomobject]@{
                        FacadeValue = $PS.Logging.EngineHealthEvent
                        VariableValue = $LogEngineHealthEvent
                        ConsumerFallback = $getSettingWithFallback.Invoke('LogEngineHealthEvent', $true)
                        QualifiedGlobal = $getSettingWithFallback.Invoke('global:LogEngineHealthEvent', $true)
                    }
                }
                """);

            var result = Assert.Single(shell.Invoke<PSObject>());
            Assert.Empty(shell.Streams.Error);
            Assert.Null(result.Properties["FacadeValue"].Value);
            Assert.Null(result.Properties["VariableValue"].Value);
            Assert.Equal(true, result.Properties["ConsumerFallback"].Value);
            Assert.Equal(false, result.Properties["QualifiedGlobal"].Value);
        }

        [Fact]
        public void RemotingConsumerResolvesScopedAndExplicitNamesWithoutTransport()
        {
            var initialState = InitialSessionState.CreateDefault2();
            initialState.Commands.Add(new SessionStateCmdletEntry(
                "Get-ScopedRemotingProbe", typeof(ScopedRemotingProbeCmdlet), null));

            using var runspace = RunspaceFactory.CreateRunspace(initialState);
            runspace.Open();
            using var shell = PowerShell.Create();
            shell.Runspace = runspace;

            string result;
            if (ExperimentalFeature.PSScopedProperties)
            {
                shell.AddScript("""
                    $global:PSSessionConfigurationName = 'GlobalConfiguration'
                    $global:PSSessionApplicationName = 'GlobalApplication'
                    & {
                        $PS.Remoting.ConfigurationName = 'ScopedConfiguration'
                        $PS.Remoting.ApplicationName = 'ScopedApplication'
                        Get-ScopedRemotingProbe
                    }
                    """);
                result = Assert.Single(shell.Invoke<string>());
                Assert.Equal("ScopedConfiguration|ScopedApplication|ExplicitShell|ExplicitApp", result);
            }
            else
            {
                shell.AddScript("""
                    $global:PSSessionConfigurationName = 'GlobalConfiguration'
                    $global:PSSessionApplicationName = 'GlobalApplication'
                    Get-ScopedRemotingProbe
                    """);
                result = Assert.Single(shell.Invoke<string>());
                Assert.Equal("GlobalConfiguration|GlobalApplication|ExplicitShell|ExplicitApp", result);
            }

            Assert.Empty(shell.Streams.Error);
        }

        [Fact]
        public void ApplicationPrivateDataStartupUsesGlobalRawSnapshotAndOpeningGate()
        {
            using var runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            using var shell = PowerShell.Create();
            shell.Runspace = runspace;

            var globalData = new System.Collections.Hashtable(StringComparer.OrdinalIgnoreCase)
            {
                ["marker"] = "global",
            };
            var childData = new System.Collections.Hashtable(StringComparer.OrdinalIgnoreCase)
            {
                ["marker"] = "child",
            };
            runspace.SessionStateProxy.SetVariable("globalData", globalData);
            runspace.SessionStateProxy.SetVariable("childData", childData);
            shell.AddScript("$global:PSApplicationPrivateData = $globalData; & { $PSApplicationPrivateData = $childData }");
            Assert.Empty(shell.Invoke());
            Assert.Empty(shell.Streams.Error);

            var current = new PSPrimitiveDictionary();
            current.Add("current", true);
            var openedData = ServerRunspacePoolDriver.ResolveStartupApplicationPrivateData(runspace, RunspacePoolState.Opened, current);
            Assert.Same(current, openedData);

            var startupData = ServerRunspacePoolDriver.ResolveStartupApplicationPrivateData(runspace, RunspacePoolState.Opening, current);
            Assert.NotSame(globalData, startupData);
            Assert.Equal("global", startupData["marker"]);

            shell.Commands.Clear();
            shell.AddScript("$global:PSApplicationPrivateData['marker'] = 'changed after startup'");
            Assert.Empty(shell.Invoke());
            Assert.Empty(shell.Streams.Error);
            Assert.Equal("global", startupData["marker"]);

            shell.Commands.Clear();
            shell.AddScript("$global:PSApplicationPrivateData = 'invalid until startup conversion'");
            Assert.Empty(shell.Invoke());
            Assert.Empty(shell.Streams.Error);
            Assert.Throws<PSInvalidCastException>(() =>
                ServerRunspacePoolDriver.ResolveStartupApplicationPrivateData(runspace, RunspacePoolState.Opening, current));

            shell.Commands.Clear();
            shell.AddScript("$global:PSApplicationPrivateData = $null");
            Assert.Empty(shell.Invoke());
            Assert.Empty(shell.Streams.Error);
            Assert.Same(current, ServerRunspacePoolDriver.ResolveStartupApplicationPrivateData(runspace, RunspacePoolState.Opening, current));
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
