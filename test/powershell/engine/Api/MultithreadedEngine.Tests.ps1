Describe 'Multithreaded engine APIs' -Tags 'CI' {
    Context 'PowerShell::InvokeAsync' {
        It 'can invoke a single script asynchronously' {
            $r = [powershell]::Create().AddScript(@'
@(1,2,3,4,5).foreach{
    [pscustomobject]@{
        Time = [DateTime]::Now.ToString('yyyyMMddTHHmmss.fffffff')
        Value = $_
        ThreadId = [System.Threading.Thread]::CurrentThread.ManagedThreadId
    }
    Start-Sleep -Milliseconds 500
}
'@).InvokeAsync()
            [System.Threading.Tasks.Task]::WaitAll(@($r))
            $r.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
            $r.IsCompletedSuccessfully | Should -Be $true
        }

        It 'can invoke multiple scripts asynchronously' {
            $r1 = [powershell]::Create().AddScript(@'
@(1,3,5,7,9,11,13,15,17,19).foreach{
    [pscustomobject]@{
        Time = [DateTime]::Now.ToString('yyyyMMddTHHmmss.fffffff')
        Value = $_
        ThreadId = [System.Threading.Thread]::CurrentThread.ManagedThreadId
    }
    Start-Sleep -Milliseconds 500
}
'@).InvokeAsync()
            $r2 = [powershell]::Create().AddScript(@'
@(2,4,6,8,10,12,14,16,18,20).foreach{
    [pscustomobject]@{
        Time = [DateTime]::Now.ToString('yyyyMMddTHHmmss.fffffff')
        Value = $_
        ThreadId = [System.Threading.Thread]::CurrentThread.ManagedThreadId
    }
    Start-Sleep -Milliseconds 500
}
'@).InvokeAsync()
            [System.Threading.Tasks.Task]::WaitAll(@($r1, $r2))
            $r1.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
            $r1.IsCompletedSuccessfully | Should -Be $true
            $r2.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
            $r2.IsCompletedSuccessfully | Should -Be $true
            $results = @($r1.Result.foreach('Value')) + @($r2.Result.foreach('Value'))
            Compare-Object -ReferenceObject @(1..20) -DifferenceObject $results -SyncWindow 20 | Should -Be $null
        }
    }

    Context 'PowerShell::InvokeAsync with input and output' {
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
            $script = @'
$input.foreach{
    [pscustomobject]@{
        Time = [DateTime]::Now.ToString('yyyyMMddTHHmmss.fffffff')
        Value = $_
        ThreadId = [System.Threading.Thread]::CurrentThread.ManagedThreadId
    }
    Start-Sleep -Milliseconds 500
}
'@
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
