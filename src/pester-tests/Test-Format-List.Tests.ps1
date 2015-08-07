Describe "Test-Format-List" {
    BeforeEach {
        $input = New-Object PSObject
        Add-Member -InputObject $input -MemberType NoteProperty -Name testName -Value testValue
    }

    It "Should call format list without error" {
        { $input | Format-List } | Should Not Throw
    }

    It "Should be able to call the alias" {
        { $input | fl } | Should Not Throw
    }

    It "Should have the same output whether choosing alias or not" {
        $expected = $input | Format-List | Out-String
        $actual   = $input | fl          | Out-String

        $actual | Should Be $expected

    }

    It "Should produce the expected output" {
        $expected = "`n`ntestName : testValue`n`n`n`n"
        $input = New-Object PSObject
        Add-Member -InputObject $input -MemberType NoteProperty -Name testName -Value testValue

        $input | Format-List                  | Should Not BeNullOrEmpty
        $input | Format-List   | Out-String   | Should Not BeNullOrEmpty
        $input | Format-List   | Out-String   | Should Be $expected
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

        $output = ( Get-Process | Format-List -Property Name,BasePriority | Out-String)

        $output.Contains("Name")         | Should Be $true
        $output.Contains("BasePriority") | Should Be $true
    }

    It "Should not show anything other than the requested props" {
        ( Get-Process | Format-List | Out-String) | Should Match "CPU"

        $output = ( Get-Process | Format-List -Property Name | Out-String)

        $output | Should Not Match "CPU"
        $output | Should Not Match "Id"
        $output | Should Not Match "Handle"

        # Testing each element of format-list, using a for-each loop since the Format-List is so opaque
        (Get-Process | Format-List -Property CPU | Out-String) -Split "`n" | 
            Where-Object {
                $_.trim() -ne ""
            } | 

            ForEach-Object{
                $_ | Should Match "CPU :"
            }

    }

    It "Should be able to take input without piping objects to it" {
       $output = { Format-List -InputObject $input }

       $output | Should Not Throw
       $output | Should Not BeNullOrEmpty

    }

}
