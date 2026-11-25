// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal.Host;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using Xunit;

namespace PSTests.Sequential;

[Cmdlet(VerbsLifecycle.Invoke, "ScopedQuestionMarkWriteError")]
public sealed class ScopedQuestionMarkWriteErrorCommand : PSCmdlet
{
    protected override void ProcessRecord()
    {
        WriteError(new ErrorRecord(new InvalidOperationException("expected question-mark probe error"),
            "ScopedQuestionMarkProbeError", ErrorCategory.NotSpecified, null));
    }
}

[Cmdlet(VerbsLifecycle.Invoke, "ScopedNestedPrompt")]
public sealed class ScopedNestedPromptCommand : PSCmdlet
{
    protected override void ProcessRecord() => ((InternalHost)Host).EnterNestedPrompt(this);
}

public sealed class ScopedRuntimePropertiesTests
{
    public static IEnumerable<object[]> QuestionMarkExpressionCases()
    {
        string nativeCommand = OperatingSystem.IsWindows() ? "cmd.exe /d /c exit 7" : "/bin/sh -c 'exit 7'";
        var triggers = new (string Name, string Script, bool Expected)[]
        {
            ("success", "$null = 1", true),
            ("Write-Error", "Write-Error 'expected direct error' -ErrorAction Continue", false),
            ("function WriteError", "function Invoke-ScopedFunctionWriteError { Write-Error 'expected function error' -ErrorAction Continue }; Invoke-ScopedFunctionWriteError", true),
            ("PSCmdlet.WriteError", "Invoke-ScopedQuestionMarkWriteError -ErrorAction Continue", false),
            ("native exit 7", nativeCommand, false),
        };
        var expressions = new (string Name, string Legacy, string Facade)[]
        {
            ("direct", "$?", "$facade.LastStatementSuccessful"),
            ("parentheses", "($?)", "($facade.LastStatementSuccessful)"),
            ("subexpression", "$($?)", "$($facade.LastStatementSuccessful)"),
            ("array", "@($?)", "@($facade.LastStatementSuccessful)"),
            ("interpolation", "\"$?\"", "\"$($facade.LastStatementSuccessful)\""),
        };

        foreach (var trigger in triggers)
        {
            foreach (var expression in expressions)
            {
                yield return new object[] { trigger.Name, trigger.Script, expression.Name, expression.Legacy, expression.Facade, trigger.Expected };
            }
        }
    }

    [Theory]
    [MemberData(nameof(QuestionMarkExpressionCases))]
    public void LastStatementSuccessfulMatchesQuestionMarkInEveryExpressionForm(
        string triggerName,
        string trigger,
        string expressionName,
        string legacyExpression,
        string facadeExpression,
        bool expected)
    {
        PSObject legacy = InvokeQuestionMarkProbe(triggerName, trigger, legacyExpression);
        PSObject facade = InvokeQuestionMarkProbe(triggerName, trigger, facadeExpression);

        object expectedValue = expressionName == "interpolation" ? expected.ToString() : expected;
        Assert.Equal(expectedValue, legacy.BaseObject);
        Assert.Equal(legacy.BaseObject, facade.BaseObject);
    }

    [Theory]
    [MemberData(nameof(QuestionMarkTriggerCases))]
    public void CapturedFacadeReadsTheLiveQuestionMarkState(string triggerName, string trigger, bool expected)
    {
        PSObject actual = InvokeQuestionMarkProbe(triggerName, trigger, "$captured.LastStatementSuccessful", captureFacade: true);
        Assert.Equal(expected, actual.BaseObject);
    }

    public static IEnumerable<object[]> QuestionMarkTriggerCases()
    {
        foreach (object[] row in QuestionMarkExpressionCases())
        {
            if ((string)row[2] == "direct")
            {
                yield return new object[] { row[0], row[1], row[5] };
            }
        }
    }

    [Fact]
    public void NativeExitCodeRemainsIndependentFromQuestionMarkFacade()
    {
        string nativeCommand = OperatingSystem.IsWindows() ? "cmd.exe /d /c exit 7" : "/bin/sh -c 'exit 7'";
        PSObject result = InvokeQuestionMarkProbe("native exit 7", nativeCommand, "[pscustomobject]@{ Success = $facade.LastStatementSuccessful; ExitCode = $LASTEXITCODE }");
        Assert.False((bool)result.Properties["Success"].Value);
        Assert.Equal(7, result.Properties["ExitCode"].Value);
    }

