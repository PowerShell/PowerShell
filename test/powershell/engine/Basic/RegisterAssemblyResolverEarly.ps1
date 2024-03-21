# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Assembly resolvers should be registered early at startup" -Tags "CI" {

    ## The PKI module requires loading an assembly from GAC, and thus depends on the PowerShell assembly resolver to work.
    It "Can load the PKI module with 'pwsh -ExecutionPolicy bypass -NoProfile -c `"Import-Module PKI`"'" -Skip:(!$IsWindows) {
        $pwsh = Join-Path $PSHOME "pwsh.exe"
        $out = & $pwsh -ExecutionPolicy bypass -NoProfile -c "Import-Module PKI; Get-Module | % name"
        $out | Should -BeExactly "PKI"
    }
}
