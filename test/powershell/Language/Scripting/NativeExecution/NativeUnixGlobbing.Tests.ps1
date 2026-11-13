# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Native UNIX globbing tests' -tags "CI" {

    BeforeAll {
        if (-Not $IsWindows )
        {
            "" > "$TESTDRIVE/abc.txt"
            "" > "$TESTDRIVE/bbb.txt"
            "" > "$TESTDRIVE/cbb.txt"
        }

        $defaultParamValues = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues["it:skip"] = $IsWindows
        $HomeDir = $ExecutionContext.SessionState.Provider.Get("FileSystem").Home
        $Tilde = "~"
    }

    AfterAll {
        $global:PSDefaultParameterValues = $defaultParamValues
    }

    # Test * expansion
    It 'The globbing pattern *.txt should match 3 files' {
        (/bin/ls $TESTDRIVE/*.txt).Length | Should -Be 3
    }
    It 'The globbing pattern *b.txt should match 2 files whose basenames end in "b"' {
        (/bin/ls $TESTDRIVE/*b.txt).Length | Should -Be 2
    }
    # Test character classes
    It 'The globbing pattern should match 2 files whose names start with either "a" or "b"' {
        (/bin/ls $TESTDRIVE/[ab]*.txt).Length | Should -Be 2
    }
    It 'Globbing abc.* should return one file name "abc.txt"' {
        /bin/ls $TESTDRIVE/abc.* | Should -Match "abc.txt"
    }
    # Test that ? matches any single character
    It 'Globbing [cde]b?.* should return one file name "cbb.txt"' {
        /bin/ls $TESTDRIVE/[cde]b?.* | Should -Match "cbb.txt"
    }
	# Test globbing with expressions
	It 'Globbing should work with unquoted expressions' {
	    $v = "$TESTDRIVE/abc*"
		/bin/ls $v | Should -Match "abc.txt"

		$h = [pscustomobject]@{P=$v}
		/bin/ls $h.P | Should -Match "abc.txt"

		$a = $v,$v
		/bin/ls $a[1] | Should -Match "abc.txt"
    }
    # Test globbing with absolute paths - it shouldn't turn absolute paths into relative paths (#7089)
    It 'Should not normalize absolute paths' {
        $Matches = /bin/echo /etc/*
        # Matched path should start with '/etc/' not '../..'
        $Matches.substring(0,5) | Should -Be '/etc/'
    }
	It 'Globbing should not happen with quoted expressions' {
	    $v = "$TESTDRIVE/abc*"
		/bin/echo "$v" | Should -BeExactly $v
		/bin/echo '$v' | Should -BeExactly '$v'
	}
    It 'Should return the original pattern (<arg>) if there are no matches' -TestCases @(
        @{arg = '/nOSuCH*file'},               # No matching file
        @{arg = '/bin/nOSuCHdir/*'},           # Directory doesn't exist
        @{arg = '-NosUch*fIle'},               # Parameter syntax but could be file
        @{arg = '-nOsuCh*drive:nosUch*fIle'},  # Parameter w/ arg syntax, could specify drive
        @{arg = '-nOs[u]ChdrIve:nosUch*fIle'}, # Parameter w/ arg syntax, could specify drive
        @{arg = '-nOsuChdRive:nosUch*fIle'},   # Parameter w/ arg syntax, could specify drive
        @{arg = '-nOsuChdRive: nosUch*fIle'},  # Parameter w/ arg syntax, could specify drive
        @{arg = '/no[suchFilE'},               # Invalid wildcard (no closing ']')
        @{arg = '[]'}                          # Invalid wildcard
    ) {
        param($arg)
        /bin/echo $arg | Should -BeExactly $arg
    }
    $quoteTests = @(
        @{arg = '"*"';              expectedArg = "*"}
        @{arg = "'*'";              expectedArg = "*"}
        @{arg = '"$TESTDRIVE/*"';   expectedArg = "$TESTDRIVE/*"}
    )
    It 'Should not expand quoted strings: <arg>' -TestCases $quoteTests {
        param($arg, $expectedArg)
        Invoke-Expression "testexe -echoargs $arg" | Should -BeExactly "Arg 0 is <$expectedArg>"
    }
    It 'Should not expand quoted strings via splat array: <arg>' -TestCases $quoteTests {
        param($arg, $expectedArg)

        function Invoke-TestExe
        {
            testexe @args
        }
        Invoke-Expression "Invoke-TestExe -echoargs $arg" | Should -BeExactly "Arg 0 is <$expectedArg>"
    }
    It 'Should not expand quoted strings via splat hash: <arg>' -TestCases $quoteTests {
        param($arg, $expectedArg)

        function Invoke-Echo($quotedArg)
        {
            testexe -echoargs @PSBoundParameters
        }
        Invoke-Expression "Invoke-Echo -quotedArg:$arg" | Should -BeExactly "Arg 0 is <-quotedArg:$expectedArg>"

        # When specifing a space after the parameter, the space is removed when splatting.
        # This behavior is debatable, but it's worth adding this test anyway to detect
        # a change in behavior.
        Invoke-Expression "Invoke-Echo -quotedArg: $arg" | Should -BeExactly "Arg 0 is <-quotedArg:$expectedArg>"
    }
    It 'Should expand strings via splat array' {
        function Invoke-TestExe
        {
            $args.Length | Should -Be 2
            testexe @args
        }
        Invoke-TestExe -echoargs $TESTDRIVE/*.txt | Should -BeExactly @(
            "Arg 0 is <$TESTDRIVE/abc.txt>"
            "Arg 1 is <$TESTDRIVE/bbb.txt>"
            "Arg 2 is <$TESTDRIVE/cbb.txt>"
        )
    }
    It 'Should keep its literal meaning when splatted' {
        function Invoke-TestExe
        {
            testexe @args
        }
        Invoke-TestExe -echoargs $TESTDRIVE/*.txt "$TESTDRIVE/*.txt" '$TESTDRIVE/*.txt' | Should -BeExactly @(
            "Arg 0 is <$TESTDRIVE/abc.txt>"
            "Arg 1 is <$TESTDRIVE/bbb.txt>"
            "Arg 2 is <$TESTDRIVE/cbb.txt>"
            "Arg 3 is <$TESTDRIVE/*.txt>"
            'Arg 4 is <$TESTDRIVE/*.txt>'
        )
    }
    It 'Should not expand quoted strings via splat hash' {
        function Invoke-EchoArgs($quotedArg)
        {
            $PSBoundParameters.Length | Should -Be 1
            testexe -echoargs @PSBoundParameters
        }
        Invoke-EchoArgs -quotedArg:$TESTDRIVE/*.txt | Should -BeExactly @(
            "Arg 0 is <-quotedArg:$TESTDRIVE/abc.txt>"
            "Arg 1 is <-quotedArg:$TESTDRIVE/bbb.txt>"
            "Arg 2 is <-quotedArg:$TESTDRIVE/cbb.txt>"
        )

        # When specifing a space after the parameter, the space is removed when splatting.
        # This behavior is debatable, but it's worth adding this test anyway to detect
        # a change in behavior.
        Invoke-EchoArgs -quotedArg: $TESTDRIVE/*.txt | Should -BeExactly @(
            "Arg 0 is <-quotedArg:$TESTDRIVE/abc.txt>"
            "Arg 1 is <-quotedArg:$TESTDRIVE/bbb.txt>"
            "Arg 2 is <-quotedArg:$TESTDRIVE/cbb.txt>"
        )
    }
    # Test the behavior in non-filesystem drives
    It 'Should not expand patterns on non-filesystem drives' {
        /bin/echo env:ps* | Should -BeExactly "env:ps*"
    }
    # Test the behavior for files with spaces in the names
    It 'Globbing filenames with spaces should match 2 files' {
        "" > "$TESTDRIVE/foo bar.txt"
        "" > "$TESTDRIVE/foo baz.txt"
        (/bin/ls $TESTDRIVE/foo*.txt).Length | Should -Be 2
    }
    # Test ~ expansion
    It '~ should be replaced by the filesystem provider home directory <arg>' -testCases @(
        @{arg = '~';            Expected = $HomeDir }
        @{arg = '$Tilde';       Expected = $HomeDir }
        @{arg = '~/foo';        Expected = "$HomeDir/foo" }
        @{arg = '$Tilde/foo';   Expected = "$HomeDir/foo" }
    ) {
        param($arg, $Expected)
        Invoke-Expression "testexe -echoargs $arg" | Should -BeExactly "Arg 0 is <$Expected>"
    }
    It '~ should not be replaced when quoted <arg>' -testCases @(
        @{arg = "'~'";          Expected = "~" }
        @{arg = "'~/foo'";      Expected = "~/foo" }
        @{arg = '"~"';          Expected = "~" }
        @{arg = '"~/foo"';      Expected = "~/foo" }
        @{arg = '"$Tilde"';     Expected = "~" }
        @{arg = '"$Tilde/foo"'; Expected = "~/foo" }
    ) {
        param($arg, $Expected)
        Invoke-Expression "testexe -echoargs $arg" | Should -BeExactly "Arg 0 is <$Expected>"
    }
    It '~ should keep its literal meaning when splatted <splattingArgs>'-testCases @(
        @{
            splattingArgs = @'
~ ~/foo '~' "~" '~/foo' "~/foo"
'@;
            Expected = @("$HomeDir", "$HomeDir/foo", "~", "~", "~/foo", "~/foo")
        }
        @{
            splattingArgs = @'
$Tilde $Tilde/foo "$Tilde" "$Tilde/foo"
'@;
            Expected = @("$HomeDir", "$HomeDir/foo", "~", "~/foo")
        }
    ) {
        param($splattingArgs, $Expected)
        function Invoke-TestExe {
            testexe @args
        }

        Invoke-Expression "Invoke-TestExe -echoargs $splattingArgs" | Should -BeExactly @($Expected | ForEach-Object { $i = 0 } { "Arg {0} is <$_>" -f $i++ } )
    }
}
