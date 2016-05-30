
Describe "Select-XML DRT Unit Tests" -Tags DRT{
	$tmpDirectory = $TestDrive
	$testfilename = "testfile.xml"
	$testfile = Join-Path -Path $tmpDirectory -ChildPath $testfilename
	
	It "Select-XML should work"{
		$xmlContent = @"
<?xml version ="1.0" encoding="ISO-8859-1"?>
<bookstore>
	<book category="CHILDREN">
		<title lang="english">Harry Potter</title>
		<price>30.00</price>
	</book>
	<book category="WEB">
		<title lang="english">Learning XML</title>
		<price>25.00</price>
	</book>
</bookstore>
"@
		$xmlContent >$testfile
		try
		{
			$results=Select-XML -Path $testfile -XPath "/bookstore/book/title"
			$results.Count | Should Be 2
			$results[0].Node."#text" | Should Be "Harry Potter"
			$results[1].Node."#text" | Should Be "Learning XML"
		}
		finally  
		{  
			rm $testfile  
		}
	}
}
