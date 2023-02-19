# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-Date DRT Unit Tests" -Tags "CI" {
    It "Get-Date with all parameters returns proper results" {
        $date = [datetime]::Now + ([timespan]::new(0,0,30))
        $result = Get-Date -Date $date -Year 1973 -Month 2 -Day 22 -Hour 15 -Minute 40 -Second 10 -Millisecond 200
        $result | Should -BeOfType Datetime
        $result.Year | Should -Be 1973
        $result.Month | Should -Be 2
        $result.Day | Should -Be 22
        $result.Hour | Should -Be 15
        $result.Minute | Should -Be 40
        $result.Second | Should -Be 10
        $result.Millisecond | Should -Be 200
    }

    It "using -displayhint produces the correct output" {
        $d = Get-Date -Date:"Jan 1, 2020"  -DisplayHint Date | Out-String
        $d.Trim() | Should -Be "Wednesday, January 1, 2020"
    }

    It "using -format produces the correct output" {
        Get-Date -Date:"Jan 1, 2020"  -Format:"MMM-dd-yy" | Should -Be "Jan-01-20"
    }

    It "using -AsUTC produces the correct output" {
        (Get-Date -Date:"2020-01-01T00:00:00").Kind | Should -Be Unspecified
        (Get-Date -Date:"2020-01-01T00:00:00" -AsUTC).Kind | Should -Be Utc
    }

    It "using -uformat %s produces the correct output" {
        $seconds = Get-Date -Date:"Jan 1, 2020Z" -UFormat:"%s"

        $seconds | Should -Be "1577836800"
        if ($IsLinux) {
            $dateString = "01/01/2020 UTC"
            if ( (Get-PlatformInfo).Platform -eq "alpine" ) {
                $dateString = "2020-01-01"
            }
            $expected = date --date=${dateString} +%s
            $seconds | Should -Be $expected
        }
    }

    It "using -uformat 'ymdH' produces the correct output" {
        Get-Date -Date 0030-01-01T00:00:00 -UFormat %y/%m/%d-%H | Should -Be "30/01/01-00"
    }

    It "using -uformat 'aAbBcCdDehHIkljmMpr' produces the correct output" {
        Get-Date -Date 1/1/0030 -UFormat "%a%A%b%B%c%C%d%D%e%h%H%I%k%l%j%m%M%p%r" | Should -Be "TueTuesdayJanJanuaryTue 01 Jan 0030 00:00:0000101/01/30 1Jan0012 0120010100AM12:00:00 AM"
    }

    It "using -uformat 'sStTuUVwWxXyYZ' produces the correct output" {
        Get-Date -Date 1/1/0030 -UFormat %S%T%u%U%w%W%x%X%y%Y%% | Should -Be "0000:00:00202001/01/3000:00:00300030%"
    }

    # The 'week of year' test cases is from https://en.wikipedia.org/wiki/ISO_week_date
    It "using -uformat 'V' produces the correct output (<date>:<week>)" -TestCases @(
        @{date="1998-01-02"; week = "01"},
        @{date="1998-01-03"; week = "01"},
        @{date="2003-01-03"; week = "01"},
        @{date="2004-01-02"; week = "01"},
        @{date="2004-01-03"; week = "01"},
        @{date="2005-01-01"; week = "53"},
        @{date="2005-01-02"; week = "53"},
        @{date="2005-12-31"; week = "52"},
        @{date="2006-01-01"; week = "52"},
        @{date="2006-01-02"; week = "01"},
        @{date="2006-12-31"; week = "52"},
        @{date="2007-01-01"; week = "01"},
        @{date="2007-12-30"; week = "52"},
        @{date="2007-12-31"; week = "01"},
        @{date="2008-01-01"; week = "01"},
        @{date="2008-12-28"; week = "52"},
        @{date="2008-12-29"; week = "01"},
        @{date="2008-12-30"; week = "01"},
        @{date="2008-12-31"; week = "01"},
        @{date="2009-01-01"; week = "01"},
        @{date="2009-01-02"; week = "01"},
        @{date="2009-01-03"; week = "01"},
        @{date="2009-12-31"; week = "53"},
        @{date="2010-01-01"; week = "53"},
        @{date="2010-01-02"; week = "53"},
        @{date="2010-01-03"; week = "53"},
        @{date="2010-01-04"; week = "01"},
        @{date="2014-01-03"; week = "01"},
        @{date="2015-01-02"; week = "01"},
        @{date="2015-01-03"; week = "01"},
        @{date="2020-01-03"; week = "01"},
        @{date="2025-01-03"; week = "01"},
        @{date="2026-01-02"; week = "01"},
        @{date="2026-01-03"; week = "01"},
        @{date="2031-01-03"; week = "01"}
    ) {
        param($date, $week)
        Get-Date -Date $date -UFormat %V | Should -BeExactly $week
    }

    # Using the same test cases as V for ISO week date component parity, plus some more esoteric ones
    It "using -uformat 'G' produces the correct output (<date>:<year>)" -TestCases @(
        @{date="0055-12-31"; year = "0055"},
        @{date="0325-01-01"; year = "0325"},
        @{date="0777-01-01"; year = "0776"},
        @{date="1998-01-02"; year = "1998"},
        @{date="1998-01-03"; year = "1998"},
        @{date="2003-01-03"; year = "2003"},
        @{date="2004-01-02"; year = "2004"},
        @{date="2004-01-03"; year = "2004"},
        @{date="2005-01-01"; year = "2004"},
        @{date="2005-01-02"; year = "2004"},
        @{date="2005-12-31"; year = "2005"},
        @{date="2006-01-01"; year = "2005"},
        @{date="2006-01-02"; year = "2006"},
        @{date="2006-12-31"; year = "2006"},
        @{date="2007-01-01"; year = "2007"},
        @{date="2007-12-30"; year = "2007"},
        @{date="2007-12-31"; year = "2008"},
        @{date="2008-01-01"; year = "2008"},
        @{date="2008-12-28"; year = "2008"},
        @{date="2008-12-29"; year = "2009"},
        @{date="2008-12-30"; year = "2009"},
        @{date="2008-12-31"; year = "2009"},
        @{date="2009-01-01"; year = "2009"},
        @{date="2009-01-02"; year = "2009"},
        @{date="2009-01-03"; year = "2009"},
        @{date="2009-12-31"; year = "2009"},
        @{date="2010-01-01"; year = "2009"},
        @{date="2010-01-02"; year = "2009"},
        @{date="2010-01-03"; year = "2009"},
        @{date="2010-01-04"; year = "2010"},
        @{date="2014-01-03"; year = "2014"},
        @{date="2015-01-02"; year = "2015"},
        @{date="2015-01-03"; year = "2015"},
        @{date="2020-01-03"; year = "2020"},
        @{date="2025-01-03"; year = "2025"},
        @{date="2026-01-02"; year = "2026"},
        @{date="2026-01-03"; year = "2026"},
        @{date="2031-01-03"; year = "2031"}
    ) {
        param($date, $year)
        Get-Date -Date $date -UFormat %G | Should -BeExactly $year
    }

    # Using the same test cases as V for ISO week date component parity, plus some more esoteric ones
    It "using -uformat 'g' produces the correct output (<date>:<yy>" -TestCases @(
        @{date="0055-12-31"; yy = "55"},
        @{date="0325-01-01"; yy = "25"},
        @{date="0777-01-01"; yy = "76"},
        @{date="1998-01-02"; yy = "98"},
        @{date="1998-01-03"; yy = "98"},
        @{date="2003-01-03"; yy = "03"},
        @{date="2004-01-02"; yy = "04"},
        @{date="2004-01-03"; yy = "04"},
        @{date="2005-01-01"; yy = "04"},
        @{date="2005-01-02"; yy = "04"},
        @{date="2005-12-31"; yy = "05"},
        @{date="2006-01-01"; yy = "05"},
        @{date="2006-01-02"; yy = "06"},
        @{date="2006-12-31"; yy = "06"},
        @{date="2007-01-01"; yy = "07"},
        @{date="2007-12-30"; yy = "07"},
        @{date="2007-12-31"; yy = "08"},
        @{date="2008-01-01"; yy = "08"},
        @{date="2008-12-28"; yy = "08"},
        @{date="2008-12-29"; yy = "09"},
        @{date="2008-12-30"; yy = "09"},
        @{date="2008-12-31"; yy = "09"},
        @{date="2009-01-01"; yy = "09"},
        @{date="2009-01-02"; yy = "09"},
        @{date="2009-01-03"; yy = "09"},
        @{date="2009-12-31"; yy = "09"},
        @{date="2010-01-01"; yy = "09"},
        @{date="2010-01-02"; yy = "09"},
        @{date="2010-01-03"; yy = "09"},
        @{date="2010-01-04"; yy = "10"},
        @{date="2014-01-03"; yy = "14"},
        @{date="2015-01-02"; yy = "15"},
        @{date="2015-01-03"; yy = "15"},
        @{date="2020-01-03"; yy = "20"},
        @{date="2025-01-03"; yy = "25"},
        @{date="2026-01-02"; yy = "26"},
        @{date="2026-01-03"; yy = "26"},
        @{date="2031-01-03"; yy = "31"}
    ) {
        param($date, $yy)
        Get-Date -Date $date -UFormat %g | Should -BeExactly $yy
    }

    # Using the same test cases as V for ISO week date component parity
    It "using -uformat 'u' produces the correct output (<date>:<dayOfWeek>)" -TestCases @(
        @{date="1998-01-02"; dayOfWeek = "5"},
        @{date="1998-01-03"; dayOfWeek = "6"},
        @{date="2003-01-03"; dayOfWeek = "5"},
        @{date="2004-01-02"; dayOfWeek = "5"},
        @{date="2004-01-03"; dayOfWeek = "6"},
        @{date="2005-01-01"; dayOfWeek = "6"},
        @{date="2005-01-02"; dayOfWeek = "7"},
        @{date="2005-12-31"; dayOfWeek = "6"},
        @{date="2006-01-01"; dayOfWeek = "7"},
        @{date="2006-01-02"; dayOfWeek = "1"},
        @{date="2006-12-31"; dayOfWeek = "7"},
        @{date="2007-01-01"; dayOfWeek = "1"},
        @{date="2007-12-30"; dayOfWeek = "7"},
        @{date="2007-12-31"; dayOfWeek = "1"},
        @{date="2008-01-01"; dayOfWeek = "2"},
        @{date="2008-12-28"; dayOfWeek = "7"},
        @{date="2008-12-29"; dayOfWeek = "1"},
        @{date="2008-12-30"; dayOfWeek = "2"},
        @{date="2008-12-31"; dayOfWeek = "3"},
        @{date="2009-01-01"; dayOfWeek = "4"},
        @{date="2009-01-02"; dayOfWeek = "5"},
        @{date="2009-01-03"; dayOfWeek = "6"},
        @{date="2009-12-31"; dayOfWeek = "4"},
        @{date="2010-01-01"; dayOfWeek = "5"},
        @{date="2010-01-02"; dayOfWeek = "6"},
        @{date="2010-01-03"; dayOfWeek = "7"},
        @{date="2010-01-04"; dayOfWeek = "1"},
        @{date="2014-01-03"; dayOfWeek = "5"},
        @{date="2015-01-02"; dayOfWeek = "5"},
        @{date="2015-01-03"; dayOfWeek = "6"},
        @{date="2020-01-03"; dayOfWeek = "5"},
        @{date="2025-01-03"; dayOfWeek = "5"},
        @{date="2026-01-02"; dayOfWeek = "5"},
        @{date="2026-01-03"; dayOfWeek = "6"},
        @{date="2031-01-03"; dayOfWeek = "5"}
    ) {
        param($date, $dayOfWeek)
        Get-Date -Date $date -UFormat %u | Should -BeExactly $dayOfWeek
    }
    It "Passing '<name>' to -uformat produces a descriptive error (<errorId>)" -TestCases @(
        @{ name = "`$null"      ; value = $null; errorId = "ParameterArgumentValidationErrorNullNotAllowed" }
        @{ name = "empty string"; value = ""; errorId = "ParameterArgumentValidationErrorEmptyStringNotAllowed" }
    ) {
        param($value, $errorId)
        { Get-Date -Date 1/1/1970 -UFormat $value -ErrorAction Stop } | Should -Throw -ErrorId "${errorId},Microsoft.PowerShell.Commands.GetDateCommand"
    }

    It "Get-date works with pipeline input" {
        $x = New-Object System.Management.Automation.PSObject
        $x | Add-Member NoteProperty Date ([DateTime]::Now)
        $y = @($x,$x)
        ($y | Get-Date).Length | Should -Be 2
    }

    It "the LastWriteTime alias works with pipeline input" {
        $folder = "GetDateTest"
        $pathString = Join-Path -Path $TestDrive -ChildPath $folder
        New-Item -Path $TestDrive -Name $folder -ItemType directory -Force
        for($i = 0; $i -lt 10; $i++)
        {
            $temp = [guid]::NewGuid()
            $pathString2 = Join-Path -Path $pathString -ChildPath $temp
            New-Item -Path $pathString -Name $temp -ItemType file -Force

            for($j = 0; $j -lt 100; $j++)
            {
                Add-Content -Path $pathString2 -Value $j
            }

        }

        $result1 = Get-ChildItem -Path $pathString | Get-Date
        $result2 = Get-ChildItem -Path $pathString | Get-Date

        $result1.Length | Should -Be 10
        $result1.Length -eq $result2.Length | Should -BeTrue

        for($i = 0; $i -lt $result1.Length; $i++)
        {
            $result1[$i] -eq $result2[$i] | Should -BeTrue
        }

        Get-ChildItem -Path $pathString | Remove-Item
        Remove-Item -Path $pathString -Force -Recurse
    }

}

