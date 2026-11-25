# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'PSScopedProperties PSStyle configuration' -Tag 'CI' {
    $styleLeafCases = @(
        @{ Path = 'OutputRendering'; Value = [System.Management.Automation.OutputRendering]::Ansi }
        @{ Path = 'AutoSizeDefaultFormatting'; Value = $true }
        @{ Path = 'Progress.Style'; Value = "`e[36m" }
        @{ Path = 'Progress.MaxWidth'; Value = 140 }
        @{ Path = 'Progress.View'; Value = [System.Management.Automation.ProgressView]::Classic }
        @{ Path = 'Progress.UseOSCIndicator'; Value = $true }
        @{ Path = 'Formatting.FormatAccent'; Value = "`e[34m" }
        @{ Path = 'Formatting.TableHeader'; Value = "`e[35m" }
        @{ Path = 'Formatting.CustomTableHeaderLabel'; Value = "`e[36m" }
        @{ Path = 'Formatting.ErrorAccent'; Value = "`e[31m" }
        @{ Path = 'Formatting.Error'; Value = "`e[32m" }
        @{ Path = 'Formatting.Warning'; Value = "`e[33m" }
        @{ Path = 'Formatting.Verbose'; Value = "`e[34m" }
        @{ Path = 'Formatting.Debug'; Value = "`e[35m" }
        @{ Path = 'Formatting.FeedbackName'; Value = "`e[36m" }
        @{ Path = 'Formatting.FeedbackText'; Value = "`e[91m" }
        @{ Path = 'Formatting.FeedbackAction'; Value = "`e[92m" }
        @{ Path = 'FileInfo.Directory'; Value = "`e[41m" }
        @{ Path = 'FileInfo.SymbolicLink'; Value = "`e[42m" }
        @{ Path = 'FileInfo.Executable'; Value = "`e[43m" }
    )

    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues['It:Skip'] = -not [ExperimentalFeature]::IsEnabled('PSScopedProperties')
        $script:getStyleLeaf = {
            param($style, $path)
            foreach ($segment in $path.Split('.')) { $style = $style.$segment }
            $style
        }
        $script:setStyleLeaf = {
            param($style, $path, $value)
            $segments = $path.Split('.')
            if ($segments.Count -gt 1) {
                foreach ($segment in $segments[0..($segments.Count - 2)]) { $style = $style.$segment }
            }
            $propertyName = $segments[-1]
            $style.$propertyName = $value
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It 'exposes one concrete PSStyle type through the facade and legacy variable' {
        $PS.Style | Should -BeOfType ([System.Management.Automation.PSStyle])
        $PSStyle | Should -BeOfType ([System.Management.Automation.PSStyle])
        [object]::ReferenceEquals($PS.Style, $PSStyle) | Should -BeTrue
        ([Enum]::GetValues([System.Management.Automation.OutputRendering]) -contains $PS.Style.OutputRendering) | Should -BeTrue
        $PS.Style.Progress.Style | Should -Be "`e[33;1m"
        $PS.Style.Progress.MaxWidth | Should -Be 120
        $PS.Style.Progress.UseOSCIndicator | Should -BeFalse
        $PS.Style.Formatting.FeedbackName | Should -Be "`e[33m"
        $PS.Style.FileInfo.Extension['.ps1'] | Should -Be "`e[33;1m"
    }

    It 'scopes each style leaf independently and resumes inheritance after reset: <Path>' -TestCases $styleLeafCases {
        param($Path, $Value)

        $targetStyle = $PS.AtScope('Local').Style
        $baseline = & $script:getStyleLeaf $targetStyle $Path
        & {
            $targetStyle = $PS.AtScope('Local').Style
            & $script:setStyleLeaf $targetStyle $Path $Value
            (& $script:getStyleLeaf $targetStyle $Path) | Should -Be $Value
            $PS.Reset("Style.$Path")
            (& $script:getStyleLeaf $targetStyle $Path) | Should -Be $baseline
        }
    }

    It 'scopes style leaves independently and resets only the selected local value' {
        $warningBaseline = $PS.Style.Formatting.Warning
        $PS.Style.Formatting.Error = "`e[35m"
        $PS.Style.Progress.Style = "`e[36m"

        & {
            $PS.Style.Formatting.Error = "`e[31m"
            $PS.Style.Formatting.Warning = "`e[33m"
            $PS.Style.Formatting.Error | Should -Be "`e[31m"
            $PS.Reset('Style.Formatting.Error')
            $PS.Style.Formatting.Error | Should -Be "`e[35m"
            $PS.Style.Formatting.Warning | Should -Be "`e[33m"
            $PS.Style.Progress.Style | Should -Be "`e[36m"
        }

        $PS.Style.Formatting.Error | Should -Be "`e[35m"
        $PS.Style.Formatting.Warning | Should -Be $warningBaseline
    }

    It 'binds AtScope style views and rejects a captured view after its target scope expires' {
        $style = & { $PS.AtScope('Local').Style }
        { $style.Progress.Style } | Should -Throw
        foreach ($property in 'Reset', 'BlinkOff', 'Blink', 'BoldOff', 'Bold', 'DimOff', 'Dim', 'Hidden', 'HiddenOff', 'Reverse', 'ReverseOff', 'ItalicOff', 'Italic', 'UnderlineOff', 'Underline', 'StrikethroughOff', 'Strikethrough') {
            { $style.$property } | Should -Throw
        }

        $foreground = & { $PS.AtScope('Local').Style.Foreground }
        { $foreground.Red } | Should -Throw
        { $foreground.FromRgb(1, 2, 3) } | Should -Throw
    }

    It 'preserves standalone ANSI constants' {
        [System.Management.Automation.PSStyle]::Instance.Reset | Should -Be "`e[0m"
        [System.Management.Automation.PSStyle]::Instance.Bold | Should -Be "`e[1m"
    }

    It 'copies only the file extension dictionary when it is successfully mutated' {
        $PS.Style.FileInfo.Extension['.scopedtest'] = "`e[34m"
        $PS.Style.FileInfo.Extension.ContainsKey('.scopedtest') | Should -BeTrue

        & {
            $PS.Style.FileInfo.Extension.Remove('.scopedtest')
            $PS.Style.FileInfo.Extension.ContainsKey('.scopedtest') | Should -BeFalse
            $PS.Reset('Style.FileInfo.Extension')
            $PS.Style.FileInfo.Extension.ContainsKey('.scopedtest') | Should -BeTrue
        }

        $PS.Style.FileInfo.Extension.Remove('.scopedtest')
    }

    It 'does not publish invalid scalar or extension mutations' {
        $before = $PS.Style.Progress.Style
        { $PS.Style.Progress.Style = 'visible text' } | Should -Throw
        { $PS.Style.Progress.MaxWidth = 17 } | Should -Throw
        $PS.Style.Progress.Style | Should -Be $before

        $beforeZip = $PS.Style.FileInfo.Extension['.zip']
        { $PS.Style.FileInfo.Extension.Add('.zip', "`e[31m") } | Should -Throw
        $PS.Style.FileInfo.Extension['.zip'] | Should -Be $beforeZip
    }

    It 'keeps public nested style objects standalone' {
        $progress = [System.Management.Automation.PSStyle+ProgressConfiguration]::new()
        $formatting = [System.Management.Automation.PSStyle+FormattingData]::new()
        $fileInfo = [System.Management.Automation.PSStyle+FileInfoFormatting]::new()

        $progress.Style = "`e[34m"
        $progress.View = [System.Management.Automation.ProgressView]::Classic
        $formatting.Error = "`e[35m"
        $fileInfo.Extension.Add('.standalone', "`e[36m")

        $progress.Style | Should -Be "`e[34m"
        $progress.View | Should -Be ([System.Management.Automation.ProgressView]::Classic)
        $formatting.Error | Should -Be "`e[35m"
        $fileInfo.Extension['.standalone'] | Should -Be "`e[36m"
        $PS.Style.Progress.Style | Should -Not -Be "`e[34m"
        $PS.Style.Formatting.Error | Should -Not -Be "`e[35m"
        $PS.Style.FileInfo.Extension.ContainsKey('.standalone') | Should -BeFalse
    }

    It 'resolves unbound style views in the caller runspace and rejects bound views from another runspace' {
        $first = [powershell]::Create()
        $second = [powershell]::Create()
        try {
            $unboundStyle = $first.AddScript({ $PS.Style }).Invoke()[0]
            $values = $second.AddScript({
                param($styleView)
                $PS.Style.OutputRendering = [System.Management.Automation.OutputRendering]::Ansi
                @($styleView.OutputRendering, $styleView.Reset)
            }).AddArgument($unboundStyle).Invoke()
            $values[0] | Should -Be ([System.Management.Automation.OutputRendering]::Ansi)
            $values[1] | Should -Be "`e[0m"

            $first.Commands.Clear()
            $boundStyle = $first.AddScript({ $PS.AtScope('Global').Style }).Invoke()[0]
            $second.Commands.Clear()
            $second.Streams.Error.Clear()
            foreach ($property in 'OutputRendering', 'Bold', 'Reset') {
                $second.Commands.Clear()
                $second.Streams.Error.Clear()
                $second.AddScript({ param($styleView, $propertyName) $styleView.$propertyName }).AddArgument($boundStyle).AddArgument($property) | Out-Null
                $null = $second.Invoke()
                $second.HadErrors | Should -BeTrue
                $second.Streams.Error[0].Exception.Message | Should -Match 'runspace'
            }
        }
        finally {
            $first.Dispose()
            $second.Dispose()
        }
    }
}
