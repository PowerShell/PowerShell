Describe "Get-FileHash" -Tags "CI" {

    BeforeAll {
        $testDocument = Join-Path -Path $PSScriptRoot -ChildPath assets testablescript.ps1
        Write-Host $testDocument
    }

    Context "Default result tests" {
        It "Should default to correct algorithm, hash and path" {
            $result = Get-FileHash $testDocument
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
        It "Should be able to get the correct hash from <algorithm> algorithm" -TestCases $testCases {
            param($algorithm, $hash)
            $algorithmResult = Get-FileHash $testDocument -Algorithm $algorithm
            $algorithmResult.Hash | Should Be $hash
        }

        It "Should be throw for wrong algorithm name" {
            try {
                Get-FileHash Get-FileHash $testDocument -Algorithm wrongAlgorithm
                throw "No Exception!"
            }
            catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetFileHashCommand"
            }
        }
    }

    Context "Paths tests" {
        It "With '-Path': no file exist" {
            try {
                Get-FileHash -Path nofileexist.ttt -ErrorAction Stop
                throw "No Exception!"
            }
            catch {
                $_.FullyQualifiedErrorId | Should Be "FileNotFound,Microsoft.PowerShell.Commands.GetFileHashCommand"
            }
        }

        It "With '-LiteralPath': no file exist" {
            try {
                Get-FileHash -LiteralPath nofileexist.ttt -ErrorAction Stop
                throw "No Exception!"
            }
            catch {
                $_.FullyQualifiedErrorId | Should Be "FileNotFound,Microsoft.PowerShell.Commands.GetFileHashCommand"
            }
        }

        It "With '-Path': file exist" {
            $result = Get-FileHash -Path $testDocument
            $result.Path | Should Be $testDocument
        }

        It "With '-LiteralPath': file exist" {
            $result = Get-FileHash -LiteralPath $testDocument
            $result.Path | Should Be $testDocument
        }
    }
}
