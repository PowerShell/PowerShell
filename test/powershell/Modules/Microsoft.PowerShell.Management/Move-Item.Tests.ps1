Describe "Move-Item tests" -Tag "CI" {
    BeforeAll {
        $content = "This is content"
        Setup -f originalfile.txt -content "This is content"
        $source = "$TESTDRIVE/originalfile.txt"
        $target = "$TESTDRIVE/ItemWhichHasBeenMoved.txt"
    }
    It "Move-Item will move a file" {
        Move-Item $source $target
        test-path $source | Should be $false
        test-path $target | Should be $true
        "$target" | Should ContainExactly "This is content"
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
            New-Item -ItemType Directory -Path $filterPath
            New-Item -ItemType Directory -Path $moveToPath
            New-Item -ItemType File -Path $fooPath -Value $fooContent
            New-Item -ItemType File -Path $barPath -Value $barContent
            New-Item -ItemType File -Path $booPath -Value $booContent
        }
        AfterEach {
            Remove-Item $filterPath -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Item $moveToPath -Recurse -Force -ErrorAction SilentlyContinue
        }
        It "Can move to different directory, filtered with -Include" {
            Move-Item -Path $filePath -Destination $moveToPath -Include "bar*"
            Test-Path -Path $barPath | Should Be $false
            Test-Path -Path $newBarPath | Should Be $true
            $newBarPath | Should ContainExactly $barContent
        }
        It "Can move to different directory, filtered with -Exclude" {
            Move-Item -Path $filePath -Destination $moveToPath -Exclude "b*"
            Test-Path -Path $fooPath | Should Be $false
            Test-Path -Path $newFooPath | Should Be $true
            $newFooPath | Should ContainExactly $fooContent
        }
        It "Can move to different directory, filtered with -Filter" {
            Move-Item -Path $filePath -Destination $moveToPath -Filter "bo*"
            Test-Path -Path $booPath | Should Be $false
            Test-Path -Path $newBooPath | Should Be $true
            $newBooPath | Should ContainExactly $booContent
        }

        It "Can rename via move, filtered with -Include" {
            Move-Item -Path $filePath -Destination $renameToPath -Include "bar*"
            Test-Path -Path $renameToPath | Should Be $true
            Test-Path -Path $barPath | Should Be $false
            $renameToPath | Should ContainExactly $barContent
        }
        It "Can rename via move, filtered with -Exclude" {
            Move-Item -Path $filePath -Destination $renameToPath -Exclude "b*"
            Test-Path -Path $renameToPath | Should Be $true
            Test-Path -Path $fooPath | Should Be $false
            $renameToPath | Should ContainExactly $fooContent
        }
        It "Can rename via move, filtered with -Filter" {
            Move-Item -Path $filePath -Destination $renameToPath -Filter "bo*"
            Test-Path -Path $renameToPath | Should Be $true
            Test-Path -Path $booPath | Should Be $false
            $renameToPath | Should ContainExactly $booContent
        }
    }
}
