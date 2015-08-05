Describe "Test-Format-List" {
    It "Should call format list without error" {
        { Get-Process | Format-List } | Should Not Throw

        { Get-Process | fl } | Should Not Throw
    }

    It "Should be able to call format list on piped in variable objects" {
        $a = Get-Process

        { $a | Format-List } | Should Not Throw

        { $a | fl } | Should Not Throw
    }

    It "Should be able to call a property of the piped input" {
        #Tested on two input commands to veryify functionality.
        $testCommand1 = Get-Process
        $testCommand2 = Get-Date

        { $testCommand1 | Format-List -Property Name }        | Should Not Throw
        { $testCommand2 | Format-List -Property DisplayName } | Should Not Throw


        { $testCommand1 | fl -Property Name }        | Should Not Throw
        { $testCommand2 | fl -Property DisplayName } | Should Not Throw
    }

    It "Should be able to display a list of props when separated by a comma" {
        $testCommand = Get-Process

        { $testCommand | Format-List -Property Name,BasePriority } | Should Not Throw

        { $testCommand | fl -Property Name,BasePriority } | Should Not Throw
    }

    It "Should not show only the requested props" {
        $testCommand = Get-Process

        ( $testCommand | Format-List                | Out-String).Contains("CPU") | Should Be $true
        ( $testCommand | Format-List -Property Name | Out-String).Contains("CPU") | Should Be $false

        ( $testCommand | fl                 | Out-String).Contains("CPU") | Should Be $true
        ( $testCommand | fl -Property Name  | Out-String).Contains("CPU") | Should Be $false
    }

    It "Should be able to take input without piping objects to it" {
        $input = (Get-Process)[0]

        { Format-List -InputObject $input } | Should Not Throw

        { fl -InputObject $input } | Should Not Throw
    }

}
