Describe "Move-Item tests" -Tag "CI" {
    BeforeAll {
        $content = "This is content"
        Setup -f originalfile.txt -content "This is content"
        $source = "$TESTDRIVE/originalfile.txt"
        $target = "$TESTDRIVE/ItemWhichHasBeenRenamed.txt"
    }
    It "Rename-Item will rename a file" {
        Rename-Item $source $target
        test-path $source | Should be $false
        test-path $target | Should be $true
        "$target" | Should ContainExactly "This is content"
    }
}
