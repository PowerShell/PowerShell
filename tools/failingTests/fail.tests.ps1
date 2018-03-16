# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Failing test used to test CI Scripts" -Tags 'CI' {
    It "Should fail" {
        1 | should be 2
    }
}
