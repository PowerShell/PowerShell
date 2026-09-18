# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Read-Host" -Tags "Slow","Feature" {
    Context "[Console]::ReadKey() implementation on non-Windows" {
        BeforeDiscovery {
            $skip = $IsWindows -or -not (Get-Command expect -ErrorAction Ignore)
        }

        BeforeAll {
            $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"
            $assetsDir = Join-Path -Path $PSScriptRoot -ChildPath assets

            $expectFile = Join-Path $assetsDir "Read-Host.Output.expect"

            if (-not $IsWindows) {
                chmod a+x $expectFile
            }
        }

        It "Should output correctly" -Skip:$skip {
            & $expectFile $powershell | Out-Null
            $LASTEXITCODE | Should -Be 0
        }
    }
}
