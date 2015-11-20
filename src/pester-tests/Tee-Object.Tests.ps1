$here = Split-Path -Parent $MyInvocation.MyCommand.Path

Describe "Tee-Object" {

    Context "Validate Tee-Object is correctly forking output" {

        It "Should return the output to the screen and to the variable" {
            $teefile = "$here/assets/testfile.txt"
	    echo teeobjecttest1    |    Tee-Object -variable teeresults
	    $teeresults                 |     Should Be "teeobjecttest1"
	    Remove-Item $teefile -ErrorAction SilentlyContinue
	}

        It "Should return the output to the screen and to the variable from the alias" {
	     $teefile = "$here/assets/testfile.txt"
            echo teeobjecttest2    |    tee -variable teeresults
            $teeresults                 |    Should Be "teeobjecttest2"
	     Remove-Item $teefile -ErrorAction SilentlyContinue
	 }

         It "Should tee the output to a file" {
	     $teefile = "$here/assets/testfile.txt"
	     echo teeobjecttest3  | Tee-Object $teefile
	     Get-Content $teefile | Should Be "teeobjecttest3"
	     Remove-Item $teefile -ErrorAction SilentlyContinue
	 }

         It "Should tee the output to a file using the alias" {
	     $teefile = "$here/assets/testfile.txt"
	     echo teeobjecttest4  | tee $teefile
	     Get-Content $teefile | Should Be "teeobjecttest4"
	     Remove-item $teefile -ErrorAction SilentlyContinue
	 }      
    }
}
