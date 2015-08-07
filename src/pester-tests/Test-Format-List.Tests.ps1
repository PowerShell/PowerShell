Describe "Test-Format-List" {
    It "Should call format list without error" {
        $input = New-Object PSObject
        Add-Member -InputObject $input -MemberType NoteProperty -Name testName -Value testValue

        { $input | Format-List } | Should Not Throw

    }

    It "Should be able to call the alias" {
        $input = New-Object PSObject
        Add-Member -InputObject $input -MemberType NoteProperty -Name testName -Value testValue

        { $input | fl } | Should Not Throw

    }

    It "Should have the same output whether choosing alias or not" {
        $input = New-Object PSObject
        Add-Member -InputObject $input -MemberType NoteProperty -Name testName -Value testValue

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
        <# the structure of the output of format-list, although iterable, is not a proper collection of 
           objects we can test

           gps | fl | ForEach-Object{$_.ToString()}  confirms that each item is a format object.

           (Get-Process | Format-List | Out-String).split("\n") | ForEach-Object { $_} will list objects,
           but not allow interaction with them.
        #>

        ( Get-Process | Format-List | Out-String).Contains("CPU") | Should Be $true


        $output = ( Get-Process | Format-List -Property Name | Out-String)

        $output.Contains("CPU")     | Should Be $false
        $output.Contains("Id")      | Should Be $false
        $output.Contains("Handle")  | Should Be $false
    }

    It "Should be able to take input without piping objects to it" {
        $input = New-Object PSObject
        Add-Member -InputObject $input -MemberType NoteProperty -Name testName -Value testValue

        $output = { Format-List -InputObject $input }

       $output | Should Not Throw
       $output | Should Not BeNullOrEmpty

    }

}
