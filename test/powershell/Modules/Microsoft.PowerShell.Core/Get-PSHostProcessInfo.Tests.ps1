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
    }

    AfterAll {
        $pwsh | Stop-Process
    }

    It "Should return own self" {
        Get-PSHostProcessInfo | Select-Object -ExpandProperty ProcessId | Should -Contain $pid
    }

    It "Should list info for other PowerShell hosted processes" {
        # Creation of the named pipe is async
        Wait-UntilTrue {
            Get-PSHostProcessInfo | Where-Object { $_.ProcessId -eq $pwsh.Id }
        }
        $pshosts = Get-PSHostProcessInfo
        $pshosts.Count | Should -BeGreaterOrEqual 1
        $pshosts | Select-Object -ExpandProperty ProcessId | Should -Contain $pwsh.Id
    }
}
