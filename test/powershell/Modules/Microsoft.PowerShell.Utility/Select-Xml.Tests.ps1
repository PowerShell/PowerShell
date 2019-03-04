# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Select-Xml DRT Unit Tests" -Tags "CI" {

	BeforeAll {
		$testfile = "TestDrive:\testfile.xml"
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
		Set-Content -Path $testfile -Value $xmlContent
	}

	It "Can select text from an XML document"{
		$results = Select-Xml -Path $testfile -XPath "/bookstore/book/title"
		$results | Should -HaveCount 2
		$results[0].Node."#text" | Should -BeExactly "Harry Potter"
		$results[1].Node."#text" | Should -BeExactly "Learning XML"
	}
}

Describe "Select-Xml Feature Tests" -Tags "Feature" {

	BeforeAll {
		$fileName = New-Item -Path "TestDrive:\testSelectXml.xml"
		$xmlContent = @"
<Root>
   <Node Attribute='blah' />
</Root>
"@

		Set-Content -Path $fileName -Value $xmlContent

		$fileNameWithDots = $fileName.FullName.Replace("\", "\.\")

		$driveLetter = [string]($fileName.FullName)[0]
		$fileNameAsNetworkPath = "\\localhost\$driveLetter`$" + $fileName.FullName.SubString(2)

		$testCases = @(
			@{testName = 'Literalpath with relative paths'; testParameter = @{LiteralPath = $fileName.Name; XPath = 'Root'}},
			@{testName = 'Literalpath with absolute paths'; testParameter = @{LiteralPath = $fileName.FullName; XPath = 'Root'}},
			@{testName = 'Literalpath with path with dots'; testParameter = @{LiteralPath = $fileNameWithDots; XPath = 'Root'}},
			@{testName = 'Path with relative paths'; testParameter = @{Path = $fileName.Name; XPath = 'Root'}},
			@{testName = 'Path with absolute paths'; testParameter = @{Path = $fileName.FullName; XPath = 'Root'}},
			@{testName = 'Path with path with dots'; testParameter = @{Path = $fileNameWithDots; XPath = 'Root'}}
		)

		if ( ! $IsCoreCLR ) {
			$testcases += @{testName = 'Literalpath with network paths'; testParameter = @{LiteralPath = $fileNameAsNetworkPath; XPath = 'Root'}}
			$testcases += @{testName = 'Path with network paths'; testParameter = @{LiteralPath = $fileNameAsNetworkPath; XPath = 'Root'}}
		}

		Push-Location -Path $fileName.Directory
	}

	AfterAll {
		Pop-Location
	}

	It "Can work with input files using <testName>" -TestCases $testCases {
		param($testParameter)

		$node = Select-Xml @testParameter
		$node | Should -HaveCount 1
		$node.Path | Should -Be $fileName.FullName
	}

	It "Can work with input streams" {
		[xml]$xml = "<a xmlns='bar'><b xmlns:b='foo'>hello</b><c>world</c></a>"
		$node = Select-Xml -Xml $xml -XPath "//c:b" -Namespace @{c='bar'}
		$node.Path | Should -BeExactly "InputStream"
		$node.Pattern | Should -BeExactly "//c:b"
		$node.ToString() | Should -BeExactly "hello"
	}

	It "Can throw on non filesystem paths using <parameter>" -TestCases @(
		@{parameter="LiteralPath"; expectedError='ProcessingFile,Microsoft.PowerShell.Commands.SelectXmlCommand'},
		@{parameter="Path";        expectedError='ProcessingFile,Microsoft.PowerShell.Commands.SelectXmlCommand'}
	) {
		param($parameter, $expectedError)
		try
		{
			$env:xmltestfile = $xmlContent
			$file = 'env:xmltestfile'
			$params = @{$parameter=$file}
			$err = $null
			Select-Xml @params "Root" -ErrorVariable err -ErrorAction SilentlyContinue
			$err.FullyQualifiedErrorId | Should -Be $expectedError
		}
		finally
		{
			Remove-Item -Path 'env:xmltestfile'
		}
	}

	It "Can throw for invalid XML file" {
		$testfile = "TestDrive:\test.xml"
		Set-Content -Path $testfile -Value "<a><b>"
		$err = $null
		Select-Xml -Path $testfile -XPath foo -ErrorVariable err -ErrorAction SilentlyContinue
		$err.FullyQualifiedErrorId | Should -Be 'ProcessingFile,Microsoft.PowerShell.Commands.SelectXmlCommand'
	}

	It "Can throw for invalid XML namespace" {
		[xml]$xml = "<a xmlns='bar'><b xmlns:b='foo'>hello</b><c>world</c></a>"
		$err = $null
		Select-Xml -Xml $xml -XPath foo -Namespace @{c=$null} -ErrorVariable err -ErrorAction SilentlyContinue
		$err.FullyQualifiedErrorId | Should -Be 'PrefixError,Microsoft.PowerShell.Commands.SelectXmlCommand'
	}

	It "Can throw for invalid XML content" {
		$err = $null
		Select-Xml -Content "hello" -XPath foo -ErrorVariable err -ErrorAction SilentlyContinue
		$err.FullyQualifiedErrorId | Should -Be 'InvalidCastToXmlDocument,Microsoft.PowerShell.Commands.SelectXmlCommand'
	}

	It "Can use ToString() on nested XML node" {
		$node = Select-Xml -Content "<a><b>one<c>hello</c></b></a>" -XPath "//b"
		$node.ToString() | Should -BeExactly "one<c>hello</c>"
	}

	It "Can use ToString() with file" {
		$testfile = Join-Path $TestDrive "test.xml"
		Set-Content -Path $testfile -Value "<a><b>hello</b></a>"
		$node = Select-Xml -Path $testfile -XPath "//b"
		$node.ToString() | Should -BeExactly "hello:$testfile"
	}
}
