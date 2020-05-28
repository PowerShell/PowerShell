# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Failing test used to test CI Scripts" -Tags 'CI' {
    It "Should fail" {
        1 | Should -Be 2
    }
}
