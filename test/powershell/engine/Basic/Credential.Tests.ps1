# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Credential tests" -Tags "CI" {
    It "Explicit cast for an empty credential returns null" {
         # We should explicitly check that the expression returns $null
         [PSCredential]::Empty.GetNetworkCredential() | Should -BeNullOrEmpty
    }

    It "Explicit credential cast with string produces an exception message without value" {
        $ex = { [pscredential]"1234" } | Should -Throw -ErrorId "ConvertToFinalInvalidCastException" -PassThru
        $ex.Exception.Message | Should -Not -Match "1234"
    }
}
