# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe ".NET class interoperability" -Tags "CI" {
    It "Should access types in System.Console" {
        [System.Console]::TreatControlCAsInput | Should -Be $false
    }
}
