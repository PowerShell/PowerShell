# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Get-RunspaceDebug" -Tags "CI" {

    Context "Check return types of RunspaceDebug" {

        It "Should return Microsoft.Powershell.Commands.PSRunspaceDebug as the return type" {
            $rs = Get-RunspaceDebug -ErrorAction SilentlyContinue
            $rs | Should -Not -BeNullOrEmpty
            $rs[0] | Should -BeOfType Microsoft.PowerShell.Commands.PSRunspaceDebug
        }
    }
}