    private static PSObject InvokeQuestionMarkProbe(string triggerName, string trigger, string expression, bool captureFacade = false)
    {
        InitialSessionState initialState = InitialSessionState.CreateDefault2();
        initialState.Commands.Add(new SessionStateCmdletEntry(
            "Invoke-ScopedQuestionMarkWriteError",
            typeof(ScopedQuestionMarkWriteErrorCommand),
            helpFileName: null));

        using Runspace runspace = RunspaceFactory.CreateRunspace(initialState);
        runspace.Open();
        runspace.SessionStateProxy.SetVariable("facade", new PSScopedPropertyFacade());
        using PowerShell shell = PowerShell.Create();
        shell.Runspace = runspace;
        string capture = captureFacade ? "$captured = $facade; " : string.Empty;
        Collection<PSObject> output = shell.AddScript($"$ErrorActionPreference = 'Continue'; {capture}{trigger}; {expression}").Invoke();
        if (triggerName is "Write-Error" or "function WriteError" or "PSCmdlet.WriteError")
        {
            Assert.NotEmpty(shell.Streams.Error);
        }
        else
        {
            Assert.Empty(shell.Streams.Error);
        }

        return Assert.Single(output);
    }

    [Fact]
    public void DebugContextExistsOnlyDuringDebuggerStop()
    {
        using Runspace runspace = RunspaceFactory.CreateRunspace();
        runspace.Open();
        runspace.Debugger.SetDebugMode(DebugModes.LocalScript);
        var facade = new PSScopedPropertyFacade();
        runspace.SessionStateProxy.SetVariable("facade", facade);

        using PowerShell shell = PowerShell.Create();
        shell.Runspace = runspace;
        shell.AddScript("$null -eq (Get-Variable PSDebugContext -ErrorAction Ignore); $null -eq $facade.Diagnostics.DebugContext");
        Collection<PSObject> beforeStop = shell.Invoke();
        Assert.Equal("True", beforeStop[0].BaseObject.ToString());
        Assert.Equal("True", beforeStop[1].BaseObject.ToString());
        shell.Commands.Clear();

        bool sawStop = false;
        string legacyAndFacadeDuringStop = null;
        EventHandler<DebuggerStopEventArgs> onStop = (sender, args) =>
        {
            sawStop = true;
            var debugger = (Debugger)sender;
            var command = new PSCommand();
            command.AddScript("$null -ne $PSDebugContext; [object]::ReferenceEquals($PSDebugContext, $facade.Diagnostics.DebugContext)");
            var output = new PSDataCollection<PSObject>();
            debugger.ProcessCommand(command, output);
            legacyAndFacadeDuringStop = string.Join(",", output.Select(item => item.BaseObject.ToString()));
            args.ResumeAction = DebuggerResumeAction.Continue;
        };

        runspace.Debugger.DebuggerStop += onStop;
        CommandBreakpoint breakpoint = runspace.Debugger.SetCommandBreakpoint("Write-Output", null, null);
        try
        {
            shell.AddScript("Write-Output 'debug-stop'; 'completed'");
            Collection<PSObject> result = shell.Invoke();
            Assert.Empty(shell.Streams.Error);
            Assert.True(sawStop);
            Assert.Equal("True,True", legacyAndFacadeDuringStop);
            Assert.Contains(result, item => item.BaseObject?.ToString() == "completed");

            shell.Commands.Clear();
            shell.AddScript("$null -eq (Get-Variable PSDebugContext -ErrorAction Ignore); $null -eq $facade.Diagnostics.DebugContext");
            Collection<PSObject> afterResume = shell.Invoke();
            Assert.Equal("True", afterResume[0].BaseObject.ToString());
            Assert.Equal("True", afterResume[1].BaseObject.ToString());
        }
        finally
        {
            runspace.Debugger.DebuggerStop -= onStop;
            runspace.Debugger.RemoveBreakpoint(breakpoint);
        }
    }

    [Fact]
    public void CurrentlyExecutingCommandAndNestedPromptLevelRestoreAfterHostPrompt()
    {
        var host = new NestedPromptProbeHost();
        var initialState = InitialSessionState.CreateDefault2();
        initialState.Commands.Add(new SessionStateCmdletEntry("Invoke-ScopedNestedPrompt", typeof(ScopedNestedPromptCommand), null));
        using Runspace runspace = RunspaceFactory.CreateRunspace(host, initialState);
        host.Runspace = runspace;
        runspace.Open();
        runspace.SessionStateProxy.SetVariable("facade", new PSScopedPropertyFacade());
        runspace.SessionStateProxy.SetVariable("CurrentlyExecutingCommand", new object());

        using PowerShell shell = PowerShell.Create();
        shell.Runspace = runspace;
        shell.AddScript("$beforeCommandBinding = Get-Variable CurrentlyExecutingCommand -ErrorAction Ignore; $beforeCommand = $facade.Session.CurrentlyExecutingCommand; $beforeLevel = $facade.Session.NestedPromptLevel; Invoke-ScopedNestedPrompt; $afterCommandBinding = Get-Variable CurrentlyExecutingCommand -ErrorAction Ignore; $afterCommand = $facade.Session.CurrentlyExecutingCommand; $afterLevel = $facade.Session.NestedPromptLevel; [pscustomobject]@{ CommandRestored = [object]::ReferenceEquals($beforeCommand, $afterCommand); LevelRestored = $beforeLevel -eq $afterLevel; BindingRestored = (($null -ne $beforeCommandBinding) -eq ($null -ne $afterCommandBinding)) }");
        Collection<PSObject> results = shell.Invoke();

        Assert.Empty(shell.Streams.Error);
        Assert.Equal(1, host.EnterCount);
        PSObject afterPrompt = Assert.Single(results);
        Assert.True((bool)afterPrompt.Properties["CommandRestored"].Value);
        Assert.True((bool)afterPrompt.Properties["LevelRestored"].Value);
        Assert.True((bool)afterPrompt.Properties["BindingRestored"].Value);
        Assert.NotNull(host.NestedPromptLevelAtEntry);
        Assert.True(Convert.ToInt32(host.NestedPromptLevelAtEntry, CultureInfo.InvariantCulture) > 0);
        Assert.Same(host.NestedCommandAtEntry, host.NestedCommandAtEntryFacade);
        Assert.NotNull(host.NestedCommandAtEntry);
        Assert.Equal(host.NestedPromptLevelAtEntry, host.NestedPromptLevelAtEntryFacade);
    }

