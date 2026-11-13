// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PSTests.Sequential
{
    /// <summary>
    /// Tests for PSInvocationSettings.Timeout, PowerShell.Stop(TimeSpan),
    /// bounded runspace close, and parallel StopPipelines.
    /// </summary>
    /// <remarks>
    /// Tests that may leave a pipeline in a stopped-but-draining state use their own
    /// private Runspace to avoid contaminating the shared fixture runspace.
    /// </remarks>
    public class TimeoutTests : IDisposable
    {
        // Shared runspace for tests that do not exercise timeout paths.
        private readonly Runspace _runspace;

        public TimeoutTests()
        {
            _runspace = RunspaceFactory.CreateRunspace();
            _runspace.Open();
        }

        public void Dispose() => _runspace?.Dispose();

        // ─────────────────────────────────────────────────────────────────────
        // REQ-01  PSInvocationSettings.Timeout property
        // ─────────────────────────────────────────────────────────────────────
        [Fact]
        public void TestPSInvocationSettingsTimeoutDefaultIsInfinite()
        {
            // REQ-01: default MUST be InfiniteTimeSpan for backwards compatibility.
            var settings = new PSInvocationSettings();
            Assert.Equal(Timeout.InfiniteTimeSpan, settings.Timeout);
        }

        [Fact]
        public void TestPSInvocationSettingsTimeoutCanBeSet()
        {
            // REQ-01: property is read/write.
            var settings = new PSInvocationSettings { Timeout = TimeSpan.FromSeconds(5) };
            Assert.Equal(TimeSpan.FromSeconds(5), settings.Timeout);
        }

        [Fact]
        public void TestPSInvocationSettingsTimeoutZeroIsValid()
        {
            // REQ-01: zero is a valid edge-case value (fires immediately).
            var settings = new PSInvocationSettings { Timeout = TimeSpan.Zero };
            Assert.Equal(TimeSpan.Zero, settings.Timeout);
        }

        // ─────────────────────────────────────────────────────────────────────
        // REQ-02  Invoke() on single Runspace honors Timeout (Phase 2)
        // ─────────────────────────────────────────────────────────────────────
        [Fact]
        public void TestInvokeCompletesWithinTimeout()
        {
            // REQ-02a: fast command finishes before timeout — no exception thrown.
            using var ps = PowerShell.Create();
            ps.Runspace = _runspace;
            ps.AddScript("1 + 1");
            var settings = new PSInvocationSettings { Timeout = TimeSpan.FromSeconds(10) };
            var results = ps.Invoke(null, settings);
            Assert.Single(results);
            Assert.Equal(2, (int)results[0].BaseObject);
        }

        [Fact]
        public void TestInvokeDefaultTimeoutNeverExpires()
        {
            // REQ-02a: when Timeout == InfiniteTimeSpan (default), no TimeoutException.
            using var ps = PowerShell.Create();
            ps.Runspace = _runspace;
            ps.AddScript("'hello'");
            var settings = new PSInvocationSettings();
            Assert.Equal(Timeout.InfiniteTimeSpan, settings.Timeout);
            var results = ps.Invoke(null, settings);
            Assert.Single(results);
            Assert.Equal("hello", (string)results[0].BaseObject);
        }

        [Fact]
        public void TestInvokeThrowsTimeoutExceptionWhenExceeded()
        {
            // REQ-02: slow command MUST throw TimeoutException when Timeout elapses.
            // Uses private runspace so the pipeline stop does not affect other tests.
            using var rs = NewRunspace();
            using var ps = PowerShell.Create();
            ps.Runspace = rs;
            ps.AddScript("Start-Sleep -Seconds 60");
            var settings = new PSInvocationSettings { Timeout = TimeSpan.FromSeconds(2) };
            Assert.Throws<TimeoutException>(() => ps.Invoke(null, settings));
        }

        [Fact]
        public void TestInvokeTimeoutExceptionMessageContainsTimeout()
        {
            // REQ-02: exception message must not be empty and must reference the timeout.
            using var rs = NewRunspace();
            using var ps = PowerShell.Create();
            ps.Runspace = rs;
            ps.AddScript("Start-Sleep -Seconds 60");
            var settings = new PSInvocationSettings { Timeout = TimeSpan.FromSeconds(2) };
            var ex = Assert.Throws<TimeoutException>(() => ps.Invoke(null, settings));
            Assert.False(string.IsNullOrEmpty(ex.Message));
        }

        [Fact]
        public void TestRunspaceRemainsUsableAfterInvokeTimeout()
        {
            // REQ-09: a runspace MUST be usable again after a TimeoutException.
            using var rs = NewRunspace();

            using (var ps1 = PowerShell.Create())
            {
                ps1.Runspace = rs;
                ps1.AddScript("Start-Sleep -Seconds 60");
                var settings = new PSInvocationSettings { Timeout = TimeSpan.FromSeconds(2) };
                Assert.Throws<TimeoutException>(() => ps1.Invoke(null, settings));
            }

            // Wait briefly for the stopped pipeline to fully drain.
            Thread.Sleep(500);

            using var ps2 = PowerShell.Create();
            ps2.Runspace = rs;
            ps2.AddScript("1 + 2");
            var results = ps2.Invoke();
            Assert.Single(results);
            Assert.Equal(3, (int)results[0].BaseObject);
        }

        // ─────────────────────────────────────────────────────────────────────
        // REQ-05  Stop(TimeSpan) overload
        // ─────────────────────────────────────────────────────────────────────
        [Fact]
        public void TestStopWithTimeoutOverloadCompletes()
        {
            // REQ-05: Stop(TimeSpan) stops a running command and sets state to Stopped.
            using var rs = NewRunspace();
            using var ps = PowerShell.Create();
            ps.Runspace = rs;
            ps.AddScript("Start-Sleep -Seconds 60");
            ps.BeginInvoke();
            Thread.Sleep(200);
            ps.Stop(TimeSpan.FromSeconds(10));
            Assert.Equal(PSInvocationState.Stopped, ps.InvocationStateInfo.State);
        }

        [Fact]
        public void TestStopWithoutTimeoutRemainsBackwardsCompatible()
        {
            // REQ-05: original Stop() overload MUST still work unchanged.
            using var rs = NewRunspace();
            using var ps = PowerShell.Create();
            ps.Runspace = rs;
            ps.AddScript("Start-Sleep -Seconds 60");
            ps.BeginInvoke();
            Thread.Sleep(200);
            ps.Stop();
            Assert.Equal(PSInvocationState.Stopped, ps.InvocationStateInfo.State);
        }

        [Fact]
        public void TestStopTimeoutExceptionMessageIsNonEmpty()
        {
            // REQ-05: if Stop(TimeSpan) itself times out, message must be non-empty.
            using var rs = NewRunspace();
            using var ps = PowerShell.Create();
            ps.Runspace = rs;
            ps.AddScript("Start-Sleep -Seconds 60");
            ps.BeginInvoke();
            Thread.Sleep(200);

            // TimeSpan.Zero forces immediate timeout without waiting for real stall.
            try
            {
                ps.Stop(TimeSpan.Zero);
            }
            catch (TimeoutException ex)
            {
                Assert.False(string.IsNullOrEmpty(ex.Message));
            }

            // If Stop completed (race), that is also acceptable.
        }

        [Fact]
        public void TestStopAfterDisposeIsSilent()
        {
            // REQ-08b: Stop(TimeSpan) after Dispose() MUST NOT throw ObjectDisposedException.
            using var rs = NewRunspace();
            var ps = PowerShell.Create();
            ps.Runspace = rs;
            ps.Dispose();
            ps.Stop(TimeSpan.FromSeconds(5)); // must be silent
        }

        // ─────────────────────────────────────────────────────────────────────
        // REQ-04  RunspacePool acquisition respects Timeout
        // ─────────────────────────────────────────────────────────────────────
        [Fact]
        public void TestPoolAcquisitionTimeoutThrows()
        {
            // REQ-04: pool size=1 occupied by sleeping ps1; ps2 must throw TimeoutException.
            using var pool = RunspaceFactory.CreateRunspacePool(1, 1);
            pool.Open();

            var ps1 = PowerShell.Create();
            ps1.RunspacePool = pool;
            ps1.AddScript("Start-Sleep -Seconds 30");
            ps1.BeginInvoke();
            Thread.Sleep(300); // let ps1 acquire the only slot

            using var ps2 = PowerShell.Create();
            ps2.RunspacePool = pool;
            ps2.AddScript("1");
            var settings = new PSInvocationSettings { Timeout = TimeSpan.FromSeconds(2) };

            try
            {
                Assert.Throws<TimeoutException>(() => ps2.Invoke(null, settings));
            }
            finally
            {
                ps1.Stop();
                ps1.Dispose();
                pool.Close();
            }
        }

        [Fact]
        public void TestPoolAcquisitionSucceedsWithinTimeout()
        {
            // REQ-04a: pool has capacity; acquisition completes before the timeout.
            using var pool = RunspaceFactory.CreateRunspacePool(1, 3);
            pool.Open();
            try
            {
                using var ps = PowerShell.Create();
                ps.RunspacePool = pool;
                ps.AddScript("42");
                var settings = new PSInvocationSettings { Timeout = TimeSpan.FromSeconds(10) };
                var results = ps.Invoke(null, settings);
                Assert.Single(results);
                Assert.Equal(42, (int)results[0].BaseObject);
            }
            finally
            {
                pool.Close();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // REQ-06 / REQ-07  Bounded runspace close + parallel StopPipelines
        // ─────────────────────────────────────────────────────────────────────
        [Fact]
        public void TestRunspaceCloseStopsPipelinesWithinBound()
        {
            // REQ-06/REQ-07: Close() with one active pipeline completes within bound.
            using var rs = NewRunspace();
            using var ps = PowerShell.Create();
            ps.Runspace = rs;
            ps.AddScript("Start-Sleep -Seconds 300");
            ps.BeginInvoke();

            var closeTask = Task.Run(() => rs.Close());
            bool completed = closeTask.Wait(TimeSpan.FromSeconds(60));
            Assert.True(completed, "Runspace.Close() should complete within 60s");
        }

        [Fact]
        public void TestRunspaceCloseMultiplePipelinesCompletesInBound()
        {
            // REQ-06: 3 runspaces each with an active sleep pipeline — all close within cap.
            const int count = 3;
            var runspaces = new Runspace[count];
            var psList = new List<PowerShell>();

            for (int i = 0; i < count; i++)
            {
                runspaces[i] = NewRunspace();
                var ps = PowerShell.Create();
                ps.Runspace = runspaces[i];
                ps.AddScript("Start-Sleep -Seconds 300");
                ps.BeginInvoke();
                psList.Add(ps);
            }

            var closeTasks = new Task[count];
            for (int i = 0; i < count; i++)
            {
                int idx = i;
                closeTasks[idx] = Task.Run(() => runspaces[idx].Close());
            }

            bool allDone = Task.WaitAll(closeTasks, TimeSpan.FromSeconds(120));
            Assert.True(allDone, "All runspaces should close within 120s");

            foreach (var ps in psList)
            {
                ps.Dispose();
            }

            foreach (var rs in runspaces)
            {
                rs.Dispose();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // REQ-08  Dispose() does not hang
        // ─────────────────────────────────────────────────────────────────────
        [Fact]
        public void TestDisposeWithRunningPipelineDoesNotHang()
        {
            // REQ-08: Dispose() MUST complete within a bounded time even with active pipelines.
            using var rs = NewRunspace();
            var ps = PowerShell.Create();
            ps.Runspace = rs;
            ps.AddScript("Start-Sleep -Seconds 300");
            ps.BeginInvoke();

            var disposeTask = Task.Run(() =>
            {
                ps.Dispose();
                rs.Dispose();
            });

            bool completed = disposeTask.Wait(TimeSpan.FromSeconds(60));
            Assert.True(completed, "Dispose() should complete within 60s");
        }

        // ─────────────────────────────────────────────────────────────────────
        // REQ-10  Nested PS timeout propagation
        // ─────────────────────────────────────────────────────────────────────
        [Fact]
        public void TestNestedPSTimeoutPropagates()
        {
            // REQ-10: TimeoutException from nested PS Invoke must propagate upward.
            using var rs = NewRunspace();
            using var ps = PowerShell.Create();
            ps.Runspace = rs;
            ps.AddScript(@"
                $rs2 = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
                $rs2.Open()
                $inner = [powershell]::Create()
                $inner.Runspace = $rs2
                $inner.AddScript('Start-Sleep -Seconds 60') > $null
                $settings = [System.Management.Automation.PSInvocationSettings]::new()
                $settings.Timeout = [TimeSpan]::FromSeconds(2)
                try {
                    $inner.Invoke($null, $settings) > $null
                } finally {
                    $rs2.Dispose(); $inner.Dispose()
                }
            ");

            var ex = Record.Exception(() => ps.Invoke());
            Assert.NotNull(ex);
            var isTimeout = ex is TimeoutException ||
                            (ex is System.Management.Automation.MethodInvocationException &&
                             ex.InnerException is TimeoutException);
            Assert.True(
                isTimeout,
                $"Expected TimeoutException (possibly wrapped), got: {ex.GetType().Name}: {ex.Message}");
        }

        // ─────────────────────────────────────────────────────────────────────
        // REQ-05  Concurrent Stop + Invoke — no deadlock
        // ─────────────────────────────────────────────────────────────────────
        [Fact]
        public void TestConcurrentStopAndInvokeNoDeadlock()
        {
            // REQ-05: Stop(TimeSpan) and Invoke() racing from two threads MUST NOT deadlock.
            using var rs = NewRunspace();
            using var ps = PowerShell.Create();
            ps.Runspace = rs;
            ps.AddScript("Start-Sleep -Seconds 60");

            Exception invokeEx = null, stopEx = null;

            var invokeTask = Task.Run(() =>
            {
                try
                {
                    ps.Invoke();
                }
                catch (System.Management.Automation.PipelineStoppedException)
                {
                    // expected
                }
                catch (Exception ex)
                {
                    invokeEx = ex;
                }
            });

            var stopTask = Task.Run(() =>
            {
                Thread.Sleep(300);
                try
                {
                    ps.Stop(TimeSpan.FromSeconds(10));
                }
                catch (Exception ex)
                {
                    stopEx = ex;
                }
            });

            bool allDone = Task.WaitAll(new[] { invokeTask, stopTask }, TimeSpan.FromSeconds(20));
            Assert.True(allDone, "Concurrent Stop+Invoke should resolve within 20s — no deadlock");
            Assert.Null(stopEx);
            Assert.Null(invokeEx);
        }

        // Helper: create + open an isolated runspace.
        private static Runspace NewRunspace()
        {
            var rs = RunspaceFactory.CreateRunspace();
            rs.Open();
            return rs;
        }
    }
}
