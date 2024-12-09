# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Get-Uptime" -Tags "CI" {
    BeforeAll {
        $IsHighResolution = [system.diagnostics.stopwatch]::IsHighResolution
        # Skip Get-Uptime all tests on Unix if IsHighResolution = false
        # because stopwatch.GetTimestamp() return DateTime.UtcNow.Ticks
        # instead of ticks from system startup
        # Skip Get-Uptime 'throw' test on Windows since we use WMI there.
    }
    It "Get-Uptime return timespan (default -Timespan)" -Skip:(! $IsHighResolution -and ! $IsWindows) {
        $upt = Get-Uptime
        $upt | Should -BeOfType Timespan
    }
    It "Get-Uptime -Since return DateTime" -Skip:(! $IsHighResolution -and ! $IsWindows) {
        $upt = Get-Uptime -Since
        $upt | Should -BeOfType DateTime
    }
    It "Get-Uptime throw if IsHighResolution == false" -Skip:(! $IsHighResolution -or $IsWindows) {
        # Enable the test hook
        [system.management.automation.internal.internaltesthooks]::SetTestHook('StopwatchIsNotHighResolution', $true)

        try {
            { Get-Uptime } | Should -Throw -ErrorId "GetUptimePlatformIsNotSupported,Microsoft.PowerShell.Commands.GetUptimeCommand"
        } finally {
            # Disable the test hook
            [system.management.automation.internal.internaltesthooks]::SetTestHook('StopwatchIsHighResolutionIsFalse', $false)
        }
    }
}
