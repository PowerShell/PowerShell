# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Import-Alias DRT Unit Tests" -Tags "CI" {
    $testAliasDirectory = Join-Path -Path $TestDrive -ChildPath ImportAliasTestDirectory
    $testAliases        = "TestAliases"
    $fulltestpath       = Join-Path -Path $testAliasDirectory -ChildPath $testAliases

    BeforeEach {
		New-Item -Path $testAliasDirectory -ItemType Directory -Force
		remove-item alias:abcd* -force -ErrorAction SilentlyContinue
		remove-item alias:ijkl* -force -ErrorAction SilentlyContinue
		set-alias abcd01 efgh01
		set-alias abcd02 efgh02
		set-alias abcd03 efgh03
		set-alias abcd04 efgh04
		set-alias ijkl01 mnop01
		set-alias ijkl02 mnop02
		set-alias ijkl03 mnop03
		set-alias ijkl04 mnop04
    }

	AfterEach {
		Remove-Item -Path $testAliasDirectory -Recurse -Force -ErrorAction SilentlyContinue
	}

	It "Import-Alias Resolve To Multiple will throw PSInvalidOperationException" {
		{ Import-Alias * -ErrorAction Stop } | Should -Throw -ErrorId "NotSupported,Microsoft.PowerShell.Commands.ImportAliasCommand"
	}

	It "Import-Alias From Exported Alias File Aliases Already Exist should throw SessionStateException" {
		{ Export-Alias  $fulltestpath abcd* } | Should -Not -Throw
		{ Import-Alias $fulltestpath -ErrorAction Stop } | Should -Throw -ErrorId "AliasAlreadyExists,Microsoft.PowerShell.Commands.ImportAliasCommand"
    }

	It "Import-Alias Into Invalid Scope should throw PSArgumentException"{
		{ Export-Alias  $fulltestpath abcd* } | Should -Not -Throw
		{ Import-Alias $fulltestpath -scope bogus } | Should -Throw -ErrorId "Argument,Microsoft.PowerShell.Commands.ImportAliasCommand"
    }

	It "Import-Alias From Exported Alias File Aliases Already Exist using force should not throw"{
		{Export-Alias  $fulltestpath abcd*} | Should -Not -Throw
		{Import-Alias $fulltestpath  -Force} | Should -Not -Throw
    }
}

Describe "Import-Alias" -Tags "CI" {
	$testAliasDirectory
	$pesteraliasfile
	$aliasPathMoreThanFourValues
	$aliasPathLessThanFourValues
	$commandToAlias

	BeforeAll {
		$newLine=[Environment]::NewLine
		# set paths and names for the alias files
		$testAliasDirectory = Join-Path -Path $TestDrive -ChildPath ImportAliasTestDirectory
		$testAliases        = "pesteralias.txt"
		$aliasFilenameMoreThanFourValues        = "aliasFileMoreThanFourValues.txt"
		$aliasFilenameLessThanFourValues        = "aliasFileLessThanFourValues.txt"

		$pesteraliasfile    = Join-Path -Path $testAliasDirectory -ChildPath $testAliases
		$aliasPathMoreThanFourValues    = Join-Path -Path $testAliasDirectory -ChildPath $aliasFileNameMoreThanFourValues
		$aliasPathLessThanFourValues    = Join-Path -Path $testAliasDirectory -ChildPath $aliasFileNameLessThanFourValues

		# define command to alias for the tests
		$commandToAlias = "echo"

		# write the files and content
		$difficultToParseString_1		= '"abc""def"'
		$difficultToParseString_2		= '"aaa"'
		$difficultToParseString_3		= '"a,b"'

		# create default pester testing file
		# has three lines of comments, then a few different aliases for the echo command.
		# the file assigns "pesterecho" as an alias to "echo"
		New-Item -Path $testAliasDirectory -ItemType Directory -Force > $null

		$pesteraliascontent ='# Alias File'+$newLine
		$pesteraliascontent+='# Exported by : alex'+$newLine
		$pesteraliascontent+='# Date/Time : Thursday, 12 November 2015 21:55:08'+$newLine
		$pesteraliascontent+='# Computer : archvm'

		# add various aliases for echo which we can then test
		$pesteraliascontent+= $newLine+'pesterecho,"'+$commandToAlias+'","","None"'
		$pesteraliascontent+= $newLine+$difficultToParseString_1+',"'+$commandToAlias+'","","None"'
		$pesteraliascontent+= $newLine+$difficultToParseString_2+',"'+$commandToAlias+'","","None"'
		$pesteraliascontent+= $newLine+$difficultToParseString_3+',"'+$commandToAlias+'","","None"'
		$pesteraliascontent > $pesteraliasfile

		# create invalid file with more than four values
		New-Item -Path $testAliasDirectory -ItemType Directory -Force > $null
		$pesteraliascontent+= $newLine+'"v_1","v_2","v_3","v_4","v_5"'
		$pesteraliascontent > $aliasPathMoreThanFourValues

		# create invalid file with less than four values
		New-Item -Path $testAliasDirectory -ItemType Directory -Force > $null
		$pesteraliascontent+= $newLine+'"v_1","v_2","v_3"'
		$pesteraliascontent > $aliasPathLessThanFourValues
	}

	AfterAll {
		Remove-Item -Path $testAliasDirectory -Recurse -Force
	}

	It "Should be able to import an alias file successfully" {
	    {Import-Alias -Path $pesteraliasfile} | Should -Not -throw
	}

	It "Should classify an alias as non existent when it is not imported yet" {
		{Get-Alias -Name pesterecho} | Should -Be null
	}

	It "Should be able to import an alias file and recognize an imported alias" {
		$aliasToTest = "pesterecho"
		Import-Alias -Path $pesteraliasfile
	    (Get-Alias -Name $aliasToTest -ErrorAction SilentlyContinue).Definition | Should -BeExactly $commandToAlias
	}

	It "Should be able to parse ""abc""""def"" into abc""def " {
		$aliasToTest = 'abc"def'
		Import-Alias -Path $pesteraliasfile
	    (Get-Alias -Name $aliasToTest -ErrorAction SilentlyContinue).Definition | Should -BeExactly $commandToAlias
	}

	It "Should be able to parse ""aaa"" into aaa " {
		$aliasToTest = "aaa"
		Import-Alias -Path $pesteraliasfile
	    (Get-Alias -Name $aliasToTest -ErrorAction SilentlyContinue).Definition | Should -BeExactly $commandToAlias

	}

	It "Should be able to parse ""a,b"" into a,b " {
		$aliasToTest = "a,b"
		Import-Alias -Path $pesteraliasfile
	    (Get-Alias -Name $aliasToTest -ErrorAction SilentlyContinue).Definition | Should -BeExactly $commandToAlias
	}

	It "Should throw an error when reading more than four values" {
	    { Import-Alias -Path $aliasPathMoreThanFourValues } | Should -Throw -ErrorId "ImportAliasFileFormatError,Microsoft.PowerShell.Commands.ImportAliasCommand"
	}

	It "Should throw an error when reading less than four values" {
	    { Import-Alias -Path $aliasPathLessThanFourValues } | Should -Throw -ErrorId "ImportAliasFileFormatError,Microsoft.PowerShell.Commands.ImportAliasCommand"
	}
}
