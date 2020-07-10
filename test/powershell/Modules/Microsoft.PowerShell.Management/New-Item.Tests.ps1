# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module HelpersCommon

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

    if ($FullyQualifiedFileInFolder -and (Test-Path $FullyQualifiedFileInFolder))
    {
        Remove-Item $FullyQualifiedFileInFolder -Force
    }

    if ($FullyQualifiedSubFolder -and (Test-Path $FullyQualifiedSubFolder))
    {
        Remove-Item $FullyQualifiedSubFolder -Force
    }

    if (Test-Path $FullyQualifiedFolder)
    {
        Remove-Item $FullyQualifiedFolder -Force
    }
}

Describe "New-Item" -Tags "CI" {
    $tmpDirectory               = $TestDrive
    $testfile                   = "testfile.txt"
    $testfolder                 = "newDirectory"
    $testsubfolder              = "newSubDirectory"
    $testlink                   = "testlink"
    $FullyQualifiedFile         = Join-Path -Path $tmpDirectory -ChildPath $testfile
    $FullyQualifiedFolder       = Join-Path -Path $tmpDirectory -ChildPath $testfolder
    $FullyQualifiedLink         = Join-Path -Path $tmpDirectory -ChildPath $testlink
    $FullyQualifiedSubFolder    = Join-Path -Path $FullyQualifiedFolder -ChildPath $testsubfolder
    $FullyQualifiedFileInFolder = Join-Path -Path $FullyQualifiedFolder -ChildPath $testfile


    BeforeEach {
        Clean-State
    }

    It "should call the function without error" {
        { New-Item -Name $testfile -Path $tmpDirectory -ItemType file } | Should -Not -Throw
    }

    It "Should create a file without error" {
        New-Item -Name $testfile -Path $tmpDirectory -ItemType file

        Test-Path $FullyQualifiedFile | Should -BeTrue

        $fileInfo = Get-ChildItem $FullyQualifiedFile
        $fileInfo.Target | Should -BeNullOrEmpty
        $fileInfo.LinkType | Should -BeNullOrEmpty
    }

    It "Should create a folder without an error" {
        New-Item -Name newDirectory -Path $tmpDirectory -ItemType directory

        Test-Path $FullyQualifiedFolder | Should -BeTrue
    }

    It "Should create a file using the ni alias" {
        ni -Name $testfile -Path $tmpDirectory -ItemType file

        Test-Path $FullyQualifiedFile | Should -BeTrue
    }

    It "Should create a file using the Type alias instead of ItemType" {
        New-Item -Name $testfile -Path $tmpDirectory -Type file

        Test-Path $FullyQualifiedFile | Should -BeTrue
    }

    It "Should create a file with sample text inside the file using the Value parameter" {
        $expected = "This is test string"
        New-Item -Name $testfile -Path $tmpDirectory -ItemType file -Value $expected

        Test-Path $FullyQualifiedFile | Should -BeTrue

        Get-Content $FullyQualifiedFile | Should -Be $expected
    }

    It "Should not create a file when the Name parameter is not used and only a directory specified" {
        #errorAction used because permissions issue in Windows
        New-Item -Path $tmpDirectory -ItemType file -ErrorAction SilentlyContinue

        Test-Path $FullyQualifiedFile | Should -BeFalse

    }

    It "Should create a file when the Name parameter is not used but a fully qualified path is specified" {
        New-Item -Path $FullyQualifiedFile -ItemType file

        Test-Path $FullyQualifiedFile | Should -BeTrue
    }

    It "Should be able to create a multiple items in different directories" {
        $FullyQualifiedFile2 = Join-Path -Path $tmpDirectory -ChildPath test2.txt
        New-Item -ItemType file -Path $FullyQualifiedFile, $FullyQualifiedFile2

        Test-Path $FullyQualifiedFile  | Should -BeTrue
        Test-Path $FullyQualifiedFile2 | Should -BeTrue

        Remove-Item $FullyQualifiedFile2
    }

    It "Should be able to call the whatif switch without error" {
        { New-Item -Name testfile.txt -Path $tmpDirectory -ItemType file -WhatIf } | Should -Not -Throw
    }

    It "Should not create a new file when the whatif switch is used" {
        New-Item -Name $testfile -Path $tmpDirectory -ItemType file -WhatIf

        Test-Path $FullyQualifiedFile | Should -BeFalse
    }

    It "Should create a hard link of a file without error" {
        New-Item -Name $testfile -Path $tmpDirectory -ItemType file
        Test-Path $FullyQualifiedFile | Should -BeTrue

        New-Item -ItemType HardLink -Target $FullyQualifiedFile -Name $testlink -Path $tmpDirectory
        Test-Path $FullyQualifiedLink | Should -BeTrue

        $fileInfo = Get-ChildItem $FullyQualifiedLink
        $fileInfo.Target | Should -BeNullOrEmpty
        $fileInfo.LinkType | Should -BeExactly "HardLink"
    }

    It "Should create a file at the root of the drive while the current working directory is not the root" {
        try {
            New-Item -Name $testfolder -Path "TestDrive:\" -ItemType directory > $null
            Push-Location -Path "TestDrive:\$testfolder"
            New-Item -Name $testfile -Path "TestDrive:\" -ItemType file > $null
            $FullyQualifiedFile | Should -Exist
        }
        finally {
            Pop-Location
        }
    }

    It "Should create a folder at the root of the drive while the current working directory is not the root" {
        $testfolder2 = "newDirectory2"
        $FullyQualifiedFolder2 = Join-Path -Path $tmpDirectory -ChildPath $testfolder2

        try {
            New-Item -Name $testfolder -Path "TestDrive:\" -ItemType directory > $null
            Push-Location -Path "TestDrive:\$testfolder"
            New-Item -Name $testfolder2 -Path "TestDrive:\" -ItemType directory > $null
            $FullyQualifiedFolder2 | Should -Exist
        }
        finally {
            Pop-Location
        }
    }

    It "Should create a file in the current directory when using Drive: notation" {
        try {
            New-Item -Name $testfolder -Path "TestDrive:\" -ItemType directory > $null
            Push-Location -Path "TestDrive:\$testfolder"
            New-Item -Name $testfile -Path "TestDrive:" -ItemType file > $null
            $FullyQualifiedFileInFolder | Should -Exist
        }
        finally {
            Pop-Location
        }
    }

    It "Should create a folder in the current directory when using Drive: notation" {
        try {
            New-Item -Name $testfolder -Path "TestDrive:\" -ItemType directory > $null
            Push-Location -Path "TestDrive:\$testfolder"
            New-Item -Name $testsubfolder -Path "TestDrive:" -ItemType file > $null
            $FullyQualifiedSubFolder | Should -Exist
        }
        finally {
            Pop-Location
        }
    }

    It "Should display an error message when a symbolic link target is not specified" {
        { New-Item -Name $testfile -Path $tmpDirectory -ItemType SymbolicLink } | Should -Throw -ErrorId 'ArgumentNull,Microsoft.PowerShell.Commands.NewItemCommand'
        $Error[0].Exception.ParamName | Should -BeExactly 'content'
        { New-Item -Name $testfile -Path $tmpDirectory -ItemType:SymbolicLink -Value {} } | Should -Throw -ErrorId 'ArgumentNull,Microsoft.PowerShell.Commands.NewItemCommand'
         $Error[0].Exception.ParamName | Should -BeExactly 'content'
    }
}

