# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Move-Item tests" -Tag "CI" {
    BeforeAll {
        $content = "This is content"
        Setup -f originalfile.txt -Content "This is content"
        $source = "$TESTDRIVE/originalfile.txt"
        $target = "$TESTDRIVE/ItemWhichHasBeenMoved.txt"
        Setup -f [orig-file].txt -Content "This is not content"
        $sourceSp = "$TestDrive/``[orig-file``].txt"
        $targetSpName = "$TestDrive/ItemWhichHasBeen[Moved].txt"
        $targetSp = "$TestDrive/ItemWhichHasBeen``[Moved``].txt"
    }
    It "Move-Item will move a file" {
        Move-Item $source $target
        $source | Should -Not -Exist
        $target | Should -Exist
        "$target" | Should -FileContentMatchExactly "This is content"
    }
    It "Move-Item will move a file when path contains special char" {
        Move-Item $sourceSp $targetSpName
        $sourceSp | Should -Not -Exist
        $targetSp | Should -Exist
        $targetSp | Should -FileContentMatchExactly "This is not content"
    }

    Context "Move-Item with filters" {
        BeforeAll {
            $filterPath = "$TESTDRIVE/filterTests"
            $moveToPath = "$TESTDRIVE/dest-dir"
            $renameToPath = Join-Path $filterPath "move.txt"
            $filePath = Join-Path $filterPath "*"
            $fooFile = "foo.txt"
            $barFile = "bar.txt"
            $booFile = "boo.txt"
            $fooPath = Join-Path $filterPath $fooFile
            $barPath = Join-Path $filterPath $barFile
            $booPath = Join-Path $filterPath $booFile
            $newFooPath = Join-Path $moveToPath $fooFile
            $newBarPath = Join-Path $moveToPath $barFile
            $newBooPath = Join-Path $moveToPath $booFile
            $fooContent = "foo content"
            $barContent = "bar content"
            $booContent = "boo content"
        }
        BeforeEach {
            New-Item -ItemType Directory -Path $filterPath | Out-Null
            New-Item -ItemType Directory -Path $moveToPath | Out-Null
            New-Item -ItemType File -Path $fooPath -Value $fooContent | Out-Null
            New-Item -ItemType File -Path $barPath -Value $barContent | Out-Null
            New-Item -ItemType File -Path $booPath -Value $booContent | Out-Null
        }
        AfterEach {
            Remove-Item $filterPath -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Item $moveToPath -Recurse -Force -ErrorAction SilentlyContinue
        }
        It "Can move to different directory, filtered with -Include" {
            Move-Item -Path $filePath -Destination $moveToPath -Include "bar*" -ErrorVariable e -ErrorAction SilentlyContinue
            $e | Should -BeNullOrEmpty
            $barPath | Should -Not -Exist
            $newBarPath | Should -Exist
            $booPath | Should -Exist
            $fooPath | Should -Exist
            $newBarPath | Should -FileContentMatchExactly $barContent
        }
        It "Can move to different directory, filtered with -Exclude" {
            Move-Item -Path $filePath -Destination $moveToPath -Exclude "b*" -ErrorVariable e -ErrorAction SilentlyContinue
            $e | Should -BeNullOrEmpty
            $fooPath | Should -Not -Exist
            $newFooPath | Should -Exist
            $booPath | Should -Exist
            $barPath | Should -Exist
            $newFooPath | Should -FileContentMatchExactly $fooContent
        }
        It "Can move to different directory, filtered with -Filter" {
            Move-Item -Path $filePath -Destination $moveToPath -Filter "bo*" -ErrorVariable e -ErrorAction SilentlyContinue
            $e | Should -BeNullOrEmpty
            $booPath | Should -Not -Exist
            $newBooPath | Should -Exist
            $barPath | Should -Exist
            $fooPath | Should -Exist
            $newBooPath | Should -FileContentMatchExactly $booContent
        }

        It "Can rename via move, filtered with -Include" {
            Move-Item -Path $filePath -Destination $renameToPath -Include "bar*" -ErrorVariable e -ErrorAction SilentlyContinue
            $e | Should -BeNullOrEmpty
            $renameToPath | Should -Exist
            $barPath | Should -Not -Exist
            $booPath | Should -Exist
            $fooPath | Should -Exist
            $renameToPath | Should -FileContentMatchExactly $barContent
        }
        It "Can rename via move, filtered with -Exclude" {
            Move-Item -Path $filePath -Destination $renameToPath -Exclude "b*" -ErrorVariable e -ErrorAction SilentlyContinue
            $e | Should -BeNullOrEmpty
            $renameToPath | Should -Exist
            $fooPath | Should -Not -Exist
            $booPath | Should -Exist
            $barPath | Should -Exist
            $renameToPath | Should -FileContentMatchExactly $fooContent
        }
        It "Can rename via move, filtered with -Filter" {
            Move-Item -Path $filePath -Destination $renameToPath -Filter "bo*" -ErrorVariable e -ErrorAction SilentlyContinue
            $e | Should -BeNullOrEmpty
            $renameToPath | Should -Exist
            $booPath | Should -Not -Exist
            $fooPath | Should -Exist
            $barPath | Should -Exist
            $renameToPath | Should -FileContentMatchExactly $booContent
        }
    }
}

