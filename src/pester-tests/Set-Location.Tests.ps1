Describe "Set-Location" {
    Context "Functionality testing" {
        $startDirectory = Get-Location

	It "Should be able to be called without error" {
            { Set-Location / }    | Should Not Throw
	}

	It "Should be able to be called on different providers" {
            { Set-Location alias: } | Should Not Throw
            { Set-Location env: }   | Should Not Throw
	}

	It "Should be able use the cd alias without error" {
            { cd / } | Should Not Throw
	}

	It "Should be able to use the chdir alias without error" {
            { chdir / } | Should Not Throw
	}

	It "Should be able to use the sl alias without error" {
            { sl / } | Should Not Throw
	}

        It "Should have the correct current location when using the set-location cmdlet" {
            Set-Location $startDirectory

            $(Get-Location).Path | Should Be $startDirectory.Path
        }

        It "Should have the correct current location when using the cd alias" {
            cd /

            $(Get-Location).Path | Should Be /
        }

        It "Should have the correct current location when using the chdir alias" {
            chdir /

            $(Get-Location).Path | Should Be /
        }

        It "Should have the correct current location when using the chdir alias" {
            sl /

            $(Get-Location).Path | Should Be /
        }

        It "Should be able to use the Path switch" {
            { Set-Location -Path / } | Should Not Throw
        }

        It "Should generate a pathinfo object when using the Passthru switch" {
            $(Set-Location / -PassThru).GetType().Name | Should Be PathInfo
        }

        Set-Location $startDirectory

    }
}
