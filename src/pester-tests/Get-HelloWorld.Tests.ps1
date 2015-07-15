$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$sut = (Split-Path -Leaf $MyInvocation.MyCommand.Path).Replace(".Tests.", ".")
. "$here\$sut"

Describe "Get-HelloWorld" {
    It "outputs 'HelloWorld!'" {
        Get-HelloWorld | Should Be 'HelloWorld!'
    }
}
