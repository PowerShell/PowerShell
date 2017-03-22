Import-Module $PSScriptRoot\..\..\Common\Test.Helpers.psm1
Describe "Get-Hash tests for files" -Tags "CI" {

    BeforeAll {
        $testDocument = Join-Path -Path $PSScriptRoot -ChildPath assets testablescript.ps1
        Write-Host $testDocument
    }

    Context "Default result tests" {
        It "Should default to correct algorithm, hash and path" {
            $result = Get-Hash $testDocument

            $result | Should BeOfType 'Microsoft.PowerShell.Commands.FileHashInfo'
            $result.Algorithm | Should Be "SHA256"
            $result.Hash | Should Be "4A6DA9F1C0827143BB19FC4B0F2A8057BC1DF55F6D1F62FA3B917BA458E8F570"
            $result.Path | Should Be $testDocument
        }
    }

    Context "Algorithm tests" {
        BeforeAll {
            # Keep "sHA1" below! It is for testing that the cmdlet accept a hash algorithm name in any case!
            $testcases =
                @{ algorithm = "sHA1";   hash = "01B865D143E07ECC875AB0EFC0A4429387FD0CF7" },
                @{ algorithm = "SHA256"; hash = "4A6DA9F1C0827143BB19FC4B0F2A8057BC1DF55F6D1F62FA3B917BA458E8F570" },
                @{ algorithm = "SHA384"; hash = "656215B6A07011E625206F43E57873F49AD7B36DFCABB70F6CDCE2303D7A603E55D052774D26F339A6D80A264340CB8C" },
                @{ algorithm = "SHA512"; hash = "C688C33027D89ACAC920545471C8053D8F64A54E21D0415F1E03766DDCDA215420E74FAFD1DC399864C6B6B5723A3358BD337339906797A39090B02229BF31FE" },
                @{ algorithm = "MD5";    hash = "7B09811D1631C9FD46B39D1D35522F0A" }
        }

        It "Should be able to get the correct hash by Path from <algorithm> algorithm" -TestCases $testCases {
            param($algorithm, $hash)
            $algorithmResult = Get-Hash -Path $testDocument -Algorithm $algorithm

            $algorithmResult | Should BeOfType 'Microsoft.PowerShell.Commands.FileHashInfo'
            $algorithmResult.Algorithm | Should Be $algorithm
            $algorithmResult.Hash | Should Be $hash
            $algorithmResult.Path | Should Be $testDocument
        }

        It "Should be able to get the correct hash by InputStream from <algorithm> algorithm" -TestCases $testCases {
            param($algorithm, $hash)
            $testFileStream = [System.IO.File]::OpenRead($testDocument)
            $algorithmResult = Get-Hash -InputStream $testFileStream -Algorithm $algorithm

            $algorithmResult | Should BeOfType 'Microsoft.PowerShell.Commands.FileHashInfo'
            $algorithmResult.Algorithm | Should Be $algorithm
            $algorithmResult.Hash | Should Be $hash
        }

        It "Should be able to get the correct hash by String from <algorithm> algorithm" -TestCases $testCases {
            param($algorithm, $hash)
            # Simple trick needed to get a test string from byte sequence because the test file contains BOM.
            # It allows to reuse the file hashes.
            $testBytes = Get-Content $testDocument -Raw -Encoding Byte
            $testString = [System.Text.Encoding]::UTF8.GetString($testBytes)
            $algorithmResult = Get-Hash -InputString $testString -Algorithm $algorithm -Encoding UTF8

            $algorithmResult | Should BeOfType 'Microsoft.PowerShell.Commands.StringHashInfo'
            $algorithmResult.Algorithm | Should Be $algorithm
            $algorithmResult.Hash | Should Be $hash
            $algorithmResult.Encoding | Should Be 'UTF8'
            $algorithmResult.HashedString | Should Be $testString
        }

        It "Should be able to get the correct hash for 'null' String" {
            $result = Get-Hash -InputString $null

            $result | Should BeOfType 'Microsoft.PowerShell.Commands.StringHashInfo'
            $result.Algorithm | Should Be 'SHA256'
            $result.Hash | Should Be $null
            $result.Encoding | Should Be 'Default'
            $result.HashedString | Should Be $null
        }

        It "Should be throw for wrong algorithm name" {
            { Get-Hash Get-Hash $testDocument -Algorithm wrongAlgorithm } | ShouldBeErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetFileHashCommand"
        }
    }

    Context "Paths tests" {
        It "With '-Path': no file exist" {
            { Get-Hash -Path nofileexist.ttt -ErrorAction Stop } | ShouldBeErrorId "FileNotFound,Microsoft.PowerShell.Commands.GetFileHashCommand"
        }

        It "With '-LiteralPath': no file exist" {
            { Get-Hash -LiteralPath nofileexist.ttt -ErrorAction Stop } | ShouldBeErrorId "FileNotFound,Microsoft.PowerShell.Commands.GetFileHashCommand"
        }

        It "With '-Path': file exist" {
            $result = Get-Hash -Path $testDocument
            $result.Path | Should Be $testDocument
        }

        It "With '-LiteralPath': file exist" {
            $result = Get-Hash -LiteralPath $testDocument
            $result.Path | Should Be $testDocument
        }
    }
}
