# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "ParserTests (admin\monad\tests\monad\src\engine\core\ParserTests.cs)" -Tags "CI" {
    BeforeAll {
		$functionDefinitionFile = Join-Path -Path $TestDrive -ChildPath "functionDefinition.ps1"
		$functionDefinition = @'
		function testcmd-parserbvt
		{
		[CmdletBinding()]
			param (
			[Parameter(Position = 0)]
			[string] $Property1 = "unset",

			[Parameter(Position = 1)]
			[string] $Property2 = "unset",

			[Parameter(Position = 2)]
			[string] $Property3 = "unset",

			[Parameter()]
			[ValidateSet("default","array","object","nestedobject","struct","mshobject","nulltostring")]
			[string]$ReturnType = "default"
			)

			BEGIN {}

			PROCESS {
				if ( ! $ReturnType ) {
					$ReturnType = "default"
				}

				switch ( $ReturnType )
				{
					"default" {
						$result = "$Property1;$Property2;$Property3"
						break
					}
					"array" {
						$result = 1,2,3
						break
					}
					"object" {
						$result = new-object psobject
						break
					}
					"nestedobject" {
						$result = [pscustomobject]@{Name="John";Person=[pscustomobject]@{Name="John";Age=30}}
						break
					}
					"struct" {
						$result = [pscustomobject]@{Name="John";Age=30}
						break
					}
					"mshobject" {
						$result = new-object psobject
						break
					}
					"nulltostring" {
						$result = $null
						break
					}
					default {
						throw ([invalidoperationexception]::new("ReturnType parameter wasn't of any Expected value!"))
						break
					}
				}
				return $result
			}

			END {}
		}
'@
		$functionDefinition>$functionDefinitionFile

        $PowerShell = [powershell]::Create()
        $PowerShell.AddScript(". $functionDefinitionFile").Invoke()
        $PowerShell.Commands.Clear()
        function ExecuteCommand {
            param ([string]$command)
            try {
                $PowerShell.AddScript($command).Invoke()
            }
            finally {
                $PowerShell.Commands.Clear()
            }
        }
    }
	BeforeEach {
		$testfile = Join-Path -Path $TestDrive -ChildPath "testfile.ps1"
		$shellfile = Join-Path -Path $TestDrive -ChildPath "testfile.cmd"
		$testfolder1 = Join-Path -Path $TestDrive -ChildPath "dir1"
		$testfolder2 = Join-Path -Path $testfolder1 -ChildPath "dir2"
		if(-Not(Test-Path $testfolder1))
		{
			New-Item $testfolder1 -Type Directory
		}
		if(-Not(Test-Path $testfolder2))
		{
			New-Item $testfolder2 -Type Directory
		}
		$testdirfile1 = Join-Path -Path $testfolder2 -ChildPath "testdirfile1.txt"
		$testdirfile2 = Join-Path -Path $testfolder2 -ChildPath "testdirfile2.txt"
		"">$testdirfile1
		"">$testdirfile2
	}
    AfterEach {
		if(Test-Path $testfile)
		{
			Remove-Item $testfile
		}
		if(Test-Path $shellfile)
		{
			Remove-Item $shellfile
		}
		if(Test-Path $testdirfile1)
		{
			Remove-Item $testdirfile1
		}
		if(Test-Path $testdirfile2)
		{
			Remove-Item $testdirfile2
		}
		if(Test-Path $testfolder2)
		{
			Remove-Item $testfolder2
		}
		if(Test-Path $testfolder1)
		{
			Remove-Item $testfolder1
		}
    }

	AfterAll {
		if(Test-Path $functionDefinitionFile)
		{
			Remove-Item $functionDefinitionFile
		}
	}

	It "Throws a syntax error when parsing a string without a closing quote. (line 164)" {
        { ExecuteCommand '"This is a test' } | Should -Throw -ErrorId "IncompleteParseException"
	}

	It "Throws an error if an open parenthesis is not closed (line 176)" {
        { ExecuteCommand "(" } | Should -Throw -ErrorId "IncompleteParseException"
    }

    It "Throws an exception if the the first statement starts with an empty pipe element (line 188)" {
        { ExecuteCommand "| get-location" } | Should -Throw -ErrorId "ParseException"
    }

	It "Throws an CommandNotFoundException exception if using a label in front of an if statement is not allowed. (line 225)"{
        $PowerShell.Streams.Error.Clear()
        ExecuteCommand ":foo if ($x -eq 3) { 1 }"
		$PowerShell.HadErrors | Should -BeTrue
        $PowerShell.Streams.Error.FullyQualifiedErrorId | Should -Be "CommandNotFoundException"
    }

	It "Pipe an expression into a value expression. (line 237)" {
        { ExecuteCommand "testcmd-parserbvt | 3" } | Should -Throw -ErrorId "ParseException"

		{ ExecuteCommand "testcmd-parserbvt | $(1 + 1)" } | Should -Throw -ErrorId "ParseException"

		{ ExecuteCommand "testcmd-parserbvt | 'abc'" } | Should -Throw -ErrorId "ParseException"
    }

    It "Throws when you pipe into a value expression (line 238)" {
        foreach($command in "1;2;3|3",'1;2;3|$(1+1)',"1;2;3|'abc'") {
            { ExecuteCommand $command } | Should -Throw -ErrorId "ParseException"
        }
    }

	It "Throws an incomplete parse exception when a comma follows an expression (line 247)" {
        { ExecuteCommand "(1+ 1)," } | Should -Throw -ErrorId "IncompleteParseException"
    }

	It "Test that invoke has a higher precedence for a script than for an executable. (line 279)" {
		"1">$testfile
        $result = ExecuteCommand ". $testfile"
		$result | Should -Be 1
    }

	It "This test will check that a path is correctly interpreted when using '..' and '.'  (line 364)" {
        $result = ExecuteCommand "set-location $TestDrive; get-childitem dir1\.\.\.\..\dir1\.\dir2\..\..\dir1\.\dir2"
		$result.Count | Should -Be 2
		$result[0].Name | Should -BeExactly "testdirfile1.txt"
		$result[1].Name | Should -BeExactly "testdirfile2.txt"
    }

	It "This test will check that the parser can handle a mix of forward slashes and back slashes in the path (line 417)" {
        $result = ExecuteCommand "get-childitem $TestDrive/dir1/./.\.\../dir1/.\dir2\../..\dir1\.\dir2"
		$result.Count | Should -Be 2
		$result[0].Name | Should -BeExactly "testdirfile1.txt"
		$result[1].Name | Should -BeExactly "testdirfile2.txt"
    }

	It "This test checks that the asterisk globs as expected. (line 545)" {
        $result = ExecuteCommand "get-childitem $TestDrive/dir1\dir2\*.txt"
		$result.Count | Should -Be 2
		$result[0].Name | Should -BeExactly "testdirfile1.txt"
		$result[1].Name | Should -BeExactly "testdirfile2.txt"
    }

	It "This test checks that we can use a range for globbing: [1-2] (line 557)" {
        $result = ExecuteCommand "get-childitem $TestDrive/dir1\dir2\testdirfile[1-2].txt"
		$result.Count | Should -Be 2
		$result[0].Name | Should -BeExactly "testdirfile1.txt"
		$result[1].Name | Should -BeExactly "testdirfile2.txt"
    }

	It "This test will check that escaping the $ sigil inside single quotes simply returns the $ character. (line 583)" {
        $result = ExecuteCommand "'`$'"
		$result | Should -BeExactly "`$"
    }

	It "Test that escaping a space just returns that space. (line 593)" {
        $result = ExecuteCommand '"foo` bar"'
		$result | Should -BeExactly "foo bar"
    }

    It "Test that escaping the character 'e' returns the ESC character (0x1b)." {
        $result = ExecuteCommand '"`e"'
        $result | Should -BeExactly ([char]0x1b)
    }

    Context "Test Unicode escape sequences." {
        # These tests require the file to be saved with a BOM.  Unfortunately when this UTF8 file is read by
        # PowerShell without a BOM, the file is incorrectly interpreted as ASCII.
        It 'Test that the bracketed Unicode escape sequence `u{0} returns minimum char.' {
            $result = ExecuteCommand '"`u{0}"'
            [int]$result[0] | Should -Be 0
        }

        It 'Test that the bracketed Unicode escape sequence `u{10FFFF} returns maximum surrogate char pair.' {
            $result = ExecuteCommand '"`u{10FFFF}"'
            [int]$result[0] | Should -BeExactly 0xDBFF # max value for high surrogate of surrogate pair
            [int]$result[1] | Should -BeExactly 0xDFFF # max value for low surrogate of surrogate pair
        }

        It 'Test that the bracketed Unicode escape sequence `u{a9} returns the © character.' {
            $result = ExecuteCommand '"`u{a9}"'
            $result | Should -BeExactly '©'
        }

        It 'Test that Unicode escape sequence `u{2195} in string returns the ↕ character.' {
            $result = ExecuteCommand '"foo`u{2195}abc"'
            $result | Should -BeExactly "foo↕abc"
        }

        It 'Test that the bracketed Unicode escape sequence `u{1f44d} returns surrogate pair for emoji 👍 character.' {
            $result = ExecuteCommand '"`u{1f44d}"'
            $result | Should -BeExactly "👍"
        }

        It 'Test that Unicode escape sequence `u{2195} in here string returns the ↕ character.' {
            $result = ExecuteCommand ("@`"`n`n" + 'foo`u{2195}abc' + "`n`n`"@")
            $result | Should -BeExactly "`nfoo↕abc`n"
        }

        It 'Test that Unicode escape sequence in single quoted is not processed.' {
            $result = ExecuteCommand '''foo`u{2195}abc'''
            $result | Should -BeExactly 'foo`u{2195}abc'
        }

        It 'Test that Unicode escape sequence in single quoted here string is not processed.' {
            $result = ExecuteCommand @"
@'

foo``u{2195}abc

'@
"@
            $result | Should -Match "\r?\nfoo``u\{2195\}abc\r?\n"
        }

        It "Test that two consecutive Unicode escape sequences are tokenized correctly." {
            $result = ExecuteCommand '"`u{007b}`u{007d}"'
            $result | Should -Be '{}'
        }

        It "Test that a Unicode escape sequence can be used in a command name." {
            function xyzzy`u{2195}($p) {$p}
            $cmd = Get-Command xyzzy`u{2195} -ErrorAction SilentlyContinue
            $cmd | Should -Not -BeNullOrEmpty
            $cmd.Name | Should -BeExactly 'xyzzy↕'
            xyzzy`u{2195} 42 | Should -Be 42
        }

        It "Test that a Unicode escape sequence can be used in a variable name." {
            ${fooxyzzy`u{2195}} = 42
            $var = Get-Variable -Name fooxyzzy* -ErrorAction SilentlyContinue
            $var | Should -Not -BeNullOrEmpty
            $var.Name | Should -BeExactly "fooxyzzy↕"
            $var.Value | Should -Be 42
        }

        It "Test that a Unicode escape sequence can be used in an argument." {
            Write-Output `u{a9}` Acme` Inc | Should -BeExactly "© Acme Inc"
        }
    }

	It "Test that escaping any character with no special meaning just returns that char. (line 602)" {
        $result = ExecuteCommand '"fo`obar"'
		$result | Should -BeExactly "foobar"
    }

	Context "Test that we support all of the C# escape sequences. We use the ` instead of \. (line 613)" {
		# the first two sequences are tricky, because we need to provide something to
		# execute without causing an incomplete parse error
		$tests = @{ sequence = "write-output ""`'"""; expected = ([char]39) },
			@{ sequence = 'write-output "`""'; expected = ([char]34) },
			# this is a string, of 2 "\", the initial backtick should essentially be ignored
			@{ sequence = '"`\\"'; expected = '\\' },
			# control sequences
			@{ sequence = '"`0"'; expected = ([char]0) }, # null
			@{ sequence = '"`a"'; expected = ([char]7) },
			@{ sequence = '"`b"'; expected = ([char]8) }, # backspace
			@{ sequence = '"`f"'; expected = ([char]12) }, # form
			@{ sequence = '"`n"'; expected = ([char]10) }, # newline
			@{ sequence = '"`r"'; expected = ([char]13) }, # return
			@{ sequence = '"`t"'; expected = ([char]9) }, # tab
			@{ sequence = '"`v"'; expected = ([char]11) }
		It "C# escape sequence <sequence> is supported using `` instead of \. (line 613)" -TestCases $tests {
			param ( $sequence, $expected )
			$result = ExecuteCommand $sequence
			$result | Should -BeExactly $expected
		}
    }

	It "This test checks that array substitution occurs inside double quotes. (line 646)" {
        $result = ExecuteCommand '$MyArray = "a","b";"Hello $MyArray"'
		$result | Should -BeExactly "Hello a b"
    }

	It "This tests declaring an array in nested variable tables. (line 761)" {
        $result = ExecuteCommand "`$Variable:vtbl1:vtbl2:b=@(5,6);`$Variable:vtbl1:vtbl2:b"
		$result.Count | Should -Be 2
		$result[0] | Should -Be 5
		$result[1] | Should -Be 6
    }

	It "Test a simple multiple assignment. (line 773)" {
        $result = ExecuteCommand '$one,$two = 1,2,3; "One = $one"; "Two = $two"'
		$result.Count | Should -Be 2
		$result[0] | Should -Be "One = 1"
		$result[1] | Should -Be "Two = 2 3"
    }

	It "Tests script, global and local scopes from a function inside a script. (line 824)" {
		"`$var = 'script';function func { `$var; `$var = 'local'; `$local:var; `$script:var; `$global:var };func;`$var;">$testfile
		ExecuteCommand "`$var = 'global'"
        $result = ExecuteCommand "$testfile"
		$result.Count | Should -Be 5
		$result[0] | Should -BeExactly "script"
		$result[1] | Should -BeExactly "local"
		$result[2] | Should -BeExactly "script"
		$result[3] | Should -BeExactly "global"
		$result[4] | Should -BeExactly "script"
    }

	It "Use break inside of a loop that is inside another loop. (line 945)" {
		$commands = " while (1) { 1; while(1) { 2; break; 3; }; 4; break; 5;  } ",
                " for (;;) { 1; for(;;) { 2; break; 3; }; 4; break; 5; } ",
                " foreach(`$a in 1,2,3) { 1; foreach( `$b in 1,2,3 ) { 2; break; 3; }; 4; break; 5; } "
		$results = "1", "2", "4"
        $i = 0
		for(;$i -lt $commands.Count;$i++)
		{
			$result = ExecuteCommand $commands[$i]
			$result | Should -Be $results
		}
    }

	It "Use break in two loops with same label. (line 967)" {
		$commands = " :foo while (1) { 1; :foo while(1) { 2; break foo; 3; }; 4; break; 5;  } ",
                " :foo for (;;) { 1; :foo for(;;) { 2; break foo; 3; }; 4; break; 5; } ",
                " :foo foreach(`$a in 1,2,3) { 1; :foo foreach( `$b in 1,2,3 ) { 2; break foo; 3; }; 4; break; 5; } "
		$results = "1", "2", "4"
        $i = 0
		for(;$i -lt $commands.Count;$i++)
		{
			$result = ExecuteCommand $commands[$i]
			$result | Should -Be $results
		}
    }

	It "Try continue inside of different loop statements. (line 1039)" {
		$commands = " `$a = 0; while (`$a -lt 2) { `$a; `$a += 1; continue; 2; } ",
                " for (`$a = 0;`$a -lt 2; `$a += 1) { 9; continue; 3; } ",
                " foreach(`$a in 0,1) { `$a; continue; 2; } "
		$result = ExecuteCommand $commands[0]
		$result | Should -Be "0", "1"
		$result = ExecuteCommand $commands[1]
		$result | Should -Be "9", "9"
		$result = ExecuteCommand $commands[2]
		$result | Should -Be "0", "1"
    }

	It "Use a label to continue an inner loop. (line 1059)" {
		$commands = " `$x = 0; while (`$x -lt 1) { `$x += 1; `$x; `$a = 0; :foo while(`$a -lt 2) { `$a += 1; `$a; continue foo; 3; }; 4; continue; 5;  } ",
                " for (`$x = 0;`$x -lt 1;`$x += 1) { 1; :foo for(`$a = 0; `$a -lt 2; `$a += 1) { `$a; continue foo; 3; }; 4; continue; 5; } ",
                " foreach(`$a in 1) { 1; :foo foreach( `$b in 1,2 ) { `$b; continue foo; 3; }; 4; continue; 5; } "
		$result = ExecuteCommand $commands[0]
		$result | Should -Be "1", "1", "2", "4"
		$result = ExecuteCommand $commands[1]
		$result | Should -Be "1", "0", "1", "4"
		$result = ExecuteCommand $commands[2]
		$result | Should -Be "1", "1", "2", "4"
    }

	It "Use continue with a label on a nested loop. (line 1059)" {
		$commands = " `$x = 0; :foo while (`$x -lt 2) { `$x; `$x += 1; :bar while(1) { 2; continue foo; 3; }; 4; continue; 5;  } ",
                " :foo for (`$x = 0;`$x -lt 2;`$x += 1) { 1; :bar for(;;) { 2; continue foo; 3; }; 4; continue; 5; } ",
                " :foo foreach(`$a in 1,2) { 1; :bar foreach( `$b in 1,2,3 ) { 2; continue foo; 3; }; 4; continue; 5; } "
		$result = ExecuteCommand $commands[0]
		$result | Should -Be "0", "2", "1", "2"
		$result = ExecuteCommand $commands[1]
		$result | Should -Be "1", "2", "1", "2"
		$result = ExecuteCommand $commands[2]
		$result | Should -Be "1", "2", "1", "2"
    }

	It "This test will check that it is a syntax error to use if without a code block. (line 1141)" {
        { ExecuteCommand 'if ("true")' } | Should -Throw -ErrorId "IncompleteParseException"
    }

	It "This test will check that it is a syntax error if the if condition is not complete. (line 1150)" {
        { ExecuteCommand 'if (' } | Should -Throw -ErrorId "IncompleteParseException"
    }

	It "This test will check that it is a syntax error to have an if condition without parentheses. (line 1159)" {
        { ExecuteCommand 'if "true" { 1} else {2}' } | Should -Throw -ErrorId "ParseException"
    }

	It "This test will check that the parser throws a syntax error when the if condition is missing the closing parentheses. (line 1168)" {
        { ExecuteCommand 'if ("true"  { 1};' } | Should -Throw -ErrorId "ParseException"
    }

	It "This test will check that it is a syntax error to have an else keyword without the corresponding code block. (line 1177)" {
        { ExecuteCommand 'if ("true") {1} else' } | Should -Throw -ErrorId "IncompleteParseException"
    }

	It "This test will check that the parser throws a syntax error when a foreach loop is not complete. (line 1238)" {
        { ExecuteCommand '$count=0;$files = $(get-childitem / -filter *.txt );foreach ($i ;$count' } | Should -Throw -ErrorId "ParseException"
    }

	It "This test will check that the parser throws a syntax error if the foreach loop is not complete. (line 1248)" {
        { ExecuteCommand '$count=0;$files = $(get-childitem / -filter *.txt );foreach ($i in ;$count' } | Should -Throw -ErrorId "ParseException"
    }

	It "This will test that the parser throws a syntax error if the foreach loop is missing a closing parentheses. (line 1258)" {
        { ExecuteCommand '$count=0;$files = $(get-childitem / -filter *.txt );foreach ($i in $files ;$count' } | Should -Throw -ErrorId "ParseException"
    }

	It "Test that if an exception is thrown from the try block it will be caught in the appropropriate catch block and that the finally block will run regardless of whether an exception is thrown. (line 1317)" {
        $result = ExecuteCommand 'try { try { throw (new-object System.ArgumentException) } catch [System.DivideByZeroException] { } finally { "Finally" } } catch { $_.Exception.GetType().FullName }'
		$result | Should -Be "Finally", "System.ArgumentException"
    }

    It "Test that a break statement in a finally block results in a ParseException" {
        { ExecuteCommand 'try {} finally { break }' } | Should -Throw -ErrorId ParseException
    }

    It "Test that a switch statement with a break in a finally doesn't trigger a parse error" {
        ExecuteCommand 'try {"success"} finally {switch (1) {foo  {break}}}' | Should -BeExactly 'success'
    }

	It "Test that null can be passed to a method that expects a reference type. (line 1439)" {
        $result = ExecuteCommand '$test = "String";$test.CompareTo($())'
		$result | Should -Be 1
    }

	It "Tests that command expansion operators can be used as a parameter to an object method. (line 1507)" {
        $result = ExecuteCommand '$test = "String";$test.SubString($("hello" | foreach-object { $_.length - 2 } ))'
		$result | Should -Be "ing"
    }

	It "Test that & can be used as a parameter as long as it is quoted. (line 1606)" {
        $result = ExecuteCommand 'testcmd-parserbvt `&get-childitem'
		$result | Should -Be "&get-childitem;unset;unset"
		$result = ExecuteCommand 'testcmd-parserbvt `&*'
		$result | Should -Be "&*;unset;unset"
    }

	It "Run a command with parameters. (line 1621)" {
        $result = ExecuteCommand 'testcmd-parserBVT -Property1 set'
		$result | Should -Be "set;unset;unset"
    }

	It "Test that typing a number at the command line will return that number. (line 1630)" {
        $result = ExecuteCommand '3'
		$result | Should -Be "3"
		$result.gettype() |should -Be ([int])
    }

	It "This test will check that an msh script can be run without invoking. (line 1641)" {
        "1">$testfile
        $result = ExecuteCommand ". $testfile"
		$result | Should -Be 1
    }

	It "Test that an alias is resolved before a function. (line 1657)" {
        $result = ExecuteCommand 'set-alias parserInvokeTest testcmd-parserBVT;function parserInvokeTest { 3 };parserInvokeTest'
		$result | Should -Be "unset;unset;unset"
    }

	It "Test that functions are resolved before cmdlets. (line 1678)"{
        $result_cmdlet = $PowerShell.AddScript('function test-parserfunc { [CmdletBinding()] Param() PROCESS { "cmdlet" } };test-parserfunc').Invoke()
        $result_func = ExecuteCommand 'function test-parserfunc { "func" };test-parserfunc'
        $PowerShell.Commands.Clear()
        $result_cmdlet | Should -Be "cmdlet"
        $result_func | Should -Be "func"
    }

	It "Check that a command that uses shell execute can be run from the command line and that no exception is thrown. (line 1702)" {
		if ( $IsLinux -or $IsMacOS ) {
            # because we execute on *nix based on executable bit, and the file name doesn't matter
            # so we can use the same filename as for windows, just make sure it's executable with chmod
            "#!/bin/sh`necho ""Hello World""" | out-file -encoding ASCII $shellfile
            /bin/chmod +x $shellfile
        }
        else {
            "@echo Hello, I'm a Cmd script!">$shellfile
        }
        { ExecuteCommand "$shellfile" } | Should -Not -Throw
    }

	Context "Boolean Tests (starting at line 1723 to line 1772)" {
        $testData = @(
            @{ Script = '"False"'; Expected = $true }
			@{ Script = 'if ("A") { $true } else { $false }'; Expected = $true }
            @{ Script = 'if (" ") { $true } else { $false }'; Expected = $true }
            @{ Script = 'if ("String with spaces") { $true } else { $false }'; Expected = $true }
            @{ Script = 'if ("DoubleQuoted") { $true } else { $false }'; Expected = $true }
            @{ Script = 'if ("StringWithNullVar$aEmbedded") { $true } else { $false }'; Expected = $true }
			@{ Script = 'if (0) { $true } else { $false }'; Expected = $false }
			@{ Script = '$a = $(0);if ($a) { $true } else { $false }'; Expected = $false }
			@{ Script = '$obj = testcmd-parserBVT -ReturnType object;if ($obj) { $true } else { $false }'; Expected = $true }
		)
        It "<Script> should return <Expected>" -TestCases $testData {
            param ( $Script, $Expected )
            ExecuteCommand $Script | Should -Be $Expected
        }
    }

	Context "Comparison operator Tests (starting at line 1785 to line 1842)" {
        $testData = @(
			@{ Script = 'if (1 -and 1) { $true } else { $false }'; Expected = $true }
            @{ Script = 'if (1 -and 0) { $true } else { $false }'; Expected = $false }
            @{ Script = 'if (0 -and 1) { $true } else { $false }'; Expected = $false }
            @{ Script = 'if (0 -and 0) { $true } else { $false }'; Expected = $false }
			@{ Script = 'if (1 -or 1) { $true } else { $false }'; Expected = $true }
            @{ Script = 'if (1 -or 0) { $true } else { $false }'; Expected = $true }
            @{ Script = 'if (0 -or 1) { $true } else { $false }'; Expected = $true }
            @{ Script = 'if (0 -or 0) { $true } else { $false }'; Expected = $false }
            #-eq
            @{ Script = 'if ($False -eq $True -and $False) { $true } else { $false }'; Expected = $false }
            @{ Script = 'if ($False -and $True -eq $False) { $true } else { $false }'; Expected = $false }
            #-ieq
            @{ Script = 'if ($False -ieq $True -and $False) { $true } else { $false }'; Expected = $false }
            @{ Script = 'if ($False -and $True -ieq $False) { $true } else { $false }'; Expected = $false }
            #-le
            @{ Script = 'if ($False -le $True -and $False) { $true } else { $false }'; Expected = $false }
            @{ Script = 'if ($False -and $True -le $False) { $true } else { $false }'; Expected = $false }
            #-ile
            @{ Script = 'if ($False -ile $True -and $False) { $true } else { $false }'; Expected = $false }
            @{ Script = 'if ($False -and $True -ile $False) { $true } else { $false }'; Expected = $false }
            #-ge
            @{ Script = 'if ($False -ge $True -and $False) { $true } else { $false }'; Expected = $false }
            @{ Script = 'if ($False -and $True -ge $False) { $true } else { $false }'; Expected = $false }
            #-ige
            @{ Script = 'if ($False -ige $True -and $False) { $true } else { $false }'; Expected = $false }
            @{ Script = 'if ($False -and $True -ige $False) { $true } else { $false }'; Expected = $false }
            #-like
            @{ Script = 'if ($False -like $True -and $False) { $true } else { $false }'; Expected = $false }
            @{ Script = 'if ($False -and $True -like $False) { $true } else { $false }'; Expected = $false }
            #!
            @{ Script = 'if (!$True -and $False) { $true } else { $false }'; Expected = $false }
		)
        It "<Script> should return <Expected>" -TestCases $testData {
            param ( $Script, $Expected )
            ExecuteCommand $Script | Should -Be $Expected
        }
    }

    Context "Arithmetic and String Comparison Tests (starting at line 1848 to line 1943)" {
        $testData = @(
            @{ Script = '$a=10; if( !$a -eq 10) { $a=1 } else {$a=2};$a'; Expected = 2 }
            @{ Script = "!3"; Expected = $false }
            @{ Script = '$a=10; if($a -lt 15) { $a=1 } else {$a=2};$a'; Expected = 1 }
            @{ Script = "'aaa' -ilt 'AAA'"; Expected = $false }
            @{ Script = "'aaa' -lt 'AAA'"; Expected = $false }
            @{ Script = "'aaa' -clt 'AAA'"; Expected = $true }
            @{ Script = '$a=10; if($a -gt 15) { $a=1 } else {$a=2};$a'; Expected = 2 }
            @{ Script = "'AAA' -igt 'aaa'"; Expected = $false }
            @{ Script = "'AAA' -gt 'aaa'"; Expected = $false }
            @{ Script = "'AAA' -cgt 'aaa'"; Expected = $true }
            @{ Script = '$a=10; if($a -ge 10) { $a=1 } else {$a=2};$a'; Expected = 1 }
            @{ Script = "'aaa' -ge 'aaa'"; Expected = $true }
            @{ Script = '$a=10; if($a -ne 10) { $a=1 } else {$a=2};$a'; Expected = 2 }
            @{ Script = "'aaa' -ine 'AAA'"; Expected = $false }
            @{ Script = "'aaa' -ne 'AAA'"; Expected = $false }
            @{ Script = "'aaa' -cne 'AAA'"; Expected = $true }
            @{ Script = '$a="abc"; if($a -ieq "ABC") { $a=1 } else {$a=2};$a'; Expected = 1 }
            @{ Script = "'aaa' -ieq 'AAA'"; Expected = $true }
            @{ Script = '$a="abc"; if($a -igt "bbc") { $a=1 } else {$a=2};$a'; Expected = 2 }
            @{ Script = "'aaa' -igt 'AAA'"; Expected = $false }
            @{ Script = '$a="abc"; if($a -ile "bbc") { $a=1 } else {$a=2};$a'; Expected = 1 }
            @{ Script = "'aaa' -ile 'AAA'"; Expected = $true }
        )
        It "<Script> should return <Expected>" -TestCases $testData {
            param ( $Script, $Expected )
            ExecuteCommand $Script | Should -Be $Expected
        }
    }

	Context "Comparison Operators with Arrays Tests (starting at line 2015 to line 2178)" {
        $testData = @(
            @{ Script = "@(3) -eq 3"; Expected = "3" }
            @{ Script = "@(4) -eq 3"; Expected = $null }
			@{ Script = "@() -eq 3"; Expected = $null }
			@{ Script = '$test = 1,2,@(3,4);$test -eq 3'; Expected = $null }
			@{ Script = '$test = 1,2,@(3,4);$test -eq 2'; Expected = "2" }
			@{ Script = '$test = 1,2,@(3,4);$test -eq 0'; Expected = $null }
        )
        It "<Script> should return <Expected>" -TestCases $testData {
            param ( $Script, $Expected )
            ExecuteCommand $Script | Should -Be $Expected
        }
    }

	It "A simple test for trapping a specific exception. Expected Result: The exception is caught and ignored. (line 2265)" {
        { ExecuteCommand "trap [InvalidCastException] { continue;  }; [int] 'abc'" } | Should -Not -Throw
    }

	It "Test that assign to input var and use then execute a script block with piped input. (line 2297)"{
        $result = ExecuteCommand '$input = 1,2,3;4,-5,6 | & { $input }'
		 $result -join "" | Should -Be (4,-5,6 -join "")
    }

	It "Test that pipe objects into a script and use arguments. (line 2313)"{
		"`$input; `$args;">$testfile
        $result = ExecuteCommand "1,2,3 | $testfile"
		$result -join "" | Should -Be (1, 2, 3 -join "")
		$result = ExecuteCommand "$testfile 4 -5 6 -blah -- foo -bar"
		$result | Should -BeExactly "4", "-5", "6", "-blah", "foo", "-bar"
    }

	Context "Numerical Notations Tests (starting at line 2374 to line 2452)" {
        $testData = @(
			#Test various numbers using the standard notation.
            @{ Script = "0"; Expected = "0" }
			@{ Script = "-2"; Expected = "-2" }
			@{ Script = "2"; Expected = "2" }
			@{ Script = $([int32]::MaxValue); Expected = $([int32]::MaxValue) }
			@{ Script = $([int32]::MinValue); Expected = $([int32]::MinValue) }
			#Tests for hexadecimal notation.
			@{ Script = "0x0"; Expected = "0" }
			@{ Script = "0xF"; Expected = "15" }
			@{ Script = "0x80000000"; Expected = $([int32]::MinValue) }
			@{ Script = "0xFFFFFFFF"; Expected = "-1" }
			@{ Script = "0x7fffffff"; Expected = $([int32]::MaxValue) }
			@{ Script = "0x100000000"; Expected = [int64]0x100000000 }
			#Tests for exponential notation.
			@{ Script = "0e0"; Expected = "0" }
			@{ Script = "0e1"; Expected = "0" }
			@{ Script = "1e2"; Expected = "100" }
			@{ Script = $([int32]::MaxValue); Expected = $([int32]::MaxValue) }
			@{ Script = "0e2"; Expected = "0" }
			@{ Script = "-2e2"; Expected = "-200" }
			@{ Script = "-0e2"; Expected = "0" }
			@{ Script = "3e0"; Expected = "3" }
			#Tests for floating point notation.
			@{ Script = ".01"; Expected = "0.01" }
			@{ Script = "0.0"; Expected = "0" }
			@{ Script = "-0.1"; Expected = "-0.1" }
			@{ Script = "9.12"; Expected = "9.12" }
			@{ Script = $([single]::MinValue); Expected = $([float]::MinValue).ToString() }
			@{ Script = $([float]::MaxValue); Expected = $([float]::MaxValue).ToString() }
			#Tests for the K suffix for numbers.
			@{ Script = "0kb"; Expected = "0" }
			@{ Script = "1kb"; Expected = "1024" }
			@{ Script = "-2KB"; Expected = "-2048" }
        )
        It "<Script> should return <Expected>" -TestCases $testData {
            param ( $Script, $Expected )
            ExecuteCommand $Script | Should -Be $Expected
        }
    }

	It "This is a simple test of the concatenation of two arrays. (line 2460)"{
        $result = ExecuteCommand '1,2,3 + 4,5,6'
		$result -join "" | Should -Be (1, 2, 3, 4, 5, 6 -join "")
    }

	It "Test that an incomplete parse exception is thrown if the array is unfinished. (line 2473)"{
		{ ExecuteCommand '1,2,' } | Should -Throw -ErrorId "IncompleteParseException"
    }

	It "Test that the unary comma is not valid in cmdlet parameters. (line 2482)"{
		{ ExecuteCommand 'write-output 2,,1' } | Should -Throw -ErrorId "ParseException"
    }

	It 'Test that "$var:" will expand to nothing inside a string. (line 2551)'{
		{ ExecuteCommand '"$var:"' } | Should -Throw -ErrorId "ParseException"
    }

	It "Tests the assignment to a read-only property (line 2593)"{
		$result = ExecuteCommand '$A=$(testcmd-parserBVT -returntype array); $A.rank =5;$A.rank'
        $result | Should -Be "1"
    }

	It 'Tests accessing using null as index. (line 2648)'{
        $PowerShell.Streams.Error.Clear()
		ExecuteCommand '$A=$(testcmd-parserBVT -returntype array); $A[$NONEXISTING_VARIABLE];'
        $PowerShell.HadErrors | Should -BeTrue
        $PowerShell.Streams.Error.FullyQualifiedErrorId | Should -Be "NullArrayIndex"
    }

	It 'Tests the parser response to ArrayName[. (line 2678)'{
        { ExecuteCommand '$A=$(testcmd-parserBVT -returntype array); $A[ ;' } | Should -Throw -ErrorId "ParseException"
    }

	It 'Tests the parser response to ArrayName[]. (line 2687)'{
		{ ExecuteCommand '$A=$(testcmd-parserBVT -returntype array); $A[] ;' } | Should -Throw -ErrorId "ParseException"
    }

	#Issue#1430
	It "Tests function scopes in a script. (line 2800)" -Pending{
		" function global:func { 'global' }; " +
                " function func { 'default' }; " +
                " local:func; " +
                " script:func; " +
                " global:func; ">$testfile
        $result = ExecuteCommand "function func { 'notcalled' };. $testfile"
		$result -join "" | Should -Be ("default", "default", "global" -join "")
		$result = ExecuteCommand "func"
		$result | Should -Be "global"
    }

	It 'Test piping arguments to a script block. The objects should be accessible from "$input". (line 2870)'{
		ExecuteCommand '$script = { $input; };$results = @(0,0),-1 | &$script'
		$result = ExecuteCommand '$results[0][0]'
        $result | Should -Be "0"
		$result = ExecuteCommand '$results[0][1]'
        $result | Should -Be "0"
		$result = ExecuteCommand '$results[1]'
        $result | Should -Be "-1"
    }

	It 'Test piping null into a scriptblock. The script block should not be passed anything. (line 2903)'{
		$result = ExecuteCommand '$() | &{ $count = 0; foreach ($i in $input) { $count++ }; $count }'
        $result | Should -Be "1"
		$result = ExecuteCommand '$() | &{ $input }'
        $result | Should -BeNullOrEmpty
    }

	It 'Test that types in System.dll are found automatically. (line 2951)'{
		$result = ExecuteCommand '[   System.IO.FileInfo]'
        $result | Should -Be "System.IO.FileInfo"
    }

	Context "Mathematical Operations Tests (starting at line 2975 to line 3036)" {
        $testData = @(
            @{ Script = '$a=6; $a -= 2;$a'; Expected = 4 }
			@{ Script = "20 %
			6"; Expected = "2" }
			@{ Script = "(20 %
			6 *
			4  +
			2 ) /
			2 -
			3"; Expected = "2" }
        )
        It "<Script> should return <Expected>" -TestCases $testData {
            param ( $Script, $Expected )
            ExecuteCommand $Script | Should -Be $Expected
        }
    }

	It 'This test will call a cmdlet that returns an array and assigns it to a variable.  Then it will concatenate this array with itself and check that what results is an array of double the size of the original. (line 3148)'{
		$result = ExecuteCommand '$list=$(testcmd-parserBVT -ReturnType "array"); $list = $list + $list;$list.length'
        $result | Should -Be 6
    }

    It "A here string must have one line (line 3266)" {
        { ExecuteCommand "@`"`"@" } | Should -Throw -ErrorId "ParseException"
    }

    It "A here string should not throw on '`$herestr=@`"``n'`"'``n`"@'" {
        # Issue #2780
        { ExecuteCommand "`$herestr=@`"`n'`"'`n`"@" } | Should -Not -Throw
    }

    It "Throw better error when statement should be put in named blocks - <name>" -TestCases @(
        @{ script = "Function foo { [CmdletBinding()] param() DynamicParam {} Hi"; name = "function" }
        @{ script = "{ begin {} Hi"; name = "script-block" }
        @{ script = "begin {} Hi"; name = "script-file" }
    ) {
        param($script)

        $err = { ExecuteCommand $script } | Should -Throw -ErrorId "ParseException" -PassThru
        $err.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly "MissingNamedBlocks"
    }

    It "IncompleteParseException should be thrown when only ending curly is missing" {
        $err = { ExecuteCommand "Function foo { [CmdletBinding()] param() DynamicParam {} " } | Should -Throw -ErrorId "IncompleteParseException" -PassThru
        $err.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly "MissingEndCurlyBrace"
    }

    Context "#requires nested scan tokenizer tests" {
        BeforeAll {
            $settings = [System.Management.Automation.PSInvocationSettings]::new()
            $settings.AddToHistory = $true

            $ps = [powershell]::Create()
        }

        AfterAll {
            $ps.Dispose()
        }

        AfterEach {
            $ps.Commands.Clear()
        }

        $testCases = @(
            @{ script = "#requires"; firstToken = $null; lastToken = $null },
            @{ script = "#requires -Version 5.0`n10"; firstToken = "10"; lastToken = "10" },
            @{ script = "Write-Host 'Hello'`n#requires -Version 5.0`n7"; firstToken = "Write-Host"; lastToken = "7" },
            @{ script = "Write-Host 'Hello'`n#requires -Version 5.0"; firstToken = "Write-Host"; lastToken = "Hello"}
        )

        It "Correctly resets the first and last tokens in the tokenizer after nested scan in script" -TestCases $testCases {
            param($script, $firstToken, $lastToken)

            $ps.AddScript($script)
            $ps.AddScript("(`$^,`$`$)")
            $tokens = $ps.Invoke(@(), $settings)

            $tokens[0] | Should -BeExactly $firstToken
            $tokens[1] | Should -BeExactly $lastToken
        }
    }
}
