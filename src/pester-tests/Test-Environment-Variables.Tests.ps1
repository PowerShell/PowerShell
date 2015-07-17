$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$sut = (Split-Path -Leaf $MyInvocation.MyCommand.Path).Replace(".Tests.", ".")
. "$here/$sut"

Describe "Test-Environment-Variables" {
    It "Should have environment variable" {
        Get-Item ENV: | Should Not BeNullOrEmpty
    }

    It "Should be able to access the members of the environment variable in two ways" {
        (Get-Item ENV:HOME).Value     | Should be "/root"
	(Get-Item ENV:HOSTNAME).Value | Should Not BeNullOrEmpty
	(Get-Item ENV:PATH).Value     | Should Not BeNullOrEmpty

        (ls ENV:HOME).Value     | Should be "/root"
	(ls ENV:HOSTNAME).Value | Should Not BeNullOrEmpty
	(ls ENV:PATH).Value     | Should Not BeNullOrEmpty
    }
}
