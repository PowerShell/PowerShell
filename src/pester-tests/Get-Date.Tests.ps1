Describe "Get-Date" {
    It "Should return a DateTime object upon being called" {
        (Get-Date).GetType().Name.Equals('DateTime') | Should Be $true
    }

    It "Should have colons when ToString method is used" {
        (Get-Date).ToString().Contains(":")                   | Should be $true
        (Get-Date -DisplayHint Time).ToString().Contains(":") | Should be $true
        (Get-Date -DisplayHint Date).ToString().Contains(":") | Should be $true
    }

    It "Should be able to use the format flag" {
        # You would think that one could use simple loops here, but apparently powershell in windows returns different values in loops

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
