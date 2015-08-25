Describe "Test-New-Item" {
    $tmpDirectory = "/tmp"
    $testfile = "testfile.txt"
    $testfolder = "newDirectory"

    $FullyQualifiedFile = $tmpDirectory + "/" + $testfile
    $FullyQualifiedFolder = $tmpDirectory +"/" + $testfolder

    AfterEach {
        if (Test-Path $FullyQualifiedFile)
        {
            { Remove-Item $FullyQualifiedFile -Force} | Should Not Throw
        }

        if (Test-Path $FullyQualifiedFolder)
        {
            { Remove-Item $FullyQualifiedFolder -Recurse -Force } | Should Not Throw
        }
    }

    It "should call the function without error" {
        { New-Item -Name $testfile -Path $tmpDirectory -ItemType file } | Should Not Throw
    }

    It "Should create a file without error" {
        New-Item -Name $testfile -Path $tmpDirectory -ItemType file

        Test-Path $FullyQualifiedFile | Should Be $true
    }

    It "Should create a folder without an error" {
        New-Item -Name newDirectory -Path $tmpDirectory -ItemType directory

        Test-Path $FullyQualifiedFolder | Should Be $true
    }

    It "Should create a file using the ni alias" {
        ni -Name $testfile -Path $tmpDirectory -ItemType file

        Test-Path $FullyQualifiedFile | Should Be $true
    }

    It "Should create a file using the Type alias instead of ItemType" {
        New-Item -Name $testfile -Path $tmpDirectory -Type file

        Test-Path $FullyQualifiedFile | Should Be $true
    }

    It "Should create a file with sample text inside the file using the Value switch" {
        $expected = "This is test string"
        New-Item -Name $testfile -Path $tmpDirectory -ItemType file -Value $expected

        Test-Path $FullyQualifiedFile | Should Be $true

        Get-Content $FullyQualifiedFile | Should Be $expected
    }

    It "Should not create a file when the Name switch is not used and only a directory specified" {
        #errorAction used because permissions issue in windows
        New-Item -Path $tmpDirectory -ItemType file -ErrorAction SilentlyContinue

        Test-Path $FullyQualifiedFile | Should Be $false

    }

    It "Should create a file when the Name switch is not used but a fully qualified path is specified" {
        New-Item -Path $FullyQualifiedFile -ItemType file 

        Test-Path $FullyQualifiedFile | Should Be $true
    }

    It "Should be able to create a multiple items in different directories" {
        $FullyQualifiedFile2 = $tmpDirectory + "/" + "test2.txt"
        New-Item -ItemType file -Path $FullyQualifiedFile, $FullyQualifiedFile2

        Test-Path $FullyQualifiedFile  | Should Be $true
        Test-Path $FullyQualifiedFile2 | Should Be $true

        Remove-Item $FullyQualifiedFile2
    }
}
