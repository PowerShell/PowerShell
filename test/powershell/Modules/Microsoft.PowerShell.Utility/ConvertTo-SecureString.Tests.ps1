Describe "ConvertTo-SecureString" -Tags "CI" {

    Context "Checking return types of ConvertTo-SecureString" {

	It "Should return System.Security.SecureString after converting plaintext variable"{
	    $PesterTestConvert = (ConvertTo-SecureString "plaintextpester" -AsPlainText -force)
	    $PesterTestConvert | Should BeOfType securestring

	}
    }
}
