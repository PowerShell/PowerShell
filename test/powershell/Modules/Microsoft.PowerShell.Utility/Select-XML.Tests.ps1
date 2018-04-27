# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Select-XML DRT Unit Tests" -Tags "CI" {

	BeforeAll {
		$testfile = Join-Path -Path $TestDrive -ChildPath "testfile.xml"
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
	}

	It "Select-XML should work"{
		$results = Select-XML -Path $testfile -XPath "/bookstore/book/title"
		$results.Count | Should -Be 2
		$results[0].Node."#text" | Should -BeExactly "Harry Potter"
		$results[1].Node."#text" | Should -BeExactly "Learning XML"
	}
}
