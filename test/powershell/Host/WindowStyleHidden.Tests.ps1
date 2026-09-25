# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "WindowStyle Hidden console flash fix (Issue #3028)" -Tag @('CI','Feature') {

    BeforeAll {
        $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"
    }

    Context "Manifest contains consoleAllocationPolicy" {
        It "pwsh.exe embedded manifest declares consoleAllocationPolicy as detached" -Skip:(!$IsWindows) {
            # Extract the embedded manifest from the PE binary.
            # pwsh.exe is a native binary, so use Win32 resource extraction.
            $pwshExe = Join-Path -Path $PSHOME -ChildPath "pwsh.exe"
            $tempFile = [System.IO.Path]::GetTempFileName()
            try {
                $proc = Start-Process -FilePath "cmd.exe" -ArgumentList "/c","mt.exe -inputresource:`"$pwshExe`" -out:`"$tempFile`"" -Wait -PassThru -NoNewWindow 2>$null
                if ((Test-Path $tempFile) -and (Get-Item $tempFile).Length -gt 0) {
                    $content = Get-Content $tempFile -Raw
                    $content | Should -Match "consoleAllocationPolicy"
                    $content | Should -Match "detached"
                } else {
                    # If mt.exe is unavailable, check the source manifest relative to the repo root.
                    $repoRoot = Split-Path -Path (Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent) -Parent
                    $srcManifest = Join-Path -Path $repoRoot -ChildPath "assets/pwsh.manifest"
                    if (Test-Path $srcManifest) {
                        $content = Get-Content $srcManifest -Raw
                        $content | Should -Match "consoleAllocationPolicy"
                        $content | Should -Match "detached"
                    } else {
                        Set-ItResult -Skipped -Because "cannot extract embedded manifest (mt.exe unavailable and source manifest not found)"
                        return
                    }
                }
            } finally {
                Remove-Item $tempFile -ErrorAction SilentlyContinue
            }
        }
    }

    Context "WindowStyle Hidden produces correct output" {
        It "captures output from -WindowStyle Hidden -Command" -Skip:(!$IsWindows) {
            $output = & $powershell -NoProfile -WindowStyle Hidden -Command "'hello'"
            $output | Should -Be "hello"
        }

        It "captures pipeline output from -WindowStyle Hidden" -Skip:(!$IsWindows) {
            $output = & $powershell -NoProfile -WindowStyle Hidden -Command "1..3 | ForEach-Object { `$_ * 2 }"
            $output.Count | Should -Be 3
            $output[0] | Should -Be 2
            $output[1] | Should -Be 4
            $output[2] | Should -Be 6
        }

        It "Write-Host works under -WindowStyle Hidden" -Skip:(!$IsWindows) {
            $output = & $powershell -NoProfile -WindowStyle Hidden -Command "Write-Host 'test-output'" 6>&1
            ($output | Out-String) | Should -Match "test-output"
        }

        It "exits with correct exit code under -WindowStyle Hidden" -Skip:(!$IsWindows) {
            & $powershell -NoProfile -WindowStyle Hidden -Command "exit 42"
            $LASTEXITCODE | Should -Be 42
        }
    }

    Context "Early arg scan handles all prefix variants" {
        It "handles -w hidden (shortest prefix)" -Skip:(!$IsWindows) {
            $output = & $powershell -NoProfile -w Hidden -Command "'short-prefix'"
            $output | Should -Be "short-prefix"
        }

        It "handles -win hidden (partial prefix)" -Skip:(!$IsWindows) {
            $output = & $powershell -NoProfile -win Hidden -Command "'partial-prefix'"
            $output | Should -Be "partial-prefix"
        }

        It "handles --windowstyle hidden (double-dash)" -Skip:(!$IsWindows) {
            $output = & $powershell -NoProfile --windowstyle Hidden -Command "'double-dash'"
            $output | Should -Be "double-dash"
        }

        It "handles /windowstyle hidden (forward-slash)" -Skip:(!$IsWindows) {
            $output = & $powershell -NoProfile /windowstyle Hidden -Command "'forward-slash'"
            $output | Should -Be "forward-slash"
        }

        It "is case insensitive" -Skip:(!$IsWindows) {
            $output = & $powershell -NoProfile -WINDOWSTYLE HIDDEN -Command "'case-test'"
            $output | Should -Be "case-test"
        }
    }

    Context "Normal startup is unaffected" {
        It "starts and runs a command without -WindowStyle" -Skip:(!$IsWindows) {
            $output = & $powershell -NoProfile -Command "`$PSVersionTable.PSEdition"
            $output | Should -Be "Core"
        }

        It "handles -WindowStyle Normal without error" -Skip:(!$IsWindows) {
            $output = & $powershell -NoProfile -WindowStyle Normal -Command "'normal-test'"
            $output | Should -Be "normal-test"
        }
    }
}
