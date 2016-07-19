If ( ! $IsWindows ) { return }
# ensure that global switch is ON
$FileSystemLongPathsEnabledKeyPath = 'HKLM:\System\CurrentControlSet\Control\FileSystem'
$Key = Get-Item -LiteralPath $FileSystemLongPathsEnabledKeyPath
$GlobalSwitchValue = $Key.GetValue('LongPathsEnabled')

if ($GlobalSwitchValue -eq $null) # on pre-RS1 OS long paths are not supported - skip these tests
{
    return 
}

if ($GlobalSwitchValue -eq 0) # on RS1 switch is turned off by default, so turn it on for running these tests
{
    Set-ItemProperty -Path $FileSystemLongPathsEnabledKeyPath -Name LongPathsEnabled -Value 1
}

$testcode = @'
# tests assume that global switch is ON
# 'HKLM:\System\CurrentControlSet\Control\FileSystem\LongPathsEnabled'

# Setup for all tests

$baseDirectory = join-path $env:Temp "TestDirForLongPaths"
$tempDirectory = $baseDirectory
1..300 | %{$tempDirectory += "\Directory$_"}
$tempDirectory += "\WorkDirectory"  # this directory and child files will have a path length of around 4000 characters


$tempFileName = "A file with name of max path segment length - 255 characters"
$CharsToAdd = 255 <#The max path segment length is 255 characters#> - $tempFileName.Length - 4<#extension#>
1..$CharsToAdd | %{$tempFileName+='_'}
$tempFileName += ".txt"
$tempFile = join-path $tempDirectory $tempFileName
$tempFile2 = join-path $tempDirectory "Test-Dest-File.txt"
$tempFile3 = join-path $tempDirectory "Test File 3.txt"
$tempDrive = "T[est]"

# helper functions
function CreateFiles
{
   new-item -type directory $tempDirectory -ErrorAction Stop > $null
   new-item -type file $tempFile -ErrorAction Stop > $null
}

function DeleteFiles
{
    Remove-Item -Force $tempFile -ErrorAction SilentlyContinue
    Remove-Item -Force $tempFile2 -ErrorAction SilentlyContinue
    Remove-Item -Force $tempFile3 -ErrorAction SilentlyContinue
    Remove-Item -Force -Recurse $baseDirectory -ErrorAction SilentlyContinue
}

CreateFiles

