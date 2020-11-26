# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Tests for $PSStyle automatic variable' {
    BeforeAll {
        $PSDefaultParameterValues.Add('It:Skip', (-not $EnabledExperimentalFeatures.Contains('PSAnsiRendering')))
        $styleDefaults = @{
            Reset = "`e[0m"
            BlinkOff = "`e[5m"
            Blink = "`e[25m"
            BoldOff = "`e[22m"
            Bold = "`e[1m"
            HiddenOff = "`e[28m"
            Hidden = "`e[8m"
            ReverseOff = "`e[27m"
            Reverse = "`e[7m"
            ItalicOff = "`e[23m"
            Italic = "`e[3m"
            UnderlineOff = "`e[24m"
            Underline = "`e[4m"
        }

        $formattingDefaults = @{
            FormatAccent = "`e[32;1m"
            ErrorAccent = "`e[36;1m"
            Error = "`e[31;1m"
            Debug = "`e[33;1m"
            Verbose = "`e[33;1m"
            Warning = "`e[33;1m"
            Progress = "`e[30;43m"
        }

        $foregroundDefaults = @{
            Black = "`e[30m"
            White = "`e[37m"
            DarkGray = "`e[90m"
            LightGray = "`e[97m"
            Red = "`e[31m"
            LightRed = "`e[91m"
            Magenta = "`e[35m"
            LightMagenta = "`e[95m"
            Blue = "`e[34m"
            LightBlue = "`e[94m"
            Cyan = "`e[36m"
            LightCyan = "`e[96m"
            Green = "`e[32m"
            LightGreen = "`e[92m"
            Yellow = "`e[33m"
            LightYellow = "`e[93m"
        }

        $backgroundDefaults = @{
            Black = "`e[40m"
            White = "`e[47m"
            DarkGray = "`e[100m"
            LightGray = "`e[107m"
            Red = "`e[41m"
            LightRed = "`e[101m"
            Magenta = "`e[45m"
            LightMagenta = "`e[105m"
            Blue = "`e[44m"
            LightBlue = "`e[104m"
            Cyan = "`e[46m"
            LightCyan = "`e[106m"
            Green = "`e[42m"
            LightGreen = "`e[102m"
            Yellow = "`e[43m"
            LightYellow = "`e[103m"
        }

        function Get-TestCases($hashtable) {
            $testcases = [System.Collections.Generic.List[hashtable]]::new()
            foreach ($key in $hashtable.Keys) {
                $testcases.Add(
                    @{ Key = $key; Value = $hashtable[$key] }
                )
            }

            return $testcases
        }
    }

    AfterAll {
        $PSDefaultParameterValues.Remove('It:Skip')
    }

    It '$PSStyle has correct default for OutputRendering' {
        $PSStyle | Should -Not -BeNullOrEmpty
        $PSStyle.OutputRendering | Should -BeExactly 'Automatic'
    }

    It '$PSStyle has correct defaults for style <key>' -TestCases (Get-TestCases $styleDefaults) {
        param($key, $value)

        $PSStyle.$key | Should -BeExactly $value
    }

    It '$PSStyle.Formatting has correct default for <key>' -TestCases (Get-TestCases $formattingDefaults) {
        param($key, $value)

        $PSStyle.Formatting.$key | Should -BeExactly $value
    }

    It '$PSStyle.Foreground has correct default for <key>' -TestCases (Get-TestCases $foregroundDefaults) {
        param($key, $value)

        $PSStyle.Foreground.$key | Should -BeExactly $value
    }

    It '$PSStyle.Background has correct default for <key>' -TestCases (Get-TestCases $backgroundDefaults) {
        param($key, $value)

        $PSStyle.Background.$key | Should -BeExactly $value
    }
}
