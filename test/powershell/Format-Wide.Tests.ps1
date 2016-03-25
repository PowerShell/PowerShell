Describe "Format-Wide" {
    It "Should be able to call format wide without error" {
	{ Get-Process | Format-Wide } | Should Not Throw
    }

    It "Should be able to use the fw alias without error" {
	{ Get-Process | fw } | Should Not Throw
    }

    It "Should have the same output between the alias and the unaliased function" {
	$nonaliased = Get-ChildItem | Format-Wide
	$aliased    = Get-ChildItem | fw

	$($nonaliased | Out-String).CompareTo($($aliased | Out-String)) | Should Be 0
    }

    It "Should be able to specify the columns in output using the column switch" {
	{ ls | Format-Wide -Column 3 } | Should Not Throw
    }

    It "Should be able to use the autosize switch" {
	{ ls | Format-Wide -Autosize } | Should Not Throw
    }

    It "Should be able to take inputobject instead of pipe" {
	{ Format-Wide -InputObject $(ls) } | Should Not Throw
    }

    It "Should be able to use the property switch" {
	{ Format-Wide -InputObject $(ls) -Property Mode } | Should Not Throw
    }

    It "Should throw an error when property switch and view switch are used together" {
	{ Format-Wide -InputObject $(ls) -Property CreationTime -View aoeu } | Should Throw "Found invalid data"
    }

    It "Should throw and suggest proper input when view is used with invalid input without the property switch" {
	{ Format-Wide -InputObject $(gps) -View aoeu } | Should Throw
    }
}
