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
	$newLine=[Environment]::NewLine
	$testAliasDirectory = Join-Path -Path $TestDrive -ChildPath ImportAliasTestDirectory
	$testAliases        = "pesteralias.txt"

	# import alias fails for '"abc""def"': it throws an "Illegal characters in path" error
	$difficultToParseString_1		= '"abc""def"' 	# throws illegal character exception
	# $difficultToParseString_1		= 'abc""def'	# does not throw exception

	$difficultToParseString_2		= '"aaa"'
	$difficultToParseString_3		= '"a,b"'
    $pesteraliasfile    = Join-Path -Path $testAliasDirectory -ChildPath $testAliases

	BeforeEach {
		# create default pester testing file
		# the file assigns "pesterecho" as an alias to "echo"
		New-Item -Path $testAliasDirectory -ItemType Directory -Force

		$pesteraliascontent ='# Alias File'+$newLine
		$pesteraliascontent+='# Exported by : alex'+$newLine
		$pesteraliascontent+='# Date/Time : Thursday, 12 November 2015 21:55:08'+$newLine
		$pesteraliascontent+='# Computer : archvm'#+$newLine+$difficultToParseString+',"echo","","None"'

		# add various aliases for echo which we can then test

		$pesteraliascontent+= $newLine+'pesterecho,"echo","","None"'
		$pesteraliascontent+= $newLine+$difficultToParseString_1+',"echo","","None"'
		$pesteraliascontent+= $newLine+$difficultToParseString_2+',"echo","","None"'
		$pesteraliascontent+= $newLine+$difficultToParseString_3+',"echo","","None"'
		$pesteraliascontent > $pesteraliasfile
	}

	AfterEach {
		Remove-Item -Path $testAliasDirectory -Recurse -Force
	}

	It "Should be able to import an alias file successfully" {
	    { Import-Alias $pesteraliasfile } | Should -Not -throw
	}

	It "Should be able to import file via the Import-Alias alias of ipal" {
	    { ipal $pesteraliasfile } | Should -Not -throw
	}

	It "Should be able to import an alias file and perform imported aliased echo cmd" {
	    (Import-Alias $pesteraliasfile)
	    (pesterecho pestertesting) | Should -BeExactly "pestertesting"
	}

	It "Should be able to use ipal alias to import an alias file and perform cmd" {
	    (ipal $pesteraliasfile)
	    (pesterecho pestertesting) | Should -BeExactly "pestertesting"
	}

	It "Should be able to parse ""abc""""def"" into abc""def " {
		(Import-Alias $pesteraliasfile)
		# By setting a breakpoint after we perform get-alias, we can check how the string is parsed
		# A 'nicer' test would be to check if the parsed string is in the output of get-alias,
		# however, the way in which get-alias outputs doesn't have a nice contains() method which can check this (as far as I could see) so I therefore chose to leave this for future work.
		# $allAlias = (get-alias)
		$callingDifficultString = (abc`"def pestertesting)
		$callingDifficultString | Should -BeExactly "pestertesting"
	}

	It "Should be able to parse ""aaa"" into aaa " {
		(Import-Alias $pesteraliasfile)
		$allAlias = (get-alias)
		$callingDifficultString = (aaa pestertesting)
		$callingDifficultString | Should -BeExactly "pestertesting"
	}

	It "Should be able to parse ""a,b"" into a,b " {
		(Import-Alias $pesteraliasfile)
		# $allAlias = (get-alias)
		$callingDifficultString = (a`,b pestertesting)
		$callingDifficultString | Should -BeExactly "pestertesting"
	}
}
