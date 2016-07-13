If ( $IsWindows -or IsOSX ) { return }
# ensure that global switch is ON
# Setup for all tests

Describe "Tests for Long Path support in basic PowerShell cmdlets" -Tags "Feature" {
    BeforeAll {
        $powershell = (get-process -id $PID).MainModule.FileName


        $baseDirectory = join-path $TestDrive "TestDirForLongPaths"
        $tempDirectory = $baseDirectory
        1..290 | %{$tempDirectory += "/Directory$_"}
        $tempDirectory += "/WorkDirectory"  # this directory and child files will have a path length of around 4000 characters

        $tempFileName = "A file with name of max path segment length - 255 characters"
        $CharsToAdd = 255 - $tempFileName.Length - 5 # <#The max path segment length is 255 characters#> - $tempFileName.Length - 4<#extension#>
        1..$CharsToAdd | %{$tempFileName+='_'}
        $tempFileName += ".txt"
        $tempFile = join-path $tempDirectory $tempFileName
        $tempFile2 = join-path $tempDirectory "Test-Dest-File.txt"
        $tempFile3 = join-path $tempDirectory "Test File 3.txt"
        $tempDrive = "T[est]"

        function CreateFiles {
            $null = new-item -type directory $tempDirectory -ErrorAction Stop
            $null = new-item -type file $tempFile -ErrorAction Stop 
        }
        CreateFiles
    }
    
    It "Get-Item works with file with long paths" {
        @(get-item -LiteralPath $tempFile).Count | Should Be 1
    }

    It "Set-ItemProperty works with long paths" {
        Set-ItemProperty -LiteralPath $tempFile Attributes ReadOnly
        (get-childitem -LiteralPath $tempFile).Attributes | Should Be "ReadOnly"
        Set-ItemProperty -LiteralPath $tempFile Attributes Normal
    }

    It "Set-ItemProperty works with long paths and piping" {
        get-item -LiteralPath $tempFile | Set-ItemProperty -Name Attributes -Value ReadOnly
        (get-childitem -LiteralPath $tempFile).Attributes | Should Be "ReadOnly"
        Set-ItemProperty -LiteralPath $tempFile Attributes Normal
    }

    It "Copy-Item works with long paths" {
        Copy-Item -LiteralPath $tempFile $tempFile2
        @(get-item -LiteralPath $tempFile).Count | Should Be 1
        @(get-item -LiteralPath $tempFile2).Count | Should Be 1
        Remove-Item -LiteralPath $tempFile2 -force
    }

    It "Copy-Item works with long paths and piping" {
        Get-Item -LiteralPath $tempFile | Copy-Item -Destination $tempFile2
        @(get-item -LiteralPath $tempFile).Count | Should Be 1
        @(get-item -LiteralPath $tempFile2).Count | Should Be 1
        Remove-Item -LiteralPath $tempFile2 -force
    }

    It "Move-Item works with long paths" {
        new-item -force $tempFile2 > $null
        Move-Item -LiteralPath $tempFile2 $tempFile3
        @(get-item -LiteralPath $tempFile2 -ea SilentlyContinue).Count | Should Be 0
        @(get-item -LiteralPath $tempFile3).Count | Should Be 1
        Remove-Item -LiteralPath $tempFile3 -force
    }

    It "Move-Item works with long paths and piping" {
        new-item -force $tempFile2 > $null
        get-item -LiteralPath $tempFile2 | move-item -Destination $tempFile3
        @(get-item -LiteralPath $tempFile2 -ea SilentlyContinue).Count | Should Be 0
        @(get-item -LiteralPath $tempFile3).Count | Should Be 1
        Remove-Item -LiteralPath $tempFile3 -force
    }

    It "Copy-Item/Move-Item works with long paths and directory trees" {
        $baseDirectory2 = $baseDirectory + "Dir2"
        $baseDirectory3 = $baseDirectory + "Dir3"

        Copy-Item -Recurse -LiteralPath $baseDirectory $baseDirectory2
        $tempFile2 = $tempFile.Replace($baseDirectory,$baseDirectory2)
        @(get-item -LiteralPath $tempFile2).Count | Should Be 1

        Move-Item -Force -LiteralPath $baseDirectory2 $baseDirectory3
        $tempFile3 = $tempFile.Replace($baseDirectory,$baseDirectory3)
        @(get-item -LiteralPath $tempFile2 -ea SilentlyContinue).Count | Should Be 0
        @(get-item -LiteralPath $tempFile3).Count | Should Be 1

        Remove-Item -Recurse -Force -LiteralPath $baseDirectory3
    }

    It "Get-ChildItem works with long paths" {
        @(Get-ChildItem -LiteralPath $tempDirectory).Count | Should Be 1 # expecting only $tempFile there
    }

    It "Get-ChildItem works with long paths and piping" {
        @(Get-Item -LiteralPath $tempDirectory | Get-ChildItem).Count | Should Be 1 # expecting only $tempFile there
    }

    It "Resolve-Path works with long paths" {
        @(Resolve-Path -LiteralPath $tempFile).Count | Should Be 1
    }

    It "Resolve-Path works with long paths and piping" {
        @(Get-ChildItem -LiteralPath $tempFile | Resolve-Path).Count | Should Be 1
    }
    
    It "Test-Path works with long paths" {
        Test-Path -LiteralPath $tempFile | Should Be $true
    }

    It "Test-Path works with long paths and piping" {
        Get-Item -LiteralPath $tempFile | Test-Path | Should Be $true
    }

    It "Convert-Path works with long paths" {
        @(Convert-Path -LiteralPath $tempFile).Count | Should Be 1
    }

    It "Convert-Path works with long paths and piping" {
        @(get-item -literalPath $tempFile | Convert-Path).Count | Should Be 1
    }

    It "Remove-Item works with long paths" {
        Remove-Item -Recurse -LiteralPath $tempDirectory
        $tempFile | Should Not Exist
        $tempDirectory | Should Not Exist
        CreateFiles
    }

    It "Remove-Item works with long paths and piping" {
        get-item -LiteralPath $tempDirectory | Remove-Item -Recurse
        $tempFile | Should Not Exist
        $tempDirectory | Should Not Exist
        CreateFiles
    }

    It "Get-PsDrive/Remove-PsDrive work with long paths" {
        New-PSDrive $tempDrive FileSystem $tempDirectory > $null
        (Get-PsDrive -LiteralName $tempDrive).Name | Should Be $tempDrive
        $f = $tempDrive + ":/TestDriveFile.ps1"
        "1+1" | Out-File -LiteralPath $f -Force
        $scriptResult = &$f
        $scriptResult | Should Be 2
        Remove-Item -Recurse -Force -LiteralPath $f
        Remove-PSDrive -LiteralName $tempDrive
        @(Get-PsDrive -LiteralName $tempDrive).Count | Should Be 0
    }
    
    It "Get-Location/Set-Location work with long paths" {
        $oldLocation = get-location
        Set-Location -LiteralPath $tempDirectory
        Get-Location | Should Be $tempDirectory
        @(Get-ChildItem).Count | Should Be 1 # expecting only $tempFile there
        Set-Location -LiteralPath $oldLocation
    }

    It "Get-Location/Set-Location work with long paths and piping" {
        $oldLocation = get-location
        $tempItem = get-item -LiteralPath $tempDirectory
        $tempItem | Set-Location
        $currentLocation = get-item -LiteralPath (get-location).Path
        $currentLocation.PsPath | Should Be $tempItem.PsPath
        @(Get-ChildItem).Count | Should Be 1 # expecting only $tempFile there
        Set-Location -LiteralPath $oldLocation
    }

    It "Push-Location works with long paths" {
        $oldLocation = get-location
        push-location -LiteralPath $tempDirectory
        get-location | Should Be $tempDirectory
        Set-Location -LiteralPath $oldLocation
    }

    It "Push-Location works with long paths and piping" {
        $oldLocation = get-location
        $tempItem = get-item -LiteralPath $tempDirectory
        $tempItem | push-location
        $currentLocation = get-item -LiteralPath (get-location).Path
        $currentLocation.PsPath | Should Be $tempItem.PsPath
        Set-Location -LiteralPath $oldLocation
    }


    It "Get-Content/Add-Content/Set-Content/Clear-Content work with long paths" {
        Clear-Content -LiteralPath $tempFile
        (Get-Content -LiteralPath $tempFile).Length | Should Be 0

        Add-Content $tempFile "Hello World" -ErrorAction SilentlyContinue
        Get-Content -LiteralPath $tempFile | Should Be "Hello World"

        Clear-Content -LiteralPath $tempFile
        (Get-Content -LiteralPath $tempFile).Length | Should Be 0

        Set-Content -LiteralPath $tempFile "Hello World"
        Get-Content -LiteralPath $tempFile | Should Be "Hello World"

        Remove-Item -LiteralPath $tempFile
        Set-Content -LiteralPath $tempFile "Hello World"
        Get-Content -LiteralPath $tempFile | Should Be "Hello World"
    }

    It "Get-Content/Add-Content/Set-Content/Clear-Content work with long paths and piping" {
        Get-Item -LiteralPath $tempFile | Clear-Content
        Add-Content $tempFile "Hello World" -ErrorAction SilentlyContinue
        Get-Item -LiteralPath $tempFile | Get-Content | Should Be "Hello World"
    }

    It "PowerShell can invoke a script with a long path" {
        $f = $tempDirectory + "/TestScriptFile.ps1"
        "1+1" | Out-File -LiteralPath $f -Force
        $scriptResult = &$f
        $scriptResult | Should Be 2
    }

    It "PowerShell can start from a directory with a long path" {
        $oldLocation = get-location
        Set-Location -LiteralPath $tempDirectory
        $p = Start-Process $powershell -ArgumentList @('-c','exit 5') -Wait -PassThru 
        $p.ExitCode | Should Be 5
        Set-Location -LiteralPath $oldLocation
    }

    It "PowerShell can start another process from a long path" {
        $oldLocation = get-location
        Set-Location -LiteralPath $tempDirectory
        $p = Start-Process /sbin/ifconfig -Wait -PassThru -RedirectStandardOutput /dev/null
        $p.ExitCode | Should Be 0
        Set-Location -LiteralPath $oldLocation
    }
}
