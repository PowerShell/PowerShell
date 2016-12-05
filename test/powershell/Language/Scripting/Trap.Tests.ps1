
Describe "Test trap" -Tags "CI" {
    Context "Trap with flow control" {
        It "Line after exception should NOT be continued when it's from a nested script block" {
            $a = . {trap {"trapped"; continue;}; . {"hello"; throw "exception"; "world"}}
            $a.Length | Should Be 2
            $a -join "," | Should Be "hello,trapped"
        }

        It "Line after exception should NOT be continued and both inner and outter traps should be triggered" {
            $a = . {trap {"outter trap"; continue;}; . {trap {"inner trap"; break;}; "hello"; throw "exception"; "world"}}
            $a.Length | Should Be 3
            $a -join "," | Should Be "hello,inner trap,outter trap"
        }

        It "Line after exception should be invoked after continue" {
            $a = . {trap {"outter trap"; continue;} "hello"; throw "exception"; "world"}
            $a.Length | Should Be 3
            $a -join "," | Should Be "hello,outter trap,world"
        }

        It "Line after exception should NOT be invoked and inner trap should not be triggered" {
            $a = . {trap {"outter trap"; continue;}; . {trap [system.Argumentexception] {"inner trap"; continue;}; "hello"; throw "exception"; "world"}}
            $a.Length | Should Be 2
            $a -join "," | Should Be "hello,outter trap"
        }
    }
}
