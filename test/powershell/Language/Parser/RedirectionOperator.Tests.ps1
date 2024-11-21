# Copyright (c) Microsoft Corporation.
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
        $oldDefaultParameterValues = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues = @{}
    }
    AfterAll {
        # be sure to tidy up afterwards
        $global:psDefaultParameterValues = $oldDefaultParameterValues
    }
    BeforeEach {
        # start each test with a clean plate!
        $PSDefaultParameterValues.Remove("Out-File:Encoding")
    }
    AfterEach {
        # end each test with a clean plate!
        $PSDefaultParameterValues.Remove("Out-File:Encoding")
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

    $availableEncodings =
        @([System.Text.Encoding]::ASCII
          [System.Text.Encoding]::BigEndianUnicode
          [System.Text.UTF32Encoding]::new($true,$true)
          [System.Text.Encoding]::Unicode
          [System.Text.Encoding]::UTF7
          [System.Text.Encoding]::UTF8
          [System.Text.Encoding]::UTF32)

    foreach($encoding in $availableEncodings) {

        $encodingName = $encoding.EncodingName
        $msg = "Overriding encoding for Out-File is respected for $encodingName"
        $BOM = $encoding.GetPreamble()
        $TXT = $encoding.GetBytes($asciiString)
        $CR  = $encoding.GetBytes($asciiCR)
        $expectedBytes = @( $BOM; $TXT; $CR )
        $PSDefaultParameterValues["Out-File:Encoding"] = $encoding
        $asciiString > TESTDRIVE:/file.txt
        $observedBytes = Get-Content -AsByteStream TESTDRIVE:/file.txt
        # THE TEST
        It $msg {
            $observedBytes.Count | Should -Be $expectedBytes.Count
            for($i = 0;$i -lt $observedBytes.Count; $i++) {
                $observedBytes[$i] | Should -Be $expectedBytes[$i]
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
    BeforeAll {
        $originalErrorView = $ErrorView
        $ErrorView = "NormalView"
    }

    AfterAll {
        $ErrorView = $originalErrorView
    }

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

Describe "Redirection and Set-Variable -append tests" -tags CI {
    Context "variable redirection should work" {
        BeforeAll {
            if ( $EnabledExperimentalFeatures -contains "PSRedirectToVariable" ) {
                $skipTest = $false
            }
            else {
                $skipTest = $true
            }
            $testCases = @{ Name = "Variable should be created"; scriptBlock = { 1..3>variable:a }; Validation = { ($a -join "") | Should -Be ((1..3) -join "") } },
                @{ Name = "variable should be appended"; scriptBlock = {1..3>variable:a; 4..6>>variable:a}; Validation = { ($a -join "") | Should -Be ((1..6) -join "")}},
                @{ Name = "variable should maintain type"; scriptBlock = {@{one=1}>variable:a};Validation = {$a | Should -BeOfType [hashtable]}},
                @{
                    Name = "variable should maintain type for multiple objects"
                    scriptBlock = {@{one=1}>variable:a;@{two=2}>>variable:a;1>>variable:a;"string">>variable:a}
                    Validation = {
                        $a.count | Should -Be 4
                        ,$a | Should -BeOfType [array]
                        $a[0] | Should -BeOfType [hashtable]
                        $a[1] | Should -BeOfType [hashtable]
                        $a[2] | Should -BeOfType [int]
                        $a[3] | Should -BeOfType [string]
                        $a[0].one | Should -Be 1
                        $a[1].two | Should -Be 2
                        $a[2] | Should -Be 1
                        $a[3] | Should -Be "string"
                        }
                    },
                 @{ Name = "Error stream should be redirectable"
                     scriptBlock = { write-error bad 2>variable:a}
                     validation = {
                        $a |Should -BeOfType [System.Management.Automation.ErrorRecord]
                        $a.Exception.Message | Should -BeExactly "bad"
                        }
                     },
                 @{ Name = "Warning stream should be redirectable"
                     scriptBlock = { write-warning warn 3>variable:a}
                     validation = {
                        $a |Should -BeOfType [System.Management.Automation.WarningRecord]
                        $a.Message | Should -BeExactly "warn"
                        }
                     },
                 @{ Name = "Verbose stream should be redirectable"
                     scriptBlock = { write-verbose -verbose verb 4>variable:a}
                     validation = {
                        $a |Should -BeOfType [System.Management.Automation.VerboseRecord]
                        $a.Message | Should -BeExactly "verb"
                        }
                     },
                 @{ Name = "Debug stream should be redirectable"
                     scriptBlock = { write-debug -debug deb 5>variable:a}
                     validation = {
                        $a |Should -BeOfType [System.Management.Automation.DebugRecord]
                        $a.Message | Should -BeExactly "deb"
                        }
                     },
                 @{ Name = "Information stream should be redirectable"
                     scriptBlock = { write-information info 6>variable:a}
                     validation = {
                         $a |Should -BeOfType [System.Management.Automation.InformationRecord]
                         $a.MessageData | Should -BeExactly "info"
                         }
                     },
                 @{ Name = "Complex redirection should be supported"
                     scriptBlock = {
                        . {
                            write-error bad
                            write-information info
                            } *>variable:a
                        }
                    validation = {
                        $a.Count | Should -Be 2
                        $a[0] | Should -BeOfType [System.Management.Automation.ErrorRecord]
                        $a[0].Exception.Message | Should -Be "bad"
                        $a[1] |Should -BeOfType [System.Management.Automation.InformationRecord]
                        $a[1].MessageData | Should -BeExactly "info"
                        }
                    },
                  @{
                    Name = "multiple redirections should work"
                     scriptBlock = {
                        . {
                            write-error bad
                            write-information info
                            } 2>variable:e 6>variable:i
                        }
                     validation = {
                        $e | Should -BeOfType [System.Management.Automation.ErrorRecord]
                        $e.Exception.Message | Should -Be "bad"
                        $i | Should -BeOfType [System.Management.Automation.InformationRecord]
                        $i.MessageData | Should -BeExactly "info"
                     }
                 }


        }
        It "<name>" -TestCases $testCases -skip:$skipTest {
            param ( $scriptBlock, $validation )
            . $scriptBlock
            . $validation
        }

        It 'Redirection of a native application is correct' {
            $expected = @('Arg 0 is <hi>','Arg 1 is <bye>')
            testexe -echoargs hi bye > variable:observed
            $observed | Should -Be $expected
        }

        It 'Redirection while in variable provider is correct' {
            $expected = @('Arg 0 is <hi>','Arg 1 is <bye>')
            try {
                Push-Location variable:
                testexe -echoargs hi bye > observed
            }
            finally {
                Pop-Location
            }
            $observed | Should -Be $expected
        }
    }
}
