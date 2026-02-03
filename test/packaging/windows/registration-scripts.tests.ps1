# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe -Name "Registration Scripts" -Fixture {
    BeforeAll {
        Set-StrictMode -Off

        function Test-Elevated {
            [CmdletBinding()]
            [OutputType([bool])]
            Param()

            return (([Security.Principal.WindowsIdentity]::GetCurrent()).Groups -contains "S-1-5-32-544")
        }

        function Test-IsMuEnabled {
            $sm = $null
            try {
                $sm = New-Object -ComObject Microsoft.Update.ServiceManager
                $mu = $sm.Services | Where-Object { $_.ServiceId -eq '7971f918-a847-4430-9279-4a52d1efe18d' }
                if ($mu) {
                    return $true
                }
                return $false
            }
            finally {
                if ($null -ne $sm) {
                    [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($sm)
                }
            }
        }

        function Unregister-MicrosoftUpdate {
            $sm = $null
            try {
                $sm = New-Object -ComObject Microsoft.Update.ServiceManager
                $mu = $sm.Services | Where-Object { $_.ServiceId -eq '7971f918-a847-4430-9279-4a52d1efe18d' }
                if ($mu) {
                    $sm.RemoveService($mu.ServiceID)
                    return $true
                }
            }
            catch {
                Write-Warning "Failed to unregister Microsoft Update: $_"
            }
            finally {
                if ($null -ne $sm) {
                    [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($sm)
                }
            }
            return $false
        }

        $registrationScriptsPath = Join-Path (Split-Path $PSScriptRoot -Parent | Split-Path -Parent | Split-Path -Parent) 'src\PowerShell.Core.Instrumentation'
        $muScriptPath = Join-Path (Split-Path $PSScriptRoot -Parent | Split-Path -Parent | Split-Path -Parent) 'assets\MicrosoftUpdate\RegisterMicrosoftUpdate.ps1'

        if (!(Test-Path $registrationScriptsPath)) {
            Write-Warning "Registration scripts path not found: $registrationScriptsPath"
        }
        if (!(Test-Path $muScriptPath)) {
            Write-Warning "MU script path not found: $muScriptPath"
        }
    }

    Context "RegisterMicrosoftUpdate.ps1" {
        BeforeEach {
            # Ensure MU is not registered before each test
            Unregister-MicrosoftUpdate | Out-Null
        }

        It "Should register Microsoft Update when not already registered" {
            if (!(Test-Elevated)) {
                Set-ItResult -Skipped -Because "requires elevation"
                return
            }
            & $muScriptPath
            $LASTEXITCODE | Should -Be 0 -Because "script should exit 0"
            Test-IsMuEnabled | Should -Be $true -Because "Microsoft Update should be registered"
        }

        It "Should exit 0 when already registered (idempotent)" {
            if (!(Test-Elevated)) {
                Set-ItResult -Skipped -Because "requires elevation"
                return
            }
            # Register first time
            & $muScriptPath | Out-Null
            
            # Try to register again
            & $muScriptPath
            $LASTEXITCODE | Should -Be 0 -Because "script should exit 0 even when already registered"
            Test-IsMuEnabled | Should -Be $true -Because "Microsoft Update should still be registered"
        }

        It "Should handle timeout gracefully with Hang test hook" {
            if (!(Test-Elevated)) {
                Set-ItResult -Skipped -Because "requires elevation"
                return
            }
            & $muScriptPath -TestHook Hang
            $LASTEXITCODE | Should -Be 0 -Because "script should exit 0 even on timeout"
        }

        It "Should handle failure gracefully with Fail test hook" {
            if (!(Test-Elevated)) {
                Set-ItResult -Skipped -Because "requires elevation"
                return
            }
            & $muScriptPath -TestHook Fail
            $LASTEXITCODE | Should -Be 0 -Because "script should exit 0 even on failure"
        }
    }

    Context "RegisterManifest.ps1" {
        BeforeAll {
            $manifestScriptPath = Join-Path $registrationScriptsPath 'RegisterManifest.ps1'
            $manifestPath = Join-Path $registrationScriptsPath 'PowerShell.Core.Instrumentation.man'
            $binaryPath = Join-Path $registrationScriptsPath 'PowerShell.Core.Instrumentation.dll'

            if (!(Test-Path $manifestScriptPath)) {
                Write-Warning "Manifest script not found: $manifestScriptPath"
            }
        }

        It "Should not fail when manifest files don't exist" {
            if (!(Test-Elevated)) {
                Set-ItResult -Skipped -Because "requires elevation"
                return
            }
            & $manifestScriptPath -Path 'C:\nonexistent'
            $LASTEXITCODE | Should -Be 0 -Because "script should exit 0 gracefully when files don't exist"
        }

        It "Should exit 0 on successful registration" {
            if (!(Test-Elevated)) {
                Set-ItResult -Skipped -Because "requires elevation"
                return
            }
            & $manifestScriptPath
            $LASTEXITCODE | Should -Be 0 -Because "script should exit 0"
        }

        It "Should handle unregister gracefully" {
            if (!(Test-Elevated)) {
                Set-ItResult -Skipped -Because "requires elevation"
                return
            }
            & $manifestScriptPath -Unregister
            $LASTEXITCODE | Should -Be 0 -Because "script should exit 0 on unregister"
        }

        It "Should be idempotent on re-registration" {
            if (!(Test-Elevated)) {
                Set-ItResult -Skipped -Because "requires elevation"
                return
            }
            # Register first time
            & $manifestScriptPath
            $LASTEXITCODE | Should -Be 0

            # Register again - should still exit 0
            & $manifestScriptPath
            $LASTEXITCODE | Should -Be 0 -Because "script should exit 0 when already registered"
        }
    }
}
