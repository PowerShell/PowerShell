Describe "Validate basic FileSystem Cmdlets" -Tags "CI" {
    BeforeAll {
        $testDir = "testDir"
        $testFile = "testFile.txt"
        $restoreLocation = Get-Location
    }

    AfterAll {
        #restore the previous location
        Set-Location -Path $restoreLocation
    }

    BeforeEach {
        Set-Location -Path "TestDrive:\"
        New-Item -Path $testDir -ItemType Directory > $null
        New-Item -Path $testFile -ItemType File > $null
    }

    AfterEach {
        Set-Location -Path "TestDrive:\"
        Remove-Item -Path * -Recurse -Force -ErrorAction SilentlyContinue
    }

    It "Verify New-Item for directory" {
        $newDir = New-Item -Path "newTestDir" -ItemType Directory
        $directoryExists = Test-Path "newTestDir"
        $directoryExists | Should Be $true
        $newDir.Name | Should Be "newTestDir"
    }

    It "Verify New-Item for file" {
        $newFile = New-Item -Path "newTestFile.txt" -ItemType File
        $fileExists = Test-Path "newTestFile.txt"
        $fileExists | Should Be $true
        $newFile.Name | Should Be "newTestFile.txt"
    }

    It "Verify Remove-Item for directory" {
        $existsBefore = Test-Path $testDir
        Remove-Item -Path $testDir -Recurse -Force
        $existsAfter = Test-Path $testDir
        $existsBefore | Should Be $true
        $existsAfter | Should Be $false
    }

    It "Verify Remove-Item for file" {
        $existsBefore = Test-Path $testFile
        Remove-Item -Path $testFile -Force
        $existsAfter = Test-Path $testFile
        $existsBefore | Should Be $true
        $existsAfter | Should Be $false
    }

    It "Verify Copy-Item" {
        $newFile = Copy-Item -Path $testFile -Destination "copyFile.txt" -PassThru
        $fileExists = Test-Path "copyFile.txt"
        $fileExists | Should Be $true
        $newFile.Name | Should Be "copyFile.txt"
    }

    It "Verify Move-Item" {
        $newFile = Move-Item -Path $testFile -Destination "moveFile.txt" -PassThru
        $fileExists = Test-Path "moveFile.txt"
        $fileExists | Should Be $true
        $newFile.Name | Should Be "moveFile.txt"
    }

    It "Verify Get-ChildItem" {
        $dirContents = Get-ChildItem "."
        $dirContents.Count | Should Be 2
    }
}

Describe "Validate basic host navigation functionality" -Tags "CI" {
    BeforeAll {
        $restoreLocation = Get-Location
        Set-Location -Path "TestDrive:\"

        #build semi-complex directory structure to test navigation within
        $level1_0 = "Level1_0"
        $level2_0 = "Level2_0"
        $level2_1 = "Level2_1"
        New-Item -Path $level1_0 -ItemType Directory > $null
        New-Item -Path (Join-Path $level1_0 $level2_0) -ItemType Directory > $null
        New-Item -Path (Join-Path $level1_0 $level2_1) -ItemType Directory > $null
    }

    AfterAll {
        #restore the previous location
        Set-Location -Path $restoreLocation
    }

    BeforeEach {
        Set-Location -Path "TestDrive:\"
    }

    It "Verify Get-Location and Set-Location" {
        $currentLoc = Get-Location
        Set-Location $level1_0
        $level1Loc = Get-Location
        Set-Location $level2_0
        $level2Loc = Get-Location
        $currentLoc.Path | Should Be (Join-Path "TestDrive:" "")
        $level1Loc.Path | Should Be (Join-Path "TestDrive:" $level1_0)
        $level2Loc.Path | Should Be (Join-Path (Join-Path "TestDrive:" $level1_0) $level2_0)
    }

    It "Verify Push-Location and Pop-Location" {
        #push a bunch of locations
        Push-Location
        $push0 = Get-Location
        Set-Location $level1_0
        Push-Location
        $push1 = Get-Location
        Set-Location $level2_0
        Push-Location
        $push2 = Get-Location

        #navigate back home to change path out of all pushed locations
        Set-Location "TestDrive:\"

        #pop locations off
        Pop-Location
        $pop0 = Get-Location
        Pop-Location
        $pop1 = Get-Location
        Pop-Location
        $pop2 = Get-Location

        $pop0.Path | Should Be $push2.Path
        $pop1.Path | Should Be $push1.Path
        $pop2.Path | Should Be $push0.Path
    }
}

Describe "Validate basic Content Cmdlets for the FileSystem provider" -Tags "CI" {
    BeforeAll {
        $testFile = "testFile.txt"
        $restoreLocation = Get-Location
        Set-Location "TestDrive:\"
    }

    AfterAll {
        #restore the previous location
        Set-Location -Path $restoreLocation
    }

    BeforeEach {
        New-Item -Path $testFile -ItemType File > $null
    }

    AfterEach {
        Remove-Item -Path $testFile -Force -ErrorAction SilentlyContinue
    }

    It "Set-Content to a file" {
        $content =  Set-Content -Value "some content" -Path $testFile -PassThru
        $content | Should BeExactly "some content"
    } 

    It "Add-Content to a file" {
        $content = Set-Content -Value "some content" -Path $testFile -PassThru
        $addContent = Add-Content -Value " more content" -Path $testFile -PassThru
        $fullContent = Get-Content -Path $testFile
        $content | Should Match "some content"
        $addContent | Should Match "more content"
        ($fullContent[0] + $fullContent[1]) | Should Match "some content more content"
    }

    It "Clear-Content of a file" {
        Set-Content -Value "some content" -Path $testFile
        $contentBefore = Get-Content -Path $testFile
        Clear-Content -Path $testFile
        $contentAfter = Get-Content -Path $testFile
        $contentBefore.Count | Should Be 1
        $contentAfter.Count | Should Be 0
    }
}

Describe "Validate Resolve-Path Cmdlet Parameters" -Tags "CI" {
    BeforeAll {
        $restoreLocation = Get-Location
        Set-Location "TestDrive:\"
    }

    AfterAll {
        #restore the previous location
        Set-Location -Path $restoreLocation
    }

    It "Verify HOME" {
        $homePath = $HOME
        $tildePath = (resolve-path -Path ~).Path
        $homePath | Should Be $tildePath
    }

    It "Verify relative" {
        $relativePath = Resolve-Path -Path . -Relative
        $relativePath | Should Be (Join-Path "." "")
    }
}

Describe "Validate Basic Path Cmdlets" -Tags "CI" {
    It "Verify Convert-Path" {
        $result = Convert-Path "."
        $result | Should Be (Get-Location).Path
    }

    It "Verify Join-Path" {
        $result = Join-Path -Path "TestDrive:" -ChildPath temp

        if ($IsWindows) {
            $result | Should BeExactly "TestDrive:\temp"
        }
        else {
            $result | Should BeExactly "TestDrive:/temp"
        }
    }

    It "Verify Split-Path" {
        $testPath = Join-Path "TestDrive:" "MyTestFile.txt"
        $result = Split-Path $testPath -qualifier
        $result | Should BeExactly "TestDrive:"
    }

    It "Verify Test-Path" {
        $result = Test-Path $HOME
        $result | Should Be $true
    }
}
