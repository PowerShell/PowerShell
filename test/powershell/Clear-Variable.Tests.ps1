Describe "Clear-Variable" {
    BeforeEach {
	$var1 = 3
    }

    It "Should be able to clear a variable using the Name switch" {
	Clear-Variable -Name var1
	$var1 | Should BeNullOrEmpty
	{ Get-Variable var1 } | Should Not Throw
    }

    It "Should be able to clear a variable without using the Name switch" {
	Clear-Variable var1
	$var1 | Should BeNullOrEmpty
	{ Get-Variable var1 } | Should Not Throw
    }

    It "Should have a null value after clearing the variable" {
	Clear-Variable var1
	$var1 | Should BeNullOrEmpty
    }

    It "Should call without error using the clv alias" {
	{ clv -Name var1 } | Should Not Throw
    }

    It "Should be able to include a set of variables to clear" {
	$var1      = 2
	$var2      = 3
	$var3      = 4
	$variable1 = 23
	$variable2 = 4
	$variable3 = 2
	$w         = 3

	Clear-Variable -Name w, vari* -Include w, vari*

	$variable1 | Should BeNullOrEmpty
	$variable2 | Should BeNullOrEmpty
	$variable3 | Should BeNullOrEmpty
	$w         | Should BeNullOrEmpty

	$var1 | Should Not BeNullOrEmpty
	$var2 | Should Not BeNullOrEmpty
	$var3 | Should Not BeNullOrEmpty

    }

    It "Should be able to exclude a set of variables to clear" {
	$var1      = 2
	$var2      = 3
	$var3      = 4
	$variable1 = 23
	$variable2 = 4
	$variable3 = 2
	$w         = 3

	Clear-Variable -Name w, vari* -Exclude var*

	$variable1 | Should Not BeNullOrEmpty
	$variable2 | Should Not BeNullOrEmpty
	$variable3 | Should Not BeNullOrEmpty
	$var1      | Should Not BeNullOrEmpty
	$var2      | Should Not BeNullOrEmpty
	$var3      | Should Not BeNullOrEmpty

	$w         | Should BeNullOrEmpty
    }

    It "Should be able to pass the cleared object through the pipeline using the passthru switch" {
	{ Clear-Variable -Name var1 -PassThru | Format-Wide -Property Value } | Should Not Throw
    }

    It "Should not clear environment variables" {
	$env:TEMPVARIABLE = "test data"

	Clear-Variable -Name env:TEMPVARIABLE -ErrorAction SilentlyContinue | Should Throw
    }

    It "Should clear variable even if it is read-only using the Force parameter" {
	New-Variable -Name var2 -Option ReadOnly -Value 100

	Clear-Variable -Name var1
	Clear-Variable -Name var2 -Force

	$var1 | Should BeNullOrEmpty
	$var2 | Should BeNullOrEmpty

	Remove-Variable -Name var2 -Force
    }

    It "Should throw error when trying to clear variable that is read-only without using the Force parameter" {
	New-Variable -Name var2 -Option ReadOnly -Value 100

	Clear-Variable -Name var2 -ErrorAction SilentlyContinue | Should Throw

	$var2 | Should Not BeNullOrEmpty

	Remove-Variable -Name var2 -Force
    }

    Context "Scope Tests" {
	# This will violate the DRY principle.  Tread softly.

	It "Should be able to clear a global scope variable using the global switch" {
	    New-Variable globalVar -Value 1 -Scope global -Force

	    Clear-Variable -Name globalVar -Scope global

	    $globalVar | Should BeNullOrEmpty
	}

	It "Should not be able to clear a global scope variable using the local switch" {
	    New-Variable globalVar -Value 1 -Scope global -Force

	    Clear-Variable -Name globalVar -Scope local -ErrorAction SilentlyContinue | Should Throw
	}

	It "Should not be able to clear a global variable using the script scope switch" {
	    New-Variable globalVar -Value 1 -Scope global -Force

	    Clear-Variable -Name localVar -Scope script -ErrorAction SilentlyContinue | Should Throw
	}

	It "Should be able to clear an item locally using the local switch" {
	    New-Variable localVar -Value 2 -Scope local -Force

	    Clear-Variable -Name localVar -Scope local

	    $localVar | Should BeNullOrEmpty

	    Clear-Variable -Name localVar -Scope script -ErrorAction SilentlyContinue | Should Throw
	}

	It "Should not be able to clear an item locally using the global switch" {
	    New-Variable localVar -Value 2 -Scope local -Force

	    Clear-Variable -Name localVar -Scope global -ErrorAction SilentlyContinue | Should Throw
	}

	It "Should not be able to clear a local variable using the script scope switch" {
	    New-Variable localVar -Value 2 -Scope local -Force

	    Clear-Variable -Name localVar -Scope script -ErrorAction SilentlyContinue | Should Throw
	}

	It "Should be able to clear a script variable created using the script switch" {
	    {
		New-Variable -Name derp2 -Value 3 -Scope script -Force

		Clear-Variable -Name derp2 -Scope script
	    }| Should Not Throw
	}

	It "Should be able to clear a global script variable that was created using the script scope switch" {
	    {
		New-Variable -Name derpx -Value 4 -Scope script -Force

		Clear-Variable -Name derpx -Scope script
	    } | Should Not Throw
	}
    }
}
