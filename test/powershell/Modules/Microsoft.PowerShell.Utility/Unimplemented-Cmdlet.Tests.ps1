# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Unimplemented Utility Cmdlet Tests" -Tags "CI" {

    $Commands = @(
        "ConvertFrom-SddlString"
    )

    foreach ($Command in $Commands) {
        It "$Command should only be available on Windows" {
            [bool](Get-Command $Command -ErrorAction SilentlyContinue) | Should -Be $IsWindows
        }
    }
}
