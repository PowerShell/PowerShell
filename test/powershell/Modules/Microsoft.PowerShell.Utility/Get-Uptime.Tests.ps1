# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Get-Uptime" -Tags "CI" {
    BeforeAll {
        $IsHighResolution = [system.diagnostics.stopwatch]::IsHighResolution
        # Skip Get-Uptime test if IsHighResolution = false
        # because stopwatch.GetTimestamp() return DateTime.UtcNow.Ticks
        # instead of ticks from system startup
        if ( ! $IsHighResolution )
        {
            $origDefaults = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues['it:skip'] = $true
        }
    }
    AfterAll {
        if ( ! $IsHighResolution ){
            $global:PSDefaultParameterValues = $origDefaults
        }
    }
    It "Get-Uptime return timespan (default -Timespan)" {
        $upt = Get-Uptime
        $upt | Should -BeOfType Timespan
    }
    It "Get-Uptime -Since return DateTime" {
        $upt = Get-Uptime -Since
        $upt | Should -BeOfType DateTime
    }
    It "Get-Uptime throw if IsHighResolution == false" {
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
