Describe "Get-Date DRT Unit Tests" -Tags "CI" {
    It "Get-Date with all parameters returns proper results" {
        $date = [datetime]::Now + ([timespan]::new(0,0,30))
        $result = Get-Date -Date $date -Year 1973 -Month 2 -Day 22 -Hour 15 -Minute 40 -Second 10 -Millisecond 200
        $result | Should BeOfType Datetime
        $result.Year | Should be 1973
        $result.Month| Should be 2
        $result.Day | Should be 22
        $result.Hour | Should be 15
        $result.Minute | Should be 40
        $result.Second | Should be 10
        $result.Millisecond | Should be 200
    }

    It "using -displayhint produces the correct output" {
        $d = Get-date -Date:"Jan 1, 2020"  -DisplayHint Date | Out-String
        $d.Trim() | Should be "Wednesday, January 1, 2020"
    }

    It "using -format produces the correct output" {
        Get-date -Date:"Jan 1, 2020"  -Format:"MMM-dd-yy" | Should be "Jan-01-20"
    }

    It "using -uformat produces the correct output" {
        Get-date -Date:"Jan 1, 2020"  -UFormat:"%s"  | Should be "1577836800"
    }

    It "using -uformat 'ymdH' produces the correct output" {
        Get-date -Date 0030-01-01T00:00:00 -uformat %y/%m/%d-%H | Should be "30/01/01-00"
    }

    It "using -uformat 'aAbBcCdDehHIjmMpr' produces the correct output" {
        Get-date -Date 1/1/0030 -uformat %a%A%b%B%c%C%d%D%e%h%H%I%j%m%M%p%r | Should be "TueTuesdayJanJanuaryTue Jan  1 00:00:00 003000101/01/30 1Jan001210100AM12:00:00 AM"
    }

    It "using -uformat 'StTuUVwWxXyYZ' produces the correct output" {
        Get-date -Date 1/1/0030 -uformat %S%T%u%U%V%w%W%x%X%y%Y%% | Should be "0000:00:002012001/01/3000:00:00300030%"
    }

    It "Get-date works with pipeline input" {
        $x = new-object System.Management.Automation.PSObject
        $x | add-member NoteProperty Date ([DateTime]::Now)
        $y = @($x,$x)
        ($y | Get-date).Length | Should be 2
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

        $result1.Length | Should be 10
        $result1.Length -eq $result2.Length | Should be $true

        for($i = 0; $i -lt $result1.Length; $i++)
        {
            $result1[$i] -eq $result2[$i] | Should be $true
        }

        Get-ChildItem -Path $pathString | Remove-Item
        Remove-Item -Path $pathString -Force -Recurse
    }


}


Describe "Get-Date" -Tags "CI" {
    It "Should have colons when ToString method is used" {
	(Get-Date).ToString().Contains(":")                   | Should be $true
	(Get-Date -DisplayHint Time).ToString().Contains(":") | Should be $true
	(Get-Date -DisplayHint Date).ToString().Contains(":") | Should be $true
    }

    It "Should be able to use the format flag" {
	# You would think that one could use simple loops here, but apparently powershell in Windows returns different values in loops

	(Get-Date -Format d).Contains("/") | Should be $true
	(Get-Date -Format D).Contains(",") | Should be $true
	(Get-Date -Format f).Contains(",") -and (Get-Date -Format f).Contains(":") | Should be $true
	(Get-Date -Format F).Contains(",") -and (Get-Date -Format F).Contains(":") | Should be $true
	(Get-Date -Format g).Contains("/") -and (Get-Date -Format g).Contains(":") | Should be $true
	(Get-Date -Format G).Contains("/") -and (Get-Date -Format G).Contains(":") | Should be $true
	(Get-Date -Format m).Contains(",") -or `
	  (Get-Date -Format m).Contains(":")  -or `
	  (Get-Date -Format m).Contains("/") | Should be $false
    }

    It "Should check that Get-Date can return the correct datetime from the system time" {
	$timeDifference = $(Get-Date).Subtract([System.DateTime]::Now)

	$timeDifference.Days         | Should Be 0
	$timeDifference.Hours        | Should Be 0
	$timeDifference.Minutes      | Should Be 0
	$timeDifference.Milliseconds | Should BeLessThan 1
	$timeDifference.Ticks        | Should BeLessThan 10000
    }

}
