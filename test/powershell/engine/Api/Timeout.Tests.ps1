# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
#
# Pester tests for PowerShell Hosting API bounded-wait / timeout support.
# Spec reference: SPECIFICATION.md
# Tag "CI"     = runs in standard CI pipeline
# Tag "Feature" = longer-running tests that verify behavioural contracts

Describe 'PowerShell Hosting Timeout Support' -Tag "CI" {

    # ─────────────────────────────────────────────────────────────────────
    # REQ-01  PSInvocationSettings.Timeout property
    # ─────────────────────────────────────────────────────────────────────
    Context 'PSInvocationSettings.Timeout property' {

        It 'defaults to InfiniteTimeSpan' {
            # REQ-01
            $settings = [System.Management.Automation.PSInvocationSettings]::new()
            $settings.Timeout | Should -Be ([System.Threading.Timeout]::InfiniteTimeSpan)
        }

        It 'can be set to a finite value' {
            # REQ-01
            $settings = [System.Management.Automation.PSInvocationSettings]::new()
            $settings.Timeout = [TimeSpan]::FromSeconds(5)
            $settings.Timeout.TotalSeconds | Should -Be 5
        }

        It 'zero timeout is a valid edge-case value' {
            # REQ-01 edge: TimeSpan.Zero is valid (fires immediately)
            $settings = [System.Management.Automation.PSInvocationSettings]::new()
            $settings.Timeout = [TimeSpan]::Zero
            $settings.Timeout | Should -Be ([TimeSpan]::Zero)
        }

        It 'can be reset to InfiniteTimeSpan explicitly' {
            # REQ-01: round-trip set + reset
            $settings = [System.Management.Automation.PSInvocationSettings]::new()
            $settings.Timeout = [TimeSpan]::FromSeconds(3)
            $settings.Timeout = [System.Threading.Timeout]::InfiniteTimeSpan
            $settings.Timeout | Should -Be ([System.Threading.Timeout]::InfiniteTimeSpan)
        }
    }

    # ─────────────────────────────────────────────────────────────────────
    # REQ-02 / REQ-09  Invoke() on single Runspace honors Timeout (Phase 2)
    # ─────────────────────────────────────────────────────────────────────
    Context 'Invoke with Timeout (single runspace)' {

        It 'completes fast script before timeout — no exception' {
            # REQ-02a
            $rs = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
            $rs.Open()
            try {
                $ps = [powershell]::Create()
                $ps.Runspace = $rs
                $ps.AddScript('1 + 1') > $null
                $settings = [System.Management.Automation.PSInvocationSettings]::new()
                $settings.Timeout = [TimeSpan]::FromSeconds(10)
                $r = $ps.Invoke($null, $settings)
                $r.Count | Should -Be 1
                [int]$r[0] | Should -Be 2
                $ps.Dispose()
            } finally { $rs.Dispose() }
        }

        It 'throws TimeoutException when timeout exceeded' {
            # REQ-02
            $rs = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
            $rs.Open()
            try {
                $ps = [powershell]::Create()
                $ps.Runspace = $rs
                $ps.AddScript('Start-Sleep -Seconds 60') > $null
                $settings = [System.Management.Automation.PSInvocationSettings]::new()
                $settings.Timeout = [TimeSpan]::FromSeconds(2)
                $threw = $false
                try {
                    $ps.Invoke($null, $settings)
                }
                catch {
                    $threw = $true
                    $inner = $_.Exception
                    while ($inner -is [System.Management.Automation.MethodInvocationException] -or
                           $inner -is [System.Reflection.TargetInvocationException]) {
                        $inner = $inner.InnerException
                    }
                    $inner | Should -BeOfType ([TimeoutException])
                }
                $threw | Should -BeTrue -Because 'Invoke should throw TimeoutException'
                $ps.Dispose()
            } finally { $rs.Dispose() }
        }

        It 'default InfiniteTimeSpan never causes TimeoutException' {
            # REQ-02a — verify the default path does not inject overhead
            $rs = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
            $rs.Open()
            try {
                $ps = [powershell]::Create()
                $ps.Runspace = $rs
                $ps.AddScript('"ok"') > $null
                # No settings.Timeout set — uses default (infinite)
                $r = $ps.Invoke()
                $r[0] | Should -BeExactly 'ok'
                $ps.Dispose()
            } finally { $rs.Dispose() }
        }

        It 'runspace is reusable after TimeoutException' {
            # REQ-09
            $rs = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
            $rs.Open()
            try {
                $ps1 = [powershell]::Create()
                $ps1.Runspace = $rs
                $ps1.AddScript('Start-Sleep -Seconds 60') > $null
                $settings = [System.Management.Automation.PSInvocationSettings]::new()
                $settings.Timeout = [TimeSpan]::FromSeconds(2)
                try { $ps1.Invoke($null, $settings) > $null } catch [TimeoutException] { Write-Verbose "Expected timeout" }
                $ps1.Dispose()

                # Brief pause for the stopped pipeline to drain before reuse.
                Start-Sleep -Milliseconds 600

                $ps2 = [powershell]::Create()
                $ps2.Runspace = $rs
                $ps2.AddScript('99') > $null
                $r = $ps2.Invoke()
                [int]$r[0] | Should -Be 99
                $ps2.Dispose()
            } finally { $rs.Dispose() }
        }
    }

    # ─────────────────────────────────────────────────────────────────────
    # REQ-05  Stop(TimeSpan) overload
    # ─────────────────────────────────────────────────────────────────────
    Context 'Stop(TimeSpan) overload' {

        It 'stops a running command within timeout' {
            # REQ-05
            $rs = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
            $rs.Open()
            try {
                $ps = [powershell]::Create()
                $ps.Runspace = $rs
                $ps.AddScript('Start-Sleep -Seconds 60') > $null
                $null = $ps.BeginInvoke()
                Start-Sleep -Milliseconds 200
                $ps.Stop([TimeSpan]::FromSeconds(10))
                $ps.InvocationStateInfo.State | Should -Be 'Stopped'
                $ps.Dispose()
            } finally { $rs.Dispose() }
        }

        It 'original Stop() overload still works unchanged' {
            # REQ-05: backwards compatibility
            $rs = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
            $rs.Open()
            try {
                $ps = [powershell]::Create()
                $ps.Runspace = $rs
                $ps.AddScript('Start-Sleep -Seconds 60') > $null
                $null = $ps.BeginInvoke()
                Start-Sleep -Milliseconds 200
                $ps.Stop()
                $ps.InvocationStateInfo.State | Should -Be 'Stopped'
                $ps.Dispose()
            } finally { $rs.Dispose() }
        }

        It 'Stop after Dispose is silent — no exception' {
            # REQ-08b: Stop(TimeSpan) on a disposed PS object must not throw
            $rs = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
            $rs.Open()
            try {
                $ps = [powershell]::Create()
                $ps.Runspace = $rs
                $ps.Dispose()
                { $ps.Stop([TimeSpan]::FromSeconds(5)) } | Should -Not -Throw
            } finally { $rs.Dispose() }
        }
    }

    # ─────────────────────────────────────────────────────────────────────
    # REQ-04  RunspacePool exhaustion
    # ─────────────────────────────────────────────────────────────────────
    Context 'RunspacePool exhaustion' {

        It 'TimeoutException when pool is exhausted' {
            # REQ-04
            $pool = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspacePool(1, 1)
            $pool.Open()
            try {
                $ps1 = [powershell]::Create()
                $ps1.RunspacePool = $pool
                $ps1.AddScript('Start-Sleep -Seconds 30') > $null
                $null = $ps1.BeginInvoke()
                Start-Sleep -Milliseconds 300

                $ps2 = [powershell]::Create()
                $ps2.RunspacePool = $pool
                $ps2.AddScript('1') > $null
                $settings = [System.Management.Automation.PSInvocationSettings]::new()
                $settings.Timeout = [TimeSpan]::FromSeconds(2)

                $threw = $false
                try {
                    $ps2.Invoke($null, $settings)
                }
                catch {
                    $threw = $true
                    $inner = $_.Exception
                    while ($inner -is [System.Management.Automation.MethodInvocationException] -or
                           $inner -is [System.Reflection.TargetInvocationException]) {
                        $inner = $inner.InnerException
                    }
                    $inner | Should -BeOfType ([TimeoutException])
                }
                $threw | Should -BeTrue -Because 'Invoke should throw TimeoutException on exhausted pool'

                $ps1.Stop(); $ps1.Dispose()
                $ps2.Dispose()
            } finally { $pool.Close(); $pool.Dispose() }
        }

        It 'succeeds when pool has capacity' {
            # REQ-04a
            $pool = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspacePool(1, 3)
            $pool.Open()
            try {
                $ps = [powershell]::Create()
                $ps.RunspacePool = $pool
                $ps.AddScript('42') > $null
                $settings = [System.Management.Automation.PSInvocationSettings]::new()
                $settings.Timeout = [TimeSpan]::FromSeconds(10)
                $r = $ps.Invoke($null, $settings)
                $r.Count | Should -Be 1
                [int]$r[0] | Should -Be 42
                $ps.Dispose()
            } finally { $pool.Close(); $pool.Dispose() }
        }
    }

    # ─────────────────────────────────────────────────────────────────────
    # REQ-06  Parallel StopPipelines
    # ─────────────────────────────────────────────────────────────────────
    Context 'Parallel StopPipelines' {

        It 'Close with 3 active pipelines completes within 120s' -Tag "Feature" {
            # REQ-06: parallel stop is faster than sequential N*single
            $job = Start-Job {
                $runspaces = @()
                $psList = @()
                for ($i = 0; $i -lt 3; $i++) {
                    $rs = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
                    $rs.Open()
                    $ps = [powershell]::Create()
                    $ps.Runspace = $rs
                    $ps.AddScript('Start-Sleep -Seconds 300') > $null
                    $null = $ps.BeginInvoke()
                    $runspaces += $rs
                    $psList += $ps
                }
                $tasks = $runspaces | ForEach-Object { $rs = $_; [System.Threading.Tasks.Task]::Run([System.Action]{ $rs.Close() }) }
                [System.Threading.Tasks.Task]::WaitAll($tasks)
                foreach ($ps in $psList) { $ps.Dispose() }
                return $true
            }
            $result = $job | Wait-Job -Timeout 120
            $result | Should -Not -BeNullOrEmpty -Because "3-runspace close should finish within 120s"
            Receive-Job $job | Should -BeTrue
            Remove-Job $job -Force
        }
    }

    # ─────────────────────────────────────────────────────────────────────
    # REQ-08  Dispose does not hang
    # ─────────────────────────────────────────────────────────────────────
    Context 'Dispose safety' {

        It 'Dispose with running pipeline completes within 60s' -Tag "Feature" {
            # REQ-08
            $job = Start-Job {
                $rs = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
                $rs.Open()
                $ps = [powershell]::Create()
                $ps.Runspace = $rs
                $ps.AddScript('Start-Sleep -Seconds 300') > $null
                $null = $ps.BeginInvoke()
                $ps.Dispose()
                $rs.Dispose()
                return $true
            }
            $result = $job | Wait-Job -Timeout 60
            $result | Should -Not -BeNullOrEmpty -Because "Dispose should not hang indefinitely"
            Receive-Job $job | Should -BeTrue
            Remove-Job $job -Force
        }
    }
}
