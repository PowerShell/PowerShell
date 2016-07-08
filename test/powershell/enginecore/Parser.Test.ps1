Describe "ParserTests (admin\monad\tests\monad\src\engine\core\ParserTests.cs)" {
    BeforeAll {
		$functionDefinitionFile = Join-Path -Path $TestDrive -ChildPath "functionDefinition.ps1"
		$functionDefinition = @"
		function testcmd-parserbvt
		{
		[CmdletBinding()]
			param (
			[Parameter(Position = 0)]
			[string] `$Property1 = "unset",
			
			[Parameter(Position = 1)]
			[string] `$Property2 = "unset",
			
			[Parameter(Position = 2)]
			[string] `$Property3 = "unset",

			[Parameter()]
			[ValidateSet("default","array","object","nestedobject","struct","mshobject","nulltostring")]
			[string]`$ReturnType = "default"
			)

			BEGIN {}
			
			PROCESS {
				if ( ! `$ReturnType ) {
					`$ReturnType = "default"
				}
				
				switch ( `$ReturnType )
				{
					"default" { 
						`$result = "`$Property1;`$Property2;`$Property3"
						break
					}
					"array" { 
						`$result = 1,2,3
						break
					}
					"object" { 
						`$result = new-object psobject
						break
					}
					"nestedobject" { 
						`$result = [pscustomobject]@{Name="John";Person=[pscustomobject]@{Name="John";Age=30}}
						break
					}
					"struct" { 
						`$result = [pscustomobject]@{Name="John";Age=30}
						break
					}
					"mshobject" { 
						`$result = new-object psobject
						break
					}
					"nulltostring" { 
						`$result = $null
						break
					}
					default { 
						throw ([invalidoperationexception]::new("ReturnType parameter wasn't of any Expected value!"))
						break 
					}
				}
				return `$result
			}
	
			END {}
		}
"@
		$functionDefinition>$functionDefinitionFile
		
        $PowerShell = [powershell]::Create()
        function ExecuteCommand {
            param ([string]$command)
            try {
				$PowerShell.AddScript(". $functionDefinitionFile").Invoke()
                $PowerShell.AddScript($command).Invoke()
            }
            finally {
                $PowerShell.Commands.Clear()
            }
        }
    }
	BeforeEach {
		$testfile = Join-Path -Path $TestDrive -ChildPath "testfile.ps1"
		$winshellfile = Join-Path -Path $TestDrive -ChildPath "testfile.cmd"
		$testfolder1 = Join-Path -Path $TestDrive -ChildPath "dir1"
		$testfolder2 = Join-Path -Path $testfolder1 -ChildPath "dir2"
		if(-not(Test-Path $testfolder1))
		{
			New-Item $testfolder1 -Type Directory
		}
		if(-not(Test-Path $testfolder2))
		{
			New-Item $testfolder2 -Type Directory
		}
		$testdirfile1 = Join-Path -Path $testfolder2 -ChildPath "testdirfile1.txt"
		$testdirfile2 = Join-Path -Path $testfolder2 -ChildPath "testdirfile2.txt"
		"">$testdirfile1
		"">$testdirfile2
	}
    AfterEach {
        $PowerShell.Commands.Clear()
		if(Test-Path $testfile)
		{
			Remove-Item $testfile
		}
		if(Test-Path $winshellfile)
		{
			Remove-Item $winshellfile
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
		try {
            ExecuteCommand '"This is a test' 
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | Should be "IncompleteParseException"
        }
	}
	
	It "Throws an error if an open parenthesis is not closed (line 176)" {
        try {
            ExecuteCommand "("
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | should be "IncompleteParseException"
        }
    }
	
    It "Throws an exception if the the first statement starts with an empty pipe element (line 188)" {
        try {
            ExecuteCommand "| get-location"
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | should be "ParseException"
        }
    }
	
	It "Throws an CommandNotFoundException exception if using a label in front of an if statement is not allowed. (line 225)"{
        ExecuteCommand ":foo if ($x -eq 3) { 1 }"
		$PowerShell.HadErrors | should be $true
        $PowerShell.Streams.Error.FullyQualifiedErrorId | should be "CommandNotFoundException"
    }
	
	It "Pipe an expression into a value expression. (line 237)" {
        try {
            ExecuteCommand "testcmd-parserbvt | 3"
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | should be "ParseException"
        }
		
		try {
            ExecuteCommand "testcmd-parserbvt | $(1 + 1)"
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | should be "ParseException"
        }
		
		try {
            ExecuteCommand "testcmd-parserbvt | 'abc'"
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | should be "ParseException"
        }
    }

    It "Throws when you pipe into a value expression (line 238)" {
        foreach($command in "1;2;3|3",'1;2;3|$(1+1)',"1;2;3|'abc'") {
            try {
                ExecuteCommand $command
                throw "Execution OK"
            }
            catch {
                $_.FullyQualifiedErrorId | Should be "ParseException"
            }
        }
    }
	
	It "Throws an incomplete parse exeception when a comma follows an expression (line 247)" {
        try {
            ExecuteCommand "(1+ 1)," 
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | Should be "IncompleteParseException"
        }
    }
	
		
	It "Test that invoke has a higher precedence for a script than for an executable. (line 279)" {
		"1">$testfile
        $result = ExecuteCommand ". $testfile"
		$result | should be 1
    }
	
	It "This test will check that a path is correctly interpreted when using '..' and '.'  (line 364)" {
        $result = ExecuteCommand "set-location $TestDrive; get-childitem dir1\.\.\.\..\dir1\.\dir2\..\..\dir1\.\dir2"
		$result.Count | should be 2
		$result[0].Name | should be "testdirfile1.txt"
		$result[1].Name | should be "testdirfile2.txt"
    }
	
	It "This test will check that the parser can handle a mix of forward slashes and back slashes in the path (line 417)" {
        $result = ExecuteCommand "get-childitem $TestDrive/dir1/./.\.\../dir1/.\dir2\../..\dir1\.\dir2"
		$result.Count | should be 2
		$result[0].Name | should be "testdirfile1.txt"
		$result[1].Name | should be "testdirfile2.txt"
    }
	
	It "This test checks that the asterisk globs as expected. (line 545)" {
        $result = ExecuteCommand "get-childitem $TestDrive/dir1\dir2\*.txt"
		$result.Count | should be 2
		$result[0].Name | should be "testdirfile1.txt"
		$result[1].Name | should be "testdirfile2.txt"
    }
	
	It "This test checks that we can use a range for globbing: [1-2] (line 557)" {
        $result = ExecuteCommand "get-childitem $TestDrive/dir1\dir2\testdirfile[1-2].txt"
		$result.Count | should be 2
		$result[0].Name | should be "testdirfile1.txt"
		$result[1].Name | should be "testdirfile2.txt"
    }
	
	It "This test will check that escaping the $ sigil inside single quotes simply returns the $ character. (line 583)" {
        $result = ExecuteCommand "'`$'"
		$result | should be "`$"
    }
	
	It "Test that escaping a space just returns that space. (line 593)" {
        $result = ExecuteCommand '"foo` bar"'
		$result | should be "foo bar"
    }
	
	It "Test that escaping any character with no special meaning just returns that char. (line 602)" {
        $result = ExecuteCommand '"fo`obar"'
		$result | should be "foobar"
    }
	
	It "Test that we support all of the C# escape sequences. We use the ` instead of \. (line 613)" -Pending {
		$sequences = @('"`\\"', '"`0"', '"`a"', '"`b"', '"`f"', '"`n"', '"`r"', '"`t"', '"`v"')
		$expectedSeq = @('\\', "\0", '\a', '\b', '\f', '\n', '\r', '\t', '\v')
		$i = 0
		for(;$i -lt $sequences.Count;$i++)
		{
			$result = ExecuteCommand $sequences[$i]
			$result | should be $expectedSeq[$i]
		}
    }
	
	It "This test checks that array substitution occurs inside double quotes. (line 646)" {
        $result = ExecuteCommand '$MyArray = "a","b";"Hello $MyArray"'
		$result | should be "Hello a b"
    }
	
	It "This tests declaring an array in nested variable tables. (line 761)" {
        $result = ExecuteCommand "`$Variable:vtbl1:vtbl2:b=@(5,6);`$Variable:vtbl1:vtbl2:b"
		$result.Count | should be 2
		$result[0] | should be 5
		$result[1] | should be 6
    }
	
	It "Test a simple multiple assignment. (line 773)" {
        $result = ExecuteCommand '$one,$two = 1,2,3; "One = $one"; "Two = $two"'
		$result.Count | should be 2
		$result[0] | should be "One = 1"
		$result[1] | should be "Two = 2 3"
    }
	
	It "Tests script, global and local scopes from a function inside a script. (line 824)" {
		"`$var = 'script';function func { `$var; `$var = 'local'; `$local:var; `$script:var; `$global:var };func;`$var;">$testfile
		ExecuteCommand "`$var = 'global'"
        $result = ExecuteCommand "$testfile"
		$result.Count | should be 5
		$result[0] | should be "script"
		$result[1] | should be "local"
		$result[2] | should be "script"
		$result[3] | should be "global"
		$result[4] | should be "script"
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
			$result | should be $results
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
			$result | should be $results
		}
    }
	
	It "Try continue inside of different loop statements. (line 1039)" {
		$commands = " `$a = 0; while (`$a -lt 2) { `$a; `$a += 1; continue; 2; } ", 
                " for (`$a = 0;`$a -lt 2; `$a += 1) { 9; continue; 3; } ", 
                " foreach(`$a in 0,1) { `$a; continue; 2; } "
		$result = ExecuteCommand $commands[0]
		$result | should be "0", "1"
		$result = ExecuteCommand $commands[1]
		$result | should be "9", "9"
		$result = ExecuteCommand $commands[2]
		$result | should be "0", "1"
    }
	
	It "Use a label to continue an inner loop. (line 1059)" {
		$commands = " `$x = 0; while (`$x -lt 1) { `$x += 1; `$x; `$a = 0; :foo while(`$a -lt 2) { `$a += 1; `$a; continue foo; 3; }; 4; continue; 5;  } ", 
                " for (`$x = 0;`$x -lt 1;`$x += 1) { 1; :foo for(`$a = 0; `$a -lt 2; `$a += 1) { `$a; continue foo; 3; }; 4; continue; 5; } ", 
                " foreach(`$a in 1) { 1; :foo foreach( `$b in 1,2 ) { `$b; continue foo; 3; }; 4; continue; 5; } "
		$result = ExecuteCommand $commands[0]
		$result | should be "1", "1", "2", "4"
		$result = ExecuteCommand $commands[1]
		$result | should be "1", "0", "1", "4"
		$result = ExecuteCommand $commands[2]
		$result | should be "1", "1", "2", "4"
    }
	
	It "Use continue with a label on a nested loop. (line 1059)" {
		$commands = " `$x = 0; :foo while (`$x -lt 2) { `$x; `$x += 1; :bar while(1) { 2; continue foo; 3; }; 4; continue; 5;  } ", 
                " :foo for (`$x = 0;`$x -lt 2;`$x += 1) { 1; :bar for(;;) { 2; continue foo; 3; }; 4; continue; 5; } ", 
                " :foo foreach(`$a in 1,2) { 1; :bar foreach( `$b in 1,2,3 ) { 2; continue foo; 3; }; 4; continue; 5; } "
		$result = ExecuteCommand $commands[0]
		$result | should be "0", "2", "1", "2"
		$result = ExecuteCommand $commands[1]
		$result | should be "1", "2", "1", "2"
		$result = ExecuteCommand $commands[2]
		$result | should be "1", "2", "1", "2"
    }
	
	It "This test will check that it is a syntax error to use if without a code block. (line 1141)" {
        try {
            ExecuteCommand 'if ("true")' 
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | Should be "IncompleteParseException"
        }
    }
	
	It "This test will check that it is a syntax error if the if condition is not complete. (line 1150)" {
        try {
            ExecuteCommand 'if (' 
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | Should be "IncompleteParseException"
        }
    }
	
	It "This test will check that it is a syntax error to have an if condition without parentheses. (line 1159)" {
        try {
            ExecuteCommand 'if "true" { 1} else {2}' 
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | Should be "ParseException"
        }
    }
	
	It "This test will check that the parser throws a syntax error when the if condition is missing the closing parentheses. (line 1168)" {
        try {
            ExecuteCommand 'if ("true"  { 1};' 
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | Should be "ParseException"
        }
    }
	
	It "This test will check that it is a syntax error to have an else keyword without the corresponding code block. (line 1177)" {
        try {
            ExecuteCommand 'if ("true") {1} else' 
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | Should be "IncompleteParseException"
        }
    }
	
	It "This test will check that the parser throws a syntax error when a foreach loop is not complete. (line 1238)" {
        try {
            ExecuteCommand '$count=0;$files = $(get-childitem / -filter *.txt );foreach ($i ;$count' 
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | Should be "ParseException"
        }
    }
	
	It "This test will check that the parser throws a syntax error if the foreach loop is not complete. (line 1248)" {
        try {
            ExecuteCommand '$count=0;$files = $(get-childitem / -filter *.txt );foreach ($i in ;$count' 
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | Should be "ParseException"
        }
    }
	
	It "This will test that the parser throws a syntax error if the foreach loop is missing a closing parentheses. (line 1258)" {
        try {
            ExecuteCommand '$count=0;$files = $(get-childitem / -filter *.txt );foreach ($i in $files ;$count' 
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | Should be "ParseException"
        }
    }
	
	It "Test that if an exception is thrown from the try block it will be caught in the apprpropriate catch block and that the finally block will run regardless of whether an exception is thrown. (line 1317)" {
        $result = ExecuteCommand 'try { try { throw (new-object System.ArgumentException) } catch [System.DivideByZeroException] { } finally { "Finally" } } catch { $_.Exception.GetType().FullName }'
		$result | should be "Finally", "System.ArgumentException"
    }
	
	It "Test that null can be passed to a method that expects a reference type. (line 1439)" {
        $result = ExecuteCommand '$test = "String";$test.CompareTo($())'
		$result | should be 1
    }
	
	It "Tests that command expansion operators can be used as a parameter to an object method. (line 1507)" {
        $result = ExecuteCommand '$test = "String";$test.SubString($("hello" | foreach-object { $_.length - 2 } ))'
		$result | should be "ing"
    }
	
	It "Test that & can be used as a parameter as long as it is quoted. (line 1606)" {
        $result = ExecuteCommand 'testcmd-parserbvt `&get-childitem'
		$result | should be "&get-childitem;unset;unset"
		$result = ExecuteCommand 'testcmd-parserbvt `&*'
		$result | should be "&*;unset;unset"
    }
	
	It "Run a command with parameters. (line 1621)" {
        $result = ExecuteCommand 'testcmd-parserBVT -Property1 set'
		$result | should be "set;unset;unset"
    }
	
	It "Test that typing a number at the command line will return that number. (line 1630)" {
        $result = ExecuteCommand '3'
		$result | should be "3"
    }
	
	It "This test will check that an msh script can be run without invoking. (line 1641)" {
        "1">$testfile
        $result = ExecuteCommand ". $testfile"
		$result | should be 1
    }
	
	It "Test that an alias is resolved before a function. (line 1657)" {
        $result = ExecuteCommand 'set-alias parserInvokeTest testcmd-parserBVT;function parserInvokeTest { 3 };parserInvokeTest'
		$result | should be "unset;unset;unset"
    }
	
	It "Test that functions are resolved before cmdlets. (line 1678)"{
        $result = ExecuteCommand 'function testcmd-parserBVT { 3 };testcmd-parserBVT'
		$result | should be "3"
    }
	
	It "Check that a command that uses shell execute can be run from the command line and that no exception is thrown. (line 1702)" -Skip:($IsLinux -Or $IsOSX){
        "@echo Hello, I'm a Cmd script!">$winshellfile
        { ExecuteCommand "$winshellfile" } | Should Not Throw
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
            ExecuteCommand $Script | Should be $Expected
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
            ExecuteCommand $Script | Should be $Expected
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
            ExecuteCommand $Script | Should be $Expected
        }
    }
	
	It "Newlines are allowed after operators (line 3033)" {
        $result = ExecuteCommand "(20 %
        6 *
        4 +
        2) /
        2 -
        3"
        $result | should be 2
    }
	
    It "A here string must have one line (line 3266)" {
        try {
            ExecuteCommand "@`"`"@"
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | should be "ParseException"
        }
    }
} 