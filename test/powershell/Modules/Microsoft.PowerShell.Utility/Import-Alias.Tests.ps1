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
			$ErrorActionPreference = "Stop"
		try {
			Import-Alias *
			Throw "Execution OK"
		}
		catch {
			$_.FullyQualifiedErrorId | Should be "NotSupported,Microsoft.PowerShell.Commands.ImportAliasCommand"
		}
	}

	It "Import-Alias From Exported Alias File Aliases Already Exist should throw SessionStateException" {
			$ErrorActionPreference = "Stop"
		{Export-Alias  $fulltestpath abcd*}| Should Not Throw
		try {
			Import-Alias $fulltestpath
			Throw "Execution OK"
		}
		catch {
			$_.FullyQualifiedErrorId | Should be "AliasAlreadyExists,Microsoft.PowerShell.Commands.ImportAliasCommand"
		}
    }

	It "Import-Alias Into Invalid Scope should throw PSArgumentException"{
		{Export-Alias  $fulltestpath abcd*}| Should Not Throw
		try {
			Import-Alias $fulltestpath -scope bogus
			Throw "Execution OK"
		}
		catch {
			$_.FullyQualifiedErrorId | Should be "Argument,Microsoft.PowerShell.Commands.ImportAliasCommand"
		}
    }

	It "Import-Alias From Exported Alias File Aliases Already Exist using force should not throw"{
		{Export-Alias  $fulltestpath abcd*}| Should Not Throw
		{Import-Alias $fulltestpath  -Force}| Should Not Throw
    }
}

Describe "Import-Alias" -Tags "CI" {
	$newLine=[Environment]::NewLine
	$testAliasDirectory = Join-Path -Path $TestDrive -ChildPath ImportAliasTestDirectory
    $testAliases        = "pesteralias.txt"
    $pesteraliasfile       = Join-Path -Path $testAliasDirectory -ChildPath $testAliases

	BeforeEach {
		New-Item -Path $testAliasDirectory -ItemType Directory -Force

		$pesteraliascontent ='# Alias File'+$newLine
		$pesteraliascontent+='# Exported by : alex'+$newLine
		$pesteraliascontent+='# Date/Time : Thursday, 12 November 2015 21:55:08'+$newLine
		$pesteraliascontent+='# Computer : archvm'+$newLine+'"pesterecho","echo","","None"'
		$pesteraliascontent > $pesteraliasfile
	}

	AfterEach {
		Remove-Item -Path $testAliasDirectory -Recurse -Force
	}

	It "Should be able to import an alias file successfully" {
	    { Import-Alias $pesteraliasfile } | Should Not throw
	}

	It "Should be able to import file via the Import-Alias alias of ipal" {
	    { ipal $pesteraliasfile } | Should Not throw
	}

	It "Should be able to import an alias file and perform imported aliased echo cmd" {
	    (Import-Alias $pesteraliasfile)
	    (pesterecho pestertesting) | Should Be "pestertesting"
	}

	It "Should be able to use ipal alias to import an alias file and perform cmd" {
	    (ipal $pesteraliasfile)
	    (pesterecho pestertesting) | Should be "pestertesting"
	}
}
