# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Enter-PSHostProcess tests" -Tag Feature {
    Context "By Process Id" {
        BeforeAll {
            $pwsh_started = New-TemporaryFile
            $si = [System.Diagnostics.ProcessStartInfo]::new()
            $si.FileName = "pwsh"
            $si.Arguments = "-noexit -command 'pwsh' > '$pwsh_started'"
            $si.RedirectStandardInput = $true
            $si.RedirectStandardOutput = $true
            $si.RedirectStandardError = $true
            $pwsh = [System.Diagnostics.Process]::Start($si)

            if ($IsWindows) {
                $powershell_started = New-TemporaryFile
                $si.FileName = "powershell"
                $si.Arguments = "-noexit -command 'powershell' >'$powershell_started'"
                $powershell = [System.Diagnostics.Process]::Start($si)
            }

        }

        AfterAll {
            $pwsh | Stop-Process
            Remove-Item $pwsh_started -Force -ErrorAction SilentlyContinue

            if ($IsWindows) {
                $powershell | Stop-Process
                Remove-Item $powershell_started -Force -ErrorAction SilentlyContinue
            }
        }

        It "Can enter and exit another PSHost" {
            Wait-UntilTrue { (Get-PSHostProcessInfo -Id $pwsh.Id) -ne $null }

            "Enter-PSHostProcess -Id $($pwsh.Id) -ErrorAction Stop
            `$pid
            Exit-PSHostProcess" | pwsh -c - | Should -Be $pwsh.Id
        }

        It "Can enter and exit another Windows PowerShell PSHost" -Skip:(!$IsWindows) {
            Wait-UntilTrue { (Get-PSHostProcessInfo -Id $powershell.Id) -ne $null }

            "Enter-PSHostProcess -Id $($powershell.Id) -ErrorAction Stop
            `$pid
            Exit-PSHostProcess" | pwsh -c - | Should -Be $powershell.Id
        }

        It "Can enter using NamedPipeConnectionInfo" {
            try {
                $npInfo = [System.Management.Automation.Runspaces.NamedPipeConnectionInfo]::new($pwsh.Id)
                $rs = [runspacefactory]::CreateRunspace($npInfo)
                $rs.Open()
                $ps = [powershell]::Create()
                $ps.Runspace = $rs
                $ps.AddScript('$pid').Invoke() | Should -Be $pwsh.Id
            } finally {
                $rs.Dispose()
            }
        }
    }

    Context "By CustomPipeName" {
        BeforeAll {
            # Helper function to get the correct path for the named pipe.
            function Get-PipePath {
                param (
                    $PipeName
                )
                if ($IsWindows) {
                    return "\\.\pipe\$PipeName"
                }
                "$([System.IO.Path]::GetTempPath())CoreFxPipe_$PipeName"
            }

            $pipeName = [System.IO.Path]::GetRandomFileName()
            $pipePath = Get-PipePath -PipeName $pipeName
            $pwsh_started = New-TemporaryFile
            $si = [System.Diagnostics.ProcessStartInfo]::new()
            $si.FileName = "pwsh"
            $si.Arguments = "-CustomPipeName $pipeName -noexit -command 'pwsh -CustomPipeName $pipeName' > '$pwsh_started'"
            $si.RedirectStandardInput = $true
            $si.RedirectStandardOutput = $true
            $si.RedirectStandardError = $true
            $pwsh = [System.Diagnostics.Process]::Start($si)
        }

        AfterAll {
            $pwsh | Stop-Process
            Remove-Item $pwsh_started -Force -ErrorAction SilentlyContinue
        }

        It "Can enter using CustomPipeName" {
            Wait-UntilTrue { Test-Path $pipePath }

            "Enter-PSHostProcess -CustomPipeName $pipeName -ErrorAction Stop
            `$pid
            Exit-PSHostProcess" | pwsh -c - | Should -Be $pwsh.Id
        }

        It "Can enter, exit, and re-enter using CustomPipeName" {
            Wait-UntilTrue { Test-Path $pipePath }

            "Enter-PSHostProcess -CustomPipeName $pipeName -ErrorAction Stop
            `$pid
            Exit-PSHostProcess" | pwsh -c - | Should -Be $pwsh.Id

            "Enter-PSHostProcess -CustomPipeName $pipeName -ErrorAction Stop
            `$pid
            Exit-PSHostProcess" | pwsh -c - | Should -Be $pwsh.Id
        }

        It "Should throw if CustomPipeName does not exist" {
            Wait-UntilTrue { Test-Path $pipePath }

            { Enter-PSHostProcess -CustomPipeName badpipename } | Should -Throw -ExpectedMessage "No named pipe was found with CustomPipeName: badpipename."
        }

        It "Should throw if CustomPipeName is too long on Linux or macOS" {
            $longPipeName = "DoggoipsumwaggywagssmolborkingdoggowithalongsnootforpatsdoingmeafrightenporgoYapperporgolongwatershoobcloudsbigolpupperlengthboy"

            if (!$IsWindows) {
                "`$pid" | pwsh -CustomPipeName $longPipeName -c -
                # 64 is the ExitCode for BadCommandLineParameter
                $LASTEXITCODE | Should -Be 64
            } else {
                "`$pid" | pwsh -CustomPipeName $longPipeName -c -
                $LASTEXITCODE | Should -Be 0
            }
        }
    }
}
