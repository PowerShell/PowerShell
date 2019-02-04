# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Redirection operator now supports encoding changes" -Tags "CI" {
    BeforeAll {
        $asciiString = "abc"

        if ( $IsWindows ) {
             $asciiCR = "`r`n"
        }
        else {
            $asciiCR = [string][char]10
        }

        # If Out-File -Encoding happens to have a default, be sure to
        # save it away
        $SavedValue = $null
        $oldDefaultParameterValues = $psDefaultParameterValues.Clone()
        $psDefaultParameterValues = @{}
    }
    AfterAll {
        # be sure to tidy up afterwards
        $global:psDefaultParameterValues = $oldDefaultParameterValues
    }
    BeforeEach {
        # start each test with a clean plate!
        $psdefaultParameterValues.Remove("Out-File:Encoding")
    }
    AfterEach {
        # end each test with a clean plate!
        $psdefaultParameterValues.Remove("Out-File:Encoding")
    }

    It "If encoding is unset, redirection should be UTF8 without bom" {
        $asciiString > TESTDRIVE:\file.txt
        $bytes = Get-Content -AsByteStream TESTDRIVE:\file.txt
        # create the expected - utf8 encoding without a bom
        $encoding = [Text.UTF8Encoding]::new($false)
        # we know that there will be no preamble, so don't provide any bytes
        $TXT = $encoding.GetBytes($asciiString)
        $CR  = $encoding.GetBytes($asciiCR)
        $expectedBytes = .{ $TXT; $CR }
        $bytes.Count | Should -Be $expectedBytes.count
        for($i = 0; $i -lt $bytes.count; $i++) {
            $bytes[$i] | Should -Be $expectedBytes[$i]
        }
    }

    # $availableEncodings = "unknown","string","unicode","bigendianunicode","utf8","utf7", "utf32","ascii","default","oem"
    $availableEncodings = (Get-Command Out-File).Parameters["Encoding"].Attributes.ValidValues

    foreach($encoding in $availableEncodings) {
        $skipTest = $false
        if ($encoding -eq "default") {
            # [System.Text.Encoding]::Default is exposed by 'System.Private.CoreLib.dll' at
            # runtime via reflection. However,it isn't exposed in the reference contract of
            # 'System.Text.Encoding', and therefore we cannot use 'Encoding.Default' in our
            # code. So we need to skip this encoding in the test.
            $skipTest = $true
        }

        # some of the encodings accepted by Out-File aren't real,
        # and Out-File has its own translation, so we'll
        # not do that logic here, but simply ignore those encodings
        # as they eventually are translated to "real" encoding
        $enc = [System.Text.Encoding]::$encoding
        if ( $enc )
        {
            $msg = "Overriding encoding for Out-File is respected for $encoding"
            $BOM = $enc.GetPreamble()
            $TXT = $enc.GetBytes($asciiString)
            $CR  = $enc.GetBytes($asciiCR)
            $expectedBytes = .{ $BOM; $TXT; $CR }
            $psdefaultparameterValues["Out-File:Encoding"] = "$encoding"
            $asciiString > TESTDRIVE:/file.txt
            $observedBytes = Get-Content -AsByteStream TESTDRIVE:/file.txt
            # THE TEST
            It $msg -Skip:$skipTest {
                $observedBytes.Count | Should -Be $expectedBytes.Count
                for($i = 0;$i -lt $observedBytes.Count; $i++) {
                    $observedBytes[$i] | Should -Be $expectedBytes[$i]
                }
            }

        }
    }
}

Describe "File redirection mixed with Out-Null" -Tags CI {
    It "File redirection before Out-Null should work" {
        "some text" > $TestDrive\out.txt | Out-Null
        Get-Content $TestDrive\out.txt | Should -BeExactly "some text"

        Write-Output "some more text" > $TestDrive\out.txt | Out-Null
        Get-Content $TestDrive\out.txt | Should -BeExactly "some more text"
    }
}

Describe "File redirection should have 'DoComplete' called on the underlying pipeline processor" -Tags CI {
    It "File redirection should result in the same file as Out-File" {
        $object = [pscustomobject] @{ one = 1 }
        $redirectFile = Join-Path $TestDrive fileRedirect.txt
        $outFile = Join-Path $TestDrive outFile.txt

        $object > $redirectFile
        $object | Out-File $outFile

        $redirectFileContent = Get-Content $redirectFile -Raw
        $outFileContent = Get-Content $outFile -Raw
        $redirectFileContent | Should -BeExactly $outFileContent
    }

    It "File redirection should not mess up the original pipe" {
        $outputFile = Join-Path $TestDrive output.txt
        $otherStreamFile = Join-Path $TestDrive otherstream.txt

        $result = & { $(Get-Command NonExist; 1234) > $outputFile *> $otherStreamFile; "Hello" }
        $result | Should -BeExactly "Hello"

        $outputContent = Get-Content $outputFile -Raw
        $outputContent.Trim() | Should -BeExactly '1234'

        $errorContent = Get-Content $otherStreamFile | ForEach-Object { $_.Trim() }
        $errorContent = $errorContent -join ""
        $errorContent | Should -Match "CommandNotFoundException,Microsoft.PowerShell.Commands.GetCommandCommand"
    }
}
