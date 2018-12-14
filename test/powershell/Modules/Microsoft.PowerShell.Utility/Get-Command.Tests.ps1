# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Get-Command CI tests" -Tag Feature {
    Context "-UseFuzzyMatch tests" {
        It "Should match cmdlets" {
            $cmds = Get-Command get-hlp -UseFuzzyMatch
            $cmds.Count | Should -BeGreaterThan 0
            $cmds[0].Name | Should -BeExactly 'Get-Help' -Because "This should be closest match so shows up first"
        }

        It "Should match native commands" {
            $ping = "ping"
            if ($IsWindows) {
                $ping = "PING.EXE"
            }

            $cmds = Get-Command pin -UseFuzzyMatch
            $cmds.Count | Should -BeGreaterThan 0
            $cmds.Name | Should -Contain $ping
        }
    }
}
