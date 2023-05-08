# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "FileSystem Provider Extended Tests for Get-ChildItem cmdlet" -Tags "CI" {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if ($IsLinux) {
            $PSDefaultParameterValues["it:skip"] = $true
        }

        $restoreLocation = Get-Location

        $DirSep = [IO.Path]::DirectorySeparatorChar

        $rootDir = Join-Path "TestDrive:" "TestDir"
        New-Item -Path $rootDir -ItemType Directory > $null

        Set-Location $rootDir

        New-Item -Path "file1.txt" -ItemType File > $null
        (New-Item -Path "filehidden1.doc" -ItemType File).Attributes = "Hidden"
        (New-Item -Path "filereadonly1.asd" -ItemType File).Attributes = "ReadOnly"

        New-Item -Path "subDir2" -ItemType Directory > $null
        Set-Location "subDir2"
        New-Item -Path "file2.txt" -ItemType File > $null
        (New-Item -Path "filehidden2.asd" -ItemType File).Attributes = "Hidden"
        (New-Item -Path "filereadonly2.doc" -ItemType File).Attributes = "ReadOnly"
        (New-Item -Path "subDir21" -ItemType Directory).Attributes = "Hidden"
        Set-Location "subDir21"
        New-Item -Path "file21.txt" -ItemType File > $null

        Set-Location $rootDir
        New-Item -Path "subDir3" -ItemType Directory > $null
        Set-Location "subDir3"
        New-Item -Path "file3.asd" -ItemType File > $null
        (New-Item -Path "filehidden3.txt" -ItemType File).Attributes = "Hidden"
        (New-Item -Path "filereadonly3.doc" -ItemType File).Attributes = "ReadOnly"

        Set-Location $rootDir
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues

        #restore the previous location
        Set-Location -Path $restoreLocation
    }

    Context 'Validate Get-ChildItem -Path' {
        It "Get-ChildItem -Path" {
            $result = Get-ChildItem -Path $rootDir
            $result.Count | Should -Be 4
            $result[0] | Should -BeOfType System.IO.DirectoryInfo
        }

        It "Get-ChildItem -Path -Hidden" {
            $result = Get-ChildItem -Path $rootDir -Hidden
            $result.Count | Should -Be 1
            $result | Should -BeOfType System.IO.FileInfo
            $result.Name | Should -BeExactly "filehidden1.doc"
        }

        It "Get-ChildItem -Path -Attribute Hidden" {
            $result = Get-ChildItem -Path $rootDir -Attributes Hidden
            $result.Count | Should -Be 1
            $result | Should -BeOfType System.IO.FileInfo
            $result.Name | Should -BeExactly "filehidden1.doc"
        }

        It "Get-ChildItem -Path -Force" {
            $result = Get-ChildItem -Path $rootDir -Force
            $result.Count | Should -Be 5
            $result.Name | Should -Contain "filehidden1.doc"
        }
    }

    Context 'Validate Get-ChildItem -Path -Directory/-File' {
        It "Get-ChildItem -Path -Directory" {
            $result = Get-ChildItem -Path $rootDir -Directory
            $result.Count | Should -Be 2
        }

        It "Get-ChildItem -Path -File" {
            $result = Get-ChildItem -Path $rootDir -File
            $result.Count | Should -Be 2
        }

        It "Get-ChildItem -Path -File -Hidden" {
            $result = Get-ChildItem -Path $rootDir -File -Hidden
            $result.Count | Should -Be 1
            $result | Should -BeOfType System.IO.FileInfo
            $result.Name | Should -BeExactly "filehidden1.doc"
        }
    }

    Context 'Validate Get-ChildItem -Path -Name' {
        It "Get-ChildItem -Path -Name" {
            $result = Get-ChildItem -Path $rootDir -Name
            $result.Count | Should -Be 4
            $result[0] | Should -BeOfType System.String
        }

        It "Get-ChildItem -Path -Name -Hidden" {
            $result = Get-ChildItem -Path $rootDir -Name -Hidden
            $result.Count | Should -Be 1
            $result | Should -BeOfType System.String
            $result | Should -BeExactly "filehidden1.doc"
        }

        It "Get-ChildItem -Path -Name -Attributes Hidden" {
            $result = Get-ChildItem -Path $rootDir -Name -Attributes Hidden
            $result.Count | Should -Be 1
            $result | Should -BeOfType System.String
            $result | Should -BeExactly "filehidden1.doc"
        }

        It "Get-ChildItem -Path -Name -Force" {
            $result = Get-ChildItem -Path $rootDir -Name -Force
            $result.Count | Should -Be 5
            $result | Should -BeOfType System.String
            $result | Should -Contain "filehidden1.doc"
        }

        It "Get-ChildItem -Path -Directory -Name" {
            $result = Get-ChildItem -Path $rootDir -Directory -Name
            $result.Count | Should -Be 2
            $result[0] | Should -BeOfType System.String
        }

        It "Get-ChildItem -Path -File -Name" {
            $result = Get-ChildItem -Path $rootDir -File -Name
            $result.Count | Should -Be 2
            $result[0] | Should -BeOfType System.String
        }

        It "Get-ChildItem -Path -File -Name -Hidden" {
            $result = Get-ChildItem -Path $rootDir -File -Name -Hidden
            $result.Count | Should -Be 1
            $result | Should -BeOfType System.String
            $result | Should -BeExactly "filehidden1.doc"
        }
    }

    Context 'Validate Get-ChildItem -Path -Recurse' {
        It "Get-ChildItem -Path -Recurse" {
            $result = Get-ChildItem -Path $rootDir -Recurse
            $result.Count | Should -Be 8
        }

        It "Get-ChildItem -Path -Recurse -Hidden" {
            $result = Get-ChildItem -Path $rootDir -Recurse -Hidden
            $result.Count | Should -Be 4
            $result.Where({ $_.Name -eq "filehidden1.doc"}) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "filehidden2.asd" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "subDir21" }) | Should -BeOfType System.IO.DirectoryInfo
            $result.Where({ $_.Name -eq "filehidden3.txt" }) | Should -BeOfType System.IO.FileInfo
        }

        It "Get-ChildItem -Path -Recurse -Attributes Hidden" {
            $result = Get-ChildItem -Path $rootDir -Recurse -Attributes Hidden
            $result.Count | Should -Be 4
            $result.Where({ $_.Name -eq "filehidden1.doc" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "filehidden2.asd" })| Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "subDir21" }) | Should -BeOfType System.IO.DirectoryInfo
            $result.Where({ $_.Name -eq "filehidden3.txt" }) | Should -BeOfType System.IO.FileInfo
        }

        It "Get-ChildItem -Path -Recurse -Force" {
            $result = Get-ChildItem -Path $rootDir -Recurse -Force
            $result.Count | Should -Be 13
            $result.Where({ $_.Name -eq "filehidden1.doc" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "filehidden2.asd" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "subDir21" }) | Should -BeOfType System.IO.DirectoryInfo
            $result.Where({ $_.Name -eq "filehidden3.txt" }) | Should -BeOfType System.IO.FileInfo
        }

        It "Get-ChildItem -Path -Recurse -Directory" {
            $result = Get-ChildItem -Path $rootDir -Recurse -Directory
            $result.Count | Should -Be 2
            $result | Should -BeOfType System.IO.DirectoryInfo
            $result.Where({ $_.Name -eq "subDir2" }) | Should -BeOfType System.IO.DirectoryInfo
            $result.Where({ $_.Name -eq "subDir3" }) | Should -BeOfType System.IO.DirectoryInfo
        }

        It "Get-ChildItem -Path -Recurse -File" {
            $result = Get-ChildItem -Path $rootDir -Recurse -File
            $result.Count | Should -Be 6
            $result | Should -BeOfType System.IO.FileInfo
        }

        It "Get-ChildItem -Path -Recurse -File -Hidden" {
            $result = Get-ChildItem -Path $rootDir -Recurse -File -Hidden
            $result.Count | Should -Be 3
            $result | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "filehidden1.doc" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "filehidden2.asd" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "filehidden3.txt" }) | Should -BeOfType System.IO.FileInfo
        }
    }

    Context 'Validate Get-ChildItem -Path -Depth' {
        It "Get-ChildItem -Path -Depth 0" {
            $result = Get-ChildItem -Path $rootDir -Depth 0
            $result.Count | Should -Be 4
        }

        It "Get-ChildItem -Path -Depth 0 -Force" {
            $result = Get-ChildItem -Path $rootDir -Depth 0 -Force
            $result.Count | Should -Be 5
        }

        It "Get-ChildItem -Path -Depth 1" {
            $result = Get-ChildItem -Path $rootDir -Depth 1
            $result.Count | Should -Be 8
        }

        It "Get-ChildItem -Path -Depth 1 -Force" {
            $result = Get-ChildItem -Path $rootDir -Depth 1 -Force
            $result.Count | Should -Be 12
        }

        It "Get-ChildItem -Path -Depth 2" {
            $result = Get-ChildItem -Path $rootDir -Depth 2
            $result.Count | Should -Be 8
        }

        It "Get-ChildItem -Path -Depth 2 -Force" {
            $result = Get-ChildItem -Path $rootDir -Depth 2 -Force
            $result.Count | Should -Be 13
        }
    }

    Context 'Validate Get-ChildItem -Path -Recurse -Name' {
        It "Get-ChildItem -Path -Recurse -Name" {
            $result = Get-ChildItem -Path $rootDir -Recurse -Name
            $result.Count | Should -Be 8
            $result[0] | Should -BeOfType System.String
        }

        It "Get-ChildItem -Path -Recurse -Name -Hidden" {
            $result = Get-ChildItem -Path $rootDir -Recurse -Name -Hidden
            $result.Count | Should -Be 4
            $result | Should -BeOfType System.String
            $result.Where({ $_ -eq "filehidden1.doc" }) | Should -BeOfType System.String
            $result.Where({ $_ -eq "subDir2$($DirSep)filehidden2.asd" }) | Should -BeOfType System.String
            $result.Where({ $_ -eq "subDir2$($DirSep)subDir21" }) | Should -BeOfType System.String
            $result.Where({ $_ -eq "subDir3$($DirSep)filehidden3.txt" }) | Should -BeOfType System.String
        }

        It "Get-ChildItem -Path -Recurse -Name -Force" {
            $result = Get-ChildItem -Path $rootDir -Recurse -Name -Force
            $result.Count | Should -Be 13
            $result | Should -BeOfType System.String
            $result.Where({ $_ -eq "filehidden1.doc" }) | Should -BeOfType System.String
            $result.Where({ $_ -eq "subDir2$($DirSep)filehidden2.asd" }) | Should -BeOfType System.String
            $result.Where({ $_ -eq "subDir2$($DirSep)subDir21" }) | Should -BeOfType System.String
            $result.Where({ $_ -eq "subDir3$($DirSep)filehidden3.txt" }) | Should -BeOfType System.String
        }

        It "Get-ChildItem -Path -Recurse -Name -Directory" {
            $result = Get-ChildItem -Path $rootDir -Recurse -Name -Directory
            $result.Count | Should -Be 2
            $result | Should -BeOfType System.String
            $result.Where({ $_ -eq "subDir2" }) | Should -BeOfType System.String
            $result.Where({ $_ -eq "subDir3" }) | Should -BeOfType System.String
        }

        It "Get-ChildItem -Path -Recurse -Name -Attributes Directory" {
            $result = Get-ChildItem -Path $rootDir -Recurse -Name -Attributes Directory
            $result.Count | Should -Be 2
            $result | Should -BeOfType System.String
            $result.Where({ $_ -eq "subDir2" }) | Should -BeOfType System.String
            $result.Where({ $_ -eq "subDir3" }) | Should -BeOfType System.String
        }

        It "Get-ChildItem -Path -Recurse -Name -File" {
            $result = Get-ChildItem -Path $rootDir -Recurse -Name -File
            $result.Count | Should -Be 6
            $result | Should -BeOfType System.String
        }

        It "Get-ChildItem -Path -Recurse -Name -File -Hidden" {
            $result = Get-ChildItem -Path $rootDir -Recurse -Name -File -Hidden
            $result.Count | Should -Be 3
            $result | Should -BeOfType System.String
            $result.Where({ $_ -eq "filehidden1.doc" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir2$($DirSep)filehidden2.asd" }) | Should -BeOfType System.String
            $result.Where({ $_ -eq "subDir3$($DirSep)filehidden3.txt" }) | Should -BeOfType System.String
        }
    }

    Context 'Validate Get-ChildItem -Path -Depth -Name' {
        It "Get-ChildItem -Path -Depth 0 -Name" {
            $result = Get-ChildItem -Path $rootDir -Depth 0 -Name
            $result.Count | Should -Be 4
            $result[0] | Should -BeOfType System.String
        }

        It "Get-ChildItem -Path -Depth 0 -Name -Force" {
            $result = Get-ChildItem -Path $rootDir -Depth 0 -Name -Force
            $result.Count | Should -Be 5
        }

        It "Get-ChildItem -Path -Depth 1 -Name" {
            $result = Get-ChildItem -Path $rootDir -Depth 1 -Name
            $result.Count | Should -Be 8
            $result[0] | Should -BeOfType System.String
        }

        It "Get-ChildItem -Path -Depth 1 -Name -Force" {
            $result = Get-ChildItem -Path $rootDir -Depth 1 -Name -Force
            $result.Count | Should -Be 12
            $result[0] | Should -BeOfType System.String
        }

        It "Get-ChildItem -Path -Depth 2 -Name" {
            $result = Get-ChildItem -Path $rootDir -Depth 2 -Name
            $result.Count | Should -Be 8
            $result[0] | Should -BeOfType System.String
        }

        It "Get-ChildItem -Path -Depth 2 -Name -Force" {
            $result = Get-ChildItem -Path $rootDir -Depth 2 -Name -Force
            $result.Count | Should -Be 13
            $result[0] | Should -BeOfType System.String
        }
    }

    Context 'Validate Get-ChildItem -Path -Filter' {
        It 'Get-ChildItem -Path -Filter "*.txt"' {
            $result = Get-ChildItem -Path $rootDir -Filter "*.txt"
            $result.Count | Should -Be 1
            $result[0] | Should -BeOfType System.IO.FileInfo
            $result.Name | Should -BeExactly "file1.txt"
        }

        It 'Get-ChildItem -Path -Filter "file*"' {
            $result = Get-ChildItem -Path $rootDir -Filter "file*"
            $result.Count | Should -Be 2
            $result | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "file1.txt" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "filereadonly1.asd" }) | Should -BeOfType System.IO.FileInfo
        }

        It 'Get-ChildItem -Path -Filter "file?.txt"' {
            $result = Get-ChildItem -Path $rootDir -Filter "file?.txt"
            $result.Count | Should -Be 1
            $result | Should -BeOfType System.IO.FileInfo
            $result.Name | Should -BeExactly "file1.txt"
        }

        It 'Get-ChildItem -Path -Filter "file*" -Hidden' {
            $result = Get-ChildItem -Path $rootDir -Filter "file*" -Hidden
            $result.Count | Should -Be 1
            $result | Should -BeOfType System.IO.FileInfo
            $result.Name | Should -BeExactly "filehidden1.doc"
        }

        It 'Get-ChildItem -Path -Filter "file*" -Force' {
            $result = Get-ChildItem -Path $rootDir -Filter "file*" -Force
            $result.Count | Should -Be 3
            $result | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "file1.txt" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "filehidden1.doc" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "filereadonly1.asd" }) | Should -BeOfType System.IO.FileInfo
        }
    }

    Context 'Validate Get-ChildItem -Path -Filter -Recurse' {
        It 'Get-ChildItem -Path -Filter "*.txt" -Recurse' {
            $result = Get-ChildItem -Path $rootDir -Filter "*.txt" -Recurse
            $result.Count | Should -Be 2
            $result | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "file1.txt" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "file2.txt" }) | Should -BeOfType System.IO.FileInfo
        }

        It 'Get-ChildItem -Path -Filter "file*" -Recurse' {
            $result = Get-ChildItem -Path $rootDir -Filter "file*" -Recurse
            $result.Count | Should -Be 6
            $result | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "file1.txt" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "filereadonly1.asd" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "file2.txt" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "filereadonly2.doc" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "file3.asd" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "filereadonly3.doc" }) | Should -BeOfType System.IO.FileInfo
        }

        It 'Get-ChildItem -Path -Filter "file?.*" -Recurse' {
            $result = Get-ChildItem -Path $rootDir -Filter "file?.*" -Recurse
            $result.Count | Should -Be 3
            $result | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "file1.txt" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "file2.txt" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "file3.asd" }) | Should -BeOfType System.IO.FileInfo
        }

        It 'Get-ChildItem -Path $rootDir -Filter "file*" -Hidden -Recurse' {
            $result = Get-ChildItem -Path $rootDir -Filter "file*" -Hidden -Recurse
            $result.Count | Should -Be 3
            $result | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "filehidden1.doc" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "filehidden2.asd" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "filehidden3.txt" }) | Should -BeOfType System.IO.FileInfo
        }

        It 'Get-ChildItem -Path $rootDir -Filter "file*" -Force -Recurse' {
            $result = Get-ChildItem -Path $rootDir -Filter "file*" -Force -Recurse
            $result.Count | Should -Be 10
            $result | Should -BeOfType System.IO.FileInfo
        }
    }

    Context 'Validate Get-ChildItem -Path -Filter -Recurse -Name' {
        It 'Get-ChildItem -Path -Filter "*.txt" -Recurse -Name' {
            $result = Get-ChildItem -Path $rootDir -Filter "*.txt" -Recurse -Name
            $result.Count | Should -Be 2
            $result | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "file1.txt" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir2$($DirSep)file2.txt" }) | Should -Not -BeNullOrEmpty
        }

        It 'Get-ChildItem -Path -Filter "file*" -Recurse -Name' {
            $result = Get-ChildItem -Path $rootDir -Filter "file*" -Recurse -Name
            $result.Count | Should -Be 6
            $result | Should -BeOfType System.String
            $result.Where({ $_ -eq "file1.txt" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "filereadonly1.asd" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir2$($DirSep)file2.txt" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir2$($DirSep)filereadonly2.doc" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir3$($DirSep)file3.asd" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir3$($DirSep)filereadonly3.doc" }) | Should -Not -BeNullOrEmpty
        }

        It 'Get-ChildItem -Path -Filter "file????only3.*" -Recurse -Name' {
            $result = Get-ChildItem -Path $rootDir -Filter "file????only3.*" -Recurse -Name
            $result.Count | Should -Be 1
            $result | Should -BeOfType System.String
            $result | Should -BeExactly "subDir3$($DirSep)filereadonly3.doc"
        }

        It 'Get-ChildItem -Path -Filter "file*" -Hidden -Recurse -Name' {
            $result = Get-ChildItem -Path $rootDir -Filter "file*" -Hidden -Recurse -Name
            $result.Count | Should -Be 3
            $result | Should -BeOfType System.String
            $result.Where({ $_ -eq "filehidden1.doc" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir2$($DirSep)filehidden2.asd" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir3$($DirSep)filehidden3.txt" }) | Should -Not -BeNullOrEmpty
        }

        It 'Get-ChildItem -Path -Filter "file*" -Force -Recurse -Name' {
            $result = Get-ChildItem -Path $rootDir -Filter "file*" -Force -Recurse -Name
            $result.Count | Should -Be 10
            $result | Should -BeOfType System.String
        }
    }

    Context 'Validate Get-ChildItem -Path -Include' {
        It 'Get-ChildItem -Path $-Include "*.txt"' -Pending:$true {    # Pending due to a bug
            $result = Get-ChildItem -Path $rootDir -Include "*.txt"
            $result.Count | Should -Be 1
            $result | Should -BeOfType System.IO.FileInfo
            $result.Name | Should -BeExactly "file1.txt"
        }

        It 'Get-ChildItem -Path -Include "*.txt" -Recurse' {
            $result = Get-ChildItem -Path $rootDir -Include "*.txt" -Recurse
            $result.Count | Should -Be 2
            $result | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "file1.txt" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "file2.txt" }) | Should -BeOfType System.IO.FileInfo
        }

        It 'Get-ChildItem -Path -Include "*.txt" -Recurse -Name' {
            $result = Get-ChildItem -Path $rootDir -Include "*.txt" -Recurse -Name
            $result.Count | Should -Be 2
            $result | Should -BeOfType System.String
            $result.Where({ $_ -eq "file1.txt" }) | Should -BeOfType System.String
            $result.Where({ $_ -eq "subDir2$($DirSep)file2.txt" }) | Should -BeOfType System.String
        }

        It 'Get-ChildItem -Path -Include "*.t?t" -Recurse -Name' {
            $result = Get-ChildItem -Path $rootDir -Include "*.t?t" -Recurse -Name
            $result.Count | Should -Be 2
            $result | Should -BeOfType System.String
            $result.Where({ $_ -eq "file1.txt" }) | Should -BeOfType System.String
            $result.Where({ $_ -eq "subDir2$($DirSep)file2.txt" }) | Should -BeOfType System.String
        }
    }

    Context 'Validate Get-ChildItem -Path -Include' {
        It 'Get-ChildItem -Path -Include "*.txt" -Force' -Pending:$true {    # Pending due to a bug
            $result = Get-ChildItem -Path $rootDir -Include "*.txt" -Force
            $result.Count | Should -Be 1
            $result | Should -BeOfType System.IO.FileInfo
            $result.Name | Should -BeExactly "file1.txt"
        }

        It 'Get-ChildItem -Path -Include "*.txt" -Recurse -Force' {
            $result = Get-ChildItem -Path $rootDir -Include "*.txt" -Recurse -Force
            $result.Count | Should -Be 4
            $result | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "file1.txt" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "file2.txt" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "file21.txt" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "filehidden3.txt" }) | Should -BeOfType System.IO.FileInfo
        }

        It 'Get-ChildItem -Path -Include "*.txt" -Recurse -Name -Force' {
            $result = Get-ChildItem -Path $rootDir -Include "*.txt" -Recurse -Name -Force
            $result.Count | Should -Be 4
            $result | Should -BeOfType System.String
            $result.Where({ $_ -eq "file1.txt" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir2$($DirSep)file2.txt" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir3$($DirSep)filehidden3.txt" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir2$($DirSep)subDir21$($DirSep)file21.txt" }) | Should -Not -BeNullOrEmpty
        }

        It 'Get-ChildItem -Path -Include "*.t?t" -Recurse -Name -Force' {
            $result = Get-ChildItem -Path $rootDir -Include "*.t?t" -Recurse -Name -Force
            $result.Count | Should -Be 4
            $result | Should -BeOfType System.String
            $result.Where({ $_ -eq "file1.txt" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir2$($DirSep)file2.txt" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir3$($DirSep)filehidden3.txt" } ) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir2$($DirSep)subDir21$($DirSep)file21.txt" }) | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Validate Get-ChildItem -Path -Exclude' {
        It 'Get-ChildItem -Path $rootDir -Exclude "*.txt"' {
            $result = Get-ChildItem -Path $rootDir -Exclude "*.txt"
            $result.Count | Should -Be 3
        }

        It 'Get-ChildItem -Path $rootDir -Exclude "file*"' {
            $result = Get-ChildItem -Path $rootDir -Exclude "file*"
            $result.Count | Should -Be 2
            $result | Should -BeOfType System.IO.DirectoryInfo
            $result.Where({ $_.Name -eq "subDir2" }) | Should -BeOfType System.IO.DirectoryInfo
            $result.Where({ $_.Name -eq "subDir3" }) | Should -BeOfType System.IO.DirectoryInfo
        }

        It 'Get-ChildItem -Path -Exclude "*.txt" -Recurse' {
            $result = Get-ChildItem -Path $rootDir -Exclude "*.txt" -Recurse
            $result.Count | Should -Be 6
            $result.Where({ $_.Name -like "*.txt" }) | Should -BeNullOrEmpty
        }

        It 'Get-ChildItem -Path -Exclude "*.tx?" -Recurse' {
            $result = Get-ChildItem -Path $rootDir -Exclude "*.tx?" -Recurse
            $result.Count | Should -Be 6
            $result.Where({ $_.Name -like "*.tx?" }) | Should -BeNullOrEmpty
        }

        It 'Get-ChildItem -Path -Exclude "*.txt" -Recurse -Name' {
            $result = Get-ChildItem -Path $rootDir -Exclude "*.txt" -Recurse -Name
            $result | Should -BeOfType System.String
            $result.Count | Should -Be 6
            $result.Where({ $_ -like "*.txt" }) | Should -BeNullOrEmpty
        }

        It 'Get-ChildItem -Path -Exclude "*.txt" -Recurse -Hidden' {
            $result = Get-ChildItem -Path $rootDir -Exclude "*.txt" -Recurse -Hidden
            $result.Count | Should -Be 3
            $result.Where({ $_.Name -like "*.txt" }) | Should -BeNullOrEmpty
            $result.Where({ $_.Name -eq "filehidden1.doc" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "filehidden2.asd" }) | Should -BeOfType System.IO.FileInfo
            $result.Where({ $_.Name -eq "subDir21" }) | Should -BeOfType System.IO.DirectoryInfo
        }

        It 'Get-ChildItem -Path -Exclude "*.txt" -Recurse -Hidden -Name' {
            $result = Get-ChildItem -Path $rootDir -Exclude "*.txt" -Recurse -Hidden -Name
            $result.Count | Should -Be 3
            $result.Where({ $_.Name -like "*.txt" }) | Should -BeNullOrEmpty
            $result.Where({ $_ -eq "filehidden1.doc" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir2$($DirSep)filehidden2.asd" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir2$($DirSep)subDir21" }) | Should -Not -BeNullOrEmpty
        }

        It 'Get-ChildItem -Path -Exclude "*.txt" -Include "file*" -Recurse' {
            $result = Get-ChildItem -Path $rootDir -Exclude "*.txt" -Include "file*" -Recurse
            $result.Count | Should -Be 4
            $result.Where({ $_.Name -like "*.txt" }) | Should -BeNullOrEmpty
            $result.Where({ $_.Name -like "file*" }) | Should -BeOfType System.IO.FileInfo
        }
    }

    Context 'Validate Get-ChildItem -Path -Exclude -Force' {
        It 'Get-ChildItem -Path -Exclude "*.txt" -Force' {
            $result = Get-ChildItem -Path $rootDir -Exclude "*.txt" -Force
            $result.Count | Should -Be 4
        }

        It 'Get-ChildItem -Path -Exclude "file*" -Recurse -Force' {
            $result = Get-ChildItem -Path $rootDir -Exclude "file*" -Recurse -Force
            $result.Count | Should -Be 3
            $result | Should -BeOfType System.IO.DirectoryInfo
            $result.Where({ $_.Name -eq "subDir2" }) | Should -BeOfType System.IO.DirectoryInfo
            $result.Where({ $_.Name -eq "subDir3" }) | Should -BeOfType System.IO.DirectoryInfo
            $result.Where({ $_.Name -eq "subDir21" }) | Should -BeOfType System.IO.DirectoryInfo
        }

        It 'Get-ChildItem -Path -Exclude "file*" -Force -Recurse -Name' {
            $result = Get-ChildItem -Path $rootDir -Exclude "file*" -Force -Recurse -Name
            $result.Count | Should -Be 3
            $result | Should -BeOfType System.String
            $result.Where({ $_ -eq "subDir2" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir3" }) | Should -Not -BeNullOrEmpty
            $result.Where({ $_ -eq "subDir2$($DirSep)subDir21" }) | Should -Not -BeNullOrEmpty
        }

        It 'Get-ChildItem -Path -Exclude "*.txt" -Recurse' {
            $result = Get-ChildItem -Path $rootDir -Exclude "*.txt" -Recurse
            $result.Count | Should -Be 6
            $result.Where({ $_.Name -like "*.txt" }) | Should -BeNullOrEmpty
        }

        It 'Get-ChildItem -Path -Exclude "*.txt" -Force -Include "file*" -Recurse' {
            $result = Get-ChildItem -Path $rootDir -Exclude "*.txt" -Force -Include "file*" -Recurse
            $result.Count | Should -Be 6
            $result.Where({ $_.Name -like "*.txt" }) | Should -BeNullOrEmpty
            $result.Where({ $_.Name -like "file*" }) | Should -BeOfType System.IO.FileInfo
        }
    }

    Context 'Validate Get-ChildItem -Path -Exclude/-Include with some filters' {
        It 'Get-ChildItem -Path -Exclude "*.txt","*.asd" -Force -Include "file*" -Recurse' {
            $result = Get-ChildItem -Path $rootDir -Exclude "*.txt","*.asd" -Force -Include "file*" -Recurse
            $result.Count | Should -Be 3
            $result.Where({ $_.Name -like "*.txt" }) | Should -BeNullOrEmpty
            $result.Where({ $_.Name -like "*.asd" }) | Should -BeNullOrEmpty
            $result.Where({ $_.Name -like "file*" }) | Should -BeOfType System.IO.FileInfo
        }

        It 'Get-ChildItem -Path -Exclude "*.txt","*.asd" -Include "file*" -Recurse' {
            $result = Get-ChildItem -Path $rootDir -Exclude "*.txt","*.asd" -Include "file*" -Recurse
            $result.Count | Should -Be 2
            $result.Where({ $_.Name -like "*.txt" }) | Should -BeNullOrEmpty
            $result.Where({ $_.Name -like "*.asd" }) | Should -BeNullOrEmpty
            $result.Where({ $_.Name -like "file*" }) | Should -BeOfType System.IO.FileInfo
        }

        It 'Get-ChildItem -Path -Exclude "*.txt" -Force -Include "*2.*","*3.*" -Recurse' {
            $result = Get-ChildItem -Path $rootDir -Exclude "*.txt" -Force -Include "*2.*","*3.*" -Recurse
            $result.Count | Should -Be 4
            $result.Where({ $_.Name -like "*.txt" }) | Should -BeNullOrEmpty
            $result.Where({ $_.Name -like "*2.*" -or $_.Name -like "*3.*" }) | Should -BeOfType System.IO.FileInfo
        }

        It 'Get-ChildItem -Path -Exclude "*.txt" -Include "*2.*","*3.*" -Recurse' {
            $result = Get-ChildItem -Path $rootDir -Exclude "*.txt" -Include "*2.*","*3.*" -Recurse
            $result.Count | Should -Be 3
            $result.Where({ $_.Name -like "*.txt" }) | Should -BeNullOrEmpty
            $result.Where({ $_.Name -like "*2.*" -or $_.Name -like "*3.*" }) | Should -BeOfType System.IO.FileInfo
        }
    }
}

Describe "Validate Get-Item ResolvedTarget property" -Tags "Feature","RequireAdminOnWindows" {
    BeforeAll {
        $rootDir = Join-Path "TestDrive:" "TestDir"

        Push-Location $rootDir
        $null = New-Item -Path "realDir" -ItemType Directory
        $null = New-Item -Path "toDel" -ItemType Directory
        $null = New-Item -Path "brokenLinkedDir" -ItemType SymbolicLink -Value ".\toDel"
        $null = New-Item -Path "linkedDir" -ItemType SymbolicLink -Value ".\realDir"
        Remove-Item "toDel"
        $null = New-Item -Path "realFile.fil" -ItemType File
        $null = New-Item -Path "toDel.fil" -ItemType File
        $null = New-Item -Path "brokenLinkedFile.fil" -ItemType SymbolicLink -Value ".\toDel.fil"
        $null = New-Item -Path "linkedFile.fil" -ItemType SymbolicLink -Value ".\realFile.fil"
        Remove-Item "toDel.fil"
    }

    AfterAll {
        Pop-Location
    }

    Context 'Get-Item files and folders' {
        It 'Get-Item "linkedDir"' {
            $result = Get-Item "linkedDir"
            $result.ResolvedTarget.EndsWith("realDir") | Should -BeTrue
        }

        It 'Get-Item "linkedFile.fil"' {
            $result = Get-Item "linkedFile.fil"
            $result.ResolvedTarget.EndsWith("realFile.fil") | Should -BeTrue
        }

        It 'Get-Item "brokenLinkedDir"' {
            $result = Get-Item "brokenLinkedDir"
            $result.ResolvedTarget.EndsWith("toDel") | Should -BeTrue
        }

        It 'Get-Item "brokenLinkedFile.fil"' {
            $result = Get-Item "brokenLinkedFile.fil"
            $result.ResolvedTarget.EndsWith("toDel.fil") | Should -BeTrue
        }

        It 'Get-Item "realDir"' {
            $result = Get-Item "realDir"
            $result.ResolvedTarget.EndsWith("realDir") | Should -BeTrue
        }

        It 'Get-Item "realFile.fil' {
            $result = Get-Item "realFile.fil"
            $result.ResolvedTarget.EndsWith("realFile.fil") | Should -BeTrue
        }
    }
}
