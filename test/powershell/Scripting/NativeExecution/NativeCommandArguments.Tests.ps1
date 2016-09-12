Describe "Native Command Arguments" -tags "CI" {
    $testPath = 'TestDrive:\test.txt'
    $powershellTestDir = $PSScriptRoot
    while ($powershellTestDir -notmatch 'test[\\/]powershell$') {
        $powershellTestDir = Split-Path $powershellTestDir
    }
    $echoArgsDir = Join-Path (Split-Path $powershellTestDir) EchoArgs

    function Start-EchoArgs {
        Push-Location $echoArgsDir
        try {
            dotnet run $args
        } finally {
            Pop-Location
        }
    }

    It "Should handle quoted spaces correctly" {
        $a = 'a"b c"d'
        Start-EchoArgs $a 'a"b c"d' a"b c"d >$testPath
        $testPath | Should Contain 'Arg 0 is <ab cd>'
        $testPath | Should Contain 'Arg 1 is <ab cd>'
        $testPath | Should Contain 'Arg 2 is <ab cd>'
    }

    It "Should handle spaces between escaped quotes" {
        Start-EchoArgs 'a\"b c\"d' "a\`"b c\`"d" >$testPath
        $testPath | Should Contain 'Arg 0 is <a"b c"d>'
        $testPath | Should Contain 'Arg 1 is <a"b c"d>'
    }
}