Describe "Tests for Long Path support in basic PowerShell cmdlets" -Tags "P1", "RI" {
    
    It "Get-Item works with file with long paths" {
        @(get-item -LiteralPath $tempFile).Count | Should Be 1
    }

    It "Set-ItemProperty works with long paths" {
        Set-ItemProperty -LiteralPath $tempFile Attributes ReadOnly
        (gci -LiteralPath $tempFile).Attributes | Should Be "ReadOnly"
        Set-ItemProperty -LiteralPath $tempFile Attributes Normal
    }

    It "Set-ItemProperty works with long paths and piping" {
        gi -LiteralPath $tempFile | Set-ItemProperty -Name Attributes -Value ReadOnly
        (gci -LiteralPath $tempFile).Attributes | Should Be "ReadOnly"
        Set-ItemProperty -LiteralPath $tempFile Attributes Normal
    }

    It "Copy-Item works with long paths" {
        Copy-Item -LiteralPath $tempFile $tempFile2
        @(gi -LiteralPath $tempFile).Count | Should Be 1
        @(gi -LiteralPath $tempFile2).Count | Should Be 1
        Remove-Item -LiteralPath $tempFile2 -force
    }

    It "Copy-Item works with long paths and piping" {
        Get-Item -LiteralPath $tempFile | Copy-Item -Destination $tempFile2
        @(gi -LiteralPath $tempFile).Count | Should Be 1
        @(gi -LiteralPath $tempFile2).Count | Should Be 1
        Remove-Item -LiteralPath $tempFile2 -force
    }

    It "Move-Item works with long paths" {
        new-item -force $tempFile2 > $null
        Move-Item -LiteralPath $tempFile2 $tempFile3
        @(gi -LiteralPath $tempFile2 -ea SilentlyContinue).Count | Should Be 0
        @(gi -LiteralPath $tempFile3).Count | Should Be 1
        Remove-Item -LiteralPath $tempFile3 -force
    }

    It "Move-Item works with long paths and piping" {
        new-item -force $tempFile2 > $null
        get-item -LiteralPath $tempFile2 | move-item -Destination $tempFile3
        @(gi -LiteralPath $tempFile2 -ea SilentlyContinue).Count | Should Be 0
        @(gi -LiteralPath $tempFile3).Count | Should Be 1
        Remove-Item -LiteralPath $tempFile3 -force
    }

    It "Copy-Item/Move-Item works with long paths and directory trees" {
        $baseDirectory2 = $baseDirectory + "Dir2"
        $baseDirectory3 = $baseDirectory + "Dir3"

        Copy-Item -Recurse -LiteralPath $baseDirectory $baseDirectory2
        $tempFile2 = $tempFile.Replace($baseDirectory,$baseDirectory2)
        @(gi -LiteralPath $tempFile2).Count | Should Be 1

        Move-Item -Force -LiteralPath $baseDirectory2 $baseDirectory3
        $tempFile3 = $tempFile.Replace($baseDirectory,$baseDirectory3)
        @(gi -LiteralPath $tempFile2 -ea SilentlyContinue).Count | Should Be 0
        @(gi -LiteralPath $tempFile3).Count | Should Be 1

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
        [System.IO.File]::Exists($tempFile) | Should Be $false
        [System.IO.Directory]::Exists($tempDirectory) | Should Be $false
        CreateFiles
    }

    It "Remove-Item works with long paths and piping" {
        gi -LiteralPath $tempDirectory | Remove-Item -Recurse
        [System.IO.File]::Exists($tempFile) | Should Be $false
        [System.IO.Directory]::Exists($tempDirectory) | Should Be $false
        CreateFiles
    }
    
    It "Get-PsDrive/Remove-PsDrive work with long paths" {
        New-PSDrive $tempDrive FileSystem $tempDirectory > $null
        (Get-PsDrive -LiteralName $tempDrive).Name | Should Be $tempDrive
        $f = $tempDrive + ":\TestDriveFile.ps1"
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
        $currentLocation = gi -LiteralPath (get-location).Path
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
        $currentLocation = gi -LiteralPath (get-location).Path
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

        Clear-Content -LiteralPath $tempFile
    }

    It "Get-Content/Add-Content/Set-Content/Clear-Content work with long paths and piping" {
        Get-Item -LiteralPath $tempFile | Clear-Content

        Add-Content $tempFile "Hello World" -ErrorAction SilentlyContinue
        Get-Item -LiteralPath $tempFile | Get-Content | Should Be "Hello World"

        Get-Item -LiteralPath $tempFile | Clear-Content
    }

    It "PowerShell can invoke a script with a long path" {
        $f = $tempDirectory + "\TestScriptFile.ps1"
        "1+1" | Out-File -LiteralPath $f -Force
        $scriptResult = &$f
        $scriptResult | Should Be 2
        Remove-Item -Recurse -Force -LiteralPath $f
    }

    It "PowerShell can start from a directory with a long path" {
        $oldLocation = get-location
        Set-Location -LiteralPath $tempDirectory
        $p = Start-Process powershell.exe -ArgumentList @('-c','exit 5') -Wait -PassThru -WindowStyle Minimized
        $p.ExitCode | Should Be 5
        Set-Location -LiteralPath $oldLocation
    }

    It "PowerShell can start another process from a long path" {
        $oldLocation = get-location
        Set-Location -LiteralPath $tempDirectory
        $p = Start-Process cmd.exe -ArgumentList @('/c','exit 99') -Wait -PassThru -WindowStyle Minimized
        $p.ExitCode | Should Be 99
        Set-Location -LiteralPath $oldLocation
    }
}

# Cleanup for all tests
DeleteFiles
'@

Describe "Tests for Long Path support in basic PowerShell cmdlets" -Tags "P1", "RI" {
    
    It "Long Path support tests" {
        $testFilePath = Join-Path $env:temp LongPathsTests.ps1
        $testcode | Out-File $testFilePath -Force
        $commandline = 'ipmo Pester;$results=Invoke-Pester -Script ' + $testFilePath + ';exit $results.FailedCount'
        $p = Start-Process powershell.exe -ArgumentList @('-c',$commandline) -Wait -PassThru
        $errorCount = $p.ExitCode
        $errorCount | Should Be 0
    }
}

# Cleanup for all tests
Set-ItemProperty -Path $FileSystemLongPathsEnabledKeyPath -Name LongPathsEnabled -Value $GlobalSwitchValue
