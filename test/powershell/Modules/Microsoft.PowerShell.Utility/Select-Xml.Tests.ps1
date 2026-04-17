# Copyright (c) Microsoft Corporation.
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

	BeforeDiscovery {
		$testCases = @(
			@{testName = 'Literalpath with relative paths'}
			@{testName = 'Literalpath with absolute paths'}
			@{testName = 'Literalpath with path with dots'}
			@{testName = 'Path with relative paths'}
			@{testName = 'Path with absolute paths'}
			@{testName = 'Path with path with dots'}
		)

		if ( ! $IsCoreCLR ) {
			$testcases += @{testName = 'Literalpath with network paths'}
			$testcases += @{testName = 'Path with network paths'}
		}
	}

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

		if ( ! $IsCoreCLR ) {
			$testcases += @{testName = 'Literalpath with network paths'; testParameter = @{LiteralPath = $fileNameAsNetworkPath; XPath = 'Root'}}
			$testcases += @{testName = 'Path with network paths'; testParameter = @{LiteralPath = $fileNameAsNetworkPath; XPath = 'Root'}}
		}

		# Build lookup map so It block can access runtime-resolved test parameters by name
		$testParameterMap = @{}
		foreach ($tc in $testCases) { $testParameterMap[$tc.testName] = $tc.testParameter }

		Push-Location -Path $fileName.Directory
	}

	AfterAll {
		Pop-Location
	}

	It "Can work with input files using <testName>" -TestCases $testCases {
		param($testName)

		$testParameter = $testParameterMap[$testName]
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

		$env:xmltestfile = $xmlContent
		$file = 'env:xmltestfile'
		$params = @{$parameter=$file}
		{ Select-Xml @params "Root" -ErrorAction Stop } | Should -Throw -ErrorId $expectedError
		Remove-Item -Path 'env:xmltestfile'
	}

	It "Can throw for invalid XML file" {
		$testfile = "TestDrive:\test.xml"
		Set-Content -Path $testfile -Value "<a><b>"
		{ Select-Xml -Path $testfile -XPath foo -ErrorAction Stop } | Should -Throw -ErrorId 'ProcessingFile,Microsoft.PowerShell.Commands.SelectXmlCommand'
	}

	It "Can throw for invalid XML namespace" {
		[xml]$xml = "<a xmlns='bar'><b xmlns:b='foo'>hello</b><c>world</c></a>"
		{ Select-Xml -Xml $xml -XPath foo -Namespace @{c=$null} -ErrorAction Stop } | Should -Throw -ErrorId 'PrefixError,Microsoft.PowerShell.Commands.SelectXmlCommand'
	}

	It "Can throw for invalid XML content" {
		{ Select-Xml -Content "hello" -XPath foo -ErrorAction Stop } | Should -Throw -ErrorId 'InvalidCastToXmlDocument,Microsoft.PowerShell.Commands.SelectXmlCommand'
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
