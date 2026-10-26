# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Optimized FileSystem provider instance metadata" -Tags "CI" {
    BeforeAll {
        $file = Join-Path $TestDrive "sample[1].txt"
        $driveName = "OptMeta"
        New-PSDrive -Name $driveName -PSProvider FileSystem -Root $TestDrive | Out-Null
        $script:optimizedMetadataEnabled = [bool](Get-ExperimentalFeature -Name PSOptimizedProviderObjects).Enabled
    }

    BeforeEach {
        Remove-Item -LiteralPath (Join-Path $TestDrive "moved.txt") -Force -ErrorAction SilentlyContinue
        Set-Content -LiteralPath $file -Value @("one", "two", "three")
        New-Item -ItemType Directory -Path (Join-Path $TestDrive "container") -Force | Out-Null
    }

    AfterAll {
        Remove-PSDrive -Name $driveName -Force -ErrorAction SilentlyContinue
    }

    It "registers the optimization feature" {
        $feature = Get-ExperimentalFeature -Name PSOptimizedProviderObjects
        $feature | Should -Not -BeNullOrEmpty
        $feature.Name | Should -BeExactly "PSOptimizedProviderObjects"
        $feature.Description | Should -Not -BeNullOrEmpty
    }

    It "preserves file metadata and ordinary mutation semantics" {
        $item = Get-Item -LiteralPath $file
        $item.PSPath | Should -Match "FileSystem::"
        $item.PSIsContainer | Should -BeFalse
        $item.PSObject.Properties["PSPath"].MemberType | Should -Be "NoteProperty"

        $item.PSPath = "changed"
        $item.PSPath | Should -BeExactly "changed"
        $item.PSObject.Properties.Remove("PSPath")
        $item.PSObject.Properties["PSPath"] | Should -BeNullOrEmpty
    }

    It "preserves DirectoryInfo container and path metadata" {
        $directoryPath = Join-Path $TestDrive "container"
        $directory = Get-Item -LiteralPath $directoryPath
        $directory.PSIsContainer | Should -BeTrue
        $directory.PSPath | Should -Match "FileSystem::"
        $directory.PSParentPath | Should -Match ([regex]::Escape((Split-Path -Parent $directoryPath)))
        $directory.PSChildName | Should -BeExactly "container"
    }

    It "keeps cached member reads correct across ordinary and promoted objects" {
        function Read-Path($value) { $value.PSPath }

        $optimized = Get-Item -LiteralPath $file
        $ordinary = [pscustomobject]@{ PSPath = "ordinary-path" }
        Read-Path $optimized | Should -Match "FileSystem::"
        Read-Path $ordinary | Should -BeExactly "ordinary-path"
        Read-Path $optimized | Should -Match "FileSystem::"

        $optimized.PSPath = "promoted-path"
        Read-Path $optimized | Should -BeExactly "promoted-path"
        Read-Path $ordinary | Should -BeExactly "ordinary-path"
        Read-Path $optimized | Should -BeExactly "promoted-path"
    }

    It "preserves content paths and ReadCount for individual lines" {
        $results = @(Get-Content -LiteralPath $file -ReadCount 1)
        $results.Count | Should -Be 3
        for ($i = 0; $i -lt $results.Count; $i++) {
            $results[$i].PSPath | Should -BeExactly $file
            $results[$i].PSParentPath | Should -BeExactly (Split-Path -Parent $file)
            $results[$i].ReadCount | Should -Be ($i + 1)
        }
    }

    It "preserves metadata for scalar byte-stream output" {
        $byteFile = Join-Path $TestDrive "bytes.bin"
        [System.IO.File]::WriteAllBytes($byteFile, [byte[]](7, 7, 9))
        $bytes = @(Get-Content -LiteralPath $byteFile -AsByteStream)
        $bytes.Count | Should -Be 3
        $bytes[0].GetType().FullName | Should -BeExactly "System.Byte"
        $bytes[0].ReadCount | Should -Be 1
        $bytes[1].ReadCount | Should -Be 2
        $bytes[2].ReadCount | Should -Be 3
        $bytes[0].PSPath | Should -BeExactly $byteFile
        $bytes[0].ReadCount = 99
        $bytes[0].ReadCount | Should -Be 99
        $bytes[1].ReadCount | Should -Be 2

        $raw = @(Get-Content -LiteralPath $byteFile -AsByteStream -Raw)
        $raw.Count | Should -Be 1
        $raw[0].ReadCount | Should -Be 1

        $batch = @(Get-Content -LiteralPath $byteFile -AsByteStream -ReadCount 2)
        $batch[0].ReadCount | Should -Be 2
        $batch[1].ReadCount | Should -Be 3

        $xmlPath = Join-Path $TestDrive "bytes.xml"
        Get-Content -LiteralPath $byteFile -AsByteStream | Export-Clixml -LiteralPath $xmlPath
        $roundTrip = @(Import-Clixml -LiteralPath $xmlPath)
        $roundTrip.Count | Should -Be 3
        $roundTrip[0].ReadCount | Should -Be 1
        $roundTrip[1].PSPath | Should -BeExactly $byteFile
    }

    It "preserves ReadCount for chunks and raw content" {
        $chunks = @(Get-Content -LiteralPath $file -ReadCount 2)
        $chunks.Count | Should -Be 2
        $chunks[0].ReadCount | Should -Be 2
        $chunks[1].ReadCount | Should -Be 3

        $raw = @(Get-Content -LiteralPath $file -Raw)
        $raw.Count | Should -Be 1
        $raw[0].ReadCount | Should -Be 1

        $all = @(Get-Content -LiteralPath $file -ReadCount 0)
        $all.Count | Should -Be 1
        $all[0].ReadCount | Should -Be 3
    }

    It "isolates metadata for repeated empty lines and preserves custom drive context" {
        $emptyLineFile = Join-Path $TestDrive "empty-lines.txt"
        [System.IO.File]::WriteAllText($emptyLineFile, "`n`ntail`n")

        $lines = @(Get-Content -LiteralPath $emptyLineFile -ReadCount 1)
        $lines.Count | Should -Be 3
        $lines[0].ReadCount | Should -Be 1
        $lines[1].ReadCount | Should -Be 2
        $lines[0].PSObject.Properties.Remove("ReadCount")
        $lines[1].ReadCount | Should -Be 2

        $driveLines = @(Get-Content -LiteralPath "${driveName}:\sample[1].txt" -ReadCount 1)
        $driveLines[0].PSDrive.Name | Should -BeExactly $driveName
        $driveLines[0].PSProvider.Name | Should -BeExactly "FileSystem"
        $driveLines[0].PSPath | Should -BeExactly "${driveName}:\sample[1].txt"
    }

    It "preserves metadata through Export-Clixml and Import-Clixml" {
        $item = Get-Item -LiteralPath $file
        $xmlPath = Join-Path $TestDrive "item.xml"
        $item | Export-Clixml -LiteralPath $xmlPath
        $deserialized = Import-Clixml -LiteralPath $xmlPath
        $deserialized.PSPath | Should -Be $item.PSPath
        $formatted = $deserialized | Format-Table | Out-String -Width 240
        $formatted | Should -Match ([regex]::Escape($TestDrive))
    }

    It "preserves primitive content metadata through CLIXML" {
        $xmlPath = Join-Path $TestDrive "content.xml"
        Get-Content -LiteralPath $file | Export-Clixml -LiteralPath $xmlPath
        $deserialized = @(Import-Clixml -LiteralPath $xmlPath)
        $deserialized.Count | Should -Be 3
        for ($i = 0; $i -lt $deserialized.Count; $i++) {
            $deserialized[$i].PSPath | Should -BeExactly $file
            $deserialized[$i].ReadCount | Should -Be ($i + 1)
        }
    }

    It "uses live paths when enabled and preserves legacy snapshots when disabled after FileInfo.MoveTo" {
        $item = Get-Item -LiteralPath $file
        $oldPath = $item.PSPath
        $newPath = Join-Path $TestDrive "moved.txt"
        $item.MoveTo($newPath)
        $item.Name | Should -BeExactly "moved.txt"

        if ($script:optimizedMetadataEnabled) {
            $item.PSChildName | Should -BeExactly "moved.txt"
            $expectedPath = "Microsoft.PowerShell.Core\FileSystem::{0}" -f $newPath
            $item.PSPath | Should -BeExactly $expectedPath
        }
        else {
            $item.PSChildName | Should -BeExactly "sample[1].txt"
            $item.PSPath | Should -BeExactly $oldPath
        }
    }

    It "keeps materialized metadata as a snapshot after FileInfo.MoveTo" {
        $item = Get-Item -LiteralPath $file
        $oldPath = $item.PSPath
        $oldChildName = $item.PSChildName
        $null = $item.PSObject.Properties["PSPath"]
        $newPath = Join-Path $TestDrive "moved.txt"
        $item.MoveTo($newPath)

        $item.PSPath | Should -BeExactly $oldPath
        $item.PSChildName | Should -BeExactly $oldChildName
    }

    It "preserves custom PSDrive root and PSDrive identity" {
        $driveItem = Get-Item -LiteralPath "${driveName}:\sample[1].txt"
        $driveItem.PSDrive | Should -Not -BeNullOrEmpty
        $driveItem.PSDrive.Name | Should -BeExactly $driveName
        $driveItem.PSProvider.Name | Should -BeExactly "FileSystem"
        [object]::ReferenceEquals($driveItem.PSProvider, $driveItem.PSDrive.Provider) | Should -BeTrue
        $driveItem.PSPath | Should -Match "FileSystem::"

        $literalPathItem = Get-Item -LiteralPath $file
        $driveItem.PSChildName | Should -BeExactly $literalPathItem.PSChildName
        if ($script:optimizedMetadataEnabled) {
            $driveItem.PSParentPath | Should -BeExactly $literalPathItem.PSParentPath
        }

        $driveRoot = Get-Item -LiteralPath "${driveName}:\"
        $driveRoot.PSIsContainer | Should -BeTrue
        if ($script:optimizedMetadataEnabled) {
            $expectedRootPath = "Microsoft.PowerShell.Core\FileSystem::{0}" -f $driveRoot.FullName
            $driveRoot.PSPath | Should -BeExactly $expectedRootPath
            $driveRoot.PSParentPath | Should -BeExactly ""
            $driveRoot.PSChildName | Should -BeExactly ([System.IO.DirectoryInfo]::new($TestDrive).Name)
        }
    }

    It "preserves parent and child metadata for the filesystem root" {
        $rootPath = [System.IO.Path]::GetPathRoot($file)
        $rootItem = Get-Item -LiteralPath $rootPath
        $rootItem.PSIsContainer | Should -BeTrue
        if ($script:optimizedMetadataEnabled) {
            $expectedRootPath = "Microsoft.PowerShell.Core\FileSystem::{0}" -f $rootPath
            $rootItem.PSPath | Should -BeExactly $expectedRootPath
            $rootItem.PSParentPath | Should -BeExactly ""
            $expectedRootChildName = if ($IsWindows) { $rootPath } else { "" }
            $rootItem.PSChildName | Should -BeExactly $expectedRootChildName
        }
    }

    It "exposes registry Property and a qualified parent path" -Skip:(!$IsWindows) {
        $item = Get-Item "HKCU:\Software"
        $item.PSObject.Properties["Property"] | Should -Not -BeNullOrEmpty
        $item.Property | Should -HaveCount @($item.PSObject.BaseObject.GetValueNames()).Count
        $item.PSObject.Properties["Property"].MemberType | Should -Be "NoteProperty"
        $item.PSParentPath | Should -BeExactly "Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER"
    }

    It "exposes UnixStat and derived fields" -Skip:($IsWindows) {
        $item = Get-Item -LiteralPath $file
        $item.UnixStat | Should -Not -BeNullOrEmpty
        $item.User | Should -Not -BeNullOrEmpty
        $item.Group | Should -Not -BeNullOrEmpty
        $item.Size | Should -BeGreaterThan 0
    }
}
