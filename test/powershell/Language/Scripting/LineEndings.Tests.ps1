# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'Line endings' -Tags "CI" {
    BeforeAll {
        $lf = "`n"
        $cr = "`r"
        $crlf="`r`n"
        $dq = "`""
        $sq = "'"
        $hereDB = "@`""
        $hereDE = "`"@"
        $hereSB = "@'"
        $hereSE = "'@"
        }

    $testData = @(
        @{
            Name = 'CR in single quotes here-string';
            NewLine = $cr
            Begin = $hereSB+$cr;
            End = $cr + $hereSE
        },
        @{
            Name = 'LF in single quotes here-string';
            NewLine = $lf
            Begin = $hereSB+$lf;
            End = $lf + $hereSE
        },
        @{
            Name = 'CRLF in single quotes here-string';
            NewLine = $cr+$lf
            Begin = $hereSB+$cr+$lf;
            End = $cr+$lf + $hereSE
        },
        @{
            Name = 'CR in double quotes here string';
            Begin = $hereDB + $cr;
            NewLine = $cr
            End = $cr + $hereDE
        },
        @{
            Name = 'LF in double quotes here string';
            Begin = $hereDB + $lf;
            NewLine = $lf
            End = $lf + $hereDE
        },
        @{
            Name = 'CRLF in double quotes here string';
            Begin = $hereDB + $cr + $lf;
            NewLine = $cr + $lf
            End = $cr + $lf + $hereDE
        },
        @{
            Name = 'CR in double quotes here string';
            Begin = $hereDB + $cr;
            NewLine = $cr
            End = $cr + $hereDE
        },
        @{
            Name = 'Lf in double quotes here string';
            Begin = $hereDB + $lf;
            NewLine = $lf
            End = $lf + $hereDE
        },
        @{
            Name = 'CRLF in double quotes here string';
            Begin = $hereDB + $cr + $lf;
            NewLine = $cr + $lf
            End = $cr + $lf + $hereDE
        },
        @{
            Name = 'CR in single quotes string';
            Begin = $sq;
            NewLine = $cr
            End =  $sq
        },
        @{
            Name = 'LF in single quotes string';
            Begin = $sq;
            NewLine = $lf
            End =  $sq
        },
        @{
            Name = 'CRLF in single quotes string';
            Begin = $sq;
            NewLine = $cr + $lf
            End =  $sq
        },
        @{
            Name = 'CR in double quotes string';
            Begin = $dq;
            NewLine = $cr
            End =  $dq
        },
        @{
            Name = 'LF in double quotes string';
            Begin = $dq;
            NewLine = $lf
            End =  $dq
        },
        @{
            Name = 'CRLF in double quotes string';
            Begin = $dq;
            NewLine = $cr + $lf
            End =  $dq
        }
    )

    It '<Name> in expression' -TestCases:$testData {
        param([string]$Name, $Begin, $End, $NewLine)
        # build the content string
        $expected = "This$($newline)is$($newline)a$($newline)multi$($newline)line$($newline)string"

        # wrap the content in the specified begin and end quoting characters.
        $content = "$($Begin)$($expected)$($End)"
        $actual = Invoke-Expression $content

        # $actual should be the content string ($expected) without the begin and end quoting characters.
        $actual | Should -BeExactly $expected
    }

    It '<Name> in ps file' -TestCases:$testData {
        param([string]$Name, $Begin, $End)

        $fileName = $Name.Replace(' ', '') + '.ps1'

        # build the content string
        $expected = "This$($newline)is$($newline)a$($newline)multi$($newline)line$($newline)string"

        # wrap the content in the specified begin and end quoting characters.
        $content = "$($Begin)$($expected)$($End)"
        # BUG: Set-Content is failing on linux if the file does not exit.
        $null = New-Item -Path TESTDRIVE:$fileName -Force
        $content | Set-Content -NoNewline -Encoding ascii -Path TESTDRIVE:\$fileName
        $actual = &( "TESTDRIVE:\$fileName")

        # $actual should be the content string ($expected) without the begin and end quoting characters.
        $actual | Should -BeExactly $expected
    }
}

$test = @"
"@
