# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Native Command Arguments" -tags "CI" {
    BeforeAll {
        [bool]$skipQuoteTests = ! (Get-ExperimentalFeature PSEscapeDoubleQuotedStringForNativeExecutables).Enabled
    }
    # When passing arguments to native commands, quoted segments that contain
    # spaces need to be quoted with '"' characters when they are passed to the
    # native command (or to bash or sh on Linux).
    #
    # This test checks that the proper quoting is occuring by passing arguments
    # to the testexe native command and looking at how it got the arguments.
    It "Should handle quoted spaces correctly" {
        $a = 'a"b c"d'
        $lines = testexe -echoargs $a 'a"b c"d' a"b c"d
        $lines.Count | Should -Be 3
        if ( $skipQuoteTests ) {
            $lines[0] | Should -BeExactly 'Arg 0 is <ab cd>'
            $lines[1] | Should -BeExactly 'Arg 1 is <ab cd>'
            $lines[2] | Should -BeExactly 'Arg 2 is <ab cd>'
            $lines[3] | Should -BeExactly 'Arg 3 is <ab cd>'
        }
        else {
            $lines[0] | Should -BeExactly 'Arg 0 is <a"b c"d>'
            $lines[1] | Should -BeExactly 'Arg 1 is <a"b c"d>'
            # the following is the only example of where the parser removes the quotes
            # this is because the whole string is not quoted
            $lines[2] | Should -BeExactly 'Arg 2 is <ab cd>'
            $lines[3] | Should -BeExactly 'Arg 3 is <a"b c"d>'
        }
    }

    # tests for the new experimental feature of escaping double quotes
    It "Should handle example: '<argument>'" -Skip:$skipQuoteTests -TestCases @(
        @{ argument = '"This is" "a test-1"'; expected = '"This is" "a test-1"' }
        @{ argument = """This is"" ""a test-2""" ; expected = '"This is" "a test-2"'}
        @{ argument = '"this" is "a test"'; expected = '"this" is "a test"' }
        @{ argument = '""""this is a test'; expected = '""""this is a test' }
        @{ argument = '"""this is a test'; expected = '"""this is a test' }
        @{ argument = '`"this is a test'; expected = '`"this is a test' }
        @{ argument = "`"this is a test (first quote with ``)"; expected = '"this is a test (first quote with `)' }
    ) {
        param ( $argument, $expected )
        $lines = testexe -echoargs $argument
        $lines.Count | Should -Be 1
        $expected = 'Arg 0 is <{0}>' -f $expected
        @($lines)[0] | Should -BeExactly $expected
    }

    # The following tests should work with or without the experimental feature
    # which checks to see if there is already a "\" in the string and then
    # does not add the escape
    #
    # In order to pass '"' characters so they are actually part of command line
    # arguments for native commands, they need to be escaped with a '\' (this
    # is in addition to the '`' escaping needed inside '"' quoted strings in
    # PowerShell).
    #
    # This functionality was broken in PowerShell 5.0 and 5.1, so this test
    # will fail on those versions unless the fix is backported to them.
    #
    # This test checks that the proper quoting and escaping is occurring by
    # passing arguments with escaped quotes to the testexe native command and
    # looking at how it got the arguments.
    It "Should handle spaces between escaped quotes" {
        $lines = testexe -echoargs 'a\"b c\"d' "a\`"b c\`"d"
        $lines.Count | Should -Be 2
        $lines[0] | Should -BeExactly 'Arg 0 is <a"b c"d>'
        $lines[1] | Should -BeExactly 'Arg 1 is <a"b c"d>'
    }

    It "Should correctly quote paths with spaces: <arguments>" -TestCases @(
        @{arguments = "'.\test 1\' `".\test 2\`""  ; expected = @(".\test 1\",".\test 2\")},
        @{arguments = "'.\test 1\\\' `".\test 2\\`""; expected = @(".\test 1\\\",".\test 2\\")}
    ) {
        param($arguments, $expected)
        $lines = Invoke-Expression "testexe -echoargs $arguments"
        $lines.Count | Should -Be $expected.Count
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $lines[$i] | Should -BeExactly "Arg $i is <$($expected[$i])>"
        }
    }

    It "Should handle PowerShell arrays with or without spaces correctly: <arguments>" -TestCases @(
        @{arguments = "1,2"; expected = @("1,2")}
        @{arguments = "1,2,3"; expected = @("1,2,3")}
        @{arguments = "1, 2"; expected = "1,", "2"}
        @{arguments = "1 ,2"; expected = "1", ",2"}
        @{arguments = "1 , 2"; expected = "1", ",", "2"}
        @{arguments = "1, 2,3"; expected = "1,", "2,3"}
        @{arguments = "1 ,2,3"; expected = "1", ",2,3"}
        @{arguments = "1 , 2,3"; expected = "1", ",", "2,3"}
    ) {
        param($arguments, $expected)
        $lines = @(Invoke-Expression "testexe -echoargs $arguments")
        $lines.Count | Should -Be $expected.Count
        for ($i = 0; $i -lt $expected.Count; $i++) {
            $lines[$i] | Should -BeExactly "Arg $i is <$($expected[$i])>"
        }
    }
}

Describe 'PSPath to native commands' {
    BeforeAll {
        $featureEnabled = $EnabledExperimentalFeatures.Contains('PSNativePSPathResolution')
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()

        $PSDefaultParameterValues["it:skip"] = (-not $featureEnabled)

        if ($IsWindows) {
            $cmd = "cmd"
            $cmdArg1 = "/c"
            $cmdArg2 = "type"
            $dir = "cmd"
            $dirArg1 = "/c"
            $dirArg2 = "dir"
        }
        else {
            $cmd = "cat"
            $dir = "ls"
        }

        Set-Content -Path testdrive:/test.txt -Value 'Hello'
        Set-Content -Path "testdrive:/test file.txt" -Value 'Hello'
        Set-Content -Path "env:/test var" -Value 'Hello'
        $filePath = Join-Path -Path ~ -ChildPath (New-Guid)
        Set-Content -Path $filePath -Value 'Home'
        $complexDriveName = 'My test! ;+drive'
        New-PSDrive -Name $complexDriveName -Root $testdrive -PSProvider FileSystem
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues

        Remove-Item -Path "env:/test var"
        Remove-Item -Path $filePath
        Remove-PSDrive -Name $complexDriveName
    }

    It 'PSPath with ~/path works' {
        $out = & $cmd $cmdArg1 $cmdArg2 $filePath
        $LASTEXITCODE | Should -Be 0
        $out | Should -BeExactly 'Home'
    }

    It 'PSPath with ~ works' {
        $out = & $dir $dirArg1 $dirArg2 ~
        $LASTEXITCODE | Should -Be 0
        $out | Should -Not -BeNullOrEmpty
    }

    It 'PSPath that is file system path works with native commands: <path>' -TestCases @(
        @{ path = "testdrive:/test.txt" }
        @{ path = "testdrive:/test file.txt" }
    ){
        param($path)

        $out = & $cmd $cmdArg1 $cmdArg2 "$path"
        $LASTEXITCODE | Should -Be 0
        $out | Should -BeExactly 'Hello'
    }

    It 'PSPath passed with single quotes should be treated as literal' {
        $out = & $cmd $cmdArg1 $cmdArg2 'testdrive:/test.txt'
        $LASTEXITCODE | Should -Not -Be 0
        $out | Should -BeNullOrEmpty
    }

    It 'PSPath that is not a file system path fails with native commands: <path>' -TestCases @(
        @{ path = "env:/PSModulePath" }
        @{ path = "env:/test var" }
    ){
        param($path)

        $out = & $cmd $cmdArg1 $cmdArg2 "$path"
        $LASTEXITCODE | Should -Not -Be 0
        $out | Should -BeNullOrEmpty
    }

    It 'Relative PSPath works' {
        New-Item -Path $testdrive -Name TestFolder -ItemType Directory -ErrorAction Stop
        $cwd = Get-Location
        Set-Content -Path (Join-Path -Path $testdrive -ChildPath 'TestFolder' -AdditionalChildPath 'test.txt') -Value 'hello'
        Set-Location -Path (Join-Path -Path $testdrive -ChildPath 'TestFolder')
        Set-Location -Path $cwd
        $out = & $cmd $cmdArg1 $cmdArg2 "TestDrive:test.txt"
        $LASTEXITCODE | Should -Be 0
        $out | Should -BeExactly 'Hello'
    }

    It 'Complex PSDrive name works' {
        $out = & $cmd $cmdArg1 $cmdArg2 "${complexDriveName}:/test.txt"
        $LASTEXITCODE | Should -Be 0
        $out | Should -BeExactly 'Hello'
    }
}
