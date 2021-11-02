# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Tee-Object" -Tags "CI" {

    Context "Validate Tee-Object is correctly forking output" {

        BeforeAll {
            $testfile = Join-Path $TestDrive -ChildPath "testfile.txt"
            $testvalue = "ф"
            if ($IsWindows) {
                # Expected bytes: 244 - 'ф', 13  - '`r', 10  - '`n'.
                $expectedBytes = 244,13,10 -join "-"
            } else {
                $expectedBytes = 244,10 -join "-"
            }
        }

        BeforeEach {
            Remove-Item -Path $testfile -ErrorAction SilentlyContinue -Force
        }

        It "Should return the output to the screen and to the variable" {
            Write-Output teeobjecttest1 | Tee-Object -Variable teeresults
            $teeresults | Should -BeExactly "teeobjecttest1"
        }

        It "Should tee the output to a file" {
            $teefile = $testfile
            Write-Output teeobjecttest3  | Tee-Object $teefile
            Get-Content $teefile | Should -BeExactly "teeobjecttest3"
        }

        $unicodeTestString = "A1£ę௸🤔"
        It "Should tee output to file using <encoding> encoding when selected" -TestCases @(
            @{ Encoding = "ascii"; Content = "teeobjecttest1"},
            @{ Encoding = "bigendianunicode"; Content = $unicodeTestString  },
            @{ Encoding = "default"; Content = $unicodeTestString },
            @{ Encoding = "latin1"; Content = "téèõbjêcttëst1" },
            @{ Encoding = "unicode"; Content = $unicodeTestString },
            @{ Encoding = "utf32"; Content = $unicodeTestString },
            @{ Encoding = "utf7"; Content = $unicodeTestString},
            @{ Encoding = "utf8"; Content = $unicodeTestString}
        ) {
            param($Encoding, $Content)
            $teefile = $testfile
            Write-Output -InputObject $content  | Tee-Object -FilePath $teefile -Encoding $Encoding
            Get-Content -Path $teefile -Encoding $Encoding | Should -BeExactly $content
            Remove-Item -Path $teefile -ErrorAction SilentlyContinue
        }

        It "Parameter 'Encoding' should accept encoding" {
            $teefile = $testfile
            $encoding = 1251
            $testvalue | Tee-Object -Encoding $encoding $teefile
            Get-Content $teefile -Encoding $encoding | Should -BeExactly $testvalue
            (Get-Content $teefile -AsByteStream) -join "-" | Should -BeExactly $expectedBytes
        }
    }
}

Describe "Tee-Object DRT Unit Tests" -Tags "CI" {
    BeforeAll {
        $tempFile = Join-Path $TestDrive -ChildPath "TeeObjectTestsTempFile"
    }
    It "Positive File Test" {
        $expected = "1", "2", "3"
        $results = $expected | Tee-Object -FilePath $tempFile
        $results.Length | Should -Be 3
        $results | Should -Be $expected
        $content = Get-Content $tempFile
        $content | Should -Be $expected
    }

    It "Positive File Test with Path parameter alias" {
        $expected = "1", "2", "3"
        $results = $expected | Tee-Object -Path $tempFile
        $results.Length | Should -Be 3
        $results | Should -Be $expected
        $content = Get-Content $tempFile
        $content | Should -Be $expected
    }

    It "Positive Variable Test" {
        $expected = "1", "2", "3"
        $varName = "teeObjectTestVar"
        $results = $expected | Tee-Object -Variable $varName
        $results.Length | Should -Be 3
        $results | Should -Be $expected

        $results = Get-Variable -Name $varName -ValueOnly
        $results.Length | Should -Be 3
        $results | Should -Be $expected
    }

}
