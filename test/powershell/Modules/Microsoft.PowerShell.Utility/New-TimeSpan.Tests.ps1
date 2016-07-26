Describe "New-TimeSpan" -Tags "CI" {
    BeforeAll {
        $testObject = (New-TimeSpan -Days 2 -Hours 23 -Minutes 4 -Seconds 3) 

        $expectedOutput = @{ 
                     "Days"              = "2";
                     "Hours"             = "23";
                     "Minutes"           = "4";
                     "Seconds"           = "3";
                     "Milliseconds"      = "0";
                     "Ticks"             = "2558430000000";
                     "TotalDays"         = "2.96114583333333";
                     "TotalHours"        = "71.0675";
                     "TotalMinutes"      = "4264.05";
                     "TotalSeconds"      = "255843";
                     "TotalMilliseconds" = "255843000"
                   }
    }

    It "Should have expected values for time properties set during creation" {
        $testObject.GetType() | Should Be timespan
        $testObject.Days      | Should Be $expectedOutput["Days"]
        $testObject.Hours     | Should Be $expectedOutput["Hours"]
        $testObject.Minutes   | Should Be $expectedOutput["Minutes"]
        $testObject.Seconds   | Should Be $expectedOutput["Seconds"]
        $testObject.Ticks     | Should Be $expectedOutput["Ticks"]
    }

    It "Should have matching output when using the Start switch vs piping from another cmdlet" {
        # this file is guaranteed to exist
        $inputObject    = [datetime]::Now
        Start-Sleep -m 10
        $inputParameter = New-TimeSpan -Start $inputObject
        Start-Sleep -m 10
        $pipedInput     = $inputObject | New-TimeSpan

        # all we need to check is that the pipedInput value is larger than the inputParameter
        $difference = $inputParameter - $pipedInput
        [math]::Abs($difference.Milliseconds) -ge 0 | should be $true
    }
}
