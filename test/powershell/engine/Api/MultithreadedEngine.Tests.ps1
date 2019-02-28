Describe 'Multithreaded engine APIs' -Tags 'CI' {
    BeforeAll {
        $sbStub = @'
.foreach{
    [pscustomobject]@{
        Time       = [DateTime]::Now.ToString('yyyyMMddTHHmmss.fffffff')
        Value      = $_
        ThreadId   = [System.Threading.Thread]::CurrentThread.ManagedThreadId
        RunspaceId = [runspace]::DefaultRunspace.Id
    }
    Start-Sleep -Seconds 1
}
'@
    }

    Context 'PowerShell::InvokeAsync - Single script tests' {
        BeforeAll {
            function InvokeAsyncThenWait {
                param(
                    [powershell]$PowerShell
                )
                $sb = [scriptblock]::Create("@(1,2,3,4,5)${sbStub}")
                $r = $PowerShell.AddScript($sb).InvokeAsync()
                [System.Threading.Tasks.Task]::WaitAll(@($r))
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
            $r = InvokeAsyncThenWait -PowerShell $ps
            $r.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
            $r.IsCompletedSuccessfully | Should -Be $true
        }

        It 'handles terminating errors properly when invoked asynchronously in a new runspace' {
            $ps = [powershell]::Create()
            $sb = {
                $r = $ps.AddScript(@'
try {
    Get-Process -InvalidParameter 42 -ErrorAction Stop
} catch {
    throw
}
'@).InvokeAsync()
                Set-Variable -Name r -Scope 2 -Value $r
                [System.Threading.Tasks.Task]::WaitAll(@($r))
                $r
            }
            # This test is designed to gracefully fail with an error when invoked asynchronously.
            $err = { $sb.Invoke() } | Should -Throw -ErrorId 'AggregateException' -PassThru
            $r.IsFaulted | Should -Be $true
            $err.Exception.InnerException.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly 'NamedParameterNotFound,Microsoft.PowerShell.Commands.GetProcessCommand'
        }

        It 'cannot invoke a single script asynchronously in a runspace that has not been opened' {
            $rs = [runspacefactory]::CreateRunspace()
            $ps = [powershell]::Create($rs)
            $r = $ps.AddScript('@(1..10).foreach{Start-Sleep -Seconds 1}').InvokeAsync()
            # This test is designed to fail. You cannot invoke PowerShell asynchronously
            # in a runspace that has not been opened.
            $r.IsFaulted | Should -Be $true
            $r.Exception.InnerException -is [System.Management.Automation.Runspaces.InvalidRunspaceStateException] | Should -Be $true
            $r.Exception.InnerException.CurrentState | Should -Be 'BeforeOpen'
            $r.Exception.InnerException.ExpectedState | Should -Be 'Opened'
        }

        It 'cannot invoke a single script asynchronously in a runspace that is busy' {
            $rs = [runspacefactory]::CreateRunspace()
            $rs.Open()
            $psBusy = [powershell]::Create($rs)
            $r = $psBusy.AddScript('@(1..10).foreach{Start-Sleep -Seconds 1}').InvokeAsync()
            $rs.RunspaceAvailability | Should -Be 'Busy'
            $ps = [powershell]::Create($rs)
            $sb = {
                InvokeAsyncThenWait -PowerShell $ps
            }
            # This test is designed to fail. You cannot invoke PowerShell asynchronously
            # in a runspace that is busy, because pipelines cannot be run concurrently.
            $err = { $sb.Invoke() } | Should -Throw -ErrorId 'AggregateException' -PassThru
            GetInnerErrorId -Exception $err.Exception | Should -Be 'InvalidOperation'
            $count = 0
            while (-not $r.IsCompleted -and $count -lt 10) {
                Start-Sleep -Seconds 1
                $count++
            }
        }

        It 'cannot invoke a single script asynchronously in the current runspace' {
            $ps = [powershell]::Create('CurrentRunspace')
            $sb = {
                InvokeAsyncThenWait -PowerShell $ps
            }
            # This test is designed to fail. You cannot invoke PowerShell asynchronously
            # in the current runspace because nested PowerShell instances cannot be
            # invoked asynchronously
            $err = { $sb.Invoke() } | Should -Throw -ErrorId 'AggregateException' -PassThru
            GetInnerErrorId -Exception $err.Exception | Should -Be 'InvalidOperation'
        }
    }

    Context 'PowerShell::InvokeAsync - Multiple script tests' {
        It 'can invoke multiple scripts asynchronously' {
            $sb1 = [scriptblock]::Create("@(1,3,5,7,9,11,13,15,17,19)${sbStub}")
            $sb2 = [scriptblock]::Create("@(2,4,6,8,10,12,14,16,18,20)${sbStub}")
            $r1 = [powershell]::Create().AddScript($sb1).InvokeAsync()
            $r2 = [powershell]::Create().AddScript($sb2).InvokeAsync()
            [System.Threading.Tasks.Task]::WaitAll(@($r1, $r2))
            $r1.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
            $r1.IsCompletedSuccessfully | Should -Be $true
            $r2.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
            $r2.IsCompletedSuccessfully | Should -Be $true
            $results = @($r1.Result.foreach('Value')) + @($r2.Result.foreach('Value'))
            Compare-Object -ReferenceObject @(1..20) -DifferenceObject $results -SyncWindow 20 | Should -Be $null
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
            $script = [scriptblock]::Create("`$input${sbStub}")
        }

        It 'can invoke multiple scripts asynchronously with input' {
            $r1 = [powershell]::Create().AddScript($script).InvokeAsync($d1)
            $r2 = [powershell]::Create().AddScript($script).InvokeAsync($d2)
            [System.Threading.Tasks.Task]::WaitAll(@($r1, $r2))
            $r1.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
            $r1.IsCompletedSuccessfully | Should -Be $true
            $r2.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
            $r2.IsCompletedSuccessfully | Should -Be $true
            $sortedResults = @($r1.Result) + @($r2.Result) | Sort-Object -Property Time
            Compare-Object -ReferenceObject @(1..20) -DifferenceObject $sortedResults.Value -SyncWindow 20 | Should -Be $null
        }

        It 'can invoke multiple scripts asynchronously with input and capture output' {
            $o = New-Object -TypeName 'System.Management.Automation.PSDataCollection[PSObject]'
            $r1 = [powershell]::Create().AddScript($script).InvokeAsync($d1, $o)
            $r2 = [powershell]::Create().AddScript($script).InvokeAsync($d2, $o)
            [System.Threading.Tasks.Task]::WaitAll(@($r1, $r2))
            $o.Complete()
            $r1.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
            $r1.IsCompletedSuccessfully | Should -Be $true
            $r2.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
            $r2.IsCompletedSuccessfully | Should -Be $true
            $sortedResults = $o | Sort-Object -Property Time
            Compare-Object -ReferenceObject @(1..20) -DifferenceObject $sortedResults.Value -SyncWindow 20 | Should -Be $null
        }
    }
}
