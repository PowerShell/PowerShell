Describe "Test-Push-Location" -Tags "CI" {
    New-Variable -Name startDirectory -Value $(Get-Location).Path -Scope Global -Force

    BeforeEach { cd $startDirectory }

    It "Should be called without error" {
	{ Push-Location } | Should Not Throw
    }

    It "Should be able to push to the root directory" {
	# this works cross-platform
	{ Push-Location / } | Should Not Throw
    }

    It "Should be able to use relative path to parent" {
	{ Push-Location .. } | Should Not Throw
    }

    It "Should be able to use relative path to grandparent" {
	Test-Path ../.. | Should Be $true

	{ Push-Location ../.. } | Should Not Throw
    }

    It "Should be able to push twice" {
	{ Push-Location .. } | Should Not Throw
	{ Push-Location .. } | Should Not Throw
    }

    It "Should be able to take a piped variable" {
	{ ".." | Push-Location } | Should Not Throw
    }

    It "Should be able to call the pushd alias" {
	{ pushd } | Should Not Throw
    }

    It "Should be able to push to the same location between the alias and the cmdlet" {
	pushd ..
	$aliasDirectory = $(Get-Location).Path

	cd $startDirectory
	Push-Location ..
	$cmdletDirectory = $(Get-Location).Path

	$aliasDirectory | Should Be $cmdletDirectory
    }

    It "Should produce a pathinfo object when the passthru parameter is used" {
        Push-Location .. -PassThru | ForEach-Object { $_ | Should BeOfType "System.Management.Automation.PathInfo" }
    }

    # final cleanup
    cd $startDirectory
}
