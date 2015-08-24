Describe "Test-Out-File" {
    $a = "some test text"
    $b = New-Object psobject -Property @{text=$a}
    $Testfile = "/tmp/outfileTest.txt"

    BeforeEach {
        if (Test-Path -Path $testfile)
        {
            Set-ItemProperty -Path $testfile -Name IsReadOnly -Value $false
            rm $testfile
        }
    }

    AfterEach {
        # implement in *nix to remove test files after each test if they exist
        rm $testfile
    }

    It "Should be able to be called without error" {
         { Out-File -FilePath $testfile }   | Should Not Throw
    }

    It "Should be able to accept string input" {
        { $a | Out-File -FilePath $testfile } | Should Not Throw

        { Out-File -FilePath $testfile -InputObject $a } | Should Not Throw
    }

    It "Should be able to accept object input" {
        { $b | Out-File -FilePath $testfile } | Should Not Throw

        { Out-File -FilePath $testfile -InputObject $b } | Should Not Throw
    }

    It "Should not overwrite when the noclobber switch is used" {

        Out-File -FilePath $testfile -InputObject $b

        { Out-File -FilePath $testfile -InputObject $b -NoClobber -ErrorAction SilentlyContinue }   | Should Throw "already exists."
        { Out-File -FilePath $testfile -InputObject $b -NoOverWrite -ErrorAction SilentlyContinue } | Should Throw "already exists."

        $actual = Get-Content $testfile

        $actual[0] | Should Be ""
        $actual[1] | Should Match "text"
        $actual[2] | Should Match "----"
        $actual[3] | Should Match "some test text"
    }

    It "Should Append a new line when the append switch is used" {
        { Out-File -FilePath $testfile -InputObject $b }         | Should Not Throw
        { Out-File -FilePath $testfile -InputObject $b -Append } | Should Not Throw

        $actual = Get-Content $testfile

        $actual[0]  | Should Be ""
        $actual[1]  | Should Match "text"
        $actual[2]  | Should Match "----"
        $actual[3]  | Should Match "some test text"
        $actual[4]  | Should Be ""
        $actual[5]  | Should Be ""
        $actual[6]  | Should Be ""
        $actual[7]  | Should Match "text"
        $actual[8]  | Should Match "----"
        $actual[9]  | Should Match "some test text"
        $actual[10] | Should Be ""
        $actual[11] | Should Be ""

    }

    It "Should limit each line to the specified number of characters when the width switch is used on objects" {

        Out-File -FilePath $testfile -Width 10 -InputObject $b

        $actual = Get-Content $testfile

        $actual[0] | Should Be ""
        $actual[1] | Should Be "text      "
        $actual[2] | Should Be "----      "
        $actual[3] | Should Be "some te..."

    }

    It "Should allow the cmdlet to overwrite an existing read-only file" {
        # create a read-only text file
        { Out-File -FilePath $testfile -InputObject $b }                | Should Not Throw
        Set-ItemProperty -Path $testfile -Name IsReadOnly -Value $true

        # write information to the RO file
        { Out-File -FilePath $testfile -InputObject $b -Append -Force } | Should Not Throw

        $actual = Get-Content $testfile

        $actual[0]  | Should Be ""
        $actual[1]  | Should Match "text"
        $actual[2]  | Should Match "----"
        $actual[3]  | Should Match "some test text"
        $actual[4]  | Should Be ""
        $actual[5]  | Should Be ""
        $actual[6]  | Should Be ""
        $actual[7]  | Should Match "text"
        $actual[8]  | Should Match "----"
        $actual[9]  | Should Match "some test text"
        $actual[10] | Should Be ""
        $actual[11] | Should Be ""

        # reset to not read only so it can be deleted
        Set-ItemProperty -Path $testfile -Name IsReadOnly -Value $false
    }
}
