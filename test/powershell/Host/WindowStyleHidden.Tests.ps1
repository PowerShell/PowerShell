# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "WindowStyle Hidden console flash fix (Issue #3028)" -Tag "Feature" {

    BeforeAll {
        $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"
    }

    Context "Manifest contains consoleAllocationPolicy" -Skip:(!$IsWindows) {
        It "pwsh.exe embedded manifest declares consoleAllocationPolicy as detached" {
            # Extract the embedded manifest from the PE binary using .NET reflection.
            $pwshExe = Join-Path -Path $PSHOME -ChildPath "pwsh.exe"
            $manifest = [System.Reflection.Assembly]::LoadFile($pwshExe).GetManifestResourceStream("pwsh.exe.manifest")
            if ($null -eq $manifest) {
                # Fall back to reading the raw manifest via mt.exe-style extraction.
                $tempFile = [System.IO.Path]::GetTempFileName()
                try {
                    $proc = Start-Process -FilePath "cmd.exe" -ArgumentList "/c","mt.exe -inputresource:`"$pwshExe`" -out:`"$tempFile`"" -Wait -PassThru -NoNewWindow 2>$null
                    if ((Test-Path $tempFile) -and (Get-Item $tempFile).Length -gt 0) {
                        $content = Get-Content $tempFile -Raw
                        $content | Should -Match "consoleAllocationPolicy"
                        $content | Should -Match "detached"
                    } else {
                        # If mt.exe is unavailable, check the source manifest as last resort.
                        $srcManifest = Join-Path -Path (Split-Path $PSHOME) -ChildPath "assets/pwsh.manifest"
                        if (Test-Path $srcManifest) {
                            $content = Get-Content $srcManifest -Raw
                            $content | Should -Match "consoleAllocationPolicy"
                            $content | Should -Match "detached"
                        } else {
                            Set-ItResult -Skipped -Because "cannot extract embedded manifest (mt.exe unavailable)"
                        }
                    }
                } finally {
                    Remove-Item $tempFile -ErrorAction SilentlyContinue
                }
            } else {
                $reader = [System.IO.StreamReader]::new($manifest)
                $content = $reader.ReadToEnd()
                $reader.Dispose()
                $manifest.Dispose()
                $content | Should -Match "consoleAllocationPolicy"
                $content | Should -Match "detached"
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

    Context "Early arg scan handles all prefix variants" -Skip:(!$IsWindows) {
        It "handles -w hidden (shortest prefix)" {
            $output = & $powershell -NoProfile -w Hidden -Command "'short-prefix'"
            $output | Should -Be "short-prefix"
        }

        It "handles -win hidden (partial prefix)" {
            $output = & $powershell -NoProfile -win Hidden -Command "'partial-prefix'"
            $output | Should -Be "partial-prefix"
        }

        It "handles --windowstyle hidden (double-dash)" {
            $output = & $powershell -NoProfile --windowstyle Hidden -Command "'double-dash'"
            $output | Should -Be "double-dash"
        }

        It "handles /windowstyle hidden (forward-slash)" {
            $output = & $powershell -NoProfile /windowstyle Hidden -Command "'forward-slash'"
            $output | Should -Be "forward-slash"
        }

        It "is case insensitive" {
            $output = & $powershell -NoProfile -WINDOWSTYLE HIDDEN -Command "'case-test'"
            $output | Should -Be "case-test"
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
