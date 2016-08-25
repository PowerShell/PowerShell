Describe "Test-Path" -Tags "CI" {
    BeforeAll {
	$testdirectory = $TestDrive
	$testfilename  = New-Item -path $testdirectory -Name testfile.txt -ItemType file -Value 1 -force

	# populate with additional files
	New-Item -Path $testdirectory -Name datestfile -value 1 -ItemType file | Out-Null
	New-Item -Path $testdirectory -Name gatestfile -value 1 -ItemType file | Out-Null
	New-Item -Path $testdirectory -Name usr -value 1 -ItemType directory | Out-Null

	$nonExistentDir = Join-Path -Path (Join-Path -Path $testdirectory -ChildPath usr) -ChildPath bin
	$nonExistentPath = Join-Path -Path (Join-Path -Path (Join-Path -Path $testdirectory -ChildPath usr) -ChildPath bin) -ChildPath error
    }

    It "Should be called on an existing path without error" {
	{ Test-Path $testdirectory }              | Should Not Throw
	{ Test-Path -Path $testdirectory }        | Should Not Throw
	{ Test-Path -LiteralPath $testdirectory } | Should Not Throw
    }

    It "Should allow piping objects to it" {
	{ $testdirectory | Test-Path  } | Should Not Throw

	$testdirectory                  | Test-Path | Should Be $true
	$nonExistentDir                 | Test-Path | Should Be $false
    }

    It "Should be called on a nonexistent path without error" {
	{ Test-Path -Path $nonExistentPath } | Should Not Throw
    }

    It "Should return false for a nonexistent path" {
	Test-Path -Path $nonExistentPath | Should Be $false
    }

    It "Should return true for an existing path" {
	{ Test-Path -Path $testdirectory } | Should Be $true
    }

    It "Should be able to accept a regular expression" {
	{ Test-Path -Path (Join-Path -Path $testdirectory -ChildPath "u*") }           | Should Not Throw
	{ Test-Path -Path (Join-Path -Path $testdirectory -ChildPath "u[a-z]r") }      | Should Not Throw
    }

    It "Should be able to return the correct result when a regular expression is used" {
	Test-Path -Path (Join-Path -Path $testdirectory -ChildPath "u*")               | Should Be $true
	Test-Path -Path (Join-Path -Path $testdirectory -ChildPath "u[a-z]*")          | Should Be $true

	Test-Path -Path (Join-Path -Path $testdirectory -ChildPath "aoeu*")            | Should Be $false
	Test-Path -Path (Join-Path -Path $testdirectory -ChildPath "u[A-Z]")           | Should Be $false
    }

    It "Should return false when the Leaf pathtype is used on a directory" {
	Test-Path -Path $testdirectory -PathType Leaf | Should Be $false
    }

    It "Should return true when the Leaf pathtype is used on an existing endpoint" {
	Test-Path -Path $testfilename -PathType Leaf | Should Be $true
    }

    It "Should return false when the Leaf pathtype is used on a nonexistent file" {
	Test-Path -Path "aoeu" -PathType Leaf | Should Be $false
    }

    It "Should return true when the Leaf pathtype is used on a file using the Type alias instead of PathType" {
	Test-Path -Path $testfilename -Type Leaf | Should Be $true
    }

    It "Should be able to search multiple regular expressions using the include switch" {
	{ Test-Path -Path (Join-Path -Path $testdirectory -ChildPath "*") -Include t* } | Should Be $true
    }

    It "Should be able to exclude a regular expression using the exclude switch" {
	{ Test-Path -Path (Join-Path -Path $testdirectory -ChildPath "*") -Exclude v* } | Should Be $true
    }

    It "Should be able to exclude multiple regular expressions using the exclude switch" {
	# tests whether there's any files in the usr directory that don't start with 'd' or 'g'
	{ Test-Path -Path $testfilename -Exclude d*, g* } | Should Be $true
    }

    It "Should return true if the syntax of the path is correct when using the IsValid switch" {
	{ Test-Path -Path $nonExistentPath -IsValid } | Should Be $true
    }

    It "Should return false if the syntax of the path is incorrect when using the IsValid switch" {
	$badPath = " :;!@#$%^&*(){}?+|_-"
	Test-Path -Path $badPath -IsValid | Should Be $false
    }

    It "Should return true on paths containing spaces when the path is surrounded in quotes" {
	Test-Path -Path "/totally a valid/path" -IsValid | Should Be $true
    }

    It "Should throw on paths containing spaces when the path is not surrounded in quotes" {
	{ Test-Path -Path /a path/without quotes/around/it -IsValid } | Should Throw
    }

    It "Should return true if a directory leads or trails with a space when surrounded by quotes" {
	Test-Path -Path "/a path / with/funkyspaces" -IsValid | Should Be $true
    }

    It "Should return true on a valid path when the LiteralPath switch is used" {
	Test-Path -LiteralPath $testfilename | Should Be $true
    }

    It "Should return false if regular expressions are used with the LiteralPath switch" {
	Test-Path -LiteralPath (Join-Path -Path $testdirectory -ChildPath "u*")            | Should Be $false
	Test-Path -LiteralPath (Join-Path -Path $testdirectory -ChildPath "u[a-z]r")       | Should Be $false
    }

    It "Should return false if regular expressions are used with the LiteralPath alias PSPath switch" {
	Test-Path -PSPath (Join-Path -Path $testdirectory -ChildPath "u*")            | Should Be $false
	Test-Path -PSPath (Join-Path -Path $testdirectory -ChildPath "u[a-z]r")       | Should Be $false
    }

    It "Should return true if used on components other than filesystem objects" {
	Test-Path Alias:\gci | Should Be $true
	Test-Path Env:\PATH  | Should Be $true
    }

}
