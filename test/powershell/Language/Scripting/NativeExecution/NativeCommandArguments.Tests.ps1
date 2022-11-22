# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidGlobalVars", "")]
param()

Describe "Behavior is specific for each platform" -tags "CI" {
    It "PSNativeCommandArgumentPassing is set to 'Windows' on Windows systems" -skip:(-not $IsWindows) {
        $PSNativeCommandArgumentPassing | Should -Be "Windows"
    }
    It "PSNativeCommandArgumentPassing is set to 'Standard' on non-Windows systems" -skip:($IsWindows) {
        $PSNativeCommandArgumentPassing | Should -Be "Standard"
    }
    It "Has proper behavior on Windows" -skip:(-not $IsWindows) {
        "@echo off`nSET V1=1" > "$TESTDRIVE\script 1.cmd"
        "@echo off`nSET V2=a`necho %V1%" > "$TESTDRIVE\script 2.cmd"
        "@echo off`necho %V1%`necho %V2%" > "$TESTDRIVE\script 3.cmd"
        $result = cmd /c """${TESTDRIVE}\script 1.cmd"" && ""${TESTDRIVE}\script 2.cmd"" && ""${TESTDRIVE}\script 3.cmd"""
        $result.Count | Should -Be 3
        $result[0] | Should -Be 1
        $result[1] | Should -Be 1
        $result[2] | Should Be "a"
    }

}

Describe "tests for multiple languages and extensions" -tags "CI" {
    AfterAll {
        if (-not $IsWindows) {
            return
        }
        $PSNativeCommandArgumentPassing = $passingStyle
    }
    BeforeAll {
        $testCases = @(
            @{
                Command = "cscript.exe"
                Filename = "test.wsf"
                ExpectedResults = @(
                    "Argument 0 is: <ab cd>"
                    "Argument 1 is: <ab cd>"
                    "Argument 2 is: <ab cd>"
                    "Argument 3 is: <a'b c'd>"
                )
                Script = @'
<?xml version="1.0" ?>
<job id="test">
   <script language="VBScript">
     <![CDATA[
for i = 0 to wScript.arguments.count-1
    wscript.echo "Argument " & i & " is: <" & wScript.arguments(i) & ">"
next
     ]]>
   </script>
</job>
'@
            }
            @{
                Command = "cscript.exe"
                Filename = "test.vbs"
                ExpectedResults = @(
                    "Argument 0 is: <ab cd>"
                    "Argument 1 is: <ab cd>"
                    "Argument 2 is: <ab cd>"
                    "Argument 3 is: <a'b c'd>"
                )
                Script = @'
for i = 0 to wScript.arguments.count - 1
    wscript.echo "Argument " & i & " is: <" & (wScript.arguments(i)) & ">"
next
'@
            }
            @{
                Command = "cscript"
                Filename = "test.js"
                ExpectedResults = @(
                    "Argument 0 is: <ab cd>"
                    "Argument 1 is: <ab cd>"
                    "Argument 2 is: <ab cd>"
                    "Argument 3 is: <a'b c'd>"
                )
                Script = @'
for(i = 0; i < WScript.Arguments.Count(); i++) {
    WScript.echo("Argument " + i + " is: <" + WScript.Arguments(i) + ">");
}
'@
            }
            @{
                Command = ""
                Filename = "test.bat"
                ExpectedResults = @(
                    "Argument 1 is: <a""b c""d>"
                    "Argument 2 is: <a""b c""d>"
                    "Argument 3 is: <""ab cd"">"
                    "Argument 4 is: <""a'b c'd"">"
                )
                Script = @'
@echo off
echo Argument 1 is: ^<%1^>
echo Argument 2 is: ^<%2^>
echo Argument 3 is: ^<%3^>
echo Argument 4 is: ^<%4^>
'@
            }
            @{
                Command = ""
                Filename = "test.cmd"
                ExpectedResults = @(
                    "Argument 1 is: <a""b c""d>"
                    "Argument 2 is: <a""b c""d>"
                    "Argument 3 is: <""ab cd"">"
                    "Argument 4 is: <""a'b c'd"">"
                )
                Script = @'
@echo off
echo Argument 1 is: ^<%1^>
echo Argument 2 is: ^<%2^>
echo Argument 3 is: ^<%3^>
echo Argument 4 is: ^<%4^>
'@
            }
        )

        # determine whether we should skip the tests we just defined
        # doing it in this order ensures that the test output will show each skipped test
        $skipTests = -not $IsWindows
        if ($skipTests) {
            return
        }

        # save the passing style
        $passingStyle = $PSNativeCommandArgumentPassing
        # explicitely set the passing style to Windows
        $PSNativeCommandArgumentPassing = "Windows"
    }

    It "Invoking '<Filename>' is compatible with PowerShell 5" -TestCases $testCases -Skip:$($skipTests) {
        param ( $Command, $Arguments, $Filename, $Script, $ExpectedResults )
        cscript  //h:cscript //nologo //s
        $a = 'a"b c"d'
        $scriptPath = Join-Path $TESTDRIVE $Filename
        $Script | out-file -encoding ASCII $scriptPath
        if ($Command) {
            $results = & $Command $scriptPath  $a 'a"b c"d' a"b c"d "a'b c'd" 2> "${TESTDRIVE}/error.txt"
        }
        else {
            $results = & $scriptPath  $a 'a"b c"d' a"b c"d "a'b c'd" 2> "${TESTDRIVE}/error.txt"
        }
        $errorContent = Get-Content "${TESTDRIVE}/error.txt" -ErrorAction Ignore
        $errorContent | Should -BeNullOrEmpty
        $results.Count | Should -Be 4
        $results[0] | Should -Be $ExpectedResults[0]
        $results[1] | Should -Be $ExpectedResults[1]
        $results[2] | Should -Be $ExpectedResults[2]
        $results[3] | Should -Be $ExpectedResults[3]
    }
}


