# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'WindowStyle Hidden console flash fix (Issue #3028)' -Tag 'CI' {

    BeforeAll {
        $powershell = Join-Path -Path $PSHOME -ChildPath 'pwsh'
    }

    Context 'Manifest contains consoleAllocationPolicy' {
        It 'pwsh.manifest declares consoleAllocationPolicy as detached' {
            $manifestPath = Join-Path -Path $PSHOME -ChildPath 'pwsh.manifest'
            if (Test-Path $manifestPath) {
                $content = Get-Content $manifestPath -Raw
                $content | Should -Match 'consoleAllocationPolicy'
                $content | Should -Match 'detached'
            } else {
                # Manifest is embedded in the binary at build time; skip file check.
                Set-ItResult -Skipped -Because 'manifest is embedded in binary'
            }
        }
    }

    Context 'WindowStyle Hidden produces correct output' -Skip:(!$IsWindows) {
        It 'captures output from -WindowStyle Hidden -Command' {
            $output = & $powershell -NoProfile -WindowStyle Hidden -Command "'hello'"
            $output | Should -Be 'hello'
        }

        It 'captures pipeline output from -WindowStyle Hidden' {
            $output = & $powershell -NoProfile -WindowStyle Hidden -Command '1..3 | ForEach-Object { $_ * 2 }'
            $output.Count | Should -Be 3
            $output[0] | Should -Be 2
            $output[1] | Should -Be 4
            $output[2] | Should -Be 6
        }

        It 'Write-Host works under -WindowStyle Hidden' {
            # Write-Host writes to the information stream; capture via 6>&1.
            $output = & $powershell -NoProfile -WindowStyle Hidden -Command 'Write-Host "test-output"' 6>&1
            ($output | Out-String) | Should -Match 'test-output'
        }

        It 'exits with correct exit code under -WindowStyle Hidden' {
            & $powershell -NoProfile -WindowStyle Hidden -Command 'exit 42'
            $LASTEXITCODE | Should -Be 42
        }
    }

    Context 'AllocConsoleWithOptions API availability' -Skip:(!$IsWindows) {
        It 'detects AllocConsoleWithOptions on supported Windows builds' {
            $code = @'
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
'@
            Add-Type -TypeDefinition $code
            $available = [ConsoleApiProbe]::IsAllocConsoleWithOptionsAvailable()

            # On Windows 11 26100+, the API should be available.
            $build = [System.Environment]::OSVersion.Version.Build
            if ($build -ge 26100) {
                $available | Should -BeTrue
            } else {
                # On older builds, just verify the probe doesn't crash.
                $available | Should -BeOfType [bool]
            }
        }
    }

    Context 'Normal startup is unaffected' -Skip:(!$IsWindows) {
        It 'starts and runs a command without -WindowStyle' {
            $output = & $powershell -NoProfile -Command '$PSVersionTable.PSEdition'
            $output | Should -Be 'Core'
        }

        It 'handles -WindowStyle Normal without error' {
            $output = & $powershell -NoProfile -WindowStyle Normal -Command "'normal-test'"
            $output | Should -Be 'normal-test'
        }
    }
}
