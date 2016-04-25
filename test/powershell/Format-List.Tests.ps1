Describe "Format-List" {
    $nl = [Environment]::NewLine
    BeforeEach {
        $in = New-Object PSObject
        Add-Member -InputObject $in -MemberType NoteProperty -Name testName -Value testValue
    }

    It "Should call format list without error" {
        { $in | Format-List } | Should Not BeNullOrEmpty
    }

    It "Should be able to call the alias" {
        { $in | fl } | Should Not BeNullOrEmpty
    }

    It "Should have the same output whether choosing alias or not" {
        $expected = $in | Format-List | Out-String
        $actual   = $in | fl          | Out-String

        $actual | Should Be $expected
    }

    It "Should produce the expected output" {
        $expected = "${nl}${nl}testName : testValue${nl}${nl}${nl}${nl}"
        $in = New-Object PSObject
        Add-Member -InputObject $in -MemberType NoteProperty -Name testName -Value testValue

        $in | Format-List                  | Should Not BeNullOrEmpty
        $in | Format-List   | Out-String   | Should Not BeNullOrEmpty
        $in | Format-List   | Out-String   | Should Be $expected
    }

    It "Should be able to call a property of the piped input" {
        # Tested on two input commands to verify functionality.
        { Get-Command | Format-List -Property Name }        | Should Not BeNullOrEmpty

        { Get-Date    | Format-List -Property DisplayName } | Should Not BeNullOrEmpty
    }

    It "Should be able to display a list of props when separated by a comma" {

        (Get-Command | Format-List -Property Name,Source | Out-String) -Split "${nl}" |
          Where-Object { $_.trim() -ne "" } |
          ForEach-Object { $_ | Should Match "(Name)|(Source)" }
    }

    It "Should show the requested prop in every element" {
        # Testing each element of format-list, using a for-each loop since the Format-List is so opaque
        (Get-Command | Format-List -Property Source | Out-String) -Split "${nl}" |
          Where-Object { $_.trim() -ne "" } |
          ForEach-Object { $_ | Should Match "Source :" }
    }

    It "Should not show anything other than the requested props" {
        $output = Get-Command | Format-List -Property Name | Out-String

        $output | Should Not Match "CommandType :"
        $output | Should Not Match "Source :"
        $output | Should Not Match "Module :"
    }

    It "Should be able to take input without piping objects to it" {
        $output = { Format-List -InputObject $in }

        $output | Should Not BeNullOrEmpty

    }
}
