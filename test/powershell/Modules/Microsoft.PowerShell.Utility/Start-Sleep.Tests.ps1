# Copyright (c) Microsoft Corporation. All rights reserved.
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
}

Describe "Start-Sleep" -Tags "CI" {
    Context "Validate Start-Sleep works properly" {
        It "Should only sleep for at least 1 second" {
            $result = Measure-Command { Start-Sleep -s 1 }
            $result.TotalSeconds | Should -BeGreaterThan 0.25
        }
    }
}
