# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Colorizing prompt" -Tags 'CI', 'Slow' {
    Context 'Prompt can be colorized' {
        It 'Prompt can be colorized' {
            $colors = @'
            $PSStyle.Prompt.Caption = "`e[34m"
            $PSStyle.Prompt.Message = "`e[92m"
            $PSStyle.Prompt.Help = "`e[95m"
            $PSStyle.Prompt.ChoiceDefault = "`e[41m"
            $PSStyle.Prompt.ChoiceOther = "`e[106m"
            $PSStyle.Prompt.ChoiceHelp = "`e[47m"
'@

            # Only reasonable way to force "interactivity" and capture prompts is to fork
            $command = 'Set-Variable x 0 -Confirm'
            $pwsh = (Get-Process -Id $PID).Path
            $output = (Invoke-Expression "'y' | ${pwsh} -NoProfile -Command '$colors; $command'") -join "`n"

            # Check just couple values, as they may be flaky depending on locale and window width
            $format = "(?:`e\[\d+m)"
            $output | Should -Match "`e\[34mConfirm`e\[0m"
            $output | Should -Match "`e\[92mAre you sure"
            $output | Should -Match "`e\[95m\(default is ""Y${format}*""\):`e\[0m"
            $output | Should -Match "`e\[41m\[Y\] Yes`e\[0m"
            $output | Should -Match "`e\[106m\[L\] No to All`e\[0m"
            $output | Should -Match "`e\[47m\[\?\] Help`e\[0m"
        }
    }
}
