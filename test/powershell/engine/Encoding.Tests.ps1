Describe "Encoding classes and methods are available" -Tag CI {
    BeforeAll {
        $testString = "t" + ([char]233) + "st"
        $provider = get-item $TESTDRIVE
        $testFile = "${TESTDRIVE}/file.txt"
        $preamble = @{
            Ascii = ''
            BigEndianUTF32 = '254-255'
            BigEndianUnicode = '254-255'
            Byte = '255-254'
            Default = ''
            Oem = ''
            String = '255-254'
            UTF32 = '255-254-0-0'
            UTF7 = ''
            UTF8 = ''
            UTF8BOM = '239-187-191'
            UTF8NoBOM = ''
            Unicode = '255-254'
            Unknown = ''
            WindowsLegacy = ''
        }

        function Get-FileBytes
        {
            param ( $file, [int]$count = [int]::MaxValue )
            (Get-Content $file -Encoding byte | Select-Object -First $count) -Join "-"
        }

        function Get-NewLineBytes
        {
            param ( [Microsoft.PowerShell.FileEncoding]$encoding )
            $encoder = [Microsoft.PowerShell.PowerShellEncoding]::GetEncoding($encoding)
            $encoder.GetBytes([Environment]::NewLine) -Join "-"
        }

        $preambleTests =
            @{ Encoding = 'Ascii'; Preamble = '' },
            @{ Encoding = 'BigEndianUTF32'; Preamble = '0-0-254-255' },
            @{ Encoding = 'BigEndianUnicode'; Preamble = '254-255' },
            @{ Encoding = 'Byte'; Preamble = '255-254' },
            @{ Encoding = 'Default'; Preamble = '' },
            @{ Encoding = 'Oem'; Preamble = '' },
            @{ Encoding = 'String'; Preamble = '255-254' },
            @{ Encoding = 'UTF32'; Preamble = '255-254-0-0' },
            @{ Encoding = 'UTF7'; Preamble = '' },
            @{ Encoding = 'UTF8'; Preamble = '' },
            @{ Encoding = 'UTF8BOM'; Preamble = '239-187-191' },
            @{ Encoding = 'UTF8NoBOM'; Preamble = '' },
            @{ Encoding = 'Unicode'; Preamble = '255-254' },
            @{ Encoding = 'Unknown'; Preamble = '' },
            @{ Encoding = 'WindowsLegacy'; Preamble = '' }

        $testStringEncodedBytes = @{
            Ascii = "116-63-115-116-" + (Get-NewLineBytes Ascii)
            BigEndianUTF32 = "0-0-254-255-0-0-0-116-0-0-0-233-0-0-0-115-0-0-0-116-" + (Get-NewLineBytes BigEndianUTF32)
            BigEndianUnicode = "254-255-0-116-0-233-0-115-0-116-" + (Get-NewLineBytes BigEndianUnicode)
            Byte = "255-254-116-0-233-0-115-0-116-0-" + (Get-NewLineBytes Byte)
            Default = "116-195-169-115-116-" + (Get-NewLineBytes Default)
            # Oem encoding can change depending on system, calculate the expected string
            Oem = ([Microsoft.PowerShell.PowerShellEncoding]::GetEncoding("Oem").GetBytes($testString) -join "-") + "-" + (Get-NewLineBytes Oem)
            String = "255-254-116-0-233-0-115-0-116-0-" + (Get-NewLineBytes String)
            UTF32 = "255-254-0-0-116-0-0-0-233-0-0-0-115-0-0-0-116-0-0-0-" + (Get-NewLineBytes UTF32)
            UTF7 = "116-43-65-79-107-45-115-116-" + (Get-NewLineBytes UTF7)
            UTF8 = "116-195-169-115-116-" + (Get-NewLineBytes UTF8 )
            UTF8BOM = "239-187-191-116-195-169-115-116-" + (Get-NewLineBytes UTF8BOM)
            UTF8NoBOM = "116-195-169-115-116-" + (Get-NewLineBytes UTF8NoBOM)
            Unicode = "255-254-116-0-233-0-115-0-116-0-" + (Get-NewLineBytes Unicode)
            Unknown = "116-195-169-115-116-" + (Get-NewLineBytes Unknown)
            }

        $contentTests =
            @{ Encoding = 'Ascii'; Bytes = $testStringEncodedBytes['Ascii'] },
            @{ Encoding = 'BigEndianUTF32'; Bytes = $testStringEncodedBytes['BigEndianUTF32'] },
            @{ Encoding = 'BigEndianUnicode'; Bytes = $testStringEncodedBytes['BigEndianUnicode'] },
            @{ Encoding = 'Byte'; Bytes = $testStringEncodedBytes['Byte'] },
            @{ Encoding = 'Default'; Bytes = $testStringEncodedBytes['Default'] },
            # Oem encoding can change depending on system, calculate the expected string
            @{ Encoding = 'Oem'; Bytes = $testStringEncodedBytes['Oem'] },
            @{ Encoding = 'String'; Bytes = $testStringEncodedBytes['String'] },
            @{ Encoding = 'UTF32'; Bytes = $testStringEncodedBytes['UTF32'] },
            @{ Encoding = 'UTF7'; Bytes = $testStringEncodedBytes['UTF7'] },
            @{ Encoding = 'UTF8'; Bytes = $testStringEncodedBytes['UTF8'] },
            @{ Encoding = 'UTF8BOM'; Bytes = $testStringEncodedBytes['UTF8BOM'] },
            @{ Encoding = 'UTF8NoBOM'; Bytes = $testStringEncodedBytes['UTF8NoBOM'] },
            @{ Encoding = 'Unicode'; Bytes = $testStringEncodedBytes['Unicode'] },
            @{ Encoding = 'Unknown'; Bytes = $testStringEncodedBytes['Unknown'] }

    }

    AfterEach {
        if ( Test-Path $testFile )
        {
            remove-item $testFile
        }
        $PSDefaultFileEncoding = "Unknown"
    }

    It "Encoding for '<Encoding>' should have correct preamble '<preamble>'" -TestCase $preambleTests {
        param ( $Encoding, $Preamble )
        [Microsoft.PowerShell.PowerShellEncoding]::GetEncoding($Encoding).GetPreamble() -Join "-" | Should be $Preamble
    }

    It "Encoding for '<Encoding>' should create file with proper encoding" -TestCase $contentTests {
        param ( $Encoding, $Bytes )
        $testString | out-file -encoding $Encoding $testFile
        Get-FileBytes $testFile | should be $Bytes
    }

    It "Setting PSDefaultFileEncoding to '<Encoding>' should create file with proper encoding" -TestCase $contentTests {
        param ( $Encoding, $Bytes )
        $PSDefaultFileEncoding = $Encoding
        $testString | out-file $testFile
        Get-FileBytes $testFile | should be $Bytes
    }

    It "Explicit encoding is not overridden by setting PSDefaultFileEncoding to '<Encoding>'" -TestCase $contentTests {
        param ( $Encoding, $Bytes )
        $PSDefaultFileEncoding = $Encoding
        $testString | out-file -encoding ascii $testFile
        Get-FileBytes $testFile | should be $testStringEncodedBytes['Ascii']
    }

    It "Explicit encoding set to unknown and preference variable set to unicode creates unicode file" {
        $PSDefaultFileEncoding = "Unicode"
        $testString | set-content -encoding unknown $testfile
        Get-FileBytes $testFile | should be $testStringEncodedBytes['Unicode']
    }

    It "getting the encoding for an unknown cmdlet should return utf-8" {
        $method = [microsoft.powershell.powershellencoding].getmember("GetWindowsLegacyEncoding","NonPublic,Static")
        $method.Invoke($null, "badcmdlet").BodyName | should be "utf-8"
    }

    It "When session state is null, GetEncodingPreference returns unknown" {
        [Microsoft.PowerShell.PowerShellEncoding]::GetEncodingPreference($null) | should be "unknown"
    }

    Context "GetFileEncodingFromFile tests" {
        BeforeAll {
            $TestCases = @{ Encoding = "Unicode"; Text = $testString; FilePath = $testFile },
                @{ Encoding = "UTF8NoBOM"; Text = $testString; FilePath = $testFile },
                @{ Encoding = "UTF32"; Text = $testString; FilePath = $testFile },
                @{ Encoding = "BigEndianUTF32"; Text = $testString; FilePath = $testFile },
                @{ Encoding = "UTF8Bom"; Text = $testString; FilePath = $testFile },
                @{ Encoding = "Byte"; Text = [byte[]](20..40); FilePath = $testFile },
                @{ Encoding = "UTF8NoBom"; Text = ""; FilePath = $testFile },
                @{ Encoding = "Default"; Text = ""; FilePath = "$TESTDRIVE/ThisFileCouldNotPossiblyExist" }
        }

        It "GetFileEncodingFromFile can discover a <Encoding> encoded file" -TestCase $TestCases {
            param ( $Encoding, $Text, $FilePath )
            # I need a way to not open the right file to test the missing file scenario
            $Text | set-content -encoding $Encoding $testFile
            [Microsoft.PowerShell.PowerShellEncoding]::GetFileEncodingFromFile($FilePath) | should be $encoding
        }
    }

    Context "Legacy Windows Behavior" {

        It "Add-Content creates utf8 encoded files" {
            $testString | add-content -encoding WindowsLegacy $TESTDRIVE/file.txt
            Get-FileBytes $TESTDRIVE/file.txt | should be $testStringEncodedBytes['UTF8']
        }

        It "Set-Content creates utf8 encoded files" {
            $testString | set-content -encoding WindowsLegacy $TESTDRIVE/file.txt
            Get-FileBytes $TESTDRIVE/file.txt | should be $testStringEncodedBytes['UTF8']
        }

        It "Export-CliXml creates unicode encoded files" {
            [pscustomobject]@{ text = $testString } | export-clixml -encoding WindowsLegacy $TESTDRIVE/file.clixml
            # these are the characters
            # <BOM><Obj
            # which is what Export-CliXml creates
            Get-FileBytes $TESTDRIVE/file.clixml -count 10 | should be "255-254-60-0-79-0-98-0-106-0"
        }

        It "Export-Csv creates ascii encoded files" {
            # we'll be looking for the bytes 116-63-115-116 which is what $testString looks like when encoded as ascii
            [pscustomobject]@{ text = $testString } | export-csv -encoding WindowsLegacy $TESTDRIVE/file.csv
            # Get-FileBytes for this file returns the entire contents, we should be looking only for the string which is
            # interesting to us which is "t?st" (bytes 116, 63, 115, 116
            Get-FileBytes $TESTDRIVE/file.csv | should match "116-63-115-116"
        }

        It "New-ModuleManifest creates unicode encoded files" {
            try {
                $PSDefaultFileEncoding = "WindowsLegacy"
                New-ModuleManifest -path "$TESTDRIVE/${testString}.psd1"
            }
            finally {
                $PSDefaultFileEncoding = "Unknown"
            }
            # we know what the encoding should be
            $legacyEncoding = [System.Text.Encoding]::Unicode
            $newLineBytes = $legacyEncoding.GetBytes([Environment]::NewLine)
            $newLineByteString = $newLineBytes -join "-"
            $expected = "255-254-35-0-${newLineByteString}-35-0-32-0"
            Get-FileBytes $TESTDRIVE/${testString}.psd1 -count (8 + $newLineBytes.Count) | should match $expected
        }

        It "Out-File creates properly encoded files" {
            $testString | Out-File -encoding WindowsLegacy -FilePath $TESTDRIVE/file.txt
            # we are using the first 10 bytes to convince us that we created the proper encoding
            # this doesn't include the new line
            Get-FileBytes $TESTDRIVE/file.txt -count 10 | should match "255-254-116-0-233-0-115-0-116-0"
        }

        It "Redirection creates unicode encoded files" {
            try {
                $PSDefaultFileEncoding = "WindowsLegacy"
                $testString > $TESTDRIVE/file.txt
            }
            finally {
                $PSDefaultFileEncoding = "Unknown"
            }
            # we are using the first 10 bytes to convince us that we created the proper encoding
            # this doesn't include the new line
            Get-FileBytes $TESTDRIVE/file.txt -count 10 | should match "255-254-116-0-233-0-115-0-116-0"
        }
    }
}


