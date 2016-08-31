Describe "Out-File DRT Unit Tests" -Tags "CI" {
    It "Should be able to write the contents into a file with -pspath" {
        $tempFile = Join-Path -Path $TestDrive -ChildPath "ExposeBug928965"
        { 1 | Out-File -PSPath $tempFile } | Should Not Throw
        $fileContents = Get-Content $tempFile
        $fileContents | Should be 1
        Remove-Item $tempFile -Force
    }

    It "Should be able to write the contents into a file with -pspath" {
        $tempFile = Join-Path -Path $TestDrive -ChildPath "outfileAppendTest.txt"
        { 'This is first line.' | out-file $tempFile } | Should Not Throw
        { 'This is second line.' | out-file -append $tempFile } | Should Not Throw
        $tempFile |Should Contain "first"
        $tempFile |Should Contain "second"
        Remove-Item $tempFile -Force
    }
}

Describe "Out-File" -Tags "CI" {
    $expectedContent = "some test text"
    $inObject = New-Object psobject -Property @{text=$expectedContent}
    $testfile = Join-Path -Path $TestDrive -ChildPath outfileTest.txt

    AfterEach {
	Remove-Item -Path $testfile -Force
    }

    It "Should be able to be called without error" {
	{ Out-File -FilePath $testfile }   | Should Not Throw
    }

    It "Should be able to accept string input via piping" {
	{ $expectedContent | Out-File -FilePath $testfile } | Should Not Throw

	$actual = Get-Content $testfile

	$actual | Should Be $expectedContent
    }

    It "Should be able to accept string input via the InputObject switch" {
	{ Out-File -FilePath $testfile -InputObject $expectedContent } | Should Not Throw

	$actual = Get-Content $testfile

	$actual | Should Be $expectedContent
    }

    It "Should be able to accept object input" {
	{ $inObject | Out-File -FilePath $testfile } | Should Not Throw

	{ Out-File -FilePath $testfile -InputObject $inObject } | Should Not Throw
    }

    It "Should not overwrite when the noclobber switch is used" {

	Out-File -FilePath $testfile -InputObject $inObject

	{ Out-File -FilePath $testfile -InputObject $inObject -NoClobber -ErrorAction SilentlyContinue }   | Should Throw "already exists."
	{ Out-File -FilePath $testfile -InputObject $inObject -NoOverWrite -ErrorAction SilentlyContinue } | Should Throw "already exists."

	$actual = Get-Content $testfile

	$actual[0] | Should Be ""
	$actual[1] | Should Match "text"
	$actual[2] | Should Match "----"
	$actual[3] | Should Match "some test text"
    }

    It "Should Append a new line when the append switch is used" {
	{ Out-File -FilePath $testfile -InputObject $inObject }         | Should Not Throw
	{ Out-File -FilePath $testfile -InputObject $inObject -Append } | Should Not Throw

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

	Out-File -FilePath $testfile -Width 10 -InputObject $inObject

	$actual = Get-Content $testfile

	$actual[0] | Should Be ""
	$actual[1] | Should Be "text      "
	$actual[2] | Should Be "----      "
	$actual[3] | Should Be "some te..."

    }

    It "Should allow the cmdlet to overwrite an existing read-only file" {
	# create a read-only text file
	{ Out-File -FilePath $testfile -InputObject $inObject }                | Should Not Throw
	Set-ItemProperty -Path $testfile -Name IsReadOnly -Value $true

	# write information to the RO file
	{ Out-File -FilePath $testfile -InputObject $inObject -Append -Force } | Should Not Throw

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
