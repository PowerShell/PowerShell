
Describe "Test trap" -Tags "CI" {
    Context "Line after exception should not be invoked" {
        It "line after exception should not be invoked" {
            $a = . {trap {"trapped"; continue;}; . {"hello"; throw "exception"; "world"}}
            $a.Length | Should Be 2
        }

        It "line after exception should not be invoked" {
            $a = . {trap {"outside trapped"; continue;}; . {trap {break;}; "hello"; throw "exception"; "world"}}
            $a.Length | Should Be 2
        }

        It "line after exception should be invoked after continue" {
            $a = . {trap {"outside trapped"; continue;} "hello"; throw "exception"; "world"}
            $a.Length | Should Be 3
        }

        It "line after exception should not be invoked" {
            $a = . {trap {"outside trapped"; continue;}; . {trap [system.Argumentexception] {continue;}; "hello"; throw "exception"; "world"}}
            $a.Length | Should Be 2
        }
    }
}
