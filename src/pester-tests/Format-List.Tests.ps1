Describe "Format-List" {
    BeforeEach {
        $in = New-Object PSObject
        Add-Member -InputObject $in -MemberType NoteProperty -Name testName -Value testValue
    }

    It "Should call format list without error" {
        { $in | Format-List } | Should Not Throw
        { $in | Format-List } | Should Not BeNullOrEmpty
    }

    It "Should be able to call the alias" {
        { $in | fl } | Should Not Throw
        { $in | fl } | Should Not BeNullOrEmpty
    }

    It "Should have the same output whether choosing alias or not" {
        $expected = $in | Format-List | Out-String
        $actual   = $in | fl          | Out-String

        $actual | Should Be $expected
    }

    It "Should produce the expected output" {
        $expected = "`n`ntestName : testValue`n`n`n`n"
        $in = New-Object PSObject
        Add-Member -InputObject $in -MemberType NoteProperty -Name testName -Value testValue

        $in | Format-List                  | Should Not BeNullOrEmpty
        $in | Format-List   | Out-String   | Should Not BeNullOrEmpty
        $in | Format-List   | Out-String   | Should Be $expected
    }

    It "Should be able to call a property of the piped input" {
        # Tested on two input commands to verify functionality.
        { Get-Process | Format-List -Property Name }        | Should Not Throw
        { Get-Process | Format-List -Property Name }        | Should Not BeNullOrEmpty

        { Get-Date    | Format-List -Property DisplayName } | Should Not Throw
        { Get-Date    | Format-List -Property DisplayName } | Should Not BeNullOrEmpty
    }

    It "Should be able to display a list of props when separated by a comma" {
        { Get-Process | Format-List -Property Name,BasePriority } | Should Not Throw

        (Get-Process | Format-List -Property Name,BasePriority | Out-String) -Split "`n" |
          Where-Object { $_.trim() -ne "" } |
          ForEach-Object { $_ | Should Match "(Name)|(BasePriority)" }
    }

    It "Should show the requested prop in every element" {
        # Testing each element of format-list, using a for-each loop since the Format-List is so opaque
        (Get-Process | Format-List -Property CPU | Out-String) -Split "`n" |
          Where-Object { $_.trim() -ne "" } |
          ForEach-Object { $_ | Should Match "CPU :" }
    }

    It "Should not show anything other than the requested props" {
        $output = Get-Process | Format-List -Property Name | Out-String

        $output | Should Not Match "CPU :"
        $output | Should Not Match "Id :"
        $output | Should Not Match "Handle :"
    }

    It "Should be able to take input without piping objects to it" {
        $output = { Format-List -InputObject $in }

        $output | Should Not Throw
        $output | Should Not BeNullOrEmpty

    }
}
