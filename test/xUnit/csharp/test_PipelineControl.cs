// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable SA1649 // Follow the existing test_*.cs naming convention; CodeFactor does not honor the directory setting.
#pragma warning disable SA1402 // Keep test-only cmdlet helpers with the tests that use them.

namespace PSTests.Sequential
{
    public class PipelineControlTests
    {
        /// <summary>
        /// Verifies that the exception filter recognizes engine control flow through
        /// runtime and reflection wrappers, excludes ordinary errors, and rejects null.
        /// </summary>
        [Fact]
        public void EngineExceptionFilterRecognizesControlFlow()
        {
            Exception[] exceptions =
            [
                new BreakException(),
                new ContinueException(),
                new ReturnException(null),
                new ExitException(),
                new ExitNestedPromptException(),
                new TerminateException(),
                new PipelineStoppedException(),
                new ActionPreferenceStopException(),
                new HaltCommandException(),
            ];

            foreach (Exception exception in exceptions)
            {
                Assert.True(PSCmdlet.IsPowerShellControlFlowException(exception));
                Assert.True(PSCmdlet.IsPowerShellControlFlowException(
                    new RuntimeException("Wrapper", new TargetInvocationException(exception))));
            }

            Assert.False(PSCmdlet.IsPowerShellControlFlowException(new InvalidOperationException()));
            Assert.False(PSCmdlet.IsPowerShellControlFlowException(new RuntimeException()));
            Assert.False(PSCmdlet.IsPowerShellControlFlowException(new PSArgumentException()));
            Assert.False(PSCmdlet.IsPowerShellControlFlowException(
                new TargetInvocationException(new InvalidOperationException())));
            Assert.Throws<ArgumentNullException>(() => PSCmdlet.IsPowerShellControlFlowException(null));
        }

        /// <summary>
        /// Verifies that an unhosted cmdlet cannot request upstream pipeline stopping.
        /// </summary>
        [Fact]
        public void StopOutsidePipelineIsRejected()
        {
            Assert.Throws<PSInvalidOperationException>(() => new StopInputCommand().StopUpstreamCommands());
        }

