Describe "Set-Date for admin" -Tag @('CI', 'RequireAdminOnWindows') {
    It "Set-Date should be able to set the date in an elevated context" {
        { Get-Date | Set-Date } | Should Not Throw
    }

    It "Set-Date should be able to set the date with -Date parameter" {
        $target = Get-Date
        $expected = $target
        Set-Date -Date $target | Should Be $expected
    }
}

Describe "Set-Date" -Tag 'CI' {
    # Currently CI tests on Linux/macOS are always run as sudo, so we need to skip this test on non-Windows platform.
    It "Set-Date should produce an error in a non-elevated context" -Skip:(!$IsWindows) {
        { Get-Date | Set-Date } | ShouldBeErrorId "System.ComponentModel.Win32Exception,Microsoft.PowerShell.Commands.SetDateCommand"
    }
}