Describe "Get-Date" -Tags "CI" {
    It "-Format FileDate works" {
        Get-Date -Date 0030-01-01T01:02:03.0004 -Format FileDate | Should -Be "00300101"
    }

    It "-Format FileDateTime works" {
        Get-Date -Date 0030-01-01T01:02:03.0004 -Format FileDateTime | Should -Be "00300101T0102030004"
    }

    It "-Format FileDateTimeUniversal works (1)" {
        Get-Date -Date 0030-01-01T01:02:03.0004z -Format FileDateTimeUniversal | Should -Be "00300101T0102030004Z"
    }

    It "-Format FileDateTimeUniversal works (2)" {
        Get-Date -Date 0030-01-01T01:02:03.0004z -Format FileDateUniversal | Should -Be "00300101Z"
    }

    It "Should have colons when ToString method is used" {
        (Get-Date).ToString().Contains(":")                   | Should -BeTrue
        (Get-Date -DisplayHint Time).ToString().Contains(":") | Should -BeTrue
        (Get-Date -DisplayHint Date).ToString().Contains(":") | Should -BeTrue
    }

    It "Should be able to use the format flag" {
        # You would think that one could use simple loops here, but apparently powershell in Windows returns different values in loops

        (Get-Date -Format d).Contains("/") | Should -BeTrue
        (Get-Date -Format D).Contains(",") | Should -BeTrue
        (Get-Date -Format f).Contains(",") -and (Get-Date -Format f).Contains(":") | Should -BeTrue
        (Get-Date -Format F).Contains(",") -and (Get-Date -Format F).Contains(":") | Should -BeTrue
        (Get-Date -Format g).Contains("/") -and (Get-Date -Format g).Contains(":") | Should -BeTrue
        (Get-Date -Format G).Contains("/") -and (Get-Date -Format G).Contains(":") | Should -BeTrue
        (Get-Date -Format m).Contains(",") -or `
        (Get-Date -Format m).Contains(":")  -or `
        (Get-Date -Format m).Contains("/") | Should -BeFalse
    }

    It "Should check that Get-Date can return the correct datetime from the system time" {
        $timeDifference = $(Get-Date).Subtract([System.DateTime]::Now)

        $timeDifference.Days         | Should -Be 0
        $timeDifference.Hours        | Should -Be 0
        $timeDifference.Minutes      | Should -Be 0
        $timeDifference.Milliseconds | Should -BeLessThan 1
        $timeDifference.Ticks        | Should -BeLessThan 10000
    }

    It "-UnixTimeSeconds works (<Expected>)" -TestCases @(
        @{ UnixTimeSeconds = 1577836800; Expected = [System.DateTimeOffset]::FromUnixTimeSeconds(1577836800).LocalDateTime },
        @{ UnixTimeSeconds = 0;          Expected = [System.DateTimeOffset]::UnixEpoch.LocalDateTime },
        @{ UnixTimeSeconds = -1;         Expected = [System.DateTimeOffset]::FromUnixTimeSeconds(-1).LocalDateTime }
    ) {
        param(
            [long] $UnixTimeSeconds,
            [DateTime] $Expected
        )

        Get-Date -UnixTimeSeconds $UnixTimeSeconds | Should -Be $Expected
    }
}

