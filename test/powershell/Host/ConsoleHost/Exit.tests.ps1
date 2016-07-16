$TESTS = @{
    Title = "Exit 0 is preserved"
    Script = "exit 0"
    Expected = 0
},@{
    Title = "Exit 42 is preserved"
    Script = "exit 42"
    Expected = 42
},@{
    Title = "Exit -1 is preserved (converted to 255 on non-windows)"
    Script = "exit -1"
    # linux doesn't have -1 as a exit code, covered to 255
    Expected = if ( $IsWindows ) { -1 } else { 255 }
},@{
    Title = "An explicit throw causes exit 1"
    Script = 'throw "badthing"'
    Expected = 1
},@{
    Title = "A caught throw does not leak with an explicit exit"
    Script = '$v = 0; try { 1 / $v } catch { ; }; exit 42'
    Expected = 42    
},@{
    Title = "A parse error causes an exit 1"
    Script = "{"
    Expected = 1
},@{
    Title = "Pipeline terminating errors cause exit 1"
    Script = "Remove-Item TESTDRIVE:\sdflksjlskdjlsjdlsfjlsdkjsflj -ea stop"
    Expected = 1
},@{
    Title = "Caught pipeline terminating errors with implicit exit result in 0"
    Script = "try { Remove-Item TESTDRIVE:\sdflksjlskdjlsjdlsfjlsdkjsflj -ea stop } catch { ; }"
    Expected = 0
},@{
    Title = "Failed command still exits with 0"
    Script = "Remove-Item TESTDRIVE:\lsdkfjsldkjsfdlkjsd"
    Expected = 0
},@{
    Title = "a function that throws without upper level catch causes exit 1"
    Script = "function doit { throw 'badness' }; doit"
    Expected = 1
},@{
    Title = "Command Discovery failures exit with 0"
    Script = "get-sdlkjsdflkjsdflkjsdlsjdfljkf"
    Expected = 0
},@{
    Title = "a command terminating error does not change exit code from 0"
    Script = "'aa' | out-file TESTDRIVE:\dir1\dir2\dir3\file"
    Expected = 0
},@{
    Title = "An error in a C# instance method does not change exit code from 0"
    Script = '"abc".SubString(0,10)'
    Expected = 0
}


Describe "PowerShell with -file should preserve exit codes" -Tags "Slow" {
    BeforeAll {
        $TestScript = "${TESTDRIVE}\ExitTest.ps1"
        $powershellexe = (get-process -id $PID).MainModule.FileName
    }
    AfterEach {
        Remove-Item ${TestScript} -ea SilentlyContinue
    }
    It "<title>" -testcases $TESTS {
        param ( $title, $script, $expected )
        $script > $TestScript
        & $powershellexe -noprofile -file $TestScript 
        $LASTEXITCODE| Should be $expected
    }
    #foreach ( $test in $TESTS )
    #{
    #    $test.Script > $TestScript
    #    It $Test.Title {
    #        & $powershellexe -noprofile -file $TestScript 
    #        $LASTEXITCODE| Should be $Test.Expected
    #    }
    #}

    Context "Additional Exit Tests" {
        It "A badly typed parameter changes the exit code to 1" {
            'param([int]$i)' > $TestScript
            & $powershellexe -noprofile -file $TestScript one
            $LASTEXITCODE | Should be 1
        }

        It "A set validation failure changes the exit code to 1" {
            'param([ValidateSet("nope")][string]$i)' > $TestScript
            & $powershellexe -noprofile -file $TestScript one
            $LASTEXITCODE | Should be 1
        }


        It "Setting execution mode to AllSigned causes unsigned scripts to exit 1" -skip:($IsCore) {
            "'hello world'" > $TestScript
            & $powershellexe -noprofile -ExecutionPolicy AllSigned -file $TestScript
            $LASTEXITCODE | Should Be 1
        }
    }
}
