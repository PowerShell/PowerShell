# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-PSHostProcessInfo tests" -Tag CI {
    BeforeAll {
        $si = [System.Diagnostics.ProcessStartInfo]::new()
        $si.FileName = "pwsh"
        $si.Arguments = "-noexit"
        $si.RedirectStandardInput = $true
        $si.RedirectStandardOutput = $true
        $si.RedirectStandardError = $true
        $pwsh = [System.Diagnostics.Process]::Start($si)

        if ($IsWindows) {
            $si.FileName = "powershell"
            $powershell = [System.Diagnostics.Process]::Start($si)
        }
    }

    AfterAll {
        $pwsh | Stop-Process

        if ($IsWindows) {
            $powershell | Stop-Process
        }
    }

    It "Should return own self" {
        (Get-PSHostProcessInfo).ProcessId | Should -Contain $PID
    }

    It "Should return own self window title" {
        $expected = (Get-Process -Id $PID).MainWindowTitle
        (Get-PSHostProcessInfo -Id $PID).MainWindowTitle | Should -BeExactly $expected
    }

    It "Should list info for other PowerShell hosted processes" {
        # Creation of the named pipe is async
        Wait-UntilTrue {
            Get-PSHostProcessInfo | Where-Object { $_.ProcessId -eq $pwsh.Id }
        } | Should -BeTrue
        $pshosts = Get-PSHostProcessInfo
        $pshosts.Count | Should -BeGreaterOrEqual 1
        $pshosts.ProcessId | Should -Contain $pwsh.Id
    }

    It "Should list Windows PowerShell process" -Skip:(!$IsWindows) {
        # Creation of the named pipe is async
        Wait-UntilTrue {
            Get-PSHostProcessInfo | Where-Object { $_.ProcessId -eq $powershell.Id }
        } | Should -BeTrue
        $psProcess = Get-PSHostProcessInfo | Where-Object { $_.ProcessName -eq "powershell" }
        $psProcess.Count | Should -BeGreaterOrEqual 1
        $psProcess.ProcessId | Should -Contain $powershell.id
    }

    It "Verifies named pipe filepath get method" {
        $pipeFilePath = (Get-PSHostProcessInfo -Id $pid).GetPipeNameFilePath()
        $pipeFilePath | Should -Exist
    }

    It "Verifies named pipe filepath is removed on process exit" {
        $aliveFile = Join-Path -Path $TestDrive -ChildPath 'AliveFileXXZZ.txt'
        "" | Out-File -FilePath $aliveFile
        $testfilePath = Join-Path -Path $TestDrive -ChildPath 'TestScriptXXZZ.ps1'
        @'
            param (
                [string] $LiveFilePath
            )

            $count = 0
            while ((Test-Path -Path $LiveFilePath) -and ($count++ -lt 60))
            {
                Start-Sleep -Milliseconds 500
            }

            exit
'@ | Out-File -FilePath $testfilePath

        # Create PowerShell process to monitor.
        $psFileName = $IsWindows ? 'pwsh.exe' : 'pwsh'
        $psPath = Join-Path -Path $PSHOME -ChildPath $psFileName
        $psProc = Start-Process -FilePath $psPath -ArgumentList "-noprofile -File $testfilePath -LiveFilePath $aliveFile" -PassThru
        Wait-UntilTrue -sb {
            (Get-PSHostProcessInfo -Id $psProc.Id) -ne $null
        } -TimeoutInMilliseconds 5000 -IntervalInMilliseconds 250

        # Verify named pipe file path.
        $psNamedPipePath = (Get-PSHostProcessInfo -Id $psProc.Id).GetPipeNameFilePath()
        $psNamedPipePath | Should -Exist

        # Signal PowerShell test process to exit normally.
        Remove-Item -Path $aliveFile -Force -ErrorAction Ignore
        Wait-UntilTrue -sb {
            (Test-Path -Path $psNamedPipePath) -eq $false
        } -TimeoutInMilliseconds 5000 -IntervalInMilliseconds 250

        # Verify named pipe file path is removed.
        $psNamedPipePath | Should -Not -Exist
    }
}
