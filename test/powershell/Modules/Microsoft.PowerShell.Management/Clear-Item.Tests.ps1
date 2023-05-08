# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Clear-Item tests" -Tag "CI" {
    BeforeAll {
        ${myClearItemVariableTest} = "Value is here"
    }

    It "Clear-Item can clear an item" {
        $myClearItemVariableTest | Should -BeExactly "Value is here"
        Clear-Item -Path variable:myClearItemVariableTest
        Test-Path -Path variable:myClearItemVariableTest | Should -BeTrue
        $myClearItemVariableTest | Should -BeNullOrEmpty
    }
}
