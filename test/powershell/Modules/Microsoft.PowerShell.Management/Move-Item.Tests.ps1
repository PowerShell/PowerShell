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
}
