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
            $npInfo = [System.Management.Automation.Runspaces.NamedPipeConnectionInfo]::new($pwsh.Id)
            $rs = [runspacefactory]::CreateRunspace($npInfo)
            $rs.Open()
            $ps = [powershell]::Create()
            $ps.Runspace = $rs
            $ps.AddScript('$pid').Invoke() | Should -Be $pwsh.Id
            $rs.Dispose()
        }
    }

    Context "By DebugPipeName" {
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
            $si.Arguments = "-debugpipename $pipeName -noexit -command 'pwsh -debugpipename $pipeName' > '$pwsh_started'"
            $si.RedirectStandardInput = $true
            $si.RedirectStandardOutput = $true
            $si.RedirectStandardError = $true
            $pwsh = [System.Diagnostics.Process]::Start($si)
        }

        AfterAll {
            $pwsh | Stop-Process
            Remove-Item $pwsh_started -Force -ErrorAction SilentlyContinue
        }

        It "Can enter using DebugPipeName" {
            Wait-UntilTrue { Test-Path $pipePath }

            "Enter-PSHostProcess -DebugPipeName $pipeName -ErrorAction Stop
            `$pid
            Exit-PSHostProcess" | pwsh -c - | Should -Be $pwsh.Id
        }

        It "Can enter, exit, and re-enter using DebugPipeName" {
            Wait-UntilTrue { Test-Path $pipePath }

            "Enter-PSHostProcess -DebugPipeName $pipeName -ErrorAction Stop
            `$pid
            Exit-PSHostProcess" | pwsh -c - | Should -Be $pwsh.Id

            "Enter-PSHostProcess -DebugPipeName $pipeName -ErrorAction Stop
            `$pid
            Exit-PSHostProcess" | pwsh -c - | Should -Be $pwsh.Id
        }

        It "Should throw if DebugPipeName does not exist" {
            Wait-UntilTrue { Test-Path $pipePath }

            { Enter-PSHostProcess -DebugPipeName badpipename } | Should -Throw -ExpectedMessage "No named pipe was found with DebugPipeName: badpipename."
        }

        It "Should throw if DebugPipeName is too long on Linux or macOS" {
            $longPipeName = "DoggoipsumwaggywagssmolborkingdoggowithalongsnootforpatsdoingmeafrightenporgoYapperporgolongwatershoobcloudsbigolpupperlengthboy"

            if (!$IsWindows) {
                pwsh -debugpipename $longPipeName -c `"$longPipeName`" | Should -BeLike "Cannot process the command because -DebugPipeName specified is too long.*"
            } else {
                pwsh -debugpipename $longPipeName -c `"$longPipeName`"
                $LASTEXITCODE | Should -Be 0
            }
        }

        It "Should be able to change the pipename via the API" {
            Wait-UntilTrue { Test-Path $pipePath }

            $pipeName1 = "myTestPipe1"
            $pipeName2 = "myTestPipe2"

            [System.Management.Automation.Remoting.RemoteSessionNamedPipeServer]::CreateCustomNamedPipeServer($pipeName1)
            Get-PipePath -PipeName $pipeName1 | Test-Path | Should -BeTrue

            # The second call to this method would override the first named pipe.
            [System.Management.Automation.Remoting.RemoteSessionNamedPipeServer]::CreateCustomNamedPipeServer($pipeName2)
            Get-PipePath -PipeName $pipeName2 | Test-Path | Should -BeTrue

            # Previous pipe should have been cleaned up.
            Get-PipePath -PipeName $pipeName1 | Test-Path | Should -BeFalse
        }

        It "Should throw if a pipe name is too long on Linux or macOS" {
            $longPipeName = "DoggoipsumwaggywagssmolborkingdoggowithalongsnootforpatsdoingmeafrightenporgoYapperporgolongwatershoobcloudsbigolpupperlengthboy"

            if (!$IsWindows) {
                { [System.Management.Automation.Remoting.RemoteSessionNamedPipeServer]::CreateCustomNamedPipeServer($longPipeName) } |
                    Should -Throw -ErrorId InvalidOperationException
            } else {
                { [System.Management.Automation.Remoting.RemoteSessionNamedPipeServer]::CreateCustomNamedPipeServer($longPipeName) } |
                    Should -Not -Throw
            }
        }
    }
}