        /// <summary>
        /// Verifies that binary and script cmdlets stop input after two objects,
        /// skip upstream and requesting-command end processing, preserve downstream
        /// end processing, and run upstream script cleanup without errors.
        /// </summary>
        /// <param name="command">The binary or script cmdlet requesting upstream stopping.</param>
        [Theory]
        [InlineData("Stop-TestInput")]
        [InlineData("Stop-ScriptInput")]
        public void StopPreservesDownstreamEndAndUpstreamCleanup(string command)
        {
            using var ps = CreatePowerShell();
            ps.AddScript(@"
                $global:produced = 0
                $global:upstreamEnded = $false
                $global:upstreamCleaned = $false
                function Get-TestInput {
                    [CmdletBinding()] param()
                    end {
                        foreach ($i in 1..10) {
                            $global:produced++
                            $i
                        }
                        $global:upstreamEnded = $true
                    }
                    clean { $global:upstreamCleaned = $true }
                }
                function Stop-ScriptInput {
                    [CmdletBinding()] param([Parameter(ValueFromPipeline)] [int] $Value)
                    process {
                        $Value
                        if ($Value -eq 2) { $PSCmdlet.StopUpstreamCommands() }
                    }
                    end { 'end' }
                }
                function Complete-TestInput {
                    [CmdletBinding()] param([Parameter(ValueFromPipeline)] $Value)
                    begin { $values = [System.Collections.Generic.List[object]]::new() }
                    process { $values.Add($Value) }
                    end { $values }
                }
                Get-TestInput | " + command + @" | Complete-TestInput
                $global:produced
                $global:upstreamEnded
                $global:upstreamCleaned
            ");

            Assert.Equal(
                [1, 2, 2, false, true],
                ps.Invoke().Select(result => result.BaseObject).ToArray());
            Assert.False(ps.HadErrors);
            Assert.Empty(ps.Streams.Error);
        }

        /// <summary>
        /// Verifies that stopping a standalone script command skips its remaining
        /// process and end code, runs cleanup, and allows the next statement to execute.
        /// </summary>
        [Fact]
        public void StandaloneScriptCommandCompletesNormally()
        {
            using var ps = CreatePowerShell();
            ps.AddScript(@"
                function Stop-Standalone {
                    [CmdletBinding()] param()
                    process { $PSCmdlet.StopUpstreamCommands(); 'unreachable' }
                    end { 'end' }
                    clean { 'clean' | Write-Information }
                }
                Stop-Standalone
                'after'
            ");
            Assert.Equal(
                ["after"],
                ps.Invoke().Select(result => result.BaseObject).ToArray());
            Assert.False(ps.HadErrors);
            Assert.Single(ps.Streams.Information);
        }

        /// <summary>
        /// Verifies that a worker-thread stop request is rejected without disrupting
        /// the cmdlet's execution on the pipeline thread.
        /// </summary>
        [Fact]
        public void StopFromWrongThreadIsRejected()
        {
            using var ps = CreatePowerShell();
            ps.AddCommand("Stop-TestInput").AddParameter("WrongThread");
            Assert.Equal("rejected", Assert.Single(ps.Invoke()).BaseObject);
            Assert.False(ps.HadErrors);
        }

        /// <summary>
        /// Verifies that an upstream binary cmdlet's exception filter lets downstream
        /// stopping propagate after two objects, without calling upstream end or
        /// StopProcessing callbacks or reporting errors.
        /// </summary>
        [Fact]
        public void BinaryUpstreamFilterPreservesStopAndSkipsStopProcessing()
        {
            using var ps = CreatePowerShell();
            var events = new List<string>();
            ps.AddCommand("Get-TestNumbers").AddParameter("EventLog", events)
                .AddCommand("Stop-TestInput").AddCommand("Sort-Object");

            Assert.Equal(
                [1, 2],
                ps.Invoke().Select(result => result.BaseObject).ToArray());
            Assert.Equal(["1", "2"], events);
            Assert.False(ps.HadErrors);
            Assert.Empty(ps.Streams.Error);
        }

        /// <summary>
        /// Verifies that a binary cmdlet stopping during EndProcessing still allows
        /// a downstream sorting command to complete and emit buffered output.
        /// </summary>
        [Fact]
        public void BinaryStopDuringEndCompletesDownstream()
        {
            using var ps = CreatePowerShell();
            ps.AddCommand("Stop-TestInput").AddParameter("Value", 2).AddParameter("StopAtEnd")
                .AddCommand("Sort-Object");

            Assert.Equal(2, Assert.Single(ps.Invoke()).BaseObject);
            Assert.False(ps.HadErrors);
            Assert.Empty(ps.Streams.Error);
        }

        /// <summary>
        /// Verifies that stopping from any script processing phase preserves downstream
        /// initialization and end processing, then allows the next statement to execute.
        /// </summary>
        /// <param name="phase">The begin, process, or end phase issuing the stop request.</param>
        [Theory]
        [InlineData("begin")]
        [InlineData("process")]
        [InlineData("end")]
        public void StopDuringLifecycleCompletesDownstream(string phase)
        {
            using var ps = CreatePowerShell();
            ps.AddScript(@"
                function Stop-InPhase {
                    [CmdletBinding()] param([Parameter(ValueFromPipeline)] $Value)
                    begin { if ('" + phase + @"' -eq 'begin') { $PSCmdlet.StopUpstreamCommands() } }
                    process { if ('" + phase + @"' -eq 'process') { $PSCmdlet.StopUpstreamCommands() } }
                    end { if ('" + phase + @"' -eq 'end') { $PSCmdlet.StopUpstreamCommands() } }
                }
                function Complete-InPhase {
                    [CmdletBinding()] param([Parameter(ValueFromPipeline)] $Value)
                    begin { $initialized = $true }
                    end { $initialized }
                }
                1..10 | Stop-InPhase | Complete-InPhase
                'after'
            ");

            Assert.Equal(
                [true, "after"],
                ps.Invoke().Select(result => result.BaseObject).ToArray());
            Assert.False(ps.HadErrors);
        }

        private static PowerShell CreatePowerShell()
        {
            InitialSessionState state = InitialSessionState.CreateDefault2();
            state.Commands.Add(new SessionStateCmdletEntry("Stop-TestInput", typeof(StopInputCommand), null));
            state.Commands.Add(new SessionStateCmdletEntry("Get-TestNumbers", typeof(GetNumbersCommand), null));
            var ps = PowerShell.Create(state);
            return ps;
        }
    }

    /// <summary>
    /// Test cmdlet that echoes input and requests upstream stopping when the value
    /// is two. Switches exercise stopping during end processing and rejection of
    /// worker-thread requests; exception handling preserves engine control flow.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "TestInput")]
    public class StopInputCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the input value to echo and examine for a stop request.
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public int Value { get; set; }

        /// <summary>
        /// Gets or sets whether to verify that a worker-thread stop request is rejected.
        /// </summary>
        [Parameter]
        public SwitchParameter WrongThread { get; set; }

        /// <summary>
        /// Gets or sets whether to defer the stop request until end processing.
        /// </summary>
        [Parameter]
        public SwitchParameter StopAtEnd { get; set; }

        /// <summary>
        /// Echoes input before stopping at two, or verifies worker-thread rejection.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (WrongThread)
            {
                Task.Run(() => Assert.Throws<PSInvalidOperationException>(() => StopUpstreamCommands()))
                    .GetAwaiter().GetResult();
                WriteObject("rejected");
                return;
            }

            try
            {
                WriteObject(Value);
                if (Value == 2 && !StopAtEnd)
                {
                    StopUpstreamCommands();
                }
            }
            catch (Exception e) when (!IsPowerShellControlFlowException(e))
            {
                ThrowTerminatingError(new ErrorRecord(e, "UnexpectedFailure", ErrorCategory.NotSpecified, null));
            }
        }

        /// <summary>
        /// Requests stopping when deferred, or emits a marker showing end processing ran.
        /// </summary>
        protected override void EndProcessing()
        {
            if (!WrongThread)
            {
                if (StopAtEnd)
                {
                    StopUpstreamCommands();
                }

                WriteObject("end");
            }
        }
    }