Describe "Will error correctly if an attempt to set variable to improper value" -tags "CI" {
    It "will error when setting variable incorrectly" {
        { $global:PSNativeCommandArgumentPassing = "zzz" } | Should -Throw -ExceptionType System.Management.Automation.ArgumentTransformationMetadataException
    }
}

Describe "find.exe uses legacy behavior on Windows" -Tag 'CI' {
    BeforeAll {
        $currentSetting = $PSNativeCommandArgumentPassing
        $PSNativeCommandArgumentPassing = "Windows"
        $testCases = @{ pattern = "" },
            @{ pattern = "blat" },
            @{ pattern = "bl at" }
    }
    AfterAll {
        $PSNativeCommandArgumentPassing = $currentSetting
    }
    It "The pattern '<pattern>' is used properly by find.exe" -skip:(! $IsWindows) -testCases $testCases {
        param ($pattern)
        $expr = "'foo' | find.exe --% /v ""$pattern"""
        $result = Invoke-Expression $expr
        $result | Should -Be 'foo'
    }
}

foreach ( $argumentListValue in "Standard","Legacy","Windows" ) {
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
            if ($PSNativeCommandArgumentPassing -ne "Legacy") {
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
            if ($PSNativeCommandArgumentPassing -ne "Legacy") {
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

        It "Should handle when the ':' is the parameter value" {
            $lines = testexe -echoargs awk -F: '{print $1}'
            $lines.Count | Should -Be 3
            $lines[0] | Should -BeExactly 'Arg 0 is <awk>'
            $lines[1] | Should -BeExactly 'Arg 1 is <-F:>'
            $lines[2] | Should -BeExactly 'Arg 2 is <{print $1}>'
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

        It "Should handle empty args correctly (ArgumentList=${PSNativeCommandArgumentPassing})" {
            if ($PSNativeCommandArgumentPassing -eq 'Legacy') {
                $expectedLines = 2
            }
            else {
                $expectedLines = 3
            }

            $lines = testexe -echoargs 1 '' 2
            $lines.Count | Should -Be $expectedLines
            $lines[0] | Should -BeExactly 'Arg 0 is <1>'

            if ($expectedLines -eq 2) {
                $lines[1] | Should -BeExactly 'Arg 1 is <2>'
            }
            else {
                $lines[1] | Should -BeExactly 'Arg 1 is <>'
                $lines[2] | Should -BeExactly 'Arg 2 is <2>'
            }

        }

        It 'Should treat a PSPath as literal' {
            $lines = testexe -echoargs temp:/foo
            $lines.Count | Should -Be 1
            $lines | Should -BeExactly 'Arg 0 is <temp:/foo>'
        }
    }
}
