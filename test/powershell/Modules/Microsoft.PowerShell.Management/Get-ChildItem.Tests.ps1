# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Get-ChildItem" -Tags "CI" {

    Context 'FileSystem provider' {

        BeforeAll {
            # Create Test data
            $max_Path = 260
            $item_a = "a3fe710a-31af-4834-bc29-d0b584589838"
            $item_B = "B1B691A9-B7B1-4584-AED7-5259511BEEC4"
            $item_c = "c283d143-2116-4809-bf11-4f7d61613f92"
            $item_D = "D39B4FD9-3E1D-4DD5-8718-22FE2C934CE3"
            $item_E = "EE150FEB-0F21-4AFF-8066-AF59E925810C"
            $item_F = ".F81D8514-8862-4227-B041-0529B1656A43"
            $item_G = "5560A62F-74F1-4FAE-9A23-F4EBD90D2676"
            $item_H = "5f05ebca-4859-11ec-81d3-0242ac130003"
            $item_I = "z" * ($max_Path - $TestDrive.FullName.Length - $item_H.Length - 2)
            $item_J = "32d74aae-9054-4fa7-be97-8c806d10e8b9"
            $null = New-Item -Path $TestDrive -Name $item_a -ItemType "File" -Force
            $null = New-Item -Path $TestDrive -Name $item_B -ItemType "File" -Force
            $null = New-Item -Path $TestDrive -Name $item_c -ItemType "File" -Force
            $null = New-Item -Path $TestDrive -Name $item_D -ItemType "File" -Force
            $null = New-Item -Path $TestDrive -Name $item_E -ItemType "Directory" -Force
            $null = New-Item -Path $TestDrive -Name $item_F -ItemType "File" -Force | ForEach-Object {$_.Attributes = "hidden"}
            $null = New-Item -Path (Join-Path -Path $TestDrive -ChildPath $item_E) -Name $item_G -ItemType "File" -Force
            $null = New-Item -Path $TestDrive\$item_I\$item_H -Name $item_J -ItemType "Directory" -Force

            $searchRoot = Join-Path $TestDrive -ChildPath "TestPS"
            $file1 = Join-Path $searchRoot -ChildPath "D1" -AdditionalChildPath "File1.txt"
            $file2 = Join-Path $searchRoot -ChildPath "File1.txt"

            $PathWildCardTestCases = @(
                @{Parameters = @{Path = $searchRoot; Recurse = $true; Directory = $true }; ExpectedCount = 1; Title = "directory without wildcard"},
                @{Parameters = @{Path = (Join-Path $searchRoot '*'); Recurse = $true; Directory = $true }; ExpectedCount = 1; Title = "directory with wildcard"},
                @{Parameters = @{Path = $searchRoot; Recurse = $true; File = $true }; ExpectedCount = 1; Title = "file without wildcard"},
                @{Parameters = @{Path = (Join-Path $searchRoot '*'); Recurse = $true; File = $true }; ExpectedCount = 1; Title = "file with wildcard"},
                @{Parameters = @{Path = (Join-Path $searchRoot 'F*.txt'); Recurse = $true; File = $true }; ExpectedCount = 1; Title = "file with wildcard filename"}
            )

        }

        It "Should list the contents of the current folder" {
            (Get-ChildItem .).Name.Length | Should -BeGreaterThan 0
        }

        It "Should list the contents of the root folder using Drive:\ notation" {
            (Get-ChildItem TestDrive:\).Name.Length | Should -BeGreaterThan 0
        }

        It "Should list the contents of the root folder using Drive:\ notation from within another folder" {
            try
            {
                Push-Location -Path TestDrive:\$item_E
                (Get-ChildItem TestDrive:\ -File).Name.Length | Should -BeExactly 4
            }
            finally
            {
                Pop-Location
            }
        }

        It "Should list the contents of the current folder using Drive: notation when in the root" {
            (Get-ChildItem TestDrive:).Name.Length | Should -BeGreaterThan 0
        }

        It "Should list the contents of the current folder using Drive: notation when not in the root" {
            try
            {
                Push-Location -Path TestDrive:\$item_E
                (Get-ChildItem TestDrive:).Name | Should -BeExactly $item_G
            }
            finally
            {
                Pop-Location
            }
        }

        It "Should list the contents of the home directory" {
            Push-Location $HOME
            (Get-ChildItem .).Name.Length | Should -BeGreaterThan 0
            Pop-Location
        }

        It "Should have all the proper fields and be populated" {
            $var = Get-ChildItem .

            $var.Name.Length   | Should -BeGreaterThan 0
            $var.Mode.Length   | Should -BeGreaterThan 0
            $var.LastWriteTime | Should -BeGreaterThan 0
            $var.Length.Length | Should -BeGreaterThan 0
        }

        It "Should have mode property populated for protected files on Windows" -Skip:(!$IsWindows) {
            $files = Get-ChildItem -Force ~\NT*
            $files.Count | Should -BeGreaterThan 0
            foreach ($file in $files)
            {
                $file.Mode | Should -Not -BeNullOrEmpty
            }
        }

        It "Should list files in sorted order" {
            $files = Get-ChildItem -Path $TestDrive
            $files[0].Name     | Should -Be $item_E
            $files[1].Name     | Should -Be $item_I
            $files[2].Name     | Should -Be $item_a
            $files[3].Name     | Should -Be $item_B
            $files[4].Name     | Should -Be $item_c
            $files[5].Name     | Should -Be $item_D
        }

        It "Should list hidden files as well when 'Force' parameter is used" {
            $files = Get-ChildItem -Path $TestDrive -Force
            $files | Should -Not -BeNullOrEmpty
            $files.Count | Should -Be 7
            $files.Name.Contains($item_F) | Should -BeTrue
        }

        It "Should list only hidden files when 'Hidden' parameter is used" {
            $files = Get-ChildItem -Path $TestDrive -Hidden
            $files | Should -Not -BeNullOrEmpty
            $files.Count | Should -Be 1
            $files[0].Name | Should -BeExactly $item_F
        }
        It "Should find the hidden file if specified with hidden switch" {
            $file = Get-ChildItem -Path (Join-Path $TestDrive $item_F) -Hidden
            $file | Should -Not -BeNullOrEmpty
            $file.Count | Should -Be 1
            $file.Name | Should -BeExactly $item_F
        }

        It "Should list items in current directory only with depth set to 0" {
            (Get-ChildItem -Path $TestDrive -Depth 0).Count | Should -Be 6
            (Get-ChildItem -Path $TestDrive -Depth 0 -Include *).Count | Should -Be 6
            (Get-ChildItem -Path $TestDrive -Depth 0 -Exclude IntentionallyNonexistent).Count | Should -Be 6
        }

        It "Should return items recursively when using 'Include' or 'Exclude' parameters" {
            (Get-ChildItem -Path $TestDrive -Depth 1).Count | Should -Be 8
            (Get-ChildItem -Path $TestDrive -Depth 1 -Include $item_G).Count | Should -Be 1
            (Get-ChildItem -Path $TestDrive -Depth 1 -Exclude $item_a).Count | Should -Be 7
        }

        It "Should return items recursively when using 'Include' or 'Exclude' parameters with -LiteralPath" {
            (Get-ChildItem -LiteralPath $TestDrive -Recurse -Exclude *).Count | Should -Be 0
            (Get-ChildItem -LiteralPath $TestDrive -Recurse -Include *.dll).Count | Should -Be (Get-ChildItem $TestDrive -Recurse -Include *.dll).Count
            (Get-ChildItem -LiteralPath $TestDrive -Depth 1 -Include $item_G).Count | Should -Be 1
            (Get-ChildItem -LiteralPath $TestDrive -Depth 1 -Exclude $item_a).Count | Should -Be 7
        }

        It "get-childitem path wildcard - <title>" -TestCases $PathWildCardTestCases {
            param($Parameters, $ExpectedCount)

            $null = New-Item $file1 -Force -ItemType File

            (Get-ChildItem @Parameters).Count | Should -Be $ExpectedCount
        }

        It "get-childitem with and without file in search root" {
            $null = New-Item $file2 -Force -ItemType File

            (Get-ChildItem -Path $searchRoot -File -Recurse).Count | Should -Be 2
            (Get-ChildItem -Path $searchRoot -Directory -Recurse).Count | Should -Be 1

            Remove-Item $file2 -ErrorAction SilentlyContinue -Force
            (Get-ChildItem -Path $searchRoot -File -Recurse).Count | Should -Be 1
            (Get-ChildItem -Path $searchRoot -Directory -Recurse).Count | Should -Be 1
        }

        # VSTS machines don't have a page file
        It "Should give .sys file if the fullpath is specified with hidden and force parameter" -Pending {
            # Don't remove!!! It is special test for hidden and opened file with exclusive lock.
            $file = Get-ChildItem -Path "$env:SystemDrive\\pagefile.sys" -Hidden
            $file | Should -Not -Be $null
            $file.Count | Should -Be 1
            $file.Name | Should -Be "pagefile.sys"
        }

        It "-Filter *. finds extension-less files" {
            $null = New-Item -Path TestDrive:/noextension -ItemType File
            (Get-ChildItem -File -LiteralPath TestDrive:/ -Filter noext*.*).Name | Should -BeExactly 'noextension'
        }

        # Because "dotnet API is not available, see PR 16044
        It "Understand APPEXECLINKs" -Skip {
            $app = Get-ChildItem -Path ~\appdata\local\microsoft\windowsapps\*.exe | Select-Object -First 1
            $app.Target | Should -Not -Be $app.FullName
            $app.LinkType | Should -BeExactly 'AppExeCLink'
        }

        It "Wildcard matching behavior is the same between -Path and -Include parameters when using escape characters" {
            $oldLocation = Get-Location

            try {
                Set-Location $TestDrive

                $expectedPath = 'a`[b]'
                $escapedPath = 'a```[b`]'
                New-Item -Type File $expectedPath -ErrorAction SilentlyContinue > $null

                $WithInclude = Get-ChildItem * -Include $escapedPath
                $WithPath = Get-ChildItem -Path $escapedPath

                $WithInclude.Name | Should -BeExactly $expectedPath
                $WithPath.Name | Should -BeExactly $expectedPath

            } finally {
                Set-Location $oldLocation
            }
        }

        It "Should list the folder present when path length equal to MAX_PATH" {
            (Get-ChildItem -Path TestDrive:\$item_I -Recurse -Force).Name.Length | Should -BeGreaterThan 0
        }

        It 'Trailing slash for -Path should treat it as a folder only when used with -recurse' {
            $foo = New-Item -ItemType Directory -Path TestDrive:\foo
            $foo2 = New-Item -ItemType Directory -Path TestDrive:\foo\foo
            $bar = New-Item -ItemType File -Path TestDrive:\foo\bar
            $bar2 = New-Item -ItemType File -Path TestDrive:\foo\foo\bar

            { Get-ChildItem -Path testdrive:/foo/bar/ -Recurse -ErrorAction Stop } | Should -Throw -ErrorId 'ItemNotFound,Microsoft.PowerShell.Commands.GetChildItemCommand'
            $barFiles = Get-ChildItem -Path testdrive:/foo/bar -Recurse
            $barFiles.Count | Should -Be 2
        }

        It 'Works with Windows volume paths' -Skip:(!$IsWindows) {
            $winPath = $env:windir
            if (! $winPath) {
                Write-Verbose -Verbose "variable windir is null"
                Set-ItResult -Skipped -Because "windir is null"
                return
            }

            $driveLetter = $winPath[0]
            $winPartialPath = $winPath.SubString(3) # skip the drive letter, colon, and backslash
            Write-Verbose -Verbose "Partial path is '$winPartialPath'"
            $volume = (Get-Volume -DriveLetter $driveLetter).Path
            if (! $volume) {
                Write-Verbose -Verbose "windir: $winPath"
                Set-ItResult -Skipped -Because "Get-Volume returned no volume for system drive '$driveLetter'"
                return
            }

            $items = Get-ChildItem -LiteralPath "${volume}${winPartialPath}}"
            Write-Verbose -Verbose "Trying files in '${volume}${winPartialPath}'}'"
            if ($items.Count -eq 0) {
                Write-Verbose -Verbose "`$items is null!!"
            }

            $items[0].Parent.FullName | Should -BeExactly "${volume}${winPartialPath}}"
            $items | Should -HaveCount (Get-ChildItem $winPath).Count
        }

        It 'Works with Windows pipes' -Skip:(!$IsWindows) {
            $out = pwsh -noprofile -custompipename myTestPipe { Get-ChildItem \\.\pipe\myTestPipe }
            $out.Name | Should -BeExactly 'myTestPipe'
        }
    }

    Context 'Env: Provider' {

        It 'can handle mixed case in Env variables' {
            try
            {
                $env:__FOODBAR = 'food'
                $env:__foodbar = 'bar'

                $foodbar = Get-ChildItem env: | Where-Object {$_.Name -eq '__foodbar'}
                $count = if ($IsWindows) { 1 } else { 2 }
                ($foodbar | Measure-Object).Count | Should -Be $count
            }
            catch
            {
                Get-ChildItem env: | Where-Object {$_.Name -eq '__foodbar'} | Remove-Item -ErrorAction SilentlyContinue
            }
        }
    }
}

