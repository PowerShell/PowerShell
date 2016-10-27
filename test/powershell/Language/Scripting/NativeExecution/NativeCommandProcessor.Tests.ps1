Describe "Native Command Processor" -tags "CI" {

    BeforeAll {
        # Find where test/powershell is so we can find the createchildprocess command relative to it
        $powershellTestDir = $PSScriptRoot
        while ($powershellTestDir -notmatch 'test[\\/]powershell$') {
            $powershellTestDir = Split-Path $powershellTestDir
        }
        $createchildprocess = Join-Path (Split-Path $powershellTestDir) tools/CreateChildProcess/bin/createchildprocess
    }

    # If powershell receives a StopProcessing, it should kill the native process and all child processes

    It "Should kill native process tree" {
        
        # make sure no test processes are running
        Get-Process createchildprocess -ErrorAction SilentlyContinue | Stop-Process
        
        [int] $numToCreate = 2

        $ps = [PowerShell]::Create().AddCommand($createchildprocess)
        $ps.AddParameter($numToCreate)
        $async = $ps.BeginInvoke()

        [bool] $found = $false
        while (!$found)
        {
            $childprocesses = Get-Process createchildprocess -ErrorAction SilentlyContinue
            if ($childprocesses.count -eq $numToCreate+1)
            {
                $found = $true
            }
        }

        $ps.Stop() | Out-Null

        Get-Process createchildprocess -ErrorAction SilentlyContinue | Should BeNullOrEmpty
    }

}
