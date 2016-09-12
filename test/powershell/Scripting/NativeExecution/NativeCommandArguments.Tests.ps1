Describe "Native Command Arguments" -tags "CI" {
    $testPath = 'TestDrive:\test.txt'
    $powershellTestDir = $PSScriptRoot
    while ($powershellTestDir -notmatch 'test[\\/]powershell$') {
        $powershellTestDir = Split-Path $powershellTestDir
    }
    $echoArgs = Join-Path (Split-Path $powershellTestDir) tools/EchoArgs/bin/echoargs

    It "Should handle quoted spaces correctly" {
        $a = 'a"b c"d'
        & $echoArgs $a 'a"b c"d' a"b c"d >$testPath
        $testPath | Should Contain 'Arg 0 is <ab cd>'
        $testPath | Should Contain 'Arg 1 is <ab cd>'
        $testPath | Should Contain 'Arg 2 is <ab cd>'
    }

    It "Should handle spaces between escaped quotes" {
        & $echoArgs 'a\"b c\"d' "a\`"b c\`"d" >$testPath
        $testPath | Should Contain 'Arg 0 is <a"b c"d>'
        $testPath | Should Contain 'Arg 1 is <a"b c"d>'
    }
}
