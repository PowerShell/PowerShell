Describe "Start-Sleep" {

    Context "Validate Start-Sleep works properly" {
        It "Should only sleep for at least 3 seconds"{
           $result = Measure-Command { Start-Sleep -s 3 }
	     $result.Seconds | Should BeGreaterThan 2
	 }

        It "Should sleep for at least 3 seconds using the alias" {
	    $result = Measure-Command { sleep -s 3 }
	    $result.Seconds | Should BeGreaterThan 2
	}
    }
}
