$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$sut = (Split-Path -Leaf $MyInvocation.MyCommand.Path).Replace(".Tests.", ".")
. "$here\$sut"

Describe "Get-Alias" {
    It "should return an array of 3 objects" {
		$val = Get-Alias a*
        $val.CommandType | Should Not BeNullOrEmpty
        $val.Name 	 | Should Not BeNullOrEmpty
        $val.ModuleName  | Should BeNullOrEmpty

        $val.GetType().BaseType.Name | Should Be "Array"
    }
}
