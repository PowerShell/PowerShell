Describe "Remove-Alias" -Tags "CI" {
    It "Remove-Alias should remove a non-readonly alias"{
        {
            Set-Alias -Name "tral" -Value "Remove-Alias" -ErrorAction Stop
            Remove-Alias -Name "tral" -ErrorAction Stop
            Get-Alias -Name "tral" -ErrorAction Stop
        } | ShouldBeErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
    }

    It "Remove-Alias should throw on a readonly alias"{
        {
            Set-Alias -Name "tral" -Value "Remove-Alias" -Option ReadOnly -ErrorAction Stop
            Remove-Alias -Name "tral" -ErrorAction Stop
        } | ShouldBeErrorId 'AliasNotRemovable,Microsoft.PowerShell.Commands.RemoveAliasCommand'
    }

    It "Remove-Alias should remove a non-readonly alias with force"{
        {
            Set-Alias -Name "tral" -Value "Remove-Alias" -ErrorAction Stop
            Remove-Alias -Name "tral" -Force -ErrorAction Stop
            Get-Alias -Name "tral" -ErrorAction Stop
        } | ShouldBeErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
    }

    It "Remove-Alias should remove a readonly alias with force"{
        {
            Set-Alias -Name "tral" -Value "Remove-Alias" -Option ReadOnly -ErrorAction Stop
            Remove-Alias -Name "tral" -Force -ErrorAction Stop
            Get-Alias -Name "tral" -ErrorAction Stop
        } | ShouldBeErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
    }

    It "Remove-Alias should throw if alias does not exist"{
        {
            Get-Alias -Name "tral" -ErrorAction SilentlyContinue | Should BeNullorEmpty
            Remove-Alias -Name "tral" -ErrorAction Stop
        } | ShouldBeErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.RemoveAliasCommand'
    }

    It "Remove-Alias should remove multiple alias at once"{
        {
            Set-Alias -Name "tral" -Value "Remove-Alias" -ErrorAction Stop
            Set-Alias -Name "tral2" -Value "Remove-Alias" -ErrorAction Stop
            Set-Alias -Name "tral3" -Value "Remove-Alias" -ErrorAction Stop
            Remove-Alias -Name "tral","tral2","tral3" -ErrorAction Stop
            Get-Alias -Name "tral" -ErrorAction Stop | ShouldBeErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
            Get-Alias -Name "tral2" -ErrorAction Stop | ShouldBeErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
            Get-Alias -Name "tral3" -ErrorAction Stop | ShouldBeErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
        }
    }

    It "Remove-Alias should throw on out-of-range scope"{
        {
            Set-Alias -Name "tral" -Value "Remove-Alias" -ErrorAction Stop
            Remove-Alias -Name "tral" -Scope 99999 -ErrorAction Stop
        } | ShouldBeErrorId "ArgumentOutOfRange,Microsoft.PowerShell.Commands.RemoveAliasCommand"
    }
}