    /// <summary>
    /// Upstream test cmdlet that emits numbers one through ten and records production,
    /// ordinary-error handling, and lifecycle callbacks so tests can verify exactly
    /// which operations run when a downstream command requests stopping.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "TestNumbers")]
    public class GetNumbersCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the shared log of emitted numbers and callback markers.
        /// </summary>
        [Parameter]
        public List<string> EventLog { get; set; }

        /// <summary>
        /// Logs and emits each number while allowing downstream engine exceptions to propagate.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                for (int value = 1; value <= 10; value++)
                {
                    EventLog.Add(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    WriteObject(value);
                }
            }
            catch (Exception e) when (!IsPowerShellControlFlowException(e))
            {
                EventLog.Add("caught");
                ThrowTerminatingError(new ErrorRecord(e, "UnexpectedFailure", ErrorCategory.NotSpecified, null));
            }
        }

        /// <summary>
        /// Records whether upstream end processing was invoked.
        /// </summary>
        protected override void EndProcessing()
        {
            EventLog.Add("end");
        }

        /// <summary>
        /// Records whether the engine invoked the upstream stopping callback.
        /// </summary>
        protected override void StopProcessing()
        {
            EventLog.Add("stop");
        }
    }
}

#pragma warning restore SA1402
#pragma warning restore SA1649
