# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Tests for $PSStyle automatic variable' {
    BeforeAll {
        $PSDefaultParameterValues.Add('It:Skip', (-not $EnabledExperimentalFeatures.Contains('PSAnsiRendering')))
        $styleDefaults = @{
            Reset = "`e[0m"
            BlinkOff = "`e[25m"
            Blink = "`e[5m"
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
            StrikethroughOff = "`e[29m"
            Strikethrough = "`e[9m"
        }

        $formattingDefaults = @{
            FormatAccent = "`e[32;1m"
            TableHeader = "`e[32;1m"
            ErrorAccent = "`e[36;1m"
            Error = "`e[31;1m"
            Debug = "`e[33;1m"
            Verbose = "`e[33;1m"
            Warning = "`e[33;1m"
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

    It '$PSStyle.Foreground.FromRGB(r, g, b) works' {
        $o = $PSStyle.Foreground.FromRGB(11,22,33)
        $o | Should -BeExactly "`e[38;2;11;22;33m" -Because ($o | Format-Hex | Out-String)
    }

    It '$PSStyle.Foreground.FromRGB(rgb) works' {
        $o = $PSStyle.Foreground.FromRGB(0x223344)
        $o | Should -BeExactly "`e[38;2;34;51;68m" -Because ($o | Format-Hex | Out-String)
    }

    It '$PSStyle.Background.FromRGB(r, g, b) works' {
        $o = $PSStyle.Background.FromRGB(33,44,55)
        $o | Should -BeExactly "`e[48;2;33;44;55m" -Because ($o | Format-Hex | Out-String)
    }

    It '$PSStyle.Background.FromRGB(rgb) works' {
        $o = $PSStyle.Background.FromRGB(0x445566)
        $o | Should -BeExactly "`e[48;2;68;85;102m" -Because ($o | Format-Hex | Out-String)
    }

    It '$PSStyle.FormatHyperlink() works' {
        $o = $PSStyle.FormatHyperlink('PSBlog','https://aka.ms/psblog')
        $o | Should -BeExactly "`e]8;;https://aka.ms/psblog`e\PSBlog`e]8;;`e\" -Because ($o | Format-Hex | Out-String)
    }

    It '$PSStyle.Formatting.FormatAccent is applied to Format-List' {
        $old = $PSStyle.Formatting.FormatAccent

        try {
            $PSStyle.Formatting.FormatAccent = $PSStyle.Foreground.Yellow + $PSStyle.Background.Red + $PSStyle.Italic
            $out = $PSVersionTable | Format-List | Out-String
            $out | Should -BeLike "*$($PSStyle.Formatting.FormatAccent.Replace('[',"``["))*"
        }
        finally {
            $PSStyle.Formatting.FormatAccent = $old
        }
    }

    It '$PSStyle.Formatting.TableHeader is applied to Format-Table' {
        $old = $PSStyle.Formatting.TableHeader

        try {
            $PSStyle.Formatting.TableHeader = $PSStyle.Foreground.Blue + $PSStyle.Background.White + $PSStyle.Bold
            $out = $PSVersionTable | Format-Table | Out-String
            $out | Should -BeLike "*$($PSStyle.Formatting.TableHeader.Replace('[',"``["))*"
        }
        finally {
            $PSStyle.Formatting.TableHeader = $old
        }
    }
}
