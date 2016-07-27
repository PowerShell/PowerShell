Describe "Set-Location" -Tags "CI" {
    $startDirectory = Get-Location

    if ($IsWindows)
    {
	$target = "C:\"
    }
    else
    {
	$target = "/"
    }

    It "Should be able to be called without error" {
	{ Set-Location $target }    | Should Not Throw
    }

    It "Should be able to be called on different providers" {
	{ Set-Location alias: } | Should Not Throw
	{ Set-Location env: }   | Should Not Throw
    }

    It "Should be able use the cd alias without error" {
	{ cd $target } | Should Not Throw
    }

    It "Should be able to use the chdir alias without error" {
	{ chdir $target } | Should Not Throw
    }

    It "Should be able to use the sl alias without error" {
	{ sl $target } | Should Not Throw
    }

    It "Should have the correct current location when using the set-location cmdlet" {
	Set-Location $startDirectory

	$(Get-Location).Path | Should Be $startDirectory.Path
    }

    It "Should have the correct current location when using the cd alias" {
	cd $target

	$(Get-Location).Path | Should Be $target
    }

    It "Should have the correct current location when using the chdir alias" {
	chdir $target

	$(Get-Location).Path | Should Be $target
    }

    It "Should have the correct current location when using the chdir alias" {
	sl $target

	$(Get-Location).Path | Should Be $target
    }

    It "Should be able to use the Path switch" {
	{ Set-Location -Path $target } | Should Not Throw
    }

    It "Should generate a pathinfo object when using the Passthru switch" {
	$(Set-Location $target -PassThru).GetType().Name | Should Be PathInfo
    }

    Set-Location $startDirectory
}
