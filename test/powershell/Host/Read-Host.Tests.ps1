# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Read-Host" -Tags "Slow","Feature" {
    Context "[Console]::ReadKey() implementation on non-Windows" {
        BeforeAll {
            $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"
            $assetsDir = Join-Path -Path $PSScriptRoot -ChildPath assets
            if ($IsWindows) {
                $ItArgs = @{ skip = $true }
            } elseif (-not (Get-Command expect -ErrorAction Ignore)) {
                $ItArgs = @{ pending = $true }
            } else {
                $ItArgs = @{ }
            }

            $expectFile = Join-Path $assetsDir "Read-Host.Output.expect"

            if (-not $IsWindows) {
                chmod a+x $expectFile
            }
        }

        It @ItArgs "Should output correctly" {
            & $expectFile $powershell | Out-Null
            $LASTEXITCODE | Should -Be 0
        }
    }
}
