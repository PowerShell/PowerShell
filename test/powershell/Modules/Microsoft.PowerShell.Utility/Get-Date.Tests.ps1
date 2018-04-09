# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Get-Date DRT Unit Tests" -Tags "CI" {
    It "Get-Date with all parameters returns proper results" {
        $date = [datetime]::Now + ([timespan]::new(0,0,30))
        $result = Get-Date -Date $date -Year 1973 -Month 2 -Day 22 -Hour 15 -Minute 40 -Second 10 -Millisecond 200
        $result | Should -BeOfType Datetime
        $result.Year | Should -Be 1973
        $result.Month| Should -Be 2
        $result.Day | Should -Be 22
        $result.Hour | Should -Be 15
        $result.Minute | Should -Be 40
        $result.Second | Should -Be 10
        $result.Millisecond | Should -Be 200
    }

    It "using -displayhint produces the correct output" {
        $d = Get-date -Date:"Jan 1, 2020"  -DisplayHint Date | Out-String
        $d.Trim() | Should -Be "Wednesday, January 1, 2020"
    }

    It "using -format produces the correct output" {
        Get-date -Date:"Jan 1, 2020"  -Format:"MMM-dd-yy" | Should -Be "Jan-01-20"
    }

    It "using -uformat %s produces the correct output" {
        $seconds = Get-date -Date:"Jan 1, 2020Z" -UFormat:"%s"

        $seconds | Should -Be "1577836800"
        if ($isLinux) {
            $seconds | Should -Be (date --date='01/01/2020 UTC' +%s)
        }
    }

    It "using -uformat 'ymdH' produces the correct output" {
        Get-date -Date 0030-01-01T00:00:00 -uformat %y/%m/%d-%H | Should -Be "30/01/01-00"
    }

    It "using -uformat 'aAbBcCdDehHIkljmMpr' produces the correct output" {
        Get-date -Date 1/1/0030 -uformat "%a%A%b%B%c%C%d%D%e%h%H%I%k%l%j%m%M%p%r" | Should -Be "TueTuesdayJanJanuaryTue 01 Jan 0030 00:00:0000101/01/30 1Jan0012 0120010100AM12:00:00 AM"
    }

    It "using -uformat 'sStTuUVwWxXyYZ' produces the correct output" {
        Get-date -Date 1/1/0030 -uformat %S%T%u%U%w%W%x%X%y%Y%% | Should -Be "0000:00:00202001/01/3000:00:00300030%"
    }

    # The 'week of year' test cases is from https://en.wikipedia.org/wiki/ISO_week_date
    It "using -uformat 'V' produces the correct output" -TestCases @(
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
        @{date="2009-12-31"; week = "53"},
        @{date="2010-01-01"; week = "53"},
        @{date="2010-01-02"; week = "53"},
        @{date="2010-01-03"; week = "53"},
        @{date="2010-01-04"; week = "01"}
    ) {
        param($date, $week)
        Get-date -Date $date -uformat %V | Should -BeExactly $week
    }

    It "Passing '<name>' to -uformat produces a descriptive error" -TestCases @(
        @{ name = "`$null"      ; value = $null }
        @{ name = "empty string"; value = "" }
    ) {
        param($value)
        { Get-date -Date 1/1/1970 -uformat $value -ErrorAction Stop } | ShouldBeErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetDateCommand"
    }

    It "Get-date works with pipeline input" {
        $x = new-object System.Management.Automation.PSObject
        $x | add-member NoteProperty Date ([DateTime]::Now)
        $y = @($x,$x)
        ($y | Get-date).Length | Should -Be 2
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

        $result1 = get-childitem -path $pathString | get-date
        $result2 = get-childitem -path $pathString | get-date

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
        Get-date -Date 0030-01-01T01:02:03.0004 -Format FileDate | Should -Be "00300101"
    }

    It "-Format FileDateTime works" {
        Get-date -Date 0030-01-01T01:02:03.0004 -Format FileDateTime | Should -Be "00300101T0102030004"
    }

    It "-Format FileDateTimeUniversal works" {
        Get-date -Date 0030-01-01T01:02:03.0004z -Format FileDateTimeUniversal | Should -Be "00300101T0102030004Z"
    }

    It "-Format FileDateTimeUniversal works" {
        Get-date -Date 0030-01-01T01:02:03.0004z -Format FileDateUniversal | Should -Be "00300101Z"
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
}
