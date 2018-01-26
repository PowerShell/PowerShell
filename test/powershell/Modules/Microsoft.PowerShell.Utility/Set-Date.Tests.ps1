Describe "Set-Date for admin" -Tag @('CI', 'RequireAdminOnWindows') {
    # Currently, CI tests on Linux/macOS are always run as normal user. So we need to skip these tests on non-Windows platform.
    It "Set-Date should be able to set the date in an elevated context" -Skip:(!$IsWindows) {
        { Get-Date | Set-Date } | Should Not Throw
    }

    It "Set-Date should be able to set the date with -Date parameter" -Skip:(!$IsWindows) {
        $target = Get-Date
        $expected = $target
        Set-Date -Date $target | Should Be $expected
    }
}

Describe "Set-Date" -Tag 'CI' {
    It "Set-Date should produce an error in a non-elevated context" {
        { Get-Date | Set-Date } | ShouldBeErrorId "System.ComponentModel.Win32Exception,Microsoft.PowerShell.Commands.SetDateCommand"
    }
}
