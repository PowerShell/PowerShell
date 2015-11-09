Describe "Set-Alias" {
    Mock Get-Date { return "Friday, October 30, 2015 3:38:08 PM" }
    It "Should be able to set alias without error" {

        { set-alias -Name gd -Value Get-Date } | Should Not Throw
    }

    It "Should be able to have the same output between set-alias and the output of the function being aliased" {
        set-alias -Name gd -Value Get-Date
        gd | Should Be $(Get-Date)
    }

    It "Should be able to use the sal alias" {
        { sal gd Get-Date } | Should Not Throw
    }

    It "Should have the same output between the sal alias and the original set-alias cmdlet" {
        sal -Name gd -Value Get-Date

        Set-Alias -Name gd2 -Value Get-Date

        gd2 | Should Be $(gd)
    }
}
