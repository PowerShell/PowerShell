Describe "Get-FileHash" -Tags "CI" {

    BeforeAll {
        $testDocument = Join-Path -Path $PSScriptRoot -ChildPath assets testablescript.ps1
        Write-Host $testDocument
    }

    Context "Default result tests" {
        It "Should default to correct algorithm, hash and path" {
            $result = Get-FileHash $testDocument
            $result.Algorithm | Should Be "SHA256"
            $result.Hash | Should Be "41620F6C9F3531722EFE90AED9ABBC1D1B31788AA9141982030D3DDE199F770C"
            $result.Path | Should Be $testDocument
        }
    }

    Context "Algorithm tests" {
        BeforeAll {
            # Keep "sHA1" below! It is for testing that the cmdlet accept a hash algorithm name in any case!
            $testcases =
                @{ algorithm = "sHA1";   hash = "0C483659B1F2D5A8F116211DE8F58BF45893CFFB" },
                @{ algorithm = "SHA256"; hash = "41620F6C9F3531722EFE90AED9ABBC1D1B31788AA9141982030D3DDE199F770C" },
                @{ algorithm = "SHA384"; hash = "EC4C4D4F0B2A79F216118C5A5059B8CE061097BA9161BE5890C098AAEB5DB169C13DAE0A6F855C9A589CD11DF47D0C87" },
                @{ algorithm = "SHA512"; hash = "6ABA8BA8B619100A6829BEB940D9D77E4A8197FDCAC2D0FE5AD6C2758DACC5A59774195FD8A7A92780B7582A966B81CA0C1576C0044C5AF7BE20F5CCF425BD76" },
                @{ algorithm = "MD5";    hash = "F9D78BD059AB162BEA21EB02BADDE001" }
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
