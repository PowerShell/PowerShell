Describe "File encoding tests" -tag CI {

    Context "ParameterType for parameter 'Encoding' should be 'Encoding'" {
        BeforeAll {
            $testCases = get-command -module Microsoft.PowerShell.* | 
                Where-Object { $_.Parameters -and $_.Parameters['Encoding'] } |
                ForEach-Object { @{ Command = $_ } }
        }
        It "Encoding parameter of command '<Command>' is type 'Encoding'" -testcase $testCases {
            param ( $command )
            $command.Parameters['Encoding'].ParameterType.FullName | Should Be "System.Text.Encoding"
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
                # new-item does not add CR/NL
                @{ Command = "new-item"; Parameters = @{ type = "file";value = $testStr; Path = $outputFile }; Expected = $utf8Bytes ; Operator = "be" }
                # the following commands add a CR/NL
                @{ Command = "set-content"; Parameters = @{ value = $testStr; Path = $outputFile }; Expected = $ExpectedWithNewline ; Operator = "be" }
                @{ Command = "add-content"; Parameters = @{ value = $testStr; Path = $outputFile }; Expected = $ExpectedWithNewline ; Operator = "be" }
                @{ Command = "out-file"; Parameters = @{ InputObject = $testStr; Path = $outputFile }; Expected = $ExpectedWithNewline ; Operator = "be" }
                # Redirection
                @{ Command = { $testStr > $outputFile } ; Expected = $ExpectedWithNewline ; Operator = "be" }
                )
            function Get-FileBytes ( $path ) {
                [io.file]::ReadAllBytes($path)
            }
        }

        AfterEach { 
            if ( test-path $outputFile ) {
                remove-item -force $outputFile
            }
        }

        It "<command> produces correct content '<Expected>'" -testcases $simpleTestCases {
            param ( $Command, $parameters, $Expected, $Operator)
            & $command @parameters
            $bytes = Get-FileBytes $outputFile
            $bytes -join "-" | should ${Operator} ($Expected -join "-")
        }

        It "Export-CSV creates file with UTF-8 encoding without BOM" {
            [pscustomobject]@{ Key = $testStr } | export-csv $outputFile
            $bytes = Get-FileBytes $outputFile
            $bytes[0,1,2] -join "-" | should not be ($utf8Preamble -join "-")
            $bytes -join "-" | should match ($utf8bytes -join "-")
        }

        It "Export-CliXml creates file with UTF-8 encoding without BOM" {
            [pscustomobject]@{ Key = $testStr } | export-clixml $outputFile
            $bytes = Get-FileBytes $outputFile
            $bytes[0,1,2] -join "-" | should not be ($utf8Preamble -join "-")
            $bytes -join "-" | should match ($utf8bytes -join "-")
        }

        It "Appends correctly on non-Windows systems" -skip:$IsWindows {
            bash -c "echo '${testStr}' > $outputFile"
            ${testStr} >> $outputFile
            $bytes = Get-FileBytes $outputFile
            $Expected = $( $ExpectedWithNewline; $ExpectedWithNewline )
            $bytes -join "-" | should be ($Expected -join "-")
        }

    }
}