# More precisely these tests require SeCreateSymbolicLinkPrivilege.
# You can see list of priveledges with `whoami /priv`.
# In the default windows setup, Admin user has this priveledge, but regular users don't.

Describe "New-Item with links" -Tags @('CI', 'RequireAdminOnWindows') {
    $tmpDirectory         = $TestDrive
    $testfile             = "testfile.txt"
    $testfolder           = "newDirectory"
    $testlink             = "testlink"
    $FullyQualifiedFile   = Join-Path -Path $tmpDirectory -ChildPath $testfile
    $FullyQualifiedFolder = Join-Path -Path $tmpDirectory -ChildPath $testfolder
    $FullyQualifiedLink   = Join-Path -Path $tmpDirectory -ChildPath $testlink
    $SymLinkMask          = [System.IO.FileAttributes]::ReparsePoint
    $DirLinkMask          = $SymLinkMask -bor [System.IO.FileAttributes]::Directory

    BeforeEach {
        Clean-State
    }

    It "Should create a symbolic link of a file without error" {
        New-Item -Name $testfile -Path $tmpDirectory -ItemType file
        Test-Path $FullyQualifiedFile | Should -BeTrue

        New-Item -ItemType SymbolicLink -Target $FullyQualifiedFile -Name $testlink -Path $tmpDirectory
        Test-Path $FullyQualifiedLink | Should -BeTrue

        $fileInfo = Get-ChildItem $FullyQualifiedLink
        $fileInfo.Target | Should -Match ([regex]::Escape($FullyQualifiedFile))
        $fileInfo.LinkType | Should -BeExactly "SymbolicLink"
        $fileInfo.Attributes -band $DirLinkMask | Should -BeExactly $SymLinkMask
    }

    It "Should create a symbolic link to a non-existing file without error" {
        $target = Join-Path $tmpDirectory "totallyBogusFile"
        New-Item -ItemType SymbolicLink -Target $target -Name $testlink -Path $tmpDirectory
        Test-Path $FullyQualifiedLink | Should -BeTrue

        $fileInfo = Get-ChildItem $FullyQualifiedLink
        $fileInfo.Target | Should -Be $target
        Test-Path $fileInfo.Target | Should -BeFalse
        $fileInfo.LinkType | Should -BeExactly "SymbolicLink"
        $fileInfo.Attributes -band $DirLinkMask | Should -BeExactly $SymLinkMask
    }

    It "Should create a symbolic link to directory without error" {
        New-Item -Name $testFolder -Path $tmpDirectory -ItemType directory
        $FullyQualifiedFolder | Should -Exist

        New-Item -ItemType SymbolicLink -Target $FullyQualifiedFolder -Name $testlink -Path $tmpDirectory
        Test-Path $FullyQualifiedLink | Should -BeTrue

        $fileInfo = Get-Item $FullyQualifiedLink
        $fileInfo.Target | Should -Match ([regex]::Escape($FullyQualifiedFolder))
        $fileInfo.LinkType | Should -BeExactly "SymbolicLink"
        $fileInfo.Attributes -band $DirLinkMask | Should -Be $DirLinkMask

        # Remove the link explicitly to avoid broken symlink issue
        Remove-Item $FullyQualifiedLink -Force
        # Test a code path removing a symbolic link (reparse point)
        Test-Path $FullyQualifiedLink | Should -BeFalse
    }

    It "New-Item -ItemType SymbolicLink should understand directory path ending with slash" {
        $folderName = [System.IO.Path]::GetRandomFileName()
        $symbolicLinkPath = New-Item -ItemType SymbolicLink -Path "$tmpDirectory/$folderName/" -Value "/bar/"
        $symbolicLinkPath | Should -Not -BeNullOrEmpty
    }

    It "New-Item -ItemType SymbolicLink should be able to create a relative link" -Skip:(!$IsWindows) {
        try {
            Push-Location $TestDrive
            $relativeFilePath = Join-Path -Path . -ChildPath "relativefile.txt"
            $file = New-Item -ItemType File -Path $relativeFilePath
            $link = New-Item -ItemType SymbolicLink -Path ./link -Target $relativeFilePath
            $link.Target | Should -BeExactly $relativeFilePath
        } finally {
            Pop-Location
        }
    }
}

