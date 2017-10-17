
#Store ErrorActionPreference Value
$storedEA = $ErrorActionPreference
$ErrorActionPreference = "Stop"
Describe "Remove-Alias" -Tags "CI" {

    It "Remove-Alias Should Remove Non-ReadOnly Alias"{
        try{
            Set-Alias -Name "foo" -Value "bar"
            Remove-Alias -Name "foo"
            Get-Alias -Name "foo"
        }
        catch{
            $_.FullyQualifiedErrorId | Should Be 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
        }
        
    }

    It "Remove-Alias Should Throw on ReadOnly Alias"{
        try{
            Set-Alias -Name "foo" -Value "bar" -Option ReadOnly
            Remove-Alias -Name "foo"
            Get-Alias -Name "foo"
        }
        catch{
            $_.FullyQualifiedErrorId | Should Be 'AliasNotRemovable,Microsoft.PowerShell.Commands.RemoveAliasCommand'
        }
    }

    It "Remove-Alias Should Remove Non-ReadOnly Alias With Force"{
        try{
            Set-Alias -Name "foo" -Value "bar"
            Remove-Alias -Name "foo" -Force
            Get-Alias -Name "foo"
        }
        catch{
            $_.FullyQualifiedErrorId | Should Be 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
        }
    }

    It "Remove-Alias Should Remove ReadOnly Alias With Force"{
        try{
            Set-Alias -Name "foo" -Value "bar" -Option ReadOnly
            Remove-Alias -Name "foo" -Force
            Get-Alias -Name "foo"
        }
        catch{
            $_.FullyQualifiedErrorId | Should Be 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
        }
    }

    It "Remove-Alias OutOfRange Scope"{
        try {
            Set-Alias -Name "foo" -Value "bar"
            Remove-Alias -Name "foo" -Scope 99999
            Get-Alias -Name "foo"
        }
        catch {
            $_.FullyQualifiedErrorId | Should be "ArgumentOutOfRange,Microsoft.PowerShell.Commands.RemoveAliasCommand"
        }
    }
}
#Reset ErrorActionPreference to old Value
$ErrorActionPreference = $storedEA