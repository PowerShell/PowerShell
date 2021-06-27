# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidGlobalVars", "")]
param()
Describe "Will error correctly if an attempt to set variable to improper value" {
    It "will error when setting variable incorrectly" {
        if ($EnabledExperimentalFeatures -contains 'PSNativeCommandArgumentPassing') {
            { $global:PSNativeCommandArgumentPassing = "zzz" } | Should -Throw -ExceptionType System.Management.Automation.ArgumentTransformationMetadataException
        }
        else {
            Set-Test -State skipped -Because "Experimental feature 'PSNativeCommandArgumentPassing' is not enabled"
        }
    }
}

foreach ( $argumentListValue in "Standard","Legacy" ) {
    $PSNativeCommandArgumentPassing = $argumentListValue
    Describe "Native Command Arguments (${PSNativeCommandArgumentPassing})" -tags "CI" {
        # When passing arguments to native commands, quoted segments that contain
        # spaces need to be quoted with '"' characters when they are passed to the
        # native command (or to bash or sh on Linux).
        #
        # This test checks that the proper quoting is occuring by passing arguments
        # to the testexe native command and looking at how it got the arguments.
        It "Should handle quoted spaces correctly (ArgumentList=${PSNativeCommandArgumentPassing})" {
            $a = 'a"b c"d'
            $lines = testexe -echoargs $a 'a"b c"d' a"b c"d "a'b c'd"
            $lines.Count | Should -Be 4
            if (($EnabledExperimentalFeatures -contains 'PSNativeCommandArgumentPassing') -and $PSNativeCommandArgumentPassing -ne "Legacy") {
                $lines[0] | Should -BeExactly 'Arg 0 is <a"b c"d>'
                $lines[1] | Should -BeExactly 'Arg 1 is <a"b c"d>'
            }
            else {
                $lines[0] | Should -BeExactly 'Arg 0 is <ab cd>'
                $lines[1] | Should -BeExactly 'Arg 1 is <ab cd>'
            }
            $lines[2] | Should -BeExactly 'Arg 2 is <ab cd>'
            $lines[3] | Should -BeExactly 'Arg 3 is <a''b c''d>'
        }

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
        It "Should handle spaces between escaped quotes (ArgumentList=${PSNativeCommandArgumentPassing})" {
            $lines = testexe -echoargs 'a\"b c\"d' "a\`"b c\`"d"
            $lines.Count | Should -Be 2
            if (($EnabledExperimentalFeatures -contains 'PSNativeCommandArgumentPassing') -and $PSNativeCommandArgumentPassing -ne "Legacy") {
                $lines[0] | Should -BeExactly 'Arg 0 is <a\"b c\"d>'
                $lines[1] | Should -BeExactly 'Arg 1 is <a\"b c\"d>'
            }
            else {
                $lines[0] | Should -BeExactly 'Arg 0 is <a"b c"d>'
                $lines[1] | Should -BeExactly 'Arg 1 is <a"b c"d>'
            }
        }

        It "Should correctly quote paths with spaces (ArgumentList=${PSNativeCommandArgumentPassing}): <arguments>" -TestCases @(
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

        It "Should handle arguments that include commas without spaces (windbg example)" {
            $lines = testexe -echoargs -k com:port=\\devbox\pipe\debug,pipe,resets=0,reconnect
            $lines.Count | Should -Be 2
            $lines[0] | Should -BeExactly "Arg 0 is <-k>"
            $lines[1] | Should -BeExactly "Arg 1 is <com:port=\\devbox\pipe\debug,pipe,resets=0,reconnect>"
        }

        It "Should handle DOS style arguments" {
            $lines = testexe -echoargs /arg1 /c:"a string"
            $lines.Count | Should -Be 2
            $lines[0] | Should -BeExactly "Arg 0 is </arg1>"
            $lines[1] | Should -BeExactly "Arg 1 is </c:a string>"
        }

        It "Should handle PowerShell arrays with or without spaces correctly (ArgumentList=${PSNativeCommandArgumentPassing}): <arguments>" -TestCases @(
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
