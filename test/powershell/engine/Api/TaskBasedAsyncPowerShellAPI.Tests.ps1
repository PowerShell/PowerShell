# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Task-based PowerShell async APIs' -Tags 'Feature' {
    BeforeAll {
        $sbStub = @'
.foreach{
    [pscustomobject]@{
        Time       = [datetime]::Now.ToString('yyyyMMddTHHmmss.fffffff')
        Value      = $_
        ThreadId   = [System.Threading.Thread]::CurrentThread.ManagedThreadId
        RunspaceId = [runspace]::DefaultRunspace.Id
    }
    Start-Sleep -Milliseconds 250
}
'@
    }

    Context 'PowerShell::InvokeAsync - Single script tests' {
        BeforeAll {
            function InvokeAsyncHelper {
                param(
                    [powershell]$PowerShell,
                    [switch]$Wait
                )
                $r = $PowerShell.AddScript("@(1,2,3,4,5)${sbStub}").InvokeAsync()
                if ($Wait) {
                    [System.Threading.Tasks.Task]::WaitAll(@($r))
                }
                $r
            }

            function GetInnerErrorId {
                param(
                    [exception]$Exception
                )
                while ($null -ne $Exception.InnerException) {
                    $Exception = $Exception.InnerException
                    if ($null -ne (Get-Member -InputObject $Exception -Name ErrorRecord)) {
                        $Exception.ErrorRecord.FullyQualifiedErrorId
                        break
                    }
                }
            }
        }

        It 'can invoke a single script asynchronously, but only in a new runspace' {
            $ps = [powershell]::Create()
            try {
                $r = InvokeAsyncHelper -PowerShell $ps -Wait
                $r.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
                $r.IsCompletedSuccessfully | Should -BeTrue
            } finally {
                $ps.Dispose()
            }
        }

        It 'handles terminating errors properly when invoked asynchronously in a new runspace' {
            $ps = [powershell]::Create()
            try {
                $sb = {
                    $r = $ps.AddScript(@'
try {
    Get-Process -Invalid 42
} catch {
    throw
}
'@).InvokeAsync()
                    Set-Variable -Name r -Scope 2 -Value $r
                    [System.Threading.Tasks.Task]::WaitAll(@($r))
                    $r
                }
                # This test is designed to gracefully fail with an error when invoked asynchronously.
                { $sb.Invoke() } | Should -Throw -ErrorId 'AggregateException'
                $r.IsFaulted | Should -BeTrue
                $r.Exception.InnerException -is [System.Management.Automation.ParameterBindingException] | Should -BeTrue
                $r.Exception.InnerException.CommandInvocation.InvocationName | Should -BeExactly 'Get-Process'
                $r.Exception.InnerException.ParameterName | Should -BeExactly 'Invalid'
                $r.Exception.InnerException.ErrorId | Should -BeExactly 'NamedParameterNotFound'
            } finally {
                $ps.Dispose()
            }
        }

        It 'cannot invoke a single script asynchronously in a runspace that has not been opened' {
            $rs = [runspacefactory]::CreateRunspace()
            $ps = [powershell]::Create($rs)

            try {
                # This test is designed to fail. You cannot invoke PowerShell asynchronously
                # in a runspace that has not been opened.
                $err = { $ps.AddScript('1+1').InvokeAsync() } | Should -Throw -ErrorId "InvalidRunspaceStateException" -PassThru

                $err.Exception | Should -BeOfType System.Management.Automation.MethodInvocationException
                $err.Exception.InnerException | Should -BeOfType System.Management.Automation.Runspaces.InvalidRunspaceStateException
                $err.Exception.InnerException.CurrentState | Should -Be 'BeforeOpen'
                $err.Exception.InnerException.ExpectedState | Should -Be 'Opened'
            } finally {
                $ps.Dispose()
                $rs.Dispose()
            }
        }

        It 'cannot invoke a single script asynchronously in a runspace that is busy' {
            $rs = [runspacefactory]::CreateRunspace()
            try {
                $rs.Open();
                $ps1 = [powershell]::Create($rs)
                $ps2 = [powershell]::Create($rs)
                try {
                    # Make the runspace busy by running an async command.
                    $ps1.AddScript('@(1..100).foreach{Start-Sleep -Milliseconds 250}').InvokeAsync()
                    Wait-UntilTrue { $rs.RunspaceAvailability -eq [System.Management.Automation.Runspaces.RunspaceAvailability]::Busy } | Should -BeTrue
                    # This test is designed to fail. You cannot invoke PowerShell asynchronously
                    # in a runspace that is busy, because pipelines cannot be run concurrently.
                    $err = { InvokeAsyncHelper -PowerShell $ps2 -Wait } | Should -Throw -ErrorId 'AggregateException' -PassThru
                    GetInnerErrorId -Exception $err.Exception | Should -Be 'InvalidOperation'
                } finally {
                    $ps1.Stop()
                    $ps1.Dispose()
                    $ps2.Dispose()
                }

            } finally {
                $rs.Dispose()
            }
        }

        It 'cannot invoke a single script asynchronously in the current runspace' {
            $ps = [powershell]::Create('CurrentRunspace')
            try {
                # This test is designed to fail. You cannot invoke PowerShell asynchronously
                # in the current runspace because nested PowerShell instances cannot be
                # invoked asynchronously
                $err = { $ps.AddScript('1+1').InvokeAsync() } | Should -Throw -ErrorId 'PSInvalidOperationException' -PassThru
                GetInnerErrorId -Exception $err.Exception | Should -Be 'InvalidOperation'
            } finally {
                $ps.Dispose()
            }
        }
    }

    Context 'PowerShell::InvokeAsync - Multiple script tests' {
        It 'can invoke multiple scripts asynchronously' {
            $ps1 = [powershell]::Create()
            $ps2 = [powershell]::Create()
            try {
                $r1 = $ps1.AddScript("@(1,3,5,7,9,11,13,15,17,19)${sbStub}").InvokeAsync()
                $r2 = $ps2.AddScript("@(2,4,6,8,10,12,14,16,18,20)${sbStub}").InvokeAsync()
                [System.Threading.Tasks.Task]::WaitAll(@($r1, $r2))
                $r1.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
                $r1.IsCompletedSuccessfully | Should -BeTrue
                $r2.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
                $r2.IsCompletedSuccessfully | Should -BeTrue
                $results = @($r1.Result.foreach('Value')) + @($r2.Result.foreach('Value'))
                Compare-Object -ReferenceObject @(1..20) -DifferenceObject $results -SyncWindow 20 | Should -Be $null
            } finally {
                $ps1.Dispose()
                $ps2.Dispose()
            }
        }
    }

    Context 'PowerShell::InvokeAsync - With input and output' {
        BeforeAll {
            $d1 = New-Object -TypeName 'System.Management.Automation.PSDataCollection[int]'
            $d2 = New-Object -TypeName 'System.Management.Automation.PSDataCollection[int]'
            foreach ($i in 1..20) {
                $d1.Add($foreach.Current)
                $foreach.MoveNext() > $null
                $d2.Add($foreach.Current)
            }
            $d1.Complete()
            $d2.Complete()
            $script = "`$input${sbStub}"
        }

        It 'can invoke multiple scripts asynchronously with input' {
            $ps1 = [powershell]::Create()
            $ps2 = [powershell]::Create()
            try {
                $r1 = $ps1.AddScript($script).InvokeAsync($d1)
                $r2 = $ps2.AddScript($script).InvokeAsync($d2)
                [System.Threading.Tasks.Task]::WaitAll(@($r1, $r2))
                $r1.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
                $r1.IsCompletedSuccessfully | Should -BeTrue
                $r2.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
                $r2.IsCompletedSuccessfully | Should -BeTrue
                $allResults = @($r1.Result) + @($r2.Result)
                Compare-Object -ReferenceObject @(1..20) -DifferenceObject $allResults.Value -SyncWindow 20 | Should -Be $null
            } finally {
                $ps1.Dispose()
                $ps2.Dispose()
            }
        }

        It 'can invoke multiple scripts asynchronously with input and capture output' {
            $ps1 = [powershell]::Create()
            $ps2 = [powershell]::Create()
            try {
                $o = New-Object -TypeName 'System.Management.Automation.PSDataCollection[PSObject]'
                $r1 = $ps1.AddScript($script).InvokeAsync($d1, $o)
                $r2 = $ps2.AddScript($script).InvokeAsync($d2, $o)
                [System.Threading.Tasks.Task]::WaitAll(@($r1, $r2))
                $o.Complete()
                $r1.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
                $r1.IsCompletedSuccessfully | Should -BeTrue
                $r2.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
                $r2.IsCompletedSuccessfully | Should -BeTrue
                Compare-Object -ReferenceObject @(1..20) -DifferenceObject $o.Value -SyncWindow 20 | Should -Be $null
            } finally {
                $ps1.Dispose()
                $ps2.Dispose()
            }
        }
    }

    Context 'PowerShell::StopAsync' {
        It 'can stop a script that is running asynchronously' {
            $ps = [powershell]::Create()
            try {
                $ir = $ps.AddScript("Start-Sleep -Seconds 60").InvokeAsync()
                Wait-UntilTrue { $ps.InvocationStateInfo.State -eq [System.Management.Automation.PSInvocationState]::Running } | Should -BeTrue
                $ps.InvocationStateInfo.State | Should -Be 'Running'
                Start-Sleep -Seconds 1 # add a sleep to wait for pipeline to start executing the command.
                $sr = $ps.StopAsync($null, $null)
                [System.Threading.Tasks.Task]::WaitAll(@($sr))
                $ps.Streams.Error | Should -HaveCount 0 -Because ($ps.Streams.Error | Out-String)
                $ps.Commands.Commands.commandtext | Should -Be "Start-Sleep -Seconds 60"
                $sr.IsCompletedSuccessfully | Should -BeTrue
                $ir.IsFaulted | Should -BeTrue -Because ($ir | Format-List -Force * | Out-String)
                $ir.Exception -is [System.AggregateException] | Should -BeTrue
                $ir.Exception.InnerException -is [System.Management.Automation.PipelineStoppedException] | Should -BeTrue
                $ps.InvocationStateInfo.State | Should -Be ([System.Management.Automation.PSInvocationState]::Stopped)
            } finally {
                $ps.Dispose()
            }
        }
    }
}
