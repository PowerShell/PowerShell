$here = Split-Path -Parent $MyInvocation.MyCommand.Path

Describe "Import-Alias" {

    #Alias file with only the addition of "pesterecho" aliased to echo
    $pesteraliasfile = "$here/assets/pesteralias.txt"

    Context "Validate ability to import alias file" {

	It "Should be able to import an alias file successfully" {
            { Import-Alias $pesteraliasfile } | Should Not throw
	}

	It "Should be able to import file via the Import-Alias alias of ipal" {
            { ipal $pesteraliasfile } | Should Not throw
        }

        It "Should be able to import an alias file and perform imported aliased echo cmd" {
	    (Import-Alias $pesteraliasfile)
	    (pesterecho pestertesting) | Should Be "pestertesting"
	}

        It "Should be able to use ipal alias to import an alias file and perform cmd" {
            (ipal $pesteraliasfile)
	    (pesterecho pestertesting) | Should be "pestertesting"
	}
        
    }
}
