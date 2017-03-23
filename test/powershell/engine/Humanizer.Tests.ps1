Describe 'Humanizer Tests for TimeSpan' -Tags 'CI' {

    BeforeAll {
        $timespantests_en_US = @(
                       @{time = "0:0:0:0.0"; result = "0 seconds"}
                       @{time = "0:0:0:0.001"; result = "0 seconds, 1 millisecond"}
                       @{time = "0:0:0:0.002"; result = "0 seconds, 2 milliseconds"}
                       @{time = "0:0:0:1.002"; result = "1 second, 2 milliseconds"}
                       @{time = "0:0:0:2.002"; result = "2 seconds, 2 milliseconds"}
                       @{time = "0:0:1:0.002"; result = "1 mimute, 0 seconds, 2 milliseconds"}
                       @{time = "0:0:2:0.002"; result = "2 mimutes, 0 seconds, 2 milliseconds"}
                       @{time = "0:1:0:0.002"; result = "1 hour, 0 seconds, 2 milliseconds"}
                       @{time = "0:2:0:0.002"; result = "2 hours, 0 seconds, 2 milliseconds"}
                       @{time = "1:0:0:0.002"; result = "1 day, 0 seconds, 2 milliseconds"}
                       @{time = "2:0:0:0.002"; result = "2 days, 0 seconds, 2 milliseconds"}
                       @{time = "1:1:1:1.1"; result = "1 day, 1 hour, 1 mimute, 1 second, 100 milliseconds"}
                       @{time = "2:2:2:2.2"; result = "2 days, 2 hours, 2 mimutes, 2 seconds, 200 milliseconds"}
                       )
        # Until Humanizer has been full localized 'en-US' is default for all cultures
        $timespantests_ru_RU = $timespantests_en_US
    }

    It 'en_US: Convert <time> to human string' -TestCases $timespantests_en_US -test {
        param($time,$result)
        [Microsoft.PowerShell.ToStringCodeMethods]::ToHumanString([timespan]$time, "en-US") | Should Be $result
    }

    It 'ru_RU: Convert <time> to human string' -TestCases $timespantests_ru_RU -test {
        param($time,$result)
        [Microsoft.PowerShell.ToStringCodeMethods]::ToHumanString([timespan]$time, "ru-RU") | Should Be $result
    }
}

Describe 'Humanizer Tests for TimeSpan: test default output to console' -Tags 'CI' {

    It 'Convert TimeSpan to human string' {
        ([timespan]"1:1:1:1.1" | Format-Custom | Out-String) -like "*1 day, 1 hour, 1 mimute, 1 second, 100 milliseconds*" | Should Be $true
    }
}
