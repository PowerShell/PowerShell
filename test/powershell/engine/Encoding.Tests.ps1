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
        function Test-GetEncoding
        {
            [CmdletBinding()]
            param (
                [Microsoft.PowerShell.FileEncoding]$Encoding
            )
            END {
                [Microsoft.PowerShell.PowerShellEncoding]::GetCmdletEncoding($pscmdlet, $encoding)
            }
        }

        $preambleTests =
            @{ Name = 'Ascii'; Preamble = '' },
            @{ Name = 'BigEndianUTF32'; Preamble = '254-255' },
            @{ Name = 'BigEndianUnicode'; Preamble = '254-255' },
            @{ Name = 'Byte'; Preamble = '255-254' },
            @{ Name = 'Default'; Preamble = '' },
            @{ Name = 'Oem'; Preamble = '' },
            @{ Name = 'String'; Preamble = '255-254' },
            @{ Name = 'UTF32'; Preamble = '255-254-0-0' },
            @{ Name = 'UTF7'; Preamble = '' },
            @{ Name = 'UTF8'; Preamble = '' },
            @{ Name = 'UTF8BOM'; Preamble = '239-187-191' },
            @{ Name = 'UTF8NoBOM'; Preamble = '' },
            @{ Name = 'Unicode'; Preamble = '255-254' },
            @{ Name = 'Unknown'; Preamble = '' },
            @{ Name = 'WindowsLegacy'; Preamble = '' }

        $contentTests =
            @{ Name = 'Ascii'; Bytes = "116-63-115-116-" + (Get-NewLineBytes Ascii) },
            @{ Name = 'BigEndianUTF32'; Bytes = "254-255-0-116-0-233-0-115-0-116-" + (Get-NewLineBytes BigEndianUTF32) },
            @{ Name = 'BigEndianUnicode'; Bytes = "254-255-0-116-0-233-0-115-0-116-" + (Get-NewLineBytes BigEndianUnicode) },
            @{ Name = 'Byte'; Bytes = "255-254-116-0-233-0-115-0-116-0-" + (Get-NewLineBytes Byte) },
            @{ Name = 'Default'; Bytes = "116-195-169-115-116-" + (Get-NewLineBytes Default) },
            # Oem encoding can change depending on system, calculate the expected string
            @{ Name = 'Oem'; Bytes = ([Microsoft.PowerShell.PowerShellEncoding]::GetEncoding("Oem").GetBytes($testString) -join "-") + "-" + (Get-NewLineBytes Oem) },
            @{ Name = 'String'; Bytes = "255-254-116-0-233-0-115-0-116-0-" + (Get-NewLineBytes String) },
            @{ Name = 'UTF32'; Bytes = "255-254-0-0-116-0-0-0-233-0-0-0-115-0-0-0-116-0-0-0-" + (Get-NewLineBytes UTF32) },
            @{ Name = 'UTF7'; Bytes = "116-43-65-79-107-45-115-116-" + (Get-NewLineBytes UTF7) },
            @{ Name = 'UTF8'; Bytes = "116-195-169-115-116-" + (Get-NewLineBytes UTF8 ) },
            @{ Name = 'UTF8BOM'; Bytes = "239-187-191-116-195-169-115-116-" + (Get-NewLineBytes UTF8BOM) },
            @{ Name = 'UTF8NoBOM'; Bytes = "116-195-169-115-116-" + (Get-NewLineBytes UTF8NoBOM) },
            @{ Name = 'Unicode'; Bytes = "255-254-116-0-233-0-115-0-116-0-" + (Get-NewLineBytes Unicode) },
            @{ Name = 'Unknown'; Bytes = "116-195-169-115-116-" + (Get-NewLineBytes Unknown) }

    }

    AfterEach {
        if ( Test-Path $testFile )
        {
            remove-item $testFile
        }
        $PSDefaultFileEncoding = "Unknown"
    }

    It "Encoding for '<Name>' should have correct preamble '<preamble>'" -TestCase $preambleTests {
        param ( $Name, $Preamble )
        [Microsoft.PowerShell.PowerShellEncoding]::GetEncoding($Name).GetPreamble() -Join "-" | Should be $Preamble
    }

    It "Encoding for '<Name>' should create file with proper encoding" -TestCase $contentTests {
        param ( $Name, $Bytes )
        $testString | out-file -encoding $Name $testFile
        Get-FileBytes $testFile | should be $Bytes
    }

    It "Setting PSDefaultFileEncoding to '<Name>' should create file with proper encoding" -TestCase $contentTests {
        param ( $Name, $Bytes )
        $PSDefaultFileEncoding = $Name
        $testString | out-file $testFile
        Get-FileBytes $testFile | should be $Bytes
    }

    It "Explicit encoding is not overridden by setting PSDefaultFileEncoding to '<Name>'" -TestCase $contentTests {
        param ( $Name, $Bytes )
        $PSDefaultFileEncoding = $Name
        $testString | out-file -encoding ascii $testFile
        Get-FileBytes $testFile | should be "116-63-115-116-10"
    }

    Context "Legacy Windows Behavior" {

        It "Add-Content creates ascii encoded files" {
            $testString | add-content -encoding WindowsLegacy $TESTDRIVE/file.txt
            Get-FileBytes $TESTDRIVE/file.txt | should be ("116-195-169-115-116-" + (Get-NewLineBytes ASCII))
        }

        It "Set-Content creates ascii encoded files" {
            $testString | set-content -encoding WindowsLegacy $TESTDRIVE/file.txt
            Get-FileBytes $TESTDRIVE/file.txt | should be ("116-195-169-115-116-" + (Get-NewLineBytes ASCII))
        }

        It "Export-CliXml creates unicode encoded files" {
            [pscustomobject]@{ text = $testString } | export-clixml -encoding WindowsLegacy $TESTDRIVE/file.clixml
            Get-FileBytes $TESTDRIVE/file.clixml -count 10 | should be "255-254-60-0-79-0-98-0-106-0"
        }

        It "Export-Csv creates ascii encoded files" {
            # we'll be looking for the bytes 116-63-115-116 which is what $testString looks like when encoded as ascii
            [pscustomobject]@{ text = $testString } | export-csv -encoding WindowsLegacy $TESTDRIVE/file.clixml
            Get-FileBytes $TESTDRIVE/file.clixml | should match "116-63-115-116"
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
            Get-FileBytes $TESTDRIVE/${testString}.psd1 -count 10 | should match $expected
        }

        It "Out-File creates properly encoded files" {
            $testString | Out-File -encoding WindowsLegacy -FilePath $TESTDRIVE/file.txt
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
            Get-FileBytes $TESTDRIVE/file.txt -count 10 | should match "255-254-116-0-233-0-115-0-116-0"
        }
    }
}


