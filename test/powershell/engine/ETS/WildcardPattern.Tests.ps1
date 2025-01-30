# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "WildCardPattern Tests" -Tag "CI" {
    It "Should escape special characters from '<String>' to '<EscapedResult>' using Escape method" -TestCases @(
        @{ String = 'abc`def'; EscapedResult = 'abc`def' }
        @{ String = 'abc?def'; EscapedResult = 'abc`?def' }
        @{ String = '?abcdef'; EscapedResult = '`?abcdef' }
        @{ String = 'abc*def'; EscapedResult = 'abc`*def' }
        @{ String = 'abc[def'; EscapedResult = 'abc`[def' }
        @{ String = 'abc]def'; EscapedResult = 'abc`]def' }
        @{ String = 'abcdef]'; EscapedResult = 'abcdef`]' }
        @{ String = 'abc[]def'; EscapedResult = 'abc`[`]def' }
    ) {
        param($String, $EscapedResult)
        $escaped = [WildcardPattern]::Escape($String)
        $escaped | Should -BeExactly $EscapedResult

        # -like comparison should also match escaped string from WildCardPattern.Escape
        $String | Should -BeLike $escaped
    }
}
