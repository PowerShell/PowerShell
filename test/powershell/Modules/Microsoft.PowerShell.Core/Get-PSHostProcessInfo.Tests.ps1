# Copyright (c) Microsoft Corporation. All rights reserved.
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
        (Get-PSHostProcessInfo).ProcessId | Should -Contain $pid
    }

    It "Should list info for other PowerShell hosted processes" {
        # Creation of the named pipe is async
        Wait-UntilTrue {
            Get-PSHostProcessInfo | Where-Object { $_.ProcessId -eq $pwsh.Id }
        }
        $pshosts = Get-PSHostProcessInfo
        $pshosts.Count | Should -BeGreaterOrEqual 1
        $pshosts.ProcessId | Should -Contain $pwsh.Id
    }

    It "Should list Windows PowerShell process" -Skip:(!$IsWindows) {
        # Creation of the named pipe is async
        Wait-UntilTrue {
            Get-PSHostProcessInfo | Where-Object { $_.ProcessId -eq $powershell.Id }
        }
        $psProcess = Get-PSHostProcessInfo | Where-Object { $_.ProcessName -eq "powershell" }
        $psProcess.Count | Should -BeGreaterOrEqual 1
        $psProcess.ProcessId | Should -Contain $powershell.id
    }
}
