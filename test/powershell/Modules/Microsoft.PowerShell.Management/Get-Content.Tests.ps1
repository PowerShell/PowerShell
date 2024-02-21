# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Get-Content" -Tags "CI" {
    $testString = "This is a test content for a file"
    $nl         = [Environment]::NewLine
    $firstline  = "Here's a first line "
    $secondline = " here's a second line"
    $thirdline  = "more text"
    $fourthline = "just to make sure"
    $fifthline  = "there's plenty to work with"
    $testString2 = $firstline + $nl + $secondline + $nl + $thirdline + $nl + $fourthline + $nl + $fifthline
    $testPath   = Join-Path -Path $TestDrive -ChildPath testfile1
    $testPath2  = Join-Path -Path $TestDrive -ChildPath testfile2
    $testContent = "AA","BB","CC","DD"
    $testDelimiterContent = "Hello1,World1","Hello2,World2","Hello3,World3","Hello4,World4"

    BeforeEach {
        New-Item -Path $testPath -ItemType file -Force -Value $testString
        New-Item -Path $testPath2 -ItemType file -Force -Value $testString2
    }

    AfterEach {
        Remove-Item -Path $testPath -Force
        Remove-Item -Path $testPath2 -Force
    }

    It "Should throw an error on a directory" {
        { Get-Content . -ErrorAction Stop } |
            Should -Throw -ErrorId "GetContainerContentException,Microsoft.PowerShell.Commands.GetContentCommand"
    }

    It "Should return an Object when listing only a single line and the correct information from a file" {
        $content = (Get-Content -Path $testPath)
        $content | Should -BeExactly $testString
        $content.Count | Should -Be 1
        $content | Should -BeOfType System.String
    }

    It "Should deliver an array object when listing a file with multiple lines and the correct information from a file" {
        $content = (Get-Content -Path $testPath2)
        @(Compare-Object $content $testString2.Split($nl) -SyncWindow 0).Length | Should -Be 0
        ,$content | Should -BeOfType System.Array
    }

    It "Should be able to return a specific line from a file" {
        (Get-Content -Path $testPath2)[1] | Should -BeExactly $secondline
    }

    It "Should be able to specify the number of lines to get the content of using the TotalCount switch" {
        $returnArray    = (Get-Content -Path $testPath2 -TotalCount 2)
        $returnArray[0] | Should -BeExactly $firstline
        $returnArray[1] | Should -BeExactly $secondline
    }

    It "Should be able to specify the number of lines to get the content of using the Head switch" {
        $returnArray    = (Get-Content -Path $testPath2 -Head 2)
        $returnArray[0] | Should -BeExactly $firstline
        $returnArray[1] | Should -BeExactly $secondline
    }

    It "Should be able to specify the number of lines to get the content of using the First switch" {
        $returnArray    = (Get-Content -Path $testPath2 -First 2)
        $returnArray[0] | Should -BeExactly $firstline
        $returnArray[1] | Should -BeExactly $secondline
    }

    It "Should return the last line of a file using the Tail switch" {
        Get-Content -Path $testPath -Tail 1 | Should -BeExactly $testString
    }

    It "Should return the last lines of a file using the Last alias" {
        Get-Content -Path $testPath2 -Last 1 | Should -BeExactly $fifthline
    }

    It 'Verifies -TotalCount reports a ParameterArgumentValidationError error for negative values' {
        {Get-Content -Path $testPath2 -TotalCount -2} | Should -Throw -ErrorId 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetContentCommand'
    }

    It 'Verifies -Tail reports a ParameterArgumentValidationError error for negative values' {
        {Get-Content -Path $testPath2 -Tail -2} | Should -Throw -ErrorId 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetContentCommand'
    }

    It "Should be able to get content within a different drive" {
        Push-Location env:
        $expectedoutput = [Environment]::GetEnvironmentVariable("PATH");
        { Get-Content PATH } | Should -Not -Throw
        Get-Content PATH     | Should -BeExactly $expectedoutput
        Pop-Location
    }

    #[BugId(BugDatabase.WindowsOutOfBandReleases, 906022)]
    It "should throw 'PSNotSupportedException' when you Set-Content to an unsupported provider" -Skip:($IsLinux -Or $IsMacOS) {
        {Get-Content -Path HKLM:\\software\\microsoft -ErrorAction Stop} | Should -Throw "IContentCmdletProvider interface is not implemented"
    }

    It 'Verifies -Tail reports a TailNotSupported error for unsupported providers' {
        {Get-Content -Path Variable:\PSHOME -Tail 1 -ErrorAction Stop} | Should -Throw -ErrorId 'TailNotSupported,Microsoft.PowerShell.Commands.GetContentCommand'
    }

    It 'Verifies using -Tail and -TotalCount together reports a TailAndHeadCannotCoexist error' {
        { Get-Content -Path Variable:\PSHOME -Tail 1 -TotalCount 5 -ErrorAction Stop} | Should -Throw -ErrorId 'TailAndHeadCannotCoexist,Microsoft.PowerShell.Commands.GetContentCommand'
    }

    It 'Verifies -Tail with content that uses an explicit/implicit encoding' -TestCases @(
        @{EncodingName = 'String'},
        @{EncodingName = 'OEM'},
        @{EncodingName = 'Unicode'},
        @{EncodingName = 'BigEndianUnicode'},
        @{EncodingName = 'BigEndianUTF32'},
        @{EncodingName = 'UTF8'},
        @{EncodingName = 'UTF8BOM'},
        @{EncodingName = 'UTF8NoBOM'},
        @{EncodingName = 'UTF7'},
        @{EncodingName = 'UTF32'},
        @{EncodingName = 'Ascii'},
        @{EncodingName = 'ANSI'}
        ){
        param($EncodingName)

        $contentSets =
            @(@('a1','aa2','aaa3','aaaa4','aaaaa5'), # utf-8
              @('€1','€€2','€€€3','€€€€4','€€€€€5'), # utf-16
              @('𐍈1','𐍈𐍈2','𐍈𐍈𐍈3','𐍈𐍈𐍈𐍈4','𐍈𐍈𐍈𐍈𐍈5')) # utf-32
        ForEach ($content in $contentSets)
        {
            $tailCount = 4
            $testPath = Join-Path -Path $TestDrive -ChildPath 'TailWithEncoding.txt'
            $content | Set-Content -Path $testPath -Encoding $EncodingName

            # read and verify using explicit encoding
            $expected = (Get-Content -Path $testPath -Encoding $EncodingName)[-$tailCount]
            $actual = Get-Content -Path $testPath -Tail $tailCount -Encoding $EncodingName
            $actual | Should -BeOfType string
            $actual.Length | Should -Be $tailCount
            $actual[0] | Should -BeExactly $expected

            # read and verify using implicit encoding
            $expected = (Get-Content -Path $testPath)[-$tailCount]
            $actual = Get-Content -Path $testPath -Tail $tailCount
            $actual | Should -BeOfType string
            $actual.Length | Should -Be $tailCount
            $actual[0] | Should -BeExactly $expected
        }
    }

    It "should Get-Content with a variety of -Tail and -ReadCount: <test>" -TestCases @(
        @{ test = "negative readcount"
            GetContentParams = @{Path = $testPath; Readcount = -1; Tail = 5}
            expectedLength = 4
            expectedContent = "AA","BB","CC","DD"
        }
        @{ test = "readcount=0"
            GetContentParams = @{Path = $testPath; Readcount = 0; Tail = 3}
            expectedLength = 3
            expectedContent = "BB","CC","DD"
        }
        @{ test = "readcount=1"
            GetContentParams = @{Path = $testPath; Readcount = 1; Tail = 3}
            expectedLength = 3
            expectedContent = "BB","CC","DD"
        }
        @{ test = "high readcount"
            GetContentParams = @{Path = $testPath; Readcount = 99999; Tail = 3}
            expectedLength = 3
            expectedContent = "BB","CC","DD"
        }
        @{ test = "readcount=2 tail=3"
            GetContentParams = @{Path = $testPath; Readcount = 2; Tail = 3}
            expectedLength = 2
            expectedContent = ("BB","CC"), "DD"
        }
        @{ test = "readcount=2 tail=2"
            GetContentParams = @{Path = $testPath; Readcount = 2; Tail = 2}
            expectedLength = 2
            expectedContent = "CC","DD"
        }
    ) {
        param($GetContentParams, $expectedLength, $expectedContent)
        Set-Content -Path $testPath $testContent
        $result = Get-Content @GetContentParams
        $result.Length | Should -Be $expectedLength
        $result | Should -BeExactly $expectedContent
    }

    It "should Get-Content with a variety of -Delimiter and -Tail: <test>" -TestCases @(
        @{ test = ", as delimiter"
            GetContentParams = @{Path = $testPath; Delimiter = ","; Tail = 2}
            expectedLength = 2
            expectedContent = "World3${nl}Hello4", "World4${nl}"
        }
        @{ test = "o as delimiter"
            GetContentParams = @{Path = $testPath; Delimiter = "o"; Tail = 3}
            expectedLength = 3
            expectedContent = "rld3${nl}Hell", '4,W', "rld4${nl}"
        }
    ) {
        param($GetContentParams, $expectedLength, $expectedContent)
        Set-Content -Path $testPath $testDelimiterContent
        $result = Get-Content @GetContentParams
        $result.Length | Should -Be $expectedLength
        $result | Should -BeExactly $expectedContent
    }

    It "should Get-Content with a variety of -Tail values and -AsByteStream parameter" -TestCases @(
        @{
            GetContentParams = @{Path = $testPath; Tail = 10; AsByteStream = $true}
            expectedLength = 10
            # Byte encoding of \r\nCC\r\nDD\r\n
            expectedWindowsContent = 13, 10, 67, 67, 13, 10, 68, 68, 13, 10
            # Byte encoding of \nBB\nCC\nDD\n
            expectedNotWindowsContent = 10, 66, 66, 10, 67, 67, 10, 68, 68, 10
        }
    ) {
        param($GetContentParams, $expectedLength, $expectedWindowsContent, $expectedNotWindowsContent)
        Set-Content -Path $testPath $testContent
        $result = Get-Content @GetContentParams
        $result.Length | Should -Be $expectedLength
        if ($IsWindows) {
            $result | Should -BeExactly $expectedWindowsContent
        } else {
            $result | Should -BeExactly $expectedNotWindowsContent
        }
    }

    #[BugId(BugDatabase.WindowsOutOfBandReleases, 905829)]
    It "should Get-Content that matches the input string"{
        Set-Content $testPath "Hello,llllWorlld","Hello2,llllWorlld2"
        $result=Get-Content $testPath -Delimiter "ll"
        $result.Length    | Should -Be 9
        $expected = 'He', 'o,', '', 'Wor', "d${nl}He", 'o2,', '', 'Wor', "d2${nl}"
        for ($i = 0; $i -lt $result.Length ; $i++) { $result[$i]    | Should -BeExactly $expected[$i]}
    }

    Context "Alternate Data Stream support on Windows" {
        It "Should support NTFS streams using colon syntax" -Skip:(!$IsWindows) {
            Set-Content "${testPath}:Stream" -Value "Foo"
            { Test-Path "${testPath}:Stream" | Should -Throw -ErrorId "ItemExistsNotSupportedError,Microsoft.PowerShell.Commands,TestPathCommand" }
            Get-Content "${testPath}:Stream" | Should -BeExactly "Foo"
            Get-Content $testPath | Should -BeExactly $testString
        }

        It "Should support NTFS streams using -Stream" -Skip:(!$IsWindows) {
            Set-Content -Path $testPath -Stream hello -Value World
            Get-Content -Path $testPath | Should -BeExactly $testString
            Get-Content -Path $testPath -Stream hello | Should -BeExactly "World"
            $item = Get-Item -Path $testPath -Stream hello
            $item | Should -BeOfType System.Management.Automation.Internal.AlternateStreamData
            $item.Stream | Should -BeExactly "hello"
            Clear-Content -Path $testPath -Stream hello
            Get-Content -Path $testPath -Stream hello | Should -BeNullOrEmpty
            Remove-Item -Path $testPath -Stream hello
            { Get-Content -Path $testPath -Stream hello -ErrorAction stop} | Should -Throw -ErrorId "GetContentReaderFileNotFoundError,Microsoft.PowerShell.Commands.GetContentCommand"
        }
    }

    It "Should support colons in filename on Linux/Mac" -Skip:($IsWindows) {
        Set-Content "${testPath}:Stream" -Value "Hello"
        "${testPath}:Stream" | Should -Exist
        Get-Content "${testPath}:Stream" | Should -BeExactly "Hello"
    }

    It "-Stream is not a valid parameter for <cmdlet> on Linux/Mac" -Skip:($IsWindows) -TestCases @(
        @{cmdlet="Get-Content"},
        @{cmdlet="Set-Content"},
        @{cmdlet="Clear-Content"},
        @{cmdlet="Add-Content"},
        @{cmdlet="Get-Item"},
        @{cmdlet="Remove-Item"}
    ) {
        param($cmdlet)
        (Get-Command $cmdlet).Parameters["stream"] | Should -BeNullOrEmpty
    }

    It "Should return no content when an empty path is used with -Raw switch" {
        Get-ChildItem $TestDrive -Filter "*.raw" | Get-Content -Raw | Should -BeNullOrEmpty
    }

    It "Should return no content when -TotalCount value is 0" {
        Get-Content -Path $testPath -TotalCount 0 | Should -BeNullOrEmpty
    }

    It "Should return no content when -Tail value is 0" {
        Get-Content -Path $testPath -Tail 0 | Should -BeNullOrEmpty
    }

    It "Should wait for content when using -Tail 0 and -Wait" {
        $testValues = @(1,2,3)
        $Job = Start-Job -ScriptBlock {Get-Content -Path $using:testPath -Tail 0 -Wait}
        Start-Sleep -Seconds 3
        Add-Content -Value $testValues -Path $testPath
        Start-Sleep -Seconds 3
        Compare-Object -ReferenceObject $testValues -DifferenceObject ($Job | Receive-Job) | Should -BeNullOrEmpty
        $Job | Remove-Job -Force
    }

    It "Should throw TailAndHeadCannotCoexist when both -Tail and -TotalCount are used" {
        {
        Get-Content -Path $testPath -Tail 1 -TotalCount 1 -ErrorAction Stop
        } | Should -Throw -ErrorId "TailAndHeadCannotCoexist,Microsoft.PowerShell.Commands.GetContentCommand"
    }

    It "Should throw TailNotSupported when -Tail used with an unsupported provider" {
        Push-Location env:
        {
        Get-Content PATH -Tail 1 -ErrorAction Stop
        } | Should -Throw -ErrorId "TailNotSupported,Microsoft.PowerShell.Commands.GetContentCommand"
        Pop-Location
    }

    It "Should throw InvalidOperation when -Tail and -Raw are used" {
        {
        Get-Content -Path $testPath -Tail 1 -ErrorAction Stop -Raw
        } | Should -Throw -ErrorId "InvalidOperation,Microsoft.PowerShell.Commands.GetContentCommand"
    }

    It "Should throw ItemNotFound when path matches no files with <variation>" -TestCases @(
        @{ variation = "no additional parameters"; params = @{} },
        @{ variation = "dynamic parameter"       ; params = @{ Raw = $true }}
    ) {
        param($params)

        { Get-Content -Path "/DoesNotExist*.txt" @params -ErrorAction Stop } | Should -Throw -ErrorId "ItemNotFound,Microsoft.PowerShell.Commands.GetContentCommand"
    }

    Context "Check Get-Content containing multi-byte chars" {
        BeforeAll {
            $firstLine = "Hello,World"
            $secondLine = "Hello2,World2"
            $thirdLine = "Hello3,World3"
            $fourthLine = "Hello4,World4"
            $fileContent = $firstLine,$secondLine,$thirdLine,$fourthLine
        }
        BeforeEach{
            Set-Content -Path $testPath $fileContent
        }
        It "Should return all lines when -Tail value is more than number of lines in the file"{
            $result = Get-Content -Path $testPath -ReadCount -1 -Tail 5 -Encoding UTF7
            $result.Length | Should -Be 4
            $expected = $fileContent
            Compare-Object -ReferenceObject $expected -DifferenceObject $result | Should -BeNullOrEmpty
        }
        It "Should return last three lines at one time for -ReadCount 0 and -Tail 3"{
            $result = Get-Content -Path $testPath -ReadCount 0 -Tail 3 -Encoding UTF7
            $result.Length | Should -Be 3
            $expected = $secondLine,$thirdLine,$fourthLine
            Compare-Object -ReferenceObject $expected -DifferenceObject $result | Should -BeNullOrEmpty
        }
        It "Should return last three lines reading one at a time for -ReadCount 1 and -Tail 3"{
            $result = Get-Content -Path $testPath -ReadCount 1 -Tail 3 -Encoding UTF7
            $result.Length | Should -Be 3
            $expected = $secondLine,$thirdLine,$fourthLine
            Compare-Object -ReferenceObject $expected -DifferenceObject $result | Should -BeNullOrEmpty
        }
        It "Should return last three lines at one time for -ReadCount 99999 and -Tail 3"{
            $result = Get-Content -Path $testPath -ReadCount 99999 -Tail 3 -Encoding UTF7
            $result.Length | Should -Be 3
            $expected = $secondLine,$thirdLine,$fourthLine
            Compare-Object -ReferenceObject $expected -DifferenceObject $result | Should -BeNullOrEmpty
        }
        It "Should return last three lines two lines at a time for -ReadCount 2 and -Tail 3"{
            $result = Get-Content -Path $testPath -ReadCount 2 -Tail 3 -Encoding UTF7
            $result.Length | Should -Be 2
            $expected = New-Object System.Array[] 2
            $expected[0] = ($secondLine,$thirdLine)
            $expected[1] = $fourthLine
            Compare-Object -ReferenceObject $expected -DifferenceObject $result | Should -BeNullOrEmpty
        }
        It "Should not return any content when -TotalCount 0"{
            $result = Get-Content -Path $testPath -TotalCount 0 -ReadCount 1 -Encoding UTF7
            $result.Length | Should -Be 0
        }
        It "Should return first three lines two lines at a time for -TotalCount 3 and -ReadCount 2"{
            $result = Get-Content -Path $testPath -TotalCount 3 -ReadCount 2 -Encoding UTF7
            $result.Length | Should -Be 2
            $expected = New-Object System.Array[] 2
            $expected[0] = ($firstLine,$secondLine)
            $expected[1] = $thirdLine
            Compare-Object -ReferenceObject $expected -DifferenceObject $result | Should -BeNullOrEmpty
        }
        It "Should return the same number of first lines as set in -TotalCount at one time for -ReadCount 0" {
            $result = Get-Content -Path $testPath -TotalCount 3 -ReadCount 0
            $result.Length | Should -Be 3
            $expected = $firstLine,$secondLine,$thirdLine
            Compare-Object -ReferenceObject $expected -DifferenceObject $result | Should -BeNullOrEmpty
        }
        It "A warning should be emitted if both -AsByteStream and -Encoding are used together" {
            [byte[]][char[]]"test" | Set-Content -Encoding Unicode -AsByteStream "${TESTDRIVE}\bfile.txt" -WarningVariable contentWarning *> $null
            $contentWarning.Message | Should -Match "-AsByteStream"
        }
    }
}

Describe "Get-Content -Raw test" -Tags "CI" {

    It "Reads - <testname> in full" -TestCases @(
      @{character = "a`nb`n"; testname = "LF-terminated files"; filename = "lf.txt"}
      @{character = "a`r`nb`r`n"; testname = "CRLF-terminated files"; filename = "crlf.txt"}
      @{character = "a`nb"; testname = "LF-separated files without trailing newline"; filename = "lf-nt.txt"}
      @{character = "a`r`nb"; testname = "CRLF-separated files without trailing newline"; filename = "crlf-nt.txt"}
    ) {
        param ($character, $filename)
        Set-Content -Encoding Ascii -NoNewline "$TestDrive\$filename" -Value $character
        Get-Content -Raw "$TestDrive\$filename" | Should -BeExactly $character
    }
}
