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

Describe "Update-FormatData basic functionality" -Tags DRT{
	$tmpDirectory = $TestDrive
    $testfilename = "testfile.ps1xml"
    $testfile = Join-Path -Path $tmpDirectory -ChildPath $testfilename
	
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
		$xmlContent>$testfile
		try
		{
			{Update-FormatData -Append $testfile -WhatIf} | Should Not Throw
			{Update-FormatData -Prepend $testfile -WhatIf} | Should Not Throw
		}
		finally
		{
			rm $testfile
		}
	}
}