Describe "New-Item: symlink with absolute/relative path test" -Tags @('CI', 'RequireAdminOnWindows') {
    BeforeAll {
        # on macOS, the /tmp directory is a symlink, so we'll resolve it here
        $TestPath = $TestDrive
        if ($IsMacOS)
        {
            $item = Get-Item $TestPath
            $dirName = $item.BaseName
            $item = Get-Item $item.PSParentPath -Force
            if ($item.LinkType -eq "SymbolicLink")
            {
                $TestPath = Join-Path $item.Target $dirName
            }
        }

        Push-Location $TestPath
        $null = New-Item -Type Directory someDir
        $null = New-Item -Type File someFile
    }

    AfterAll {
        Pop-Location
    }

    It "Symlink with absolute path to existing directory behaves like a directory" {
        New-Item -Type SymbolicLink someDirLinkAbsolute -Target (Convert-Path someDir)
        Get-Item someDirLinkAbsolute | Should -BeOfType System.IO.DirectoryInfo
    }

    It "Symlink with relative path to existing directory behaves like a directory" {
        # PowerShell should normalize '.\someDir' to './someDir' as needed.
        New-Item -Type SymbolicLink someDirLinkRelative -Target .\someDir
        Get-Item someDirLinkRelative | Should -BeOfType System.IO.DirectoryInfo
    }

    It "Symlink with absolute path to existing file behaves like a file" {
        New-Item -Type SymbolicLink someFileLinkAbsolute -Target (Convert-Path someFile)
        Get-Item someFileLinkAbsolute | Should -BeOfType System.IO.FileInfo
    }

    It "Symlink with relative path to existing file behaves like a file" {
        New-Item -Type SymbolicLink someFileLinkRelative -Target ./someFile
        Get-Item someFileLinkRelative | Should -BeOfType System.IO.FileInfo
    }
}

