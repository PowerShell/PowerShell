# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Unimplemented Utility Cmdlet Tests" -Tags "CI" {

    $Commands = @(
        "Unblock-File",
        "ConvertFrom-SddlString"
    )

    foreach ($Command in $Commands) {
        It "$Command should only be available on Windows" {
            [bool](Get-Command $Command -ErrorAction SilentlyContinue) | Should -Be $IsWindows
        }
    }
}
