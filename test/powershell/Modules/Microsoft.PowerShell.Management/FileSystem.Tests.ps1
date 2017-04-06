Import-Module $PSScriptRoot\..\..\Common\Test.Helpers.psm1
Describe "Basic FileSystem Provider Tests" -Tags "CI" {
    BeforeAll {
        $testDir = "TestDir"
        $testFile = "TestFile.txt"
        $restoreLocation = Get-Location
    }

    AfterAll {
        #restore the previous location
        Set-Location -Path $restoreLocation
    }

    BeforeEach {
        Set-Location -Path "TestDrive:\"
    }

    Context "Validate basic FileSystem Cmdlets" {
        BeforeAll {
            $newTestDir = "NewTestDir"
            $newTestFile = "NewTestFile.txt"
            $testContent = "Some Content"
            $testContent2 = "More Content"
            $reservedNames = "CON", "PRN", "AUX", "CLOCK$", "NUL",
                             "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                             "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        }

        BeforeEach {
            New-Item -Path $testDir -ItemType Directory > $null
            New-Item -Path $testFile -ItemType File > $null
        }

        AfterEach {
            Set-Location -Path "TestDrive:\"
            Remove-Item -Path * -Recurse -Force -ErrorAction SilentlyContinue
        }

        It "Verify New-Item for directory" {
            $newDir = New-Item -Path $newTestDir -ItemType Directory
            $directoryExists = Test-Path $newTestDir
            $directoryExists | Should Be $true
            $newDir.Name | Should Be $newTestDir
        }

        It "Verify New-Item for file" {
            $newFile = New-Item -Path $newTestFile -ItemType File
            $fileExists = Test-Path $newTestFile
            $fileExists | Should Be $true
            $newFile.Name | Should Be $newTestFile
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
            $newFile = Copy-Item -Path $testFile -Destination $newTestFile -PassThru
            $fileExists = Test-Path $newTestFile
            $fileExists | Should Be $true
            $newFile.Name | Should Be $newTestFile
        }

        It "Verify Move-Item" {
            $newFile = Move-Item -Path $testFile -Destination $newTestFile -PassThru
            $fileExists = Test-Path $newTestFile
            $fileExists | Should Be $true
            $newFile.Name | Should Be $newTestFile
        }

        It "Verify Get-ChildItem" {
            $dirContents = Get-ChildItem "."
            $dirContents.Count | Should Be 2
        }

        It "Set-Content to a file" {
            $content =  Set-Content -Value $testContent -Path $testFile -PassThru
            $content | Should BeExactly $testContent
        }

        It "Add-Content to a file" {
            $content = Set-Content -Value $testContent -Path $testFile -PassThru
            $addContent = Add-Content -Value $testContent2 -Path $testFile -PassThru
            $fullContent = Get-Content -Path $testFile
            $content | Should Match $testContent
            $addContent | Should Match $testContent2
            ($fullContent[0] + $fullContent[1]) | Should Match ($testContent + $testContent2)
        }

        It "Clear-Content of a file" {
            Set-Content -Value $testContent -Path $testFile
            $contentBefore = Get-Content -Path $testFile
            Clear-Content -Path $testFile
            $contentAfter = Get-Content -Path $testFile
            $contentBefore.Count | Should Be 1
            $contentAfter.Count | Should Be 0
        }

         It "Copy-Item on Windows rejects Windows reserved device names" -Skip:(-not $IsWindows) {
             foreach ($deviceName in $reservedNames)
             {
                { Copy-Item -Path $testFile -Destination $deviceName -ErrorAction Stop } | ShouldBeErrorId "CopyError,Microsoft.PowerShell.Commands.CopyItemCommand"
             }
         }

         It "Move-Item on Windows rejects Windows reserved device names" -Skip:(-not $IsWindows) {
             foreach ($deviceName in $reservedNames)
             {
                { Move-Item -Path $testFile -Destination $deviceName -ErrorAction Stop } | ShouldBeErrorId "MoveError,Microsoft.PowerShell.Commands.MoveItemCommand"
             }
         }

         It "Rename-Item on Windows rejects Windows reserved device names" -Skip:(-not $IsWindows) {
             foreach ($deviceName in $reservedNames)
             {
                { Rename-Item -Path $testFile -NewName $deviceName -ErrorAction Stop } | ShouldBeErrorId "RenameError,Microsoft.PowerShell.Commands.RenameItemCommand"
             }
         }

         It "Copy-Item on Unix succeeds with Windows reserved device names" -Skip:($IsWindows) {
             foreach ($deviceName in $reservedNames)
             {
                Copy-Item -Path $testFile -Destination $deviceName -Force -ErrorAction SilentlyContinue
                Test-Path $deviceName | Should Be $true
             }
         }

         It "Move-Item on Unix succeeds with Windows reserved device names" -Skip:($IsWindows) {
             foreach ($deviceName in $reservedNames)
             {
                Move-Item -Path $testFile -Destination $deviceName -Force -ErrorAction SilentlyContinue
                Test-Path $deviceName | Should Be $true
                New-Item -Path $testFile -ItemType File -Force -ErrorAction SilentlyContinue
             }
         }

         It "Rename-Item on Unix succeeds with Windows reserved device names" -Skip:($IsWindows) {
             foreach ($deviceName in $reservedNames)
             {
                Rename-Item -Path $testFile -NewName $deviceName -Force -ErrorAction SilentlyContinue
                Test-Path $deviceName | Should Be $true
                New-Item -Path $testFile -ItemType File -Force -ErrorAction SilentlyContinue
             }
         }
    }

    Context "Validate basic host navigation functionality" {
        BeforeAll {
            #build semi-complex directory structure to test navigation within
            $level1_0 = "Level1_0"
            $level2_0 = "Level2_0"
            $level2_1 = "Level2_1"
            $root = Join-Path "TestDrive:" "" #adds correct / or \
            $level1_0Full = Join-Path $root $level1_0
            $level2_0Full = Join-Path $level1_0Full $level2_0
            $level2_1Full = Join-Path $level1_0Full $level2_1
            New-Item -Path $level1_0Full -ItemType Directory > $null
            New-Item -Path $level2_0Full -ItemType Directory > $null
            New-Item -Path $level2_1Full -ItemType Directory > $null
        }

        It "Verify Get-Location and Set-Location" {
            $currentLoc = Get-Location
            Set-Location $level1_0
            $level1Loc = Get-Location
            Set-Location $level2_0
            $level2Loc = Get-Location
            $currentLoc.Path | Should Be $root
            $level1Loc.Path | Should Be $level1_0Full
            $level2Loc.Path | Should Be $level2_0Full
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

    Context "Validate Basic Path Cmdlets" {
        It "Verify Convert-Path" {
            $result = Convert-Path "."
            ($result.TrimEnd('/\')) | Should Be "$TESTDRIVE"
        }

        It "Verify Join-Path" {
            $result = Join-Path -Path "TestDrive:" -ChildPath "temp"

            if ($IsWindows) {
                $result | Should BeExactly "TestDrive:\temp"
            }
            else {
                $result | Should BeExactly "TestDrive:/temp"
            }
        }

        It "Verify Split-Path" {
            $testPath = Join-Path "TestDrive:" "MyTestFile.txt"
            $result = Split-Path $testPath -Qualifier
            $result | Should BeExactly "TestDrive:"
        }

        It "Verify Test-Path" {
            $result = Test-Path $HOME
            $result | Should Be $true
        }

        It "Verify HOME" {
            $homePath = $HOME
            $tildePath = (Resolve-Path -Path ~).Path
            $homePath | Should Be $tildePath
        }
    }
}

Describe "Extended FileSystem Item/Content Cmdlet Provider Tests" -Tags "Feature" {
    BeforeAll {
        $testDir = "testDir"
        $testFile = "testFile.txt"
        $testFile2 = "testFile2.txt"
        $testContent = "Test 1"
        $testContent2 = "Test 2"
        $restoreLocation = Get-Location
    }

    AfterAll {
        #restore the previous location
        Set-Location -Path $restoreLocation
    }

    BeforeEach {
        Set-Location -Path "TestDrive:\"
        New-Item -Path $testFile -ItemType File > $null
        New-Item -Path $testFile2 -ItemType File > $null
    }

    AfterEach {
        Set-Location -Path "TestDrive:\"
        Remove-Item -Path * -Recurse -Force -ErrorAction SilentlyContinue
    }

    Context "Valdiate New-Item parameters" {
        BeforeEach {
            #remove every file so that New-Item can be validated
            Remove-Item -Path * -Recurse -Force -ErrorAction SilentlyContinue
        }

        It "Verify Directory + Whatif" {
            New-Item -Path . -ItemType Directory -Name $testDir -WhatIf > $null
            try {
                Get-Item -Path $testDir -ErrorAction Stop
                throw "Expected exception not thrown"
            }
            catch { $_.FullyQualifiedErrorId | Should Be "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand" }
        }

        It "Verify Directory + Confirm bypass" {
            $result = New-Item -Path . -ItemType Directory -Name $testDir -Confirm:$false
            $result.Name | Should Be $testDir
        }

        It "Verify Directory + Force" {
            New-Item -Path . -ItemType Directory -Name $testDir > $null
            $result = New-Item -Path . -ItemType Directory -Name $testDir -Force #would normally fail without force
            $result.Name | Should Be $testDir
        }

        It "Verify File + Value" {
            $result = New-Item -Path . -ItemType File -Name $testFile -Value "Some String"
            $content = Get-Content -Path $testFile
            $result.Name | Should Be $testFile
            $content | Should Be "Some String"
        }
    }

    Context "Valdiate Get-Item parameters" {
        It "Verify Force" {
            $result = Get-Item -Path $testFile -Force
            $result.Name | Should Be $testFile
        }

        It "Verify Path Wildcard" {
            $result = Get-Item -Path "*2.txt"
            $result.Name | Should Be $testFile2
        }

        It "Verify Include" {
            $result = Get-Item -Path "TestDrive:\*" -Include "*2.txt"
            $result.Name | Should Be $testFile2
        }

        It "Verify Include and Exclude Intersection" {
            $result = Get-Item -Path "TestDrive:\*" -Include "*.txt" -Exclude "*2*"
            $result.Name | Should Be $testFile
        }

        It "Verify Filter" {
            $result = Get-Item -Path "TestDrive:\*" -filter "*2.txt"
            $result.Name | Should Be $testFile2
        }
    }

    Context "Valdiate Move-Item parameters" {
        BeforeAll {
            $altTestFile = "movedFile.txt"
        }

        BeforeEach {
            New-Item -Path $testFile -ItemType File > $null
            New-Item -Path $testFile2 -ItemType File > $null
        }

        AfterEach {
            Remove-Item -Path * -Recurse -Force -ErrorAction SilentlyContinue
        }

        It "Verify WhatIf" -Skip:$true { #Skipped until issue #2385 gets resolved
            Move-Item -Path $testFile -Destination $altTestFile -WhatIf
            try {
                Get-Item -Path $altTestFile -ErrorAction Stop
                throw "Expected exception not thrown"
            }
            catch { $_.FullyQualifiedErrorId | Should Be "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand" }
        }

        It "Verify Include and Exclude Intersection" -Skip:$true { #Skipped until issue #2385 gets resolved
            Move-Item -Path "TestDrive:\*" -Destination $altTestFile -Include "*.txt" -Exclude "*2*"
            $file1 = Get-Item $testFile2 -ErrorAction SilentlyContinue
            $file2 = Get-Item $altTestFile -ErrorAction SilentlyContinue
            $file1 | Should BeNullOrEmpty
            $file2.Name | Should Be $altTestFile
        }

        It "Verify Filter" -Skip:$true { #Skipped until issue #2385 gets resolved
            Move-Item -Path "TestDrive:\*" -Filter "*2.txt" -Destination $altTestFile
            $file1 = Get-Item $testFile2 -ErrorAction SilentlyContinue
            $file2 = Get-Item $altTestFile -ErrorAction SilentlyContinue
            $file1 | Should BeNullOrEmpty
            $file2.Name | Should Be $altTestFile
        }
    }

    Context "Valdiate Rename-Item parameters" {
        BeforeAll {
            $newFile = "NewName.txt"
        }

        It "Verify WhatIf" {
            Rename-Item -Path $testFile -NewName $newFile -WhatIf
            try {
                Get-Item -Path $newFile -ErrorAction Stop
                throw "Expected exception not thrown"
            }
            catch { $_.FullyQualifiedErrorId | Should Be "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand" }
        }

        It "Verify Confirm can be bypassed" {
            Rename-Item -Path $testFile -NewName $newFile -Confirm:$false
            $file1 = Get-Item -Path $testFile -ErrorAction SilentlyContinue
            $file2 = Get-Item -Path $newFile -ErrorAction SilentlyContinue
            $file1 | Should BeNullOrEmpty
            $file2.Name | Should Be $newFile
        }
    }

    Context "Valdiate Remove-Item parameters" {
        It "Verify WhatIf" {
            Remove-Item $testFile -WhatIf
            $result = Get-Item $testFile
            $result.Name | Should Be $testFile
        }

        It "Verify Confirm can be bypassed" {
            Remove-Item $testFile -Confirm:$false
            try {
                Get-Item $testFile -ErrorAction Stop
                throw "Expected exception not thrown"
            }
            catch { $_.FullyQualifiedErrorId | Should Be "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand" }
        }

        It "Verify LiteralPath" {
            Remove-Item -LiteralPath "TestDrive:\$testFile" -Recurse
            try {
                Get-Item $testFile -ErrorAction Stop
                throw "Expected exception not thrown"
            }
            catch { $_.FullyQualifiedErrorId | Should Be "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand" }
        }

        It "Verify Filter" {
            Remove-Item "TestDrive:\*" -Filter "*.txt"
            $result = Get-Item "*.txt"
            $result | Should BeNullOrEmpty
        }

        It "Verify Include" {
            Remove-Item "TestDrive:\*" -Include "*2.txt"
            try {
                Get-Item $testFile2 -ErrorAction Stop
                throw "Expected exception not thrown"
            }
            catch { $_.FullyQualifiedErrorId | Should Be "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand" }
        }

        It "Verify Include and Exclude Intersection" {
            Remove-Item "TestDrive:\*" -Include "*.txt" -exclude "*2*"
            $file1 = Get-Item $testFile -ErrorAction SilentlyContinue
            $file2 = Get-Item $testFile2 -ErrorAction SilentlyContinue
            $file1 | Should BeNullOrEmpty
            $file2.Name | Should Be $testFile2
        }
    }

    Context "Valdiate Set-Content parameters" {
        It "Validate Array Input for Path and Value" {
            Set-Content -Path @($testFile,$testFile2) -Value @($testContent,$testContent2)
            $content1 = Get-Content $testFile
            $content2 = Get-Content $testFile2
            $content1 | Should Be $content2
            ($content1[0] + $content1[1]) | Should Be ($testContent + $testContent2)
        }

        It "Validate LiteralPath" {
            Set-Content -LiteralPath "TestDrive:\$testFile" -Value $testContent
            $content = Get-Content $testFile
            $content | Should Be $testContent
        }

        It "Validate Confirm can be bypassed" {
            Set-Content -Path $testFile -Value $testContent -Confirm:$false
            $content = Get-Content $testFile
            $content | Should Be $testContent
        }

        It "Validate WhatIf" {
            Set-Content -Path $testFile -Value $testContent -WhatIf
            $content = Get-Content $testFile
            $content | Should BeNullOrEmpty
        }

        It "Validate Include" {
            Set-Content -Path "TestDrive:\*" -Value $testContent -Include "*2.txt"
            $content1 = Get-Content $testFile
            $content2 = Get-Content $testFile2
            $content1 | Should BeNullOrEmpty
            $content2 | Should Be $testContent
        }

        It "Validate Exclude" {
            Set-Content -Path "TestDrive:\*" -Value $testContent -Exclude "*2.txt"
            $content1 = Get-Content $testFile
            $content2 = Get-Content $testFile2
            $content1 | Should Be $testContent
            $content2 | Should BeNullOrEmpty
        }

        It "Validate Filter" {
            Set-Content -Path "TestDrive:\*" -Value $testContent -Filter "*2.txt"
            $content1 = Get-Content $testFile
            $content2 = Get-Content $testFile2
            $content1 | Should BeNullOrEmpty
            $content2 | Should Be $testContent
        }
    }

    Context "Valdiate Get-Content parameters" {
        BeforeEach {
            Set-Content -Path $testFile -Value $testContent
            Set-Content -Path $testFile2 -Value $testContent2
        }

        It "Validate Array Input for Path" {
            $result = Get-Content -Path @($testFile,$testFile2)
            $result[0] | Should Be $testContent
            $result[1] | Should Be $testContent2
        }

        It "Validate Include" {
            $result = Get-Content -Path "TestDrive:\*" -Include "*2.txt"
            $result | Should Be $testContent2
        }

        It "Validate Exclude" {
            $result = Get-Content -Path "TestDrive:\*" -Exclude "*2.txt"
            $result | Should Be $testContent
        }

        It "Validate Filter" {
            $result = Get-Content -Path "TestDrive:\*" -Filter "*2.txt"
            $result | Should Be $testContent2
        }

        It "Validate ReadCount" {
            Set-Content -Path $testFile -Value "Test Line 1`nTest Line 2`nTest Line 3`nTest Line 4`nTest Line 5`nTest Line 6"
            $result = (Get-Content -Path $testFile -ReadCount 2)
            $result[0][0] | Should Be "Test Line 1"
            $result[0][1] | Should Be "Test Line 2"
            $result[1][0] | Should Be "Test Line 3"
            $result[1][1] | Should Be "Test Line 4"
            $result[2][0] | Should Be "Test Line 5"
            $result[2][1] | Should Be "Test Line 6"
        }

        It "Validate TotalCount" {
            Set-Content -Path $testFile -Value "Test Line 1`nTest Line 2`nTest Line 3`nTest Line 4`nTest Line 5`nTest Line 6"
            $result = Get-Content -Path $testFile -TotalCount 4
            $result[0] | Should Be "Test Line 1"
            $result[1] | Should Be "Test Line 2"
            $result[2] | Should Be "Test Line 3"
            $result[3] | Should Be "Test Line 4"
            $result[4] | Should BeNullOrEmpty
        }

        It "Validate Tail" {
            Set-Content -Path $testFile -Value "Test Line 1`nTest Line 2`nTest Line 3`nTest Line 4`nTest Line 5`nTest Line 6"
            $result = Get-Content -Path $testFile -Tail 2
            $result[0] | Should Be "Test Line 5"
            $result[1] | Should Be "Test Line 6"
            $result[2] | Should BeNullOrEmpty
        }
    }
}

Describe "Extended FileSystem Path/Location Cmdlet Provider Tests" -Tags "Feature" {
    BeforeAll {
        $testDir = "testDir"
        $testFile = "testFile.txt"
        $testFile2 = "testFile2.txt"
        $testContent = "Test 1"
        $testContent2 = "Test 2"
        $restoreLocation = Get-Location

        #build semi-complex directory structure to test navigation within
        Set-Location -Path "TestDrive:\"
        $level1_0 = "Level1_0"
        $level2_0 = "Level2_0"
        $level2_1 = "Level2_1"
        $fileExt = ".ext"
        $root = Join-Path "TestDrive:" "" #adds correct / or \
        $level1_0Full = Join-Path $root $level1_0
        $level2_0Full = Join-Path $level1_0Full $level2_0
        $level2_1Full = Join-Path $level1_0Full $level2_1
        New-Item -Path $level1_0Full -ItemType Directory > $null
        New-Item -Path $level2_0Full -ItemType Directory > $null
        New-Item -Path $level2_1Full -ItemType Directory > $null
    }

    AfterAll {
        #restore the previous location
        Set-Location -Path $restoreLocation
    }

    BeforeEach {
        Set-Location -Path "TestDrive:\"
    }

    Context "Validate Resolve-Path Cmdlet Parameters" {
        It "Verify LiteralPath" {
            $result = Resolve-Path -LiteralPath "TestDrive:\"
            ($result.Path.TrimEnd('/\')) | Should Be "TestDrive:"
        }

        It "Verify relative" {
            $relativePath = Resolve-Path -Path . -Relative
            $relativePath | Should Be (Join-Path "." "")
        }
    }

    Context "Validate Join-Path Cmdlet Parameters" {
        It "Validate Resolve" {
            $result = Join-Path -Path . -ChildPath $level1_0 -Resolve
            if ($IsWindows) {
                $result | Should BeExactly "TestDrive:\$level1_0"
            }
            else {
                $result | Should BeExactly "TestDrive:/$level1_0"
            }
        }
    }

    Context "Validate Split-Path Cmdlet Parameters" {
        It "Validate Parent" {
            $result = Split-Path -Path $level1_0Full -Parent -Resolve
            ($result.TrimEnd('/\')) | Should Be "TestDrive:"
        }

        It "Validate IsAbsolute" {
            $resolved = Split-Path -Path . -Resolve -IsAbsolute
            $unresolved = Split-Path -Path . -IsAbsolute
            $resolved | Should Be $true
            $unresolved | Should Be $false
        }

        It "Validate Leaf" {
            $result = Split-Path -Path $level1_0Full -Leaf
            $result | Should Be $level1_0
        }
        
        It 'Validate LeafBase' {
            $result = Split-Path -Path "$level2_1Full$fileExt" -LeafBase
            $result | Should Be $level2_1
        }

        It 'Validate LeafBase is not over-zealous' {
            
            $result = Split-Path -Path "$level2_1Full$fileExt$fileExt" -LeafBase
            $result | Should Be "$level2_1$fileExt"
        }

        It 'Validate LeafBase' {
            $result = Split-Path -Path "$level2_1Full$fileExt" -Extension
            $result | Should Be $fileExt
        }

        It "Validate NoQualifier" {
            $result = Split-Path -Path $level1_0Full -NoQualifier
            ($result.TrimStart('/\')) | Should Be $level1_0
        }

        It "Validate Qualifier" {
            $result = Split-Path -Path $level1_0Full -Qualifier
            $result | Should Be "TestDrive:"
        }
    }

    Context "Valdiate Set-Location Cmdlets Parameters" {
        It "Without Passthru Doesn't Return a Path" {
            $result = Set-Location -Path $level1_0
            $result | Should BeNullOrEmpty
        }

        It "By LiteralPath" {
            $result = Set-Location -LiteralPath $level1_0Full -PassThru
            $result.Path | Should Be $level1_0Full
        }

        It "To Default Location Stack Does Nothing" {
            $beforeLoc = Get-Location
            Set-Location -StackName ""
            $afterLoc = Get-Location
            $beforeLoc.Path | Should Be $afterLoc.Path
        }

        It "WhatIf is Not Supported" {
            try {
                Set-Location $level1_0 -WhatIf
                throw "Expected exception not thrown"
            }
            catch { $_.FullyQualifiedErrorId | Should Be "NamedParameterNotFound,Microsoft.PowerShell.Commands.SetLocationCommand" }
        }
    }

    Context "Valdiate Push-Location and Pop-Location Cmdlets Parameters" {
        It "Verify Push + Path" {
            Push-Location -Path $level1_0
            Push-Location -Path $level2_0
            $location1 = Get-Location
            Pop-Location
            $location2 = Get-Location
            Pop-Location
            $location3 = Get-Location

            $location1.Path | Should Be $level2_0Full
            $location2.Path | Should Be $level1_0Full
            $location3.Path | Should Be $root
        }

        It "Verify Push + PassThru" {
            $location1 = Get-Location
            $passThru1 = Push-Location -PassThru
            Set-Location $level1_0
            $location2 = Get-Location
            $passThru2 = Push-Location -PassThru
            Set-Location $level2_0
            $location3 = Get-Location

            $location1.Path | Should Be $passThru1.Path
            $location2.Path | Should Be $passThru2.Path
            $location3.Path | Should Be $level2_0Full
        }

        It "Verify Push + LiteralPath" {
            Push-Location -LiteralPath $level1_0Full
            Push-Location -LiteralPath $level2_0Full
            $location1 = Get-Location
            Pop-Location
            $location2 = Get-Location
            Pop-Location
            $location3 = Get-Location

            $location1.Path | Should Be $level2_0Full
            $location2.Path | Should Be $level1_0Full
            $location3.Path | Should Be $root
        }

        It "Verify Pop + Invalid Stack Name" {
            try {
                Pop-Location -StackName UnknownStackName -ErrorAction Stop
                throw "Expected exception not thrown"
            }
            catch { $_.FullyQualifiedErrorId | Should Be "Argument,Microsoft.PowerShell.Commands.PopLocationCommand" }
        }
    }
}
