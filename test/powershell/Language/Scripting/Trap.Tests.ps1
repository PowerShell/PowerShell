#  <Test>
#    <TestType>DRT</TestType>
#    <summary>Exception handling</summary>
#  </Test>

. "$($args[0])\..\asserts.ps1"

#############################################################
#
# Line after exception should not be invoked.
#
#############################################################

$a = . {trap {"trapped"; continue;}; . {"hello"; throw "exception"; "world"}}

Assert ($a.Length -eq 2) "line after exception should not be invoked"

$a = . {trap {"outside trapped"; continue;}; . {trap {break;}; "hello"; throw "exception"; "world"}}

Assert ($a.Length -eq 2) "line after exception should not be invoked"

$a = . {trap {"outside trapped"; continue;} "hello"; throw "exception"; "world"}

Assert ($a.Length -eq 3) "line after exception should be invoked after continue"

$a = . {trap {"outside trapped"; continue;}; . {trap [system.Argumentexception] {continue;}; "hello"; throw "exception"; "world"}}

Assert ($a.Length -eq 2) "line after exception should not be invoked"
