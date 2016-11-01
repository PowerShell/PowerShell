Describe "Native Command Processor" -tags "Feature" {

    BeforeAll {
        # Find where test/powershell is so we can find the createchildprocess command relative to it
        $powershellTestDir = $PSScriptRoot
        while ($powershellTestDir -notmatch 'test[\\/]powershell$') {
            $powershellTestDir = Split-Path $powershellTestDir
        }
        $createchildprocess = Join-Path (Split-Path $powershellTestDir) tools/CreateChildProcess/bin/createchildprocess
    }

    # If powershell receives a StopProcessing, it should kill the native process and all child processes

    # this test should pass and no longer Pending when #2561 is fixed
    It "Should kill native process tree" -Pending {

        # make sure no test processes are running
        # on Linux, the Process class truncates the name so filter using Where-Object
        Get-Process | Where-Object {$_.Name -like 'createchildproc*'} | Stop-Process
        
        [int] $numToCreate = 2

        $ps = [PowerShell]::Create().AddCommand($createchildprocess)
        $ps.AddParameter($numToCreate)
        $async = $ps.BeginInvoke()
        $ps.InvocationStateInfo.State | Should Be "Running"

        [bool] $childrenCreated = $false
        while (-not $childrenCreated)
        {
            $childprocesses = Get-Process | Where-Object {$_.Name -like 'createchildproc*'} 
            if ($childprocesses.count -eq $numToCreate+1)
            {
                $childrenCreated = $true
            }
        }

        $startTime = Get-Date
        $beginsync = $ps.BeginStop($null, $async)
        # wait no more than 5 secs for the processes to be terminated, otherwise test has failed
        while (((Get-Date) - $startTime).TotalSeconds -lt 5)
        {
            if (($childprocesses.hasexited -eq $true).count -eq $numToCreate+1)
            {
                break
            }
        }
        $childprocesses = Get-Process | Where-Object {$_.Name -like 'createchildproc*'}
        $count = $childprocesses.count 
        $childprocesses | Stop-Process
        $count | Should Be 0
    }
}