Describe "Move-Item with -PreservePathFrom" -Tag "CI" {
    BeforeAll {
        $sourceDirectory = "$TESTDRIVE/PreservePathFromSource"
        $destinationDirectory = "$TESTDRIVE/PreservePathFromDestination"

        # Returns the relative paths of all files below a path, with platform
        # separators and in a stable order, so that they can be compared.
        function Get-RelativeFilePathsBelow {
            param([string]$Path)

            Get-ChildItem $Path -Recurse -File | ForEach-Object {
                $_.FullName.Substring($Path.Length).TrimStart('\', '/') -replace '/', [IO.Path]::DirectorySeparatorChar
            } | Sort-Object
        }
    }

    BeforeEach {
        Remove-Item $sourceDirectory, $destinationDirectory -Recurse -Force -ErrorAction SilentlyContinue

        New-Item -ItemType Directory -Path (Join-Path $sourceDirectory "Sub1/Deep"), (Join-Path $sourceDirectory "Sub2") -Force | Out-Null
        Set-Content (Join-Path $sourceDirectory "Root.log") -Value "root"
        Set-Content (Join-Path $sourceDirectory "Sub1/First.log") -Value "first"
        Set-Content (Join-Path $sourceDirectory "Sub1/Deep/Second.log") -Value "second"
        Set-Content (Join-Path $sourceDirectory "Sub2/Third.log") -Value "third"
    }

    AfterAll {
        Remove-Item $sourceDirectory, $destinationDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }

    It "Preserves the folder structure of piped files below the base path" {
        Get-ChildItem $sourceDirectory -File -Recurse |
            Move-Item -PreservePathFrom $sourceDirectory -Destination $destinationDirectory

        $separator = [IO.Path]::DirectorySeparatorChar
        Get-RelativeFilePathsBelow $destinationDirectory | Should -Be @(
            "Root.log"
            "Sub1${separator}Deep${separator}Second.log"
            "Sub1${separator}First.log"
            "Sub2${separator}Third.log"
        )
        (Get-ChildItem $sourceDirectory -Recurse -File).Count | Should -Be 0
    }

    It "Preserves the folder structure of piped directories" {
        Get-ChildItem $sourceDirectory -Directory |
            Move-Item -PreservePathFrom $sourceDirectory -Destination $destinationDirectory

        Join-Path $destinationDirectory "Sub1/First.log" | Should -Exist
        Join-Path $destinationDirectory "Sub1/Deep/Second.log" | Should -Exist
        Join-Path $destinationDirectory "Sub2/Third.log" | Should -Exist
        Join-Path $sourceDirectory "Sub1" | Should -Not -Exist
    }

    It "Writes a non-terminating error when the item is not below the base path" {
        $outsideFile = "$TESTDRIVE/PreservePathFromOutside.log"
        Set-Content $outsideFile -Value "outside"

        try {
            $errorRecord = $null
            Move-Item $outsideFile `
                -PreservePathFrom $sourceDirectory `
                -Destination $destinationDirectory -ErrorVariable errorRecord -ErrorAction SilentlyContinue

            $errorRecord.FullyQualifiedErrorId | Should -Be "PreservePathFromOutsideBasePath,Microsoft.PowerShell.Commands.MoveItemCommand"
            $outsideFile | Should -Exist
        }
        finally {
            Remove-Item $outsideFile -Force -ErrorAction SilentlyContinue
        }
    }

    It "Writes an error when the base path does not exist" {
        $errorRecord = $null
        Move-Item (Join-Path $sourceDirectory "Root.log") `
            -PreservePathFrom "$TESTDRIVE/DoesNotExist" `
            -Destination $destinationDirectory -ErrorVariable errorRecord -ErrorAction SilentlyContinue

        $errorRecord.FullyQualifiedErrorId | Should -Be "PreservePathFromPathNotFound,Microsoft.PowerShell.Commands.MoveItemCommand"
    }
}
