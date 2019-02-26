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

        It 'can invoke multiple scripts asynchronously with input' {
            $d1 = New-Object -TypeName 'System.Management.Automation.PSDataCollection[PSObject]'
            $d2 = New-Object -TypeName 'System.Management.Automation.PSDataCollection[PSObject]'
            foreach ($i in 1..20) {
                $d1.Add($foreach.Current)
                $foreach.MoveNext() > $null
                $d2.Add($foreach.Current)
            }
            $script = @'
@($input).foreach{
    [pscustomobject]@{
        Time = [DateTime]::Now.ToString('yyyyMMddTHHmmss.fffffff')
        Value = $_
        ThreadId = [System.Threading.Thread]::CurrentThread.ManagedThreadId
    }
    Start-Sleep -Milliseconds 500
}
'@
            $r1 = [powershell]::Create().AddScript($script).InvokeAsync($d1)
            $r2 = [powershell]::Create().AddScript($script).InvokeAsync($d2)
            [System.Threading.Tasks.Task]::WaitAll(@($r1, $r2))
            $r1.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
            $r1.IsCompletedSuccessfully | Should -Be $true
            $r2.Status | Should -Be ([System.Threading.Tasks.TaskStatus]::RanToCompletion)
            $r2.IsCompletedSuccessfully | Should -Be $true
            $groupedResults = @($r1.Result) + @($r2.Result) | Group-Object -Property ThreadId
            Compare-Object -ReferenceObject @(1..20) -DifferenceObject $groupedResults.Group.Value -SyncWindow 20 | Should -Be $null
        }

        <#
        public async Task<PSDataCollection<PSObject>> InvokeAsync<T>(PSDataCollection<T> input)
            => await Task<PSDataCollection<PSObject>>.Factory.FromAsync(BeginInvoke<T>(input), pResult => EndInvoke(pResult)).ConfigureAwait(false);

        public async Task<PSDataCollection<PSObject>> InvokeAsync<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output)
            => await Task<PSDataCollection<PSObject>>.Factory.FromAsync(BeginInvoke<TInput, TOutput>(input, output), pResult => EndInvoke(pResult)).ConfigureAwait(false);

        public async Task<PSDataCollection<PSObject>> InvokeAsync<T>(PSDataCollection<T> input, PSInvocationSettings settings, AsyncCallback callback, object state)
            => await Task<PSDataCollection<PSObject>>.Factory.FromAsync(BeginInvoke<T>(input, settings, callback, state), pResult => EndInvoke(pResult)).ConfigureAwait(false);

        public async Task<PSDataCollection<PSObject>> InvokeAsync<TInput, TOutput>(PSDataCollection<TInput> input, PSDataCollection<TOutput> output, PSInvocationSettings settings, AsyncCallback callback, object state)
            => await Task<PSDataCollection<PSObject>>.Factory.FromAsync(BeginInvoke<TInput, TOutput>(input, output, settings, callback, state), pResult => EndInvoke(pResult)).ConfigureAwait(false);
        #>

        <#
        It 'can create instance with runspace' {
            $rs = [runspacefactory]::CreateRunspace()
            $ps = [powershell]::Create($rs)
            $ps | Should -Not -BeNullOrEmpty
            $ps.Runspace | Should -Be $rs
            $ps.Dispose()
            $rs.Dispose()
        }

        It 'cannot create instance with null runspace' {
            { [powershell]::Create([runspace]$null) } | Should -Throw -ErrorId 'PSArgumentNullException'
        }

        It 'can load the default snapin "Microsoft.WSMan.Management"' -skip:(-not $IsWindows) {
            $ps = [powershell]::Create()
            $ps.AddScript('Get-Command -Name Test-WSMan') > $null

            $result = $ps.Invoke()
            $result.Count | Should -Be 1
            $result[0].Source | Should -BeExactly 'Microsoft.WSMan.Management'
        }
    }

    Context 'executioncontext' {
        It 'args are passed correctly' {
            $result = $ExecutionContext.SessionState.InvokeCommand.InvokeScript('"`$args:($args); `$input:($input)"', 1, 2, 3)
            $result | Should -BeExactly '$args:(1 2 3); $input:()'
        }
        #>
    }
}
