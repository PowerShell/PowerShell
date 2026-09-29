# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Unimplemented Management Cmdlet Tests" -Tags "CI" {

    BeforeDiscovery {
        $testCases = @(
            @{ Command = "Get-Service" },
            @{ Command = "Stop-Service" },
            @{ Command = "Start-Service" },
            @{ Command = "Suspend-Service" },
            @{ Command = "Resume-Service" },
            @{ Command = "Restart-Service" },
            @{ Command = "Set-Service" },
            @{ Command = "New-Service" },
            @{ Command = "Rename-Computer" },
            @{ Command = "Get-ComputerInfo" },
            @{ Command = "Set-TimeZone" }
        )
    }

    It "<Command> should only be available on Windows" -TestCases $testCases {
        param($Command)
        [bool](Get-Command $Command -ErrorAction SilentlyContinue) | Should -Be $IsWindows
    }
}