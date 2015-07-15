$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$sut = "test-" + (Split-Path -Leaf $MyInvocation.MyCommand.Path).Replace(".Tests.", ".")
. "$here\$sut"

Describe "Get-Alias" {
    It "should return 3 objects" {
		$val =  Microsoft.PowerShell.Utility\Get-Alias a*
        $val.CommandType | Should Not BeNullOrEmpty
        $val.Name | Should Not BeNullOrEmpty
        $val.ModuleName | Should BeNullOrEmpty

        $val.Name[0] | Should Be "ac"
        $val.Name[1] | Should Be "asnp"
    }
}
