# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "New-TimeSpan DRT Unit Tests" -Tags "CI" {

     It "Should works proper with new-timespan"{
         $results =  New-TimeSpan -Days 10 -Hours 10 -Minutes 10 -Seconds 10 -Milliseconds 10
         $results | Should -BeOfType System.Timespan
         $results.Days | Should -Be 10
         $results.Hours | Should -Be 10
         $results.Minutes | Should -Be 10
         $results.Seconds | Should -Be 10
         $results.Milliseconds | Should -Be 10
    }
}

Describe "New-TimeSpan" -Tags "CI" {

    It "Should be able to create a new timespan object" {
        New-Variable -Name testObject -Value $(New-TimeSpan)
        $testObject | Should -BeOfType System.Timespan
    }

    Context "Core Functionality Tests" {
	New-Variable -Name testObject -Value $(New-TimeSpan -Days 2 -Hours 23 -Minutes 4 -Seconds 3 -Milliseconds 2) -Force

	$expectedOutput = @{ "Days"              = "2";
			     "Hours"             = "23";
			     "Minutes"           = "4";
			     "Seconds"           = "3";
			     "Milliseconds"      = "2";
			     "Ticks"             = "2558430020000";
			     "TotalDays"         = "2.96114585648148";
			     "TotalHours"        = "71.0675005555556";
			     "TotalMinutes"      = "4264.05003333333";
			     "TotalSeconds"      = "255843.002";
			     "TotalMilliseconds" = "255843002"
			   }

	$TEN_MILLION = 10000000

	It "Should have expected values for time properties set during creation" {
	    $testObject.Days         | Should -Be $expectedOutput["Days"]
	    $testObject.Hours        | Should -Be $expectedOutput["Hours"]
	    $testObject.Minutes      | Should -Be $expectedOutput["Minutes"]
	    $testObject.Seconds      | Should -Be $expectedOutput["Seconds"]
	    $testObject.Milliseconds | Should -Be $expectedOutput["Milliseconds"]
	    $testObject.Ticks        | Should -Be $expectedOutput["Ticks"]
	}

    }

    It "Should have matching output when using the Start switch vs piping from another cmdlet" {
	# this file is guaranteed to exist
	$inputObject    = (Get-ChildItem $PSScriptRoot/New-TimeSpan.Tests.ps1)
	$inputParameter = New-TimeSpan -Start $inputObject.lastwritetime
	$pipedInput     = $inputObject | New-TimeSpan

	$difference = [math]::Abs($inputParameter.Milliseconds - $pipedInput.Milliseconds)
	# The difference between commands should be minimal
	$difference | Should -BeLessThan 100
    }
}
