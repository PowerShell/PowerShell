# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
	Describe "ConvertTo--SecureString" -Tags "CI" {

    Context "Checking return types of ConvertTo--SecureString" {

	It "Should return System.Security.SecureString after converting plaintext variable"{
        #[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Demo/doc/test secret.")]
	    $PesterTestConvert = (ConvertTo-SecureString "plaintextpester" -AsPlainText -Force)
	    $PesterTestConvert | Should -BeOfType securestring

	}
    }
}
