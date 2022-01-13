# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Start-Sleep DRT Unit Tests" -Tags "CI" {

    # WaitHandle.WaitOne(milliseconds, exitContext) is not accurate.
    # The actual wait time can vary from 1450ms to 1700ms.
    $minTime = 1450
    $maxTime = 1700

    It "Should work properly when sleeping with Second" {
        $watch = [System.Diagnostics.Stopwatch]::StartNew()
        Start-Sleep -Seconds 1.5
        $watch.Stop()
        $watch.ElapsedMilliseconds | Should -BeGreaterThan $minTime
        $watch.ElapsedMilliseconds | Should -BeLessThan $maxTime
    }

    It "Should work properly when sleeping with Milliseconds" {
        $watch = [System.Diagnostics.Stopwatch]::StartNew()
        Start-Sleep -Milliseconds 1500
        $watch.Stop()
        $watch.ElapsedMilliseconds | Should -BeGreaterThan $minTime
        $watch.ElapsedMilliseconds | Should -BeLessThan $maxTime
    }

    It "Should work properly when sleeping with a [TimeSpan]" {
        $duration = [timespan]::FromMilliseconds(1500)
        $watch = [System.Diagnostics.Stopwatch]::StartNew()
        Start-Sleep -Duration $duration
        $watch.Stop()
        $watch.ElapsedMilliseconds | Should -BeGreaterThan $minTime
        $watch.ElapsedMilliseconds | Should -BeLessThan $maxTime
    }

    It "Should work properly when sleeping with ms alias" {
        $watch = [System.Diagnostics.Stopwatch]::StartNew()
        Start-Sleep -ms 1500
        $watch.Stop()
        $watch.ElapsedMilliseconds | Should -BeGreaterThan $minTime
        $watch.ElapsedMilliseconds | Should -BeLessThan $maxTime
    }

    It "Should work properly when sleeping without parameters" {
        $watch = [System.Diagnostics.Stopwatch]::StartNew()
        Start-Sleep 1.5
        $watch.Stop()
        $watch.ElapsedMilliseconds | Should -BeGreaterThan $minTime
        $watch.ElapsedMilliseconds | Should -BeLessThan $maxTime
    }

    It "Should work properly when sleeping without parameters from [timespan]" {
        $duration = [timespan]::FromMilliseconds(1500)
        $watch = [System.Diagnostics.Stopwatch]::StartNew()
        Start-Sleep $duration
        $watch.Stop()
        $watch.ElapsedMilliseconds | Should -BeGreaterThan $minTime
        $watch.ElapsedMilliseconds | Should -BeLessThan $maxTime
    }

    It "Should validate [timespan] parameter values" {
        { Start-Sleep -Duration    '0:00:01' } | Should -Not -Throw
        { Start-Sleep -Duration   '-0:00:01' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.StartSleepCommand'
        { Start-Sleep -Duration '30.0:00:00' } | Should -Throw -ErrorId 'MaximumDurationExceeded,Microsoft.PowerShell.Commands.StartSleepCommand'
    }
}

Describe "Start-Sleep" -Tags "CI" {
    Context "Validate Start-Sleep works properly" {
        It "Should only sleep for at least 1 second" {
            $result = Measure-Command { Start-Sleep -s 1 }
            $result.TotalSeconds | Should -BeGreaterThan 0.25
        }
    }
}
