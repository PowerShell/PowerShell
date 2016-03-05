Describe "Get-FileHash" {
    New-Variable testDocument -Value (Join-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath assets) -ChildPath testablescript.ps1) -Scope Global -Force
    # The MACTripleDES and RIPEMD160 algorithms are unsupported on Linux
    $algorithms = @{"SHA1"      ="01B865D143E07ECC875AB0EFC0A4429387FD0CF7";
		    "SHA256"    = "4A6DA9F1C0827143BB19FC4B0F2A8057BC1DF55F6D1F62FA3B917BA458E8F570";
		    "SHA384"    = "656215B6A07011E625206F43E57873F49AD7B36DFCABB70F6CDCE2303D7A603E55D052774D26F339A6D80A264340CB8C";
		    "SHA512"    = "C688C33027D89ACAC920545471C8053D8F64A54E21D0415F1E03766DDCDA215420E74FAFD1DC399864C6B6B5723A3358BD337339906797A39090B02229BF31FE";
		    "MD5"       = "7B09811D1631C9FD46B39D1D35522F0A";
		   }

    Context "Cmdlet result tests" {
	It "Should default to correct hash algorithm" {
	    $result = Get-FileHash $testDocument

	    $result.Algorithm | Should Be "SHA256"
	}

	It "Should be able to set the default hash algorithm" {
	    $PSDefaultParameterValues.add("Get-FileHash:Algorithm","MD5")

	    $result = Get-FileHash $testDocument

	    $result.Algorithm | Should Be "MD5"

	    $PSDefaultParameterValues.Remove("Get-FileHash:Algorithm")
	}

	It "Should list the path of the file under test" {
	    $result = Get-FileHash $testDocument

	    $result.Path | Should Be $testDocument
	}
    }

    Context "Algorithm tests" {
	It "Should be able to get the correct hash from each algorithm" {
	    foreach ( $algorithm in $algorithms.Keys)
	    {
		$algorithmResult = Get-FileHash $testDocument -Algorithm $algorithm

		$algorithmResult.Hash | Should Be $algorithms[$algorithm]
	    }

	    #MACTripleDES and RIPEMD160 are unsupported in the existing Pester tests from Windows team
	    { Get-FileHash $testDocument -Algorithm MACTripleDES } | Should Throw "Algorithm 'MACTripleDES' is not supported in this system."
	    { Get-FileHash $testDocument -Algorithm RIPEMD160 }    | Should Throw "Algorithm 'RIPEMD160' is not supported in this system."
	}
    }
}
