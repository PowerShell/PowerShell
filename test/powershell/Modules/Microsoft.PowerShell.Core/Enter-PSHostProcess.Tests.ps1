# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Enter-PSHostProcess tests" -Tag Feature {
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

    It "Can enter using NamedPipeConnectionInfo" {
        $npInfo = [System.Management.Automation.Runspaces.NamedPipeConnectionInfo]::new($pwsh.Id)
        $rs = [runspacefactory]::CreateRunspace($npInfo)
        $rs.Open()
        $ps = [powershell]::Create()
        $ps.Runspace = $rs
        $ps.AddScript('$pid').Invoke() | Should -Be $pwsh.Id
        $rs.Dispose()
    }
}
