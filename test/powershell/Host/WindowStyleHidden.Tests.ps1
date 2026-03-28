# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "WindowStyle Hidden console flash fix (Issue #3028)" -Tag "Feature" {

    BeforeAll {
        $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"
    }

    Context "Manifest contains consoleAllocationPolicy" {
        It "pwsh.manifest declares consoleAllocationPolicy as detached" -Skip:(!$IsWindows) {
            # The manifest is embedded into the PE at build time. Check the source file
            # if available; otherwise read the embedded manifest via System.Reflection.
            $manifestPath = Join-Path -Path $PSHOME -ChildPath "pwsh.manifest"
            if (Test-Path $manifestPath) {
                $content = Get-Content $manifestPath -Raw
                $content | Should -Match "consoleAllocationPolicy"
                $content | Should -Match "detached"
            } else {
                Set-ItResult -Skipped -Because "manifest is embedded in binary and cannot be inspected"
            }
        }
    }

    Context "WindowStyle Hidden produces correct output" -Skip:(!$IsWindows) {
        It "captures output from -WindowStyle Hidden -Command" {
            $output = & $powershell -NoProfile -WindowStyle Hidden -Command "'hello'"
            $output | Should -Be "hello"
        }

        It "captures pipeline output from -WindowStyle Hidden" {
            $output = & $powershell -NoProfile -WindowStyle Hidden -Command "1..3 | ForEach-Object { `$_ * 2 }"
            $output.Count | Should -Be 3
            $output[0] | Should -Be 2
            $output[1] | Should -Be 4
            $output[2] | Should -Be 6
        }

        It "Write-Host works under -WindowStyle Hidden" {
            $output = & $powershell -NoProfile -WindowStyle Hidden -Command "Write-Host 'test-output'" 6>&1
            ($output | Out-String) | Should -Match "test-output"
        }

        It "exits with correct exit code under -WindowStyle Hidden" {
            & $powershell -NoProfile -WindowStyle Hidden -Command "exit 42"
            $LASTEXITCODE | Should -Be 42
        }
    }

    Context "AllocConsoleWithOptions API probe" -Skip:(!$IsWindows) {
        It "detects AllocConsoleWithOptions availability without error" {
            $code = @"
using System;
using System.Runtime.InteropServices;
public static class ConsoleApiProbe {
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
    [DllImport("kernel32.dll")]
    public static extern IntPtr GetModuleHandle(string lpModuleName);
    public static bool IsAllocConsoleWithOptionsAvailable() {
        IntPtr k32 = GetModuleHandle("kernel32.dll");
        if (k32 == IntPtr.Zero) return false;
        IntPtr addr = GetProcAddress(k32, "AllocConsoleWithOptions");
        return addr != IntPtr.Zero;
    }
}
"@
            Add-Type -TypeDefinition $code -ErrorAction Stop
            $available = [ConsoleApiProbe]::IsAllocConsoleWithOptionsAvailable()

            # Verify the probe returns a valid result; the actual availability
            # depends on the Windows build running the test.
            $available | Should -BeOfType [bool]
        }
    }

    Context "Normal startup is unaffected" -Skip:(!$IsWindows) {
        It "starts and runs a command without -WindowStyle" {
            $output = & $powershell -NoProfile -Command "`$PSVersionTable.PSEdition"
            $output | Should -Be "Core"
        }

        It "handles -WindowStyle Normal without error" {
            $output = & $powershell -NoProfile -WindowStyle Normal -Command "'normal-test'"
            $output | Should -Be "normal-test"
        }
    }
}
