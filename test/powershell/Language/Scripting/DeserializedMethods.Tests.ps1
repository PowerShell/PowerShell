Describe "DeserializedMethods" -Tags "CI" {

    BeforeAll {
        try {
            $temp = [io.path]::GetTempFileName()
        } catch {
            Remove-Item $temp -force -ea silentlycontinue
            return
        }
    }

    AfterAll {
	        Remove-Item $temp -force -ea silentlycontinue
        }    
    It "Deserialized objects shouldn't ever have any methods (unless they are primitive known types)" {
        $a = new-object collections.arraylist
        $null = $a.Add(1)
        $null = $a.Add(2)
        $null = $a.Add(3)
        
	    # using linkedlist that implements IEnumerable,
	    # but doesn't implement IList or IList<T>
	    $x = new-object collections.generic.linkedlist[int]
	    $null = $x.Add(123)
	    $null = $x.Add(456)

	    export-clixml -in $x -path $temp
	    $d = import-clixml $temp	
	    
	    try
	    {
		    $null = $d.Add(789)
            Throw "Execution OK"
	    }
	    catch
	    {            	        
            $_.FullyQualifiedErrorId | Should Be "MethodNotFound"
	    }       
        
    }
}