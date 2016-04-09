Describe "Export-FormatData" {

    Context "Check Export-FormatData can be called validly." {
	It "Should be able to be called without error" {
	    { Get-FormatData | Export-FormatData -Path "outputfile" } | Should Not Throw
	    Remove-Item "outputfile" -Force -ErrorAction SilentlyContinue
	}
    }

    Context "Check that the output is in the correct format" {
	It "Should not return an empty xml file" {
	    Get-FormatData | Export-FormatData -Path "outputfile"
	    $piped = Get-Content "outputfile"
	    $piped | Should Not Be ""
	    Remove-Item "outputfile" -Force -ErrorAction SilentlyContinue
	}

	It "Should have a valid xml tag at the start of the file" {
	    Get-FormatData | Export-FormatData -Path "outputfile"
	    $piped = Get-Content "outputfile"
	    $piped[0] | Should Be "<"
	    Remove-Item "outputfile" -Force -ErrorAction SilentlyContinue
	}
    }
}
