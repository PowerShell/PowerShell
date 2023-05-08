# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "ScriptBlock.GetNewClosure()" -tags "CI" {

    BeforeAll {

        ## No error should occur when calling GetNewClosure because:
        ## 1. ValidateAttributes are not evaluated on parameter default values
        ## 2. GetNewClosure no longer forces validation on existing variables
        function SimpleFunction_GetNewClosure
        {
            param([ValidateNotNull()] $Name)

            & { 'OK' }.GetNewClosure()
        }

        function ScriptCmdlet_GetNewClosure
        {
            [CmdletBinding()]
            param(
                [Parameter()]
                [ValidateNotNullOrEmpty()]
                [string] $Name = "",

                [Parameter()]
                [ValidateRange(1,3)]
                [int] $Value = 4
            )

            & { $Value; $Name }.GetNewClosure()
        }
    }

    It "Parameter attributes should not get evaluated again in GetNewClosure - SimpleFunction" {
        SimpleFunction_GetNewClosure | Should -BeExactly "OK"
    }

    It "Parameter attributes should not get evaluated again in GetNewClosure - ScriptCmdlet" {
        $result = ScriptCmdlet_GetNewClosure
        $result.Count | Should -Be 2
        $result[0] | Should -Be 4
        $result[1] | Should -BeNullOrEmpty
    }
}