    [Fact]
    public void SenderInfoIsAbsentOutsideRemotingAndTracksProducerInstallationAndRemoval()
    {
        using Runspace runspace = RunspaceFactory.CreateRunspace();
        runspace.Open();
        runspace.SessionStateProxy.SetVariable("facade", new PSScopedPropertyFacade());

        using PowerShell shell = PowerShell.Create();
        shell.Runspace = runspace;
        shell.AddScript("$null -eq (Get-Variable PSSenderInfo -ErrorAction Ignore); $null -eq $facade.Remoting.SenderInfo");
        Collection<PSObject> absent = shell.Invoke();
        Assert.Equal("True", absent[0].BaseObject.ToString());
        Assert.Equal("True", absent[1].BaseObject.ToString());

        // This uses the engine's sender-info factory to exercise the producer
        // lifecycle without opening a WinRM/SSH transport.
        PSSenderInfo senderInfo = System.Management.Automation.Internal.InternalTestHooks.GetCustomPSSenderInfo(
            "https://runtime-probe", new Version(7, 5));
        runspace.SessionStateProxy.SetVariable("PSSenderInfo", senderInfo);
        shell.Commands.Clear();
        shell.AddScript("[object]::ReferenceEquals($PSSenderInfo, $facade.Remoting.SenderInfo)");
        Collection<PSObject> installed = shell.Invoke();
        Assert.True((bool)installed[0].BaseObject);

        runspace.SessionStateProxy.PSVariable.Remove("PSSenderInfo");
        shell.Commands.Clear();
        shell.AddScript("$null -eq (Get-Variable PSSenderInfo -ErrorAction Ignore); $null -eq $facade.Remoting.SenderInfo");
        Collection<PSObject> removed = shell.Invoke();
        Assert.Equal("True", removed[0].BaseObject.ToString());
        Assert.Equal("True", removed[1].BaseObject.ToString());
    }

    private sealed class NestedPromptProbeHost : PSHost
    {
        private readonly Guid _instanceId = Guid.NewGuid();

        internal Runspace Runspace { get; set; }

        internal int EnterCount { get; private set; }

        internal object NestedCommandAtEntry { get; private set; }

        internal object NestedCommandAtEntryFacade { get; private set; }

        internal object NestedPromptLevelAtEntry { get; private set; }

        internal object NestedPromptLevelAtEntryFacade { get; private set; }

        public override CultureInfo CurrentCulture => CultureInfo.CurrentCulture;

        public override CultureInfo CurrentUICulture => CultureInfo.CurrentUICulture;

        public override Guid InstanceId => _instanceId;

        public override string Name => "Scoped runtime nested prompt probe";

        public override PSHostUserInterface UI => null;

        public override Version Version => new(1, 0);

        public override void EnterNestedPrompt()
        {
            EnterCount++;
            using Pipeline pipeline = Runspace.CreateNestedPipeline();
            pipeline.Commands.AddScript("[pscustomobject]@{ LegacyCommand = $CurrentlyExecutingCommand; FacadeCommand = $facade.Session.CurrentlyExecutingCommand; LegacyLevel = $NestedPromptLevel; FacadeLevel = $facade.Session.NestedPromptLevel }");
            PSObject nestedState = Assert.Single(pipeline.Invoke());
            NestedCommandAtEntry = nestedState.Properties["LegacyCommand"].Value;
            NestedCommandAtEntryFacade = nestedState.Properties["FacadeCommand"].Value;
            NestedPromptLevelAtEntry = nestedState.Properties["LegacyLevel"].Value;
            NestedPromptLevelAtEntryFacade = nestedState.Properties["FacadeLevel"].Value;

            using Pipeline exitPipeline = Runspace.CreateNestedPipeline();
            exitPipeline.Commands.AddScript("$Host.ExitNestedPrompt()");
            exitPipeline.Invoke();
        }

        public override void ExitNestedPrompt() { }

        public override void NotifyBeginApplication() { }

        public override void NotifyEndApplication() { }

        public override void SetShouldExit(int exitCode) { }
    }
}