Describe "New-Item with links fails for non elevated user if developer mode not enabled on Windows." -Tags "CI" {
    BeforeAll {
        # on macOS, the /tmp directory is a symlink, so we'll resolve it here
        $TestPath = $TestDrive
        if ($IsMacOS)
        {
            $item = Get-Item $TestPath
            $dirName = $item.BaseName
            $item = Get-Item $item.PSParentPath -Force
            if ($item.LinkType -eq "SymbolicLink")
            {
                $TestPath = Join-Path $item.Target $dirName
            }
        }

        $testfile             = "testfile.txt"
        $testlink             = "testlink"
        $FullyQualifiedFile   = Join-Path -Path $TestPath -ChildPath $testfile
        $TestFilePath         = Join-Path -Path $TestPath -ChildPath $testlink
        $developerModeEnabled = (Get-ItemProperty HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock -ErrorAction SilentlyContinue).AllowDevelopmentWithoutDevLicense -eq 1
        $minBuildRequired     = [System.Environment]::OSVersion.Version -ge "10.0.14972"
        $developerMode = $developerModeEnabled -and $minBuildRequired
    }

    AfterEach {
        Remove-Item -Path $testFilePath -Force -ErrorAction SilentlyContinue
    }

    It "Should error correctly when failing to create a symbolic link and not in developer mode" -Skip:(!$IsWindows -or $developerMode -or (Test-IsElevated)) {
        { New-Item -ItemType SymbolicLink -Path $TestFilePath -Target $FullyQualifiedFile -ErrorAction Stop } |
        Should -Throw -ErrorId "NewItemSymbolicLinkElevationRequired,Microsoft.PowerShell.Commands.NewItemCommand"
        $TestFilePath | Should -Not -Exist
    }

    It "Should succeed to create a symbolic link without elevation and in developer mode" -Skip:(!$IsWindows -or !$developerMode -or (Test-IsElevated)) {
        { New-Item -ItemType SymbolicLink -Path $TestFilePath -Target $FullyQualifiedFile -ErrorAction Stop } | Should -Not -Throw
        $TestFilePath | Should -Exist
    }
}

Describe "New-Item -Force allows to create an item even if the directories in the path don't exist" -Tags "CI" {
    BeforeAll {
        $testFile             = 'testfile.txt'
        $testFolder           = 'testfolder'
        $FullyQualifiedFolder = Join-Path -Path $TestDrive -ChildPath $testFolder
        $FullyQualifiedFile   = Join-Path -Path $TestDrive -ChildPath $testFolder -AdditionalChildPath $testFile
    }

    BeforeEach {
        # Explicitly removing folder and the file before tests
        Remove-Item $FullyQualifiedFolder -ErrorAction SilentlyContinue
        Remove-Item $FullyQualifiedFile   -ErrorAction SilentlyContinue
        Test-Path -Path $FullyQualifiedFolder | Should -BeFalse
        Test-Path -Path $FullyQualifiedFile   | Should -BeFalse
    }

    It "Should error correctly when -Force is not used and folder in the path doesn't exist" {
        { New-Item $FullyQualifiedFile -ErrorAction Stop } | Should -Throw -ErrorId 'NewItemIOError,Microsoft.PowerShell.Commands.NewItemCommand'
        $FullyQualifiedFile | Should -Not -Exist
    }
    It "Should create new file correctly when -Force is used and folder in the path doesn't exist" {
        { New-Item $FullyQualifiedFile -Force -ErrorAction Stop } | Should -Not -Throw
        $FullyQualifiedFile | Should -Exist
    }
}