Describe "Get-ChildItem with special path" -Tags "CI" {

    BeforeAll {
        $bracketDirName = "Test[Dir]"
        $bracketDir = "Test``[Dir``]"
        $bracketPath = Join-Path $TestDrive $bracketDir
        $null = New-Item -Path $TestDrive -Name $bracketDirName -ItemType Directory -Force
    }

    It "Should list files in directory with name containing bracket char" {
        $null = New-Item -Path $bracketPath -Name file1.txt -ItemType File
        $null = New-Item -Path $bracketPath -Name file2.txt -ItemType File
        Get-ChildItem -Path $bracketPath | Should -HaveCount 2
    }
}

Describe 'FileSystem Provider Formatting' -Tag "CI","RequireAdminOnWindows" {

    BeforeAll {
        $modeTestDir = New-Item -Path "$TestDrive/testmodedirectory" -ItemType Directory -Force
        $targetFile1 = New-Item -Path "$TestDrive/targetFile1" -ItemType File -Force
        $targetFile2 = New-Item -Path "$TestDrive/targetFile2" -ItemType File -Force
        $targetDir1 = New-Item -Path "$TestDrive/targetDir1" -ItemType Directory -Force
        $targetDir2 = New-Item -Path "$TestDrive/targetDir2" -ItemType Directory -Force

        $testcases = @(
            @{ expectedMode = "d----"; expectedModeWithoutHardlink = "d----"; itemType = "Directory"; itemName = "Directory"; fileAttributes = [System.IO.FileAttributes] "Directory"; target = $null }
            @{ expectedMode = "l----"; expectedModeWithoutHardlink = "l----"; itemType = "SymbolicLink"; itemName = "SymbolicLink-Directory"; fileAttributes = [System.IO.FileAttributes]::Directory -bor [System.IO.FileAttributes]::ReparsePoint; target = $targetDir2.FullName }
        )

        if ($IsWindows)
        {
            $junctionMode = (Test-IsWindowsArm64) ? "la---" : "l----"
            $testcases += @{ expectedMode = $junctionMode; expectedModeWithoutHardlink = $junctionMode; itemType = "Junction"; itemName = "Junction-Directory"; fileAttributes = [System.IO.FileAttributes]::Directory -bor [System.IO.FileAttributes]::ReparsePoint; target = $targetDir1.FullName }
            $testcases += @{ expectedMode = "-a---"; expectedModeWithoutHardlink = "-a---"; itemType = "File"; itemName = "ArchiveFile"; fileAttributes = [System.IO.FileAttributes] "Archive"; target = $null }
            $testcases += @{ expectedMode = "la---"; expectedModeWithoutHardlink = "la---"; itemType = "SymbolicLink"; itemName = "SymbolicLink-File"; fileAttributes = [System.IO.FileAttributes]::Archive -bor [System.IO.FileAttributes]::ReparsePoint; target = $targetFile1.FullName }
            $testcases += @{ expectedMode = "la---"; expectedModeWithoutHardlink = "-a---"; itemType = "HardLink"; itemName = "HardLink"; fileAttributes = [System.IO.FileAttributes] "Archive"; target = $targetFile2.FullName }
        }
    }

    It 'Validate Mode property - <itemName>' -TestCases $testcases {

        param($expectedMode, $expectedModeWithoutHardlink, $itemType, $itemName, $fileAttributes, $target)

        $item = if ($target)
        {
            New-Item -Path $modeTestDir -Name $itemName -ItemType $itemType -Target $target
        }
        else
        {
            New-Item -Path $modeTestDir -Name $itemName -ItemType $itemType
        }

        $item | Should -BeOfType System.IO.FileSystemInfo

        $actualMode = [Microsoft.PowerShell.Commands.FileSystemProvider]::Mode($item)
        $actualMode | Should -BeExactly $expectedMode

        $actualModeWithoutHardlink = [Microsoft.PowerShell.Commands.FileSystemProvider]::ModeWithoutHardlink($item)
        $actualModeWithoutHardlink | Should -BeExactly $expectedModeWithoutHardlink

        $item.Attributes | Should -Be $fileAttributes
    }
}
