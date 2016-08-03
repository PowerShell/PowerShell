Describe "Get-PSDrive" -Tags "CI" {

    It "Should not throw" {
	Get-PSDrive | Should Not BeNullOrEmpty
    }

    It "Should have a name and a length property" {
	(Get-PSDrive).Name        | Should Not BeNullOrEmpty
	(Get-PSDrive).Root.Length | Should Not BeLessThan 1
    }

    It "Should be able to be called with the gdr alias" {
	{ gdr } | Should Not Throw

	gdr | Should Not BeNullOrEmpty
    }

    It "Should be the same output between Get-PSDrive and gdr" {
	$alias  = gdr
	$actual = Get-PSDrive

	$alias | Should Be $actual
    }

    It "Should return drive info"{
	(Get-PSDrive Env).Name        | Should Be Env
	(Get-PSDrive Cert).Root       | Should Be \

	if ($IsWindows)
	{
	    (Get-PSDrive C).Provider.Name | Should Be FileSystem
	}
	else
	{
	    (Get-PSDrive /).Provider.Name | Should Be FileSystem
	}
    }

    It "Should be able to access a drive using the PSProvider switch" {
	(Get-PSDrive -PSProvider FileSystem).Name.Length | Should BeGreaterThan 0
    }

    It "Should return true that a drive that does not exist"{
	!(Get-PSDrive fake -ErrorAction SilentlyContinue) | Should Be $True
	Get-PSDrive fake -ErrorAction SilentlyContinue    | Should BeNullOrEmpty
    }
    It "Should be able to determine the amount of free space of a drive" {
        $dInfo = Get-PSDrive TESTDRIVE
        $dInfo.Free -ge 0 | Should be $true
    }
    It "Should be able to determine the amount of Used space of a drive" {
        $dInfo = Get-PSDrive TESTDRIVE
        $dInfo.Used -ge 0 | Should be $true
    }
}
