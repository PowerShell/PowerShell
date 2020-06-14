# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Unimplemented Management Cmdlet Tests" -Tags "CI" {

    $Commands = @(
        "Get-Service",
        "Stop-Service",
        "Start-Service",
        "Suspend-Service",
        "Resume-Service",
        "Restart-Service",
        "Set-Service",
        "New-Service",

        "Rename-Computer",

        "Get-ComputerInfo",

        "Set-TimeZone"
    )

    foreach ($Command in $Commands) {
        It "$Command should only be available on Windows" {
            [bool](Get-Command $Command -ErrorAction SilentlyContinue) | Should -Be $IsWindows
        }
    }
}
