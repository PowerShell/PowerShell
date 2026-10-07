# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Unimplemented Utility Cmdlet Tests" -Tags "CI" {

    BeforeDiscovery {
        $testCases = @(
            @{ Command = "ConvertFrom-SddlString" }
        )
    }

    It "<Command> should only be available on Windows" -TestCases $testCases {
        param($Command)
        [bool](Get-Command $Command -ErrorAction SilentlyContinue) | Should -Be $IsWindows
    }
}