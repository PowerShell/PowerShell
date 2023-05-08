# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "File encoding tests" -Tag CI {

    Context "ParameterType for parameter 'Encoding' should be 'Encoding'" {
        BeforeAll {
            $testCases = Get-Command -Module Microsoft.PowerShell.* |
                Where-Object { $_.Parameters -and $_.Parameters['Encoding'] } |
                ForEach-Object { @{ Command = $_ } }
        }

        It "Encoding parameter of command '<Command>' is type 'Encoding'" -Testcase $testCases {
            param ( $command )
            $command.Parameters['Encoding'].ParameterType.FullName | Should -BeExactly "System.Text.Encoding"
        }
    }

    Context "File contents are UTF8 without BOM" {
        BeforeAll {
            $testStr = "t" + ([char]233) + "st"
            $nl = [environment]::newline
            $utf8Bytes = 116,195,169,115,116
            $nlBytes = [byte[]][char[]]$nl
            $ExpectedWithNewline = $( $utf8Bytes ; $nlBytes )
            $outputFile = "${TESTDRIVE}/file.txt"
            $utf8Preamble = [text.encoding]::UTF8.GetPreamble()
            $simpleTestCases = @(
                # New-Item does not add CR/NL
                @{ Command = "New-Item"; Parameters = @{ type = "file";value = $testStr; Path = $outputFile }; Expected = $utf8Bytes ; Operator = "be" }
                # the following commands add a CR/NL
                @{ Command = "Set-Content"; Parameters = @{ value = $testStr; Path = $outputFile }; Expected = $ExpectedWithNewline ; Operator = "be" }
                @{ Command = "Add-Content"; Parameters = @{ value = $testStr; Path = $outputFile }; Expected = $ExpectedWithNewline ; Operator = "be" }
                @{ Command = "Out-File"; Parameters = @{ InputObject = $testStr; Path = $outputFile }; Expected = $ExpectedWithNewline ; Operator = "be" }
                # Redirection
                @{ Command = { $testStr > $outputFile } ; Expected = $ExpectedWithNewline ; Operator = "be" }
                )
            function Get-FileBytes ( $path ) {
                [io.file]::ReadAllBytes($path)
            }
        }

        AfterEach {
            if ( Test-Path $outputFile ) {
                Remove-Item -Force $outputFile
            }
        }

        It "<command> produces correct content '<Expected>'" -TestCases $simpleTestCases {
            param ( $Command, $parameters, $Expected, $Operator)
            & $command @parameters
            $bytes = Get-FileBytes $outputFile
            $bytes -join "-" | Should ${Operator} ($Expected -join "-")
        }

        It "Export-CSV creates file with UTF-8 encoding without BOM" {
            [pscustomobject]@{ Key = $testStr } | Export-Csv $outputFile
            $bytes = Get-FileBytes $outputFile
            $bytes[0,1,2] -join "-" | Should -Not -Be ($utf8Preamble -join "-")
            $bytes -join "-" | Should -Match ($utf8bytes -join "-")
        }

        It "Export-CliXml creates file with UTF-8 encoding without BOM" {
            [pscustomobject]@{ Key = $testStr } | Export-Clixml $outputFile
            $bytes = Get-FileBytes $outputFile
            $bytes[0,1,2] -join "-" | Should -Not -Be ($utf8Preamble -join "-")
            $bytes -join "-" | Should -Match ($utf8bytes -join "-")
        }

        It "Appends correctly on non-Windows systems" -Skip:$IsWindows {
            bash -c "echo '${testStr}' > $outputFile"
            ${testStr} >> $outputFile
            $bytes = Get-FileBytes $outputFile
            $Expected = $( $ExpectedWithNewline; $ExpectedWithNewline )
            $bytes -join "-" | Should -Be ($Expected -join "-")
        }
    }

    Context "Parameter 'Encoding' should accept numeric and string Ids" {
        BeforeAll {
            $testValue = "ф"
            if ($IsWindows) {
                # Expected bytes: 244 - 'ф', 13  - '`r', 10  - '`n'.
                $expectedBytes = 244,13,10 -join "-"
            } else {
                $expectedBytes = 244,10 -join "-"
            }
        }

        It "Parameter 'Encoding' should accept '<encoding>'" -TestCases @(
            @{ encoding = 1251 }
            @{ encoding = "windows-1251" }
        ) {
            param ( $encoding )
            $testFile = "${TESTDRIVE}/fileEncoding-$($encoding).txt"

            Set-Content $testFile -Encoding $encoding -Value $testValue

            Get-Content $testFile -Encoding $encoding | Should -BeExactly $testValue
            (Get-Content $testFile -AsByteStream) -join "-" | Should -BeExactly $expectedBytes
        }
    }

    Context "Using encoding utf7 results in a warning" {
        BeforeAll {
            $expectedString = "Encoding 'UTF-7' is obsolete, please use UTF-8."
            $testCases = @(
                @{ Command = 'Add-Content';   Script = { "test" | Add-Content -Encoding utf7 -Path TESTDRIVE:/file 3>TESTDRIVE:/warning } }
                @{ Command = 'Export-CliXml'; Script = { "test" | Export-Clixml -Path TESTDRIVE:/file.ps1xml -Encoding utf7 3>TESTDRIVE:/warning } }
                @{ Command = 'Export-Csv';    Script = { "test" | Export-Csv -Path TESTDRIVE:/export.csv -Encoding utf7 3>TESTDRIVE:/warning } }
                @{ Command = 'Format-Hex';    Script = { "test" | Format-Hex -Encoding utf7 > TESTDRIVE:/output 3>TESTDRIVE:/warning } }
                @{ Command = 'Get-Content';   Script = { "output" > TESTDRIVE:/input; $null = Get-Content -Path TESTDRIVE:/input -Encoding utf7 3>TESTDRIVE:/warning } }
                @{ Command = 'Import-Csv';    Script = { "test" | Export-Csv -Path TESTDRIVE:/output.csv; $null = Import-Csv -Path TESTDRIVE:/output.csv -Encoding utf7 3>TESTDRIVE:/warning } }
                @{ Command = 'Out-File';      Script = { "test" | Out-File -Path TESTDRIVE:/output.txt -Encoding utf7 3>TESTDRIVE:/warning } }
                @{ Command = 'Select-String'; Script = { "aa" | Select-String -pattern bb -Encoding utf7 3>TESTDRIVE:/warning } }
                @{ Command = 'Set-Content';   Script = { "aa" | Set-Content -Path TESTDRIVE:/output.txt -Encoding utf7 3>TESTDRIVE:/warning } }
            )
        }
        BeforeEach {
            Remove-Item TESTDRIVE:/* -force -ErrorAction Ignore
        }
        It "'<command> has a warning when '-Encoding utf7' is used" -TestCases $testCases {
            param ($command, $script)
            & $script
            $observed = Get-Content TESTDRIVE:/warning
            $observed | Should -BeExactly $expectedString
        }
    }
}
