$PesterSkip = if ( $IsWindows ) { @{ Skip = $true } } else { @{} }

Describe 'Native UNIX globbing tests' -tags "CI" {

    BeforeAll {
        if (-not $IsWindows )
        {
            "" > "$TESTDRIVE/abc.txt"
            "" > "$TESTDRIVE/bbb.txt"
            "" > "$TESTDRIVE/cbb.txt"
        }
    }

    # Test * expansion
    It 'The globbing pattern *.txt should match 3 files' @PesterSkip {
        (/bin/ls $TESTDRIVE/*.txt).Length | Should Be 3
    }
    It 'The globbing pattern *b.txt should match 2 files whose basenames end in "b"' @PesterSkip {
        (/bin/ls $TESTDRIVE/*b.txt).Length | Should Be 2
    }
    # Test character classes
    It 'The globbing pattern should match 2 files whose names start with either "a" or "b"' @PesterSkip {
        (/bin/ls $TESTDRIVE/[ab]*.txt).Length | Should Be 2
    }
    It 'Globbing abc.* should return one file name "abc.txt"' @PesterSkip {
        /bin/ls $TESTDRIVE/abc.* | Should Match "abc.txt"
    }
    # Test that ? matches any single character
    It 'Globbing [cde]b?.* should return one file name "cbb.txt"' @PesterSkip {
        /bin/ls $TESTDRIVE/[cde]b?.* | Should Match "cbb.txt"
    }
    It 'Should return the original pattern if there are no matches' @PesterSkip {
        /bin/echo $TESTDRIVE/*.nosuchfile | Should Match "\*\.nosuchfile$"
    }
    # Test the behavior in non-filesystem drives
    It 'Should not expand patterns on non-filesystem drives' @PesterSkip {
        /bin/echo env:ps* | Should BeExactly "env:ps*"
    }
    # Test the behavior for files with spaces in the names
    It 'Globbing filenames with spaces should match 2 files' @PesterSkip {
        "" > "$TESTDRIVE/foo bar.txt"
        "" > "$TESTDRIVE/foo baz.txt"
        (/bin/ls $TESTDRIVE/foo*.txt).Length | Should Be 2
    }
    # Test ~ expansion
    It 'Tilde should be replaced by the filesystem provider home directory' @PesterSkip {
        /bin/echo ~ | Should BeExactly ($executioncontext.SessionState.Provider.Get("FileSystem").Home)
    }
    # Test ~ expansion with a path fragment (e.g. ~/foo)
    It '~/foo should be replaced by the <filesystem provider home directory>/foo' @PesterSkip {
        /bin/echo ~/foo | Should BeExactly "$($executioncontext.SessionState.Provider.Get("FileSystem").Home)/foo"
    }
}
