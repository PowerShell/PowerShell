# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "ProviderIntrinsics Tests" -tags "CI" {
    BeforeAll {
        Setup -d TestDir
    }
    It 'If a childitem exists, HasChild method returns $true' {
        $ExecutionContext.InvokeProvider.ChildItem.HasChild("$TESTDRIVE") | Should -BeTrue
    }
    It 'If a childitem does not exist, HasChild method returns $false' {
        $ExecutionContext.InvokeProvider.ChildItem.HasChild("$TESTDRIVE/TestDir") | Should -BeFalse
    }
    It 'If the path does not exist, HasChild throws an exception' {
        { $ExecutionContext.InvokeProvider.ChildItem.HasChild("TESTDRIVE/ThisDirectoryDoesNotExist") } |
            Should -Throw -ErrorId 'ItemNotFoundException'
    }
}

