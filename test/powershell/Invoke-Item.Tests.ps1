function Clean-State
{
    if (Test-Path $FullyQualifiedLink)
    {
	    Remove-Item $FullyQualifiedLink -Force
    }

    if (Test-Path $FullyQualifiedFile)
    {
	    Remove-Item $FullyQualifiedFile -Force
    }

    if (Test-Path $FullyQualifiedFolder)
    {
	    Remove-Item $FullyQualifiedFolder -Force
    }
}

Describe "New-Item" {
    $tmpDirectory         = $TestDrive
    $testfile             = "testfile.txt"
    $testfolder           = "newDirectory"
    $testlink             = "testlink"
    $FullyQualifiedFile   = Join-Path -Path $tmpDirectory -ChildPath $testfile
    $FullyQualifiedFolder = Join-Path -Path $tmpDirectory -ChildPath $testfolder
    $FullyQualifiedLink   = Join-Path -Path $tmpDirectory -ChildPath $testlink

    BeforeEach {
	    Clean-State
    }

    It "Should call the function without error" {
	{ 
        New-Item -Name $testfile -Path $tmpDirectory -ItemType file } | Should Not Throw
    }

    It "Should invoke a text file without error" {
	    New-Item -Name $testfile -Path $tmpDirectory -ItemType file
	    Test-Path $FullyQualifiedFile | Should Be $true
        $fileInfo = Get-ChildItem $FullyQualifiedFile
        $fileInfo.Target | Should Be $null
        $fileInfo.LinkType | Should Be $null
        Invoke-Item $FullyQualifiedFile | Should Not throw
    }

}