Describe "Export-FormatData DRT Unit Tests" -Tags "CI" {
    It "Test basic functionality" {
        $fd = Get-FormatData
        $tempFile = Join-Path $TestDrive -ChildPath "exportFormatTest.txt"
        $results = Export-FormatData -InputObject $fd[0] -Path $tempFile
        $content = Get-Content $tempFile
        $formatViewDefinition = $fd[0].FormatViewDefinition
        $typeName = $fd[0].TypeName
        $content.Contains($typeName) | Should Be $true
        for ($i = 0; $i -lt $formatViewDefinition.Count;$i++)
        {
            $content.Contains($formatViewDefinition[$i].Name) | Should Be $true
        }
    }
}

Describe "Export-FormatData" -Tags "CI" {

    $testOutput = Join-Path -Path $TestDrive -ChildPath "outputfile"

    AfterEach {
        Remove-Item $testOutput -Force -ErrorAction SilentlyContinue
    }

    Context "Check Export-FormatData can be called validly." {
	    It "Should be able to be called without error" {
	        { Get-FormatData | Export-FormatData -Path $testOutput } | Should Not Throw
	    }
    }

    Context "Check that the output is in the correct format" {
	    It "Should not return an empty xml file" {
	        Get-FormatData | Export-FormatData -Path $testOutput
	        $piped = Get-Content $testOutput
	        $piped | Should Not Be ""
	    }

	    It "Should have a valid xml tag at the start of the file" {
	        Get-FormatData | Export-FormatData -Path $testOutput
	        $piped = Get-Content $testOutput
	        $piped[0] | Should Be "<"
	    }
    }
}
