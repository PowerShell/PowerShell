# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Out-File DRT Unit Tests" -Tags "CI" {
    It "Should be able to write the contents into a file with -pspath" {
        $tempFile = Join-Path -Path $TestDrive -ChildPath "ExposeBug928965"
        { 1 | Out-File -PSPath $tempFile } | Should -Not -Throw
        $fileContents = Get-Content $tempFile
        $fileContents | Should -Be 1
        Remove-Item $tempFile -Force
    }

    It "Should be able to write the contents into a file with -pspath" {
        $tempFile = Join-Path -Path $TestDrive -ChildPath "outfileAppendTest.txt"
        { 'This is first line.' | Out-File $tempFile } | Should -Not -Throw
        { 'This is second line.' | Out-File -Append $tempFile } | Should -Not -Throw
        $tempFile | Should -FileContentMatch "first"
        $tempFile | Should -FileContentMatch "second"
        Remove-Item $tempFile -Force
    }
}

Describe "Out-File" -Tags "CI" {
    BeforeAll {
        if ($null -ne $PSStyle) {
            $outputRendering = $PSStyle.OutputRendering
            $PSStyle.OutputRendering = 'plaintext'
        }

        $expectedContent = "some test text"
        $inObject = New-Object psobject -Property @{text=$expectedContent}
        $testfile = Join-Path -Path $TestDrive -ChildPath outfileTest.txt
    }

    AfterAll {
        if ($null -ne $PSStyle) {
            $PSStyle.OutputRendering = $outputRendering
        }
    }

    AfterEach {
        Remove-Item -Path $testfile -Force
    }

    It "Should be able to be called without error" {
        { Out-File -FilePath $testfile }   | Should -Not -Throw
    }

    It "Should be able to accept string input via piping" {
        { $expectedContent | Out-File -FilePath $testfile } | Should -Not -Throw

        $actual = Get-Content $testfile

        $actual | Should -Be $expectedContent
    }

    It "Should be able to accept string input via the InputObject switch" {
        { Out-File -FilePath $testfile -InputObject $expectedContent } | Should -Not -Throw

        $actual = Get-Content $testfile

        $actual | Should -Be $expectedContent
    }

    It "Should be able to accept object input" {
        { $inObject | Out-File -FilePath $testfile } | Should -Not -Throw

        { Out-File -FilePath $testfile -InputObject $inObject } | Should -Not -Throw
    }

    It "Should not overwrite when the noclobber switch is used" {

        Out-File -FilePath $testfile -InputObject $inObject

        { Out-File -FilePath $testfile -InputObject $inObject -NoClobber -ErrorAction SilentlyContinue }   | Should -Throw "already exists."
        { Out-File -FilePath $testfile -InputObject $inObject -NoOverWrite -ErrorAction SilentlyContinue } | Should -Throw "already exists."

        $actual = Get-Content $testfile

        $actual[0] | Should -BeNullOrEmpty
        $actual[1] | Should -Match "text"
        $actual[2] | Should -Match "----"
        $actual[3] | Should -Match "some test text"
    }

    It "Should Append a new line when the append switch is used" {
        { Out-File -FilePath $testfile -InputObject $inObject }         | Should -Not -Throw
        { Out-File -FilePath $testfile -InputObject $inObject -Append } | Should -Not -Throw

        $actual = Get-Content $testfile

        $actual[0]  | Should -BeNullOrEmpty
        $actual[1]  | Should -Match "text"
        $actual[2]  | Should -Match "----"
        $actual[3]  | Should -Match "some test text"
        $actual[4]  | Should -BeNullOrEmpty
        $actual[5]  | Should -BeNullOrEmpty
        $actual[6]  | Should -Match "text"
        $actual[7]  | Should -Match "----"
        $actual[8]  | Should -Match "some test text"
        $actual[9]  | Should -BeNullOrEmpty
        $actual[10] | Should -BeNullOrEmpty
    }

    It "Should limit each line to the specified number of characters when the width switch is used on objects" {

        Out-File -FilePath $testfile -Width 10 -InputObject $inObject

        $actual = Get-Content $testfile

        $actual[0] | Should -BeNullOrEmpty
        $actual[1] | Should -BeExactly "text"
        $actual[2] | Should -BeExactly "----"
        $actual[3] | Should -BeExactly "some test`u{2026}" # ellipsis
    }

    It "Should allow the cmdlet to overwrite an existing read-only file" {
        # create a read-only text file
        { Out-File -FilePath $testfile -InputObject $inObject }                | Should -Not -Throw
        Set-ItemProperty -Path $testfile -Name IsReadOnly -Value $true

        # write information to the RO file
        { Out-File -FilePath $testfile -InputObject $inObject -Append -Force } | Should -Not -Throw

        $actual = Get-Content $testfile

        $actual[0]  | Should -BeNullOrEmpty
        $actual[1]  | Should -Match "text"
        $actual[2]  | Should -Match "----"
        $actual[3]  | Should -Match "some test text"
        $actual[4]  | Should -BeNullOrEmpty
        $actual[5]  | Should -BeNullOrEmpty
        $actual[6]  | Should -Match "text"
        $actual[7]  | Should -Match "----"
        $actual[8]  | Should -Match "some test text"
        $actual[9]  | Should -BeNullOrEmpty
        $actual[10] | Should -BeNullOrEmpty

        # reset to not read only so it can be deleted
        Set-ItemProperty -Path $testfile -Name IsReadOnly -Value $false
    }

    It "Should be able to use the 'Path' alias for the 'FilePath' parameter" {
        { Out-File -Path $testfile } | Should -Not -Throw
    }
}
