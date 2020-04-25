# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Using module" -Tag "CI" {
    BeforeAll {
        Push-Location $PSScriptRoot
        New-Item tmp -ItemType Directory
    }
    AfterAll {
        Remove-Item tmp -Recurse
        Pop-Location
    }
    It 'correctly handles paths with ./' {
        Set-Location ./tmp

        'function Get-Foo { "hi from t.psm1" }' > t.psm1

        'using module ./t.psm1; Get-Foo' > t.ps1

        Set-Location ..
        { ./tmp/t.ps1 } | Should -Not -Throw
    }
}
