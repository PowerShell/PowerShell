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
            FormatAccent = "`e[32m"
            ErrorAccent = "`e[36m"
            Error = "`e[35m"
            Debug = "`e[33m"
            Verbose = "`e[33m"
            Warning = "`e[33m"
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
            Blue = "`[34m"
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
            Blue = "`[44m"
            LightBlue = "`e[104m"
            Cyan = "`e[46m"
            LightCyan = "`e[106m"
            Green = "`e[42m"
            LightGreen = "`e[102m"
            Yellow = "`e[43m"
            LightYellow = "`e[103m"
        }
    }

    AfterAll {
        $PSDefaultParameterValues.Remove('It:Skip')
    }

    It '$PSStyle has correct default for OutputRendering' {
        $PSStyle | Should -Not -BeNullOrEmpty
        $PSStyle.OutputRendering | Should -BeExactly 'Automatic'
    }

    It '$PSStyle has correct defaults for styles' {
        foreach ($style in $styleDefaults.Keys) {
            $PSStyle.$style | Should -BeExactly $styleDefaults[$style]
        }
    }

    It '$PSStyle.Formatting has expected members' {
        $psstyle.Formatting.psobject.properties.name | ForEach-Object {
            $formattingDefaults.Keys | Should -Contain $_
        }
    }

    It '$PSStyle.Formatting has correct defaults' {
        foreach ($style in $formattingDefaults.Keys) {
            $PSStyle.Formatting.$style | Should -BeExactly $formattingDefaults[$style]
        }
    }

    It '$PSStyle.Foreground has expected members' {
        $psstyle.Foreground.psobject.properties.name | ForEach-Object {
            $foregroundDefaults.Keys | Should -Contain $_
        }
    }

    It '$PSStyle.Foreground has correct defaults' {
        foreach ($style in $foregroundDefaults.Keys) {
            $PSStyle.Foreground.$style | Should -BeExactly $foregroundDefaults[$style]
        }
    }

    It '$PSStyle.Background has expected members' {
        $psstyle.Background.psobject.properties.name | ForEach-Object {
            $backgroundDefaults.Keys | Should -Contain $_
        }
    }

    It '$PSStyle.Background has correct defaults' {
        foreach ($style in $backgroundDefaults.Keys) {
            $PSStyle.Background.$style | Should -BeExactly $backgroundDefaults[$style]
        }
    }
}
