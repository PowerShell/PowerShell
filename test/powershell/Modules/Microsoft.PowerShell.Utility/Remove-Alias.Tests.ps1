Describe "Remove-Alias" -Tags "CI" {
    It "Remove-Alias should remove a non-readonly alias"{
        {
            Set-Alias -Name "foo" -Value "bar" -ErrorAction Stop
            Remove-Alias -Name "foo" -ErrorAction Stop
            Get-Alias -Name "foo" -ErrorAction Stop
        } | ShouldBeErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
    }

    It "Remove-Alias should throw on a readonly alias"{
        {
            Set-Alias -Name "foo" -Value "bar" -Option ReadOnly -ErrorAction Stop
            Remove-Alias -Name "foo" -ErrorAction Stop
        } | ShouldBeErrorId 'AliasNotRemovable,Microsoft.PowerShell.Commands.RemoveAliasCommand'
    }

    It "Remove-Alias should remove a non-readonly alias with force"{
        {
            Set-Alias -Name "foo" -Value "bar" -ErrorAction Stop
            Remove-Alias -Name "foo" -Force -ErrorAction Stop
            Get-Alias -Name "foo" -ErrorAction Stop
        } | ShouldBeErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
    }

    It "Remove-Alias should remove a readonly alias with force"{
        {
            Set-Alias -Name "foo" -Value "bar" -Option ReadOnly -ErrorAction Stop
            Remove-Alias -Name "foo" -Force -ErrorAction Stop
            Get-Alias -Name "foo" -ErrorAction Stop
        } | ShouldBeErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
    }

    It "Remove-Alias should throw if alias does not exist"{
        {
            Get-Alias -Name "foo" -ErrorAction SilentlyContinue | Should BeNullorEmpty
            Remove-Alias -Name "foo" -ErrorAction Stop
        } | ShouldBeErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.RemoveAliasCommand'
    }

    It "Remove-Alias should throw on out-of-range scope"{
        {
            Set-Alias -Name "foo" -Value "bar" -ErrorAction Stop
            Remove-Alias -Name "foo" -Scope 99999 -ErrorAction Stop
        } | ShouldBeErrorId "ArgumentOutOfRange,Microsoft.PowerShell.Commands.RemoveAliasCommand"
    }
}
