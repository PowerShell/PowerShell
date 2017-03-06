
Describe "New-PSSession basic test" -Tag @("CI") {
    It "New-PSSession should not crash powershell" {
        try {
            New-PSSession -ComputerName nonexistcomputer -Authentication Basic
            throw "New-PSSession should throw"
        } catch {
            $_.FullyQualifiedErrorId | Should Be "InvalidOperation,Microsoft.PowerShell.Commands.NewPSSessionCommand"
        }
    }
}