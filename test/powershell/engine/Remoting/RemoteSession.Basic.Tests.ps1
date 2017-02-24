
Describe "New-PSSession basic test" -Tag @("CI") {
    It "New-PSSession should not crash powershell" {
        { New-PSSession -ComputerName nonexistcomputer -Authentication Basic } | ShouldBeErrorId "InvalidOperation,Microsoft.PowerShell.Commands.NewPSSessionCommand"
    }
}
