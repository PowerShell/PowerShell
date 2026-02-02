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
            $sm = (New-Object -ComObject Microsoft.Update.ServiceManager)
            $mu = $sm.Services | Where-Object { $_.ServiceId -eq '7971f918-a847-4430-9279-4a52d1efe18d' }
            if ($mu) {
                return $true
            }
            return $false
        }

        function Unregister-MicrosoftUpdate {
            try {
                $sm = (New-Object -ComObject Microsoft.Update.ServiceManager)
                $mu = $sm.Services | Where-Object { $_.ServiceId -eq '7971f918-a847-4430-9279-4a52d1efe18d' }
                if ($mu) {
                    $sm.RemoveService($mu.ServiceID)
                    return $true
                }
            }
            catch {
                Write-Warning "Failed to unregister Microsoft Update: $_"
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
            $result = & $muScriptPath
            $result | Should -Be 0 -Because "script should exit 0"
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
            $result = & $muScriptPath
            $result | Should -Be 0 -Because "script should exit 0 even when already registered"
            Test-IsMuEnabled | Should -Be $true -Because "Microsoft Update should still be registered"
        }

        It "Should handle timeout gracefully with Hang test hook" {
            if (!(Test-Elevated)) {
                Set-ItResult -Skipped -Because "requires elevation"
                return
            }
            $result = & $muScriptPath -TestHook Hang
            $result | Should -Be 0 -Because "script should exit 0 even on timeout"
        }

        It "Should handle failure gracefully with Fail test hook" {
            if (!(Test-Elevated)) {
                Set-ItResult -Skipped -Because "requires elevation"
                return
            }
            $result = & $muScriptPath -TestHook Fail
            $result | Should -Be 0 -Because "script should exit 0 even on failure"
        }

        AfterEach {
            # Clean up
            Unregister-MicrosoftUpdate | Out-Null
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
            $result = & $manifestScriptPath -Path 'C:\nonexistent'
            $result | Should -Be 0 -Because "script should exit 0 gracefully when files don't exist"
        }

        It "Should exit 0 on successful registration" {
            if (!(Test-Elevated)) {
                Set-ItResult -Skipped -Because "requires elevation"
                return
            }
            $result = & $manifestScriptPath
            $result | Should -Be 0 -Because "script should exit 0"
        }

        It "Should handle unregister gracefully" {
            if (!(Test-Elevated)) {
                Set-ItResult -Skipped -Because "requires elevation"
                return
            }
            $result = & $manifestScriptPath -Unregister
            $result | Should -Be 0 -Because "script should exit 0 on unregister"
        }

        It "Should be idempotent on re-registration" {
            if (!(Test-Elevated)) {
                Set-ItResult -Skipped -Because "requires elevation"
                return
            }
            # Register first time
            $result1 = & $manifestScriptPath
            $result1 | Should -Be 0

            # Register again - should still exit 0
            $result2 = & $manifestScriptPath
            $result2 | Should -Be 0 -Because "script should exit 0 when already registered"
        }
    }
}
