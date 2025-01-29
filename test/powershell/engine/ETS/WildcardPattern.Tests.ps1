# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "WildCardPattern Tests" -Tag "CI" {
    It "Should escape special characters from '<String>' to '<Result>' using Escape method" -TestCases @(
        @{ String = 'abc`def'; Result = 'abc``def' }
        @{ String = 'abc?def'; Result = 'abc`?def' }
        @{ String = 'abc*def'; Result = 'abc`*def' }
        @{ String = 'abc[def'; Result = 'abc`[def' }
        @{ String = 'abc]def'; Result = 'abc`]def' }
        @{ String = 'abc[]def'; Result = 'abc`[`]def' }
    ) {
        param($String, $Result)
        $escaped = [WildcardPattern]::Escape($String)
        $escaped | Should -BeExactly $Result
    }
}
