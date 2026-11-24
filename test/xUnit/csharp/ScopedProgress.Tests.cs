// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.PowerShell;
using PSTests.Parallel;
using Xunit;

namespace PSTests.Sequential
{
    public static class ScopedProgressTests
    {
        [Fact]
        public static void PendingProgressUsesLatestViewAfterOriginatingScopeEnds()
        {
            var rawUI = new TestProgressRawUI();
            var progress = new PendingProgress();
            var first = new ProgressRecord(0, "First activity", "In progress");
            var second = new ProgressRecord(1, "Second activity", "In progress");

            using var runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            runspace.SessionStateProxy.SetVariable(
                "getProgressView",
                (Func<ProgressView>)(() => ScopedProperties.GetProgressView(LocalPipeline.GetExecutionContextFromTLS())));
            runspace.SessionStateProxy.SetVariable(
                "reportProgress",
                (Action<ProgressView>)(view =>
                {
                    var context = LocalPipeline.GetExecutionContextFromTLS();
                    ScopedProperties.SetProgressView(context, view);
                    progress.Update(0, second, ScopedProperties.GetProgressView(context));
                }));

            progress.Update(0, first, ProgressView.Minimal);
            using var shell = PowerShell.Create();
            shell.Runspace = runspace;
            shell.AddScript("& { $reportProgress.Invoke('Classic') }; $getProgressView.Invoke()");
            Assert.Equal(ProgressView.Minimal, Assert.Single(shell.Invoke<ProgressView>()));
            Assert.Empty(shell.Streams.Error);

            Assert.Equal(ProgressView.Classic, progress.View);
            string[] classic = progress.Render(80, 20, rawUI);
            Assert.True(classic.Length > 2);
            Assert.Contains(classic, line => line.Contains(first.Activity, StringComparison.Ordinal));
            Assert.Contains(classic, line => line.Contains(second.Activity, StringComparison.Ordinal));
            Assert.All(classic, line => Assert.DoesNotContain('\u001b', line));

            // The most recent record can be an update to an existing node.
            shell.Commands.Clear();
            shell.AddScript("$reportProgress.Invoke('Minimal')");
            Assert.Empty(shell.Invoke());
            Assert.Empty(shell.Streams.Error);

            Assert.Equal(ProgressView.Minimal, progress.View);
            string[] minimal = progress.Render(80, 20, rawUI);
            Assert.Equal(2, minimal.Length);
            Assert.All(minimal, line => Assert.Contains(PSStyle.Instance.Reset, line));
        }

        [Fact]
        public static void TerminalFallbackSelectsClassicWithoutChangingPreference()
        {
            PSStyle.ProgressConfiguration progress = PSStyle.Instance.Progress;
            ProgressView original = progress.View;
            try
            {
                progress.View = ProgressView.Minimal;
                Assert.Equal(ProgressView.Classic, ConsoleHostUserInterface.GetProgressViewForRendering(supportsVirtualTerminal: false));
                Assert.Equal(ProgressView.Minimal, progress.View);
                Assert.Equal(ProgressView.Minimal, ConsoleHostUserInterface.GetProgressViewForRendering(supportsVirtualTerminal: true));
            }
            finally
            {
                progress.View = original;
            }
        }
    }
}
