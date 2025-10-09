# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Get-Uptime" -Tags "CI" {
    It "Get-Uptime return timespan (default -Timespan)" {
        $upt = Get-Uptime
        $upt | Should -BeOfType Timespan
    }
    It "Get-Uptime -Since return DateTime" {
        $upt = Get-Uptime -Since
        $upt | Should -BeOfType DateTime
    }
}
