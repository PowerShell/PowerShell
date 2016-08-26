Describe "Rename-Item tests" -Tag "CI" {
    BeforeAll {
        $content = "This is content"
        Setup -f originalFile.txt -content "This is content"
        $source = "$TESTDRIVE/originalFile.txt"
        $target = "$TESTDRIVE/ItemWhichHasBeenRenamed.txt"
    }
    It "Rename-Item will rename a file" {
        Rename-Item $source $target
        test-path $source | Should be $false
        test-path $target | Should be $true
        "$target" | Should ContainExactly "This is content"
    }
}
