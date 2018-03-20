# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Credential tests" -Tags "CI" {
    It "Explicit cast for an empty credential returns null" {
         # We should explicitly check that the expression returns $null
         [PSCredential]::Empty.GetNetworkCredential() | Should -BeNullOrEmpty
    }
}