Describe "Get-Date -UFormat tests" -Tags "CI" {
    BeforeAll {
        $date1 = Get-Date -Date "2030-4-5 1:2:3.09"
        $date2 = Get-Date -Date "2030-4-15 13:2:3"
        $date3 = Get-Date -Date "2030-4-15 21:2:3"

        # 5 come from $date1 - 2030-4-5 is Friday - 5th day (the enum starts with 0 - Sunday)
        $shortDay1 = [System.Globalization.CultureInfo]::CurrentCulture.DateTimeFormat.AbbreviatedDayNames[5]
        $fullDay1 = [System.Globalization.CultureInfo]::CurrentCulture.DateTimeFormat.DayNames[5]

        # 3 come from $date1 - 2030-4-5 is April - 4th month (the enum starts with 0)
        $shortMonth1 = [System.Globalization.CultureInfo]::CurrentCulture.DateTimeFormat.AbbreviatedMonthNames[3]
        $fullMonth1 = [System.Globalization.CultureInfo]::CurrentCulture.DateTimeFormat.MonthNames[3]

        $fullDate1 = $date1.ToString("$([System.Globalization.CultureInfo]::CurrentCulture.DateTimeFormat.ShortDatePattern)")
        $fullTime1 = $date1.ToString("$([System.Globalization.CultureInfo]::CurrentCulture.DateTimeFormat.LongTimePattern)")

        $amUpper1 = [System.Globalization.CultureInfo]::CurrentCulture.DateTimeFormat.AMDesignator
        $amLower1 = [System.Globalization.CultureInfo]::CurrentCulture.DateTimeFormat.AMDesignator.ToLower()
        $timeZone1 = [String]::Format("{0:+00;-00}", [Timezone]::CurrentTimeZone.GetUtcOffset( $date1 ).Hours)
    }

    It "Get-Date -UFormat <format> (<result>)" -TestCases @(
            # Some format specifiers is locale sensitive:
            #   - and tests work on EN-US only
            #   - and can not be full compatible with Unix 'date' utility.
            # Commented tests mean not implemented or broken format specifiers.
            @{ date = $date1; format = "%a"; result = $shortDay1 }  # locale's abbreviated weekday name
            @{ date = $date1; format = "%A"; result = $fullDay1 }   # locale's full weekday name
            @{ date = $date1; format = "%b"; result = $shortMonth1 }# locale's abbreviated month name
            @{ date = $date1; format = "%B"; result = $fullMonth1 } # locale's full month name
            @{ date = $date1; format = "%c"; result = "$shortDay1 05 $shortMonth1 2030 01:02:03" } # locale's date and time (e.g., Thu 03 Mar 2005 23:05:25)
                                                                    # We can not get compatibility with Unix
                                                                    #
            @{ date = $date1; format = "%C"; result = "20" }        # century; like %Y, except omit last two digits (e.g., 20)
            @{ date = $date1; format = "%d"; result = "05" }        # day of month (e.g., 01)
            @{ date = $date1; format = "%D"; result = "04/05/30" }  # date; same as %m/%d/%y
            @{ date = $date1; format = "%e"; result = " 5" }        # day of month, space padded; same as %_d
            @{ date = $date2; format = "%e"; result = "15" }
            @{ date = $date1; format = "%F"; result = "2030-04-05" }# Equivalent to %Y-%m-%d (the ISO 8601 date format).
            #@{ date = $date1; format = "%g"; result = "" }         # last two digits of year of ISO week number (see %G)
                                                                    # TODO: need review. Broken C# implementation.
                                                                    #
            #@{ date = $date1; format = "%G"; result = "" }         # year of ISO week number (see %V); normally useful only with %V
                                                                    # TODO: need review. Broken C# implementation.
                                                                    #
            @{ date = $date1; format = "%h"; result = $shortMonth1 }# same as %b
            @{ date = $date1; format = "%H"; result = "01" }        # hour (00..23)
            @{ date = $date2; format = "%H"; result = "13" }
            @{ date = $date1; format = "%I"; result = "01" }        # hour (01..12)
            @{ date = $date2; format = "%I"; result = "01" }
            @{ date = $date1; format = "%j"; result = "095" }       # day of year (001..366)
            @{ date = $date1; format = "%k"; result = " 1" }        # hour, space padded ( 0..23); same as %_H
            @{ date = $date2; format = "%k"; result = "13" }
            @{ date = $date1; format = "%l"; result = " 1" }        # hour, space padded ( 1..12); same as %_I
            @{ date = $date2; format = "%l"; result = " 1" }
            @{ date = $date1; format = "%m"; result = "04" }        # month (01..12)
            @{ date = $date1; format = "%M"; result = "02" }        # minute (00..59)
            #@{ date = $date1; format = "%n"; result = "`n" }      # a newline
            #@{ date = $date1; format = "%N"; result = "090000000" }# nanoseconds (000000000..999999999)
            @{ date = $date1; format = "%p"; result = $amUpper1 }   # locale's equivalent of either AM or PM; blank if not known
            #@{ date = $date1; format = "%P"; result = $amLower1 }  # like %p, but lower case
            #@{ date = $date1; format = "%q"; result = "" }         # quarter of year (1..4) - not implemented on Ubuntu 17.10
            @{ date = $date1; format = "%r"; result = "01:02:03 AM" }# locale's 12-hour clock time (e.g., 11:11:04 PM)
            @{ date = $date3; format = "%r"; result = "09:02:03 PM" }
            @{ date = $date1; format = "%R"; result = "01:02" }     # 24-hour hour and minute; same as %H:%M
            @{ date = $date3; format = "%R"; result = "21:02" }
            #@{ date = $date1; format = "%s"; result = "1901563323" }# Separate tests is in the file. Seconds since 1970-01-01 00:00:00 UTC
                                                                    # TODO: need review
                                                                    #
                                                                    @{ date = $date1; format = "%S"; result = "03" }        # second (00..60)
            #@{ date = $date1; format = "%t"; result = "`t" }       # a tab
            @{ date = $date1; format = "%T"; result = "01:02:03" }  # time; same as %H:%M:%S
            @{ date = $date1; format = "%u"; result = "5" }         # day of week (1..7); 1 is Monday
            @{ date = $date1; format = "%U"; result = "13" }        # week number of year, with Sunday as first day of week (00..53)
                                                                    # TODO: need review.
                                                                    #
            #@{ date = $date1; format = "%V"; result = "" }         # Separate tests is in the file. ISO week number, with Monday as first day of week (01..53)
            #@{ date = $date1; format = "%w"; result = "" }         # day of week (0..6); 0 is Sunday
            @{ date = $date1; format = "%W"; result = "13" }        # week number of year, with Monday as first day of week (00..53)
                                                                    # TODO: need review compatibility with Unix
                                                                    #
            @{ date = $date1; format = "%x"; result = "04/05/30" }  # locale's date representation (e.g., 12/31/99)
                                                                    # TODO: need review compatibility with Unix
                                                                    #
            #@{ date = $date1; format = "%X"; result = $fullTime1 } # locale's time representation (e.g., 23:13:48)
                                                                    # TODO: need review compatibility with Unix. Broken C# implementation.
                                                                    #
            @{ date = $date1; format = "%y"; result = "30" }        # last two digits of year (00..99)
            @{ date = $date1; format = "%Y"; result = "2030" }      # year
            #@{ date = $date1; format = "%z"; result = "" }         # +hhmm numeric time zone (e.g., -0400)
            #@{ date = $date1; format = "%:z"; result = "" }        # +hh:mm numeric time zone (e.g., -04:00)
            #@{ date = $date1; format = "%::z"; result = "" }       # +hh:mm:ss numeric time zone (e.g., -04:00:00)
            #@{ date = $date1; format = "%:::z"; result = "" }      # numeric time zone with : to necessary precision (e.g., -04, +05:30)
            @{ date = $date1; format = "%Z"; result = $timeZone1 }  # alphabetic time zone abbreviation (e.g., EDT)
                                                                    # We can only check a time zone format from .Net
                                                                    # and can not check compatibility with Unix
        ) {
            param($date, $format, $result)

            Get-Date -Date $date -UFormat $format | Should -BeExactly $result
    }
}
