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

    Context "Move-Item with wildcard source and non-existent destination" {
        BeforeEach {
            $sourcePath = "$TESTDRIVE/install"
            $destPath = "$TESTDRIVE/copy"
            
            # Setup directory structure
            New-Item -ItemType Directory -Path "$sourcePath/bin" -Force | Out-Null
            New-Item -ItemType File -Path "$sourcePath/bin/test.dll" | Out-Null
            New-Item -ItemType Directory -Path "$sourcePath/lib" | Out-Null
            New-Item -ItemType File -Path "$sourcePath/ReadMe.md" -Value "readme content" | Out-Null
            New-Item -ItemType Directory -Path $destPath | Out-Null
        }
        
        AfterEach {
            Remove-Item "$TESTDRIVE/install" -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Item "$TESTDRIVE/copy" -Recurse -Force -ErrorAction SilentlyContinue
        }
        
        It "Should preserve directory structure when moving with wildcard to non-existent destination" {
            Move-Item -Path "$sourcePath/*" -Destination "$destPath/app"
            
            # Verify all items were moved
            "$sourcePath/bin" | Should -Not -Exist
            "$sourcePath/lib" | Should -Not -Exist
            "$sourcePath/ReadMe.md" | Should -Not -Exist
            
            # Verify directory structure is preserved (bin should not be flattened)
            "$destPath/app/bin" | Should -Exist
            "$destPath/app/bin/test.dll" | Should -Exist
            "$destPath/app/lib" | Should -Exist
            "$destPath/app/ReadMe.md" | Should -Exist
            "$destPath/app/ReadMe.md" | Should -FileContentMatchExactly "readme content"
        }
    }
}
