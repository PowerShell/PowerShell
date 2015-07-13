"starting test script" | out-host

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$here | out-host
$sut = (Split-Path -Leaf $MyInvocation.MyCommand.Path).Replace(".Tests.", ".")
$sut | out-host
. "$here\$sut"
 
Describe "Get-HelloWorld" {
    It "outputs 'Hello world!'" {
        Get-HelloWorld | Should Be 'Hello world!'
    }
}
