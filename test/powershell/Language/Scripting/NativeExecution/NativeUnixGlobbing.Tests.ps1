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
        @{arg = '"*"'},
        @{arg = "'*'"}
    )
    It 'Should not expand quoted strings: <arg>' -TestCases $quoteTests {
        param($arg)
        Invoke-Expression "/bin/echo $arg" | Should -BeExactly '*'
    }
	# Splat tests are skipped because they should work, but don't.
	# Supporting this scenario would require adding a NoteProperty
	# to each quoted string argument - maybe not worth it, and maybe
	# an argument for another way to suppress globbing.
    It 'Should not expand quoted strings via splat array: <arg>' -TestCases $quoteTests -Skip {
        param($arg)

        function Invoke-Echo
        {
            /bin/echo @args
        }
        Invoke-Expression "Invoke-Echo $arg" | Should -BeExactly '*'
    }
    It 'Should not expand quoted strings via splat hash: <arg>' -TestCases $quoteTests -Skip {
        param($arg)

        function Invoke-Echo($quotedArg)
        {
            /bin/echo @PSBoundParameters
        }
        Invoke-Expression "Invoke-Echo -quotedArg:$arg" | Should -BeExactly "-quotedArg:*"

        # When specifying a space after the parameter, the space is removed when splatting.
        # This behavior is debatable, but it's worth adding this test anyway to detect
        # a change in behavior.
        Invoke-Expression "Invoke-Echo -quotedArg: $arg" | Should -BeExactly "-quotedArg:*"
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
    It 'Tilde should be replaced by the filesystem provider home directory' {
        /bin/echo ~ | Should -BeExactly ($ExecutionContext.SessionState.Provider.Get("FileSystem").Home)
    }
    # Test ~ expansion with a path fragment (e.g. ~/foo)
    It '~/foo should be replaced by the <filesystem provider home directory>/foo' {
        /bin/echo ~/foo | Should -BeExactly "$($ExecutionContext.SessionState.Provider.Get("FileSystem").Home)/foo"
    }
	It '~ should not be replaced when quoted' {
		/bin/echo '~' | Should -BeExactly '~'
		/bin/echo "~" | Should -BeExactly '~'
		/bin/echo '~/foo' | Should -BeExactly '~/foo'
		/bin/echo "~/foo" | Should -BeExactly '~/foo'
	}
}
