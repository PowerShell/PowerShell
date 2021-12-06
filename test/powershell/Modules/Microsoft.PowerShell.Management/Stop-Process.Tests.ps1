# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Stop-Process" -Tags "CI" {
    # Parameter validation
    $command = Get-Command -Name Stop-Process

    It "Should have an Id parameter" {
        $command | Should -HaveParameter "Id" -Type [int[]] -Mandatory $false -DefaultValue $null
    }

    It "Should have an InputObject parameter" {
        $command | Should -HaveParameter "InputObject" -Type [System.Diagnostics.Process[]] -Mandatory $false -DefaultValue $null
    }

    It "Should have a Name parameter" {
        $command | Should -HaveParameter "Name" -Type [string[]] -Mandatory $false -DefaultValue $null
    }

    It "Should have a PassThru parameter" {
        $command | Should -HaveParameter "PassThru"
    }

    It "Should have a IncludeChildProcess parameter" {
        $command | Should -HaveParameter "IncludeChildProcess"
    }

    It "Should implement SupportsShouldProcess" {
        $command | Should -HaveParameter "WhatIf"
        $command | Should -HaveParameter "Confirm"
        $command | Should -HaveParameter "Force"
    }
}
