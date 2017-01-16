Describe "XmlCommand DRT basic functionality Tests" -Tags "CI" {

	BeforeAll {
		if(-not ('IsHiddenTestType' -as "type"))
		{
			Add-Type -TypeDefinition @"
		public class IsHiddenTestType
        {
            public IsHiddenTestType()
            {
                Property1 = 1;
                Property2 = "some string";
            }

            public IsHiddenTestType(int val, string data)
            {
                Property1 = val;
                Property2 = data;
            }

            public int Property1;
            public string Property2;
        }
"@
		}
    }

	BeforeEach {
		$testfile = Join-Path -Path $TestDrive -ChildPath "clixml-directive.xml"
	}

    AfterEach {
		remove-item $testfile
    }

    It "Import with CliXml directive should work" -Skip:$IsOSX{
		Get-Process | Export-Clixml $testfile
		$results = Import-Clixml $testfile
		$results.Count | Should BeGreaterThan 0
		$results[0].ToString() | Should Match "System.Diagnostics.Process"
    }

	It "Import with Rehydration should work" -Skip:$IsOSX{
		$property1 = 256
		$property2 = "abcdef"
		$isHiddenTestType = [IsHiddenTestType]::New($property1,$property2)
		$isHiddenTestType | Export-Clixml $testfile
		$results = Import-Clixml $testfile
		$results.Property1 | Should Be $property1
		$results.Property2 | Should Be $property2
    }
}
