# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
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

Describe "New-Item" -Tags "CI" {
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

    It "Should create a file with sample text inside the file using the Value switch" {
        $expected = "This is test string"
        New-Item -Name $testfile -Path $tmpDirectory -ItemType file -Value $expected

        Test-Path $FullyQualifiedFile | Should -BeTrue

        Get-Content $FullyQualifiedFile | Should -Be $expected
    }

    It "Should not create a file when the Name switch is not used and only a directory specified" {
        #errorAction used because permissions issue in Windows
        New-Item -Path $tmpDirectory -ItemType file -ErrorAction SilentlyContinue

        Test-Path $FullyQualifiedFile | Should -BeFalse

    }

    It "Should create a file when the Name switch is not used but a fully qualified path is specified" {
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
    }

    It "New-Item -ItemType SymbolicLink should understand directory path ending with slash" {
        $folderName = [System.IO.Path]::GetRandomFileName()
        $symbolicLinkPath = New-Item -ItemType SymbolicLink -Path "$tmpDirectory/$folderName/" -Value "/bar/"
        $symbolicLinkPath | Should -Not -BeNullOrEmpty
    }
}

Describe "New-Item with links fails for non elevated user." -Tags "CI" {
    $tmpDirectory         = $TestDrive
    $testfile             = "testfile.txt"
    $testfolder           = "newDirectory"
    $testlink             = "testlink"
    $FullyQualifiedFile   = Join-Path -Path $tmpDirectory -ChildPath $testfile
    $FullyQualifiedFolder = Join-Path -Path $tmpDirectory -ChildPath $testfolder

    It "Should error correctly when failing to create a symbolic link" {
        # This test expects that /sbin exists but is not writable by the user
        { New-Item -ItemType SymbolicLink -Path "/sbin/powershell-test" -Target $FullyQualifiedFolder -ErrorAction Stop } |
        Should -Throw -ErrorId "NewItemSymbolicLinkElevationRequired,Microsoft.PowerShell.Commands.NewItemCommand"
    }
}
