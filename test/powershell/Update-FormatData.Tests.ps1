Describe "Update-FormatData" {

    Context "Validate Update-FormatData update correctly" {

        It "Should not throw upon reloading previous formatting file" {
	    { Update-FormatData } | Should Not throw
	}

	It "Should validly load formatting data" {
            { Get-FormatData -typename System.Diagnostics.Process | Export-FormatData -Path "outputfile.ps1xml" }
	    { Update-FormatData -prependPath "outputfile.ps1xml" | Should Not throw }
	    { Remove-Item "outputfile.ps1xml" -ErrorAction SilentlyContinue }
	}
    }
}
