Describe "Testing of PSObject behaviour" -Tags "DRT" {
    
    It "Tests that PSObject.ToString() doesn't invoke script properties" {
        $env:Bug_2108520_Evaluated = "False"
        $x = New-Object PSObject
        $x | Add-Member ScriptProperty "Test" { $env:Bug_2108520_Evaluated = "True" }
        $x | Add-Member NoteProperty "Test2" 2
        $env:Bug_2108520_Evaluated | Should be "False"
    }
}