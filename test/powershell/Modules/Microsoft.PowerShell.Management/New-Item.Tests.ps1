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
        { New-Item -Name $testfile -Path $tmpDirectory -ItemType file } | Should Not Throw
    }

    It "Should create a file without error" {
        New-Item -Name $testfile -Path $tmpDirectory -ItemType file

        Test-Path $FullyQualifiedFile | Should Be $true

        $fileInfo = Get-ChildItem $FullyQualifiedFile
        $fileInfo.Target | Should Be $null
        $fileInfo.LinkType | Should Be $null
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
        #errorAction used because permissions issue in Windows
        New-Item -Path $tmpDirectory -ItemType file -ErrorAction SilentlyContinue

        Test-Path $FullyQualifiedFile | Should Be $false

    }

    It "Should create a file when the Name switch is not used but a fully qualified path is specified" {
        New-Item -Path $FullyQualifiedFile -ItemType file

        Test-Path $FullyQualifiedFile | Should Be $true
    }

    It "Should be able to create a multiple items in different directories" {
        $FullyQualifiedFile2 = Join-Path -Path $tmpDirectory -ChildPath test2.txt
        New-Item -ItemType file -Path $FullyQualifiedFile, $FullyQualifiedFile2

        Test-Path $FullyQualifiedFile  | Should Be $true
        Test-Path $FullyQualifiedFile2 | Should Be $true

        Remove-Item $FullyQualifiedFile2
    }

    It "Should be able to call the whatif switch without error" {
        { New-Item -Name testfile.txt -Path $tmpDirectory -ItemType file -WhatIf } | Should Not Throw
    }

    It "Should not create a new file when the whatif switch is used" {
        New-Item -Name $testfile -Path $tmpDirectory -ItemType file -WhatIf

        Test-Path $FullyQualifiedFile | Should Be $false
    }

    It "Should create a hard link of a file without error" {
        New-Item -Name $testfile -Path $tmpDirectory -ItemType file
        Test-Path $FullyQualifiedFile | Should Be $true

        New-Item -ItemType HardLink -Target $FullyQualifiedFile -Name $testlink -Path $tmpDirectory
        Test-Path $FullyQualifiedLink | Should Be $true

        $fileInfo = Get-ChildItem $FullyQualifiedLink
        $fileInfo.Target | Should Be $null
        $fileInfo.LinkType | Should Be "HardLink"
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

    BeforeEach {
        Clean-State
    }

    It "Should create a symbolic link of a file without error" {
        New-Item -Name $testfile -Path $tmpDirectory -ItemType file
        Test-Path $FullyQualifiedFile | Should Be $true

        New-Item -ItemType SymbolicLink -Target $FullyQualifiedFile -Name $testlink -Path $tmpDirectory
        Test-Path $FullyQualifiedLink | Should Be $true

        $fileInfo = Get-ChildItem $FullyQualifiedLink
        $fileInfo.Target | Should Match ([regex]::Escape($FullyQualifiedFile))
        $fileInfo.LinkType | Should Be "SymbolicLink"
    }

    It "Should create a symbolic link to a non-existing file without error" {
        $target = Join-Path $tmpDirectory "totallyBogusFile"
        New-Item -ItemType SymbolicLink -Target $target -Name $testlink -Path $tmpDirectory
        Test-Path $FullyQualifiedLink | Should Be $true

        $fileInfo = Get-ChildItem $FullyQualifiedLink
        $fileInfo.Target | Should Be $target
        Test-Path $fileInfo.Target | Should be $false
        $fileInfo.LinkType | Should Be "SymbolicLink"
    }

    It "Should create a symbolic link from directory without error" {
        New-Item -Name $testFolder -Path $tmpDirectory -ItemType directory
        Test-Path $FullyQualifiedFolder | Should Be $true

        New-Item -ItemType SymbolicLink -Target $FullyQualifiedFolder -Name $testlink -Path $tmpDirectory
        Test-Path $FullyQualifiedLink | Should Be $true

        $fileInfo = Get-ChildItem $FullyQualifiedLink
        $fileInfo.Target | Should Match ([regex]::Escape($FullyQualifiedFolder))
        $fileInfo.LinkType | Should Be "SymbolicLink"

        # Remove the link explicitly to avoid broken symlink issue
        Remove-Item $FullyQualifiedLink -Force
    }

    It "Should error correctly when failing to create a symbolic link" -Skip:($IsWindows -or $IsElevated) {
        # This test expects that /sbin exists but is not writable by the user
        try {
            New-Item -ItemType SymbolicLink -Path "/sbin/powershell-test" -Target $FullyQualifiedFolder -ErrorAction Stop
            throw "Execution OK"
        } catch {
            $_.FullyQualifiedErrorId | Should Be "NewItemSymbolicLinkElevationRequired,Microsoft.PowerShell.Commands.NewItemCommand"
        }
    }
}
