# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Native Windows tilde expansion tests' -tags "CI" {
    BeforeAll {
        $originalDefaultParams = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues["it:skip"] = -Not $IsWindows
        $EnabledExperimentalFeatures.Contains('PSNativeWindowsTildeExpansion') | Should -BeTrue
        $HomeDir = $ExecutionContext.SessionState.Provider.Get("FileSystem").Home
        $Tilde = "~"
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParams
    }

    # Test ~ expansion
    It '~ should be replaced by the filesystem provider home directory <arg>' -testCases @(
        @{arg = '~';            Expected = $HomeDir }
        @{arg = '$Tilde';       Expected = $HomeDir }
        @{arg = '~/foo';        Expected = "$HomeDir/foo" }
        @{arg = '~\foo';        Expected = "$HomeDir\foo" }
        @{arg = '$Tilde/foo';   Expected = "$HomeDir/foo" }
        @{arg = '$Tilde\foo';   Expected = "$HomeDir\foo" }
    ) {
        param($arg, $Expected)
        Invoke-Expression "cmd /c echo $arg" | Should -BeExactly $Expected
        Invoke-Expression "testexe -echoargs $arg" | Should -BeExactly "Arg 0 is <$Expected>"
    }
    It '~ should not be replaced when quoted <arg>' -testCases @(
        @{arg = "'~'";          Expected = "~" }
        @{arg = "'~/foo'";      Expected = "~/foo" }
        @{arg = "'~\foo'";      Expected = "~\foo" }
        @{arg = '"~"';          Expected = "~" }
        @{arg = '"~/foo"';      Expected = "~/foo" }
        @{arg = '"~\foo"';      Expected = "~\foo" }
        @{arg = '"$Tilde"';     Expected = "~" }
        @{arg = '"$Tilde/foo"'; Expected = "~/foo" }
        @{arg = '"$Tilde\foo"'; Expected = "~\foo" }
    ) {
        param($arg, $Expected)
        Invoke-Expression "cmd /c echo $arg" | Should -BeExactly $Expected
        Invoke-Expression "testexe -echoargs $arg" | Should -BeExactly "Arg 0 is <$Expected>"
    }
    It '~ should keep its literal meaning when splatted <splattingArgs>'-testCases @(
        @{
            splattingArgs = @'
~ ~/foo ~\foo '~' "~" '~/foo' "~/foo" '~\foo' "~\foo"
'@;
            Expected = @("$HomeDir", "$HomeDir/foo", "$HomeDir\foo", "~", "~", "~/foo", "~/foo", "~\foo", "~\foo")
        }
        @{
            splattingArgs = @'
$Tilde $Tilde/foo $Tilde\foo "$Tilde" "$Tilde/foo" "$Tilde\foo"
'@;
            Expected = @("$HomeDir", "$HomeDir/foo", "$HomeDir\foo", "~", "~/foo", "~\foo")
        }
    ) {
        param($splattingArgs, $Expected)
        function Invoke-Cmd {
            cmd @args
        }
        function Invoke-TestExe {
            testexe @args
        }

        Invoke-Expression "Invoke-Cmd /c echo $splattingArgs" | Should -BeExactly ($Expected -join ' ')
        Invoke-Expression "Invoke-TestExe -echoargs $splattingArgs" | Should -BeExactly @($Expected | ForEach-Object { $i = 0 } { "Arg {0} is <$_>" -f $i++ } )
    }
}
