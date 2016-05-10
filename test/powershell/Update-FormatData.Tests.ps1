Describe "Update-FormatData" {

    Context "Validate Update-FormatData update correctly" {

	    It "Should not throw upon reloading previous formatting file" {
	        { Update-FormatData } | Should Not throw
	    }

	    It "Should validly load formatting data" {
            $path = Join-Path -Path $TestDrive -ChildPath "outputfile.ps1xml"
	        Get-FormatData -typename System.Diagnostics.Process | Export-FormatData -Path $path
	        { Update-FormatData -prependPath $path } | Should Not throw 
	        Remove-Item $path -ErrorAction SilentlyContinue 
	    }
    }
}

Describe "Update-FormatData basic functionality" -Tags DRT{
    $testfilename = "testfile.ps1xml"
    $testfile = Join-Path -Path $TestDrive -ChildPath $testfilename
	
	It "Update-FormatData with WhatIf should work"{
		$xmlContent=@"
                <Types>
                    <Type>
                        <Name>AnyName</Name>
                        <Members>
                            <PropertySet>
                                <Name>PropertySetName</Name>
                                <ReferencedProperties>
                                    <Name>FirstName</Name> 
                                    <Name>LastName</Name> 
                                </ReferencedProperties>
                            </PropertySet>
                        </Members>
                    </Type>
                </Types>
"@
		$xmlContent > $testfile
		try
		{
			{ Update-FormatData -Append $testfile -WhatIf } | Should Not Throw
			{ Update-FormatData -Prepend $testfile -WhatIf } | Should Not Throw
		}
		finally
		{
			Remove-Item $testfile -ErrorAction SilentlyContinue
		}
	}
}