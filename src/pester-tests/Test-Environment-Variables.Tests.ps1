$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$sut = (Split-Path -Leaf $MyInvocation.MyCommand.Path).Replace(".Tests.", ".")
. "$here\$sut"

Describe "Test-Environment-Variables" {
    It "Should have environment variable" {
        Get-Item ENV: | Should Not BeNullOrEmpty
    }

    It "Should be able to access the members of the environment variable in two ways" {
        (Get-Item ENV:os).Value | Should Match 'Windows' -or 'Linux'
        
        $env:os | Should Match 'Windows' -or '*nux'
    }
}
