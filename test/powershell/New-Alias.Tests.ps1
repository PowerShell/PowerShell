Describe "New-Alias" {
    It "Should be able to be called using the name and value parameters without error" {
	{ New-Alias -Name testAlias -Value 100 } | Should Not Throw
    }

    It "Should have the same output between the alias and the original cmdlet" {
	New-Alias -Name testAlias -Value Get-Process

	$aliasId  = $(testAlias).Id
	$cmdletId = $(Get-Process).Id
	foreach ($IdNumber in $aliasId)
	{
	    $aliasId[$IdNumber] | Should Be $cmdletId[$IdNumber]
	}
    }

    It "Should be able to call the New-Alias cmdlet using the nal alias without error" {
	{ nal -Name testAlias -Value 100 } | Should Not Throw
    }

    It "Should have the same output between the nal alias and the New-Alias cmdlet" {
	nal -Name testAlias -Value Get-Process

	New-Alias -Name testalias2 -Value Get-Process

	$aliasData = $(testAlias).Id
	$cmdletData = $(testAlias2).Id

	foreach ($IdNumber in $aliasData)
	{
	    $aliasData[$IdNumber] | Should Be $cmdletData[$IdNumber]
	}
    }
}
