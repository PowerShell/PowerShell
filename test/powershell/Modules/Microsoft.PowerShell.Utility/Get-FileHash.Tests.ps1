# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Get-FileHash" -Tags "CI" {

    BeforeAll {
        $testDocument = Join-Path -Path $PSScriptRoot -ChildPath assets testablescript.ps1
        Write-Host $testDocument
    }

    Context "Default result tests" {
        It "Should default to correct algorithm, hash and path" {
            $result = Get-FileHash $testDocument
            $result.Algorithm | Should -Be "SHA256"
            $result.Hash | Should -Be "8129a08e5d748ffb9361375677785f96545a1a37619a27608efd76a870787a7a"
            $result.Path | Should -Be $testDocument
        }
    }

    Context "Algorithm tests" {
        BeforeAll {
            # Keep "sHA1" below! It is for testing that the cmdlet accept a hash algorithm name in any case!
            $testcases =
                @{ algorithm = "sHA1";   hash = "f262f3d36c279883e81218510c06dc205ef24c9b" },
                @{ algorithm = "SHA256"; hash = "8129a08e5d748ffb9361375677785f96545a1a37619a27608efd76a870787a7a" },
                @{ algorithm = "SHA384"; hash = "77cdffd27d3dcd5810c3d32b4eca656f3ce61cb0081c5ca9bf21be856c0007f9fef2f588bae512a6ecf8dc56618aedc3" },
                @{ algorithm = "SHA512"; hash = "82e3bf7da14b6872b82d67af6580d25123b3612ba2dfcd0746036f609c7752e74af41e97130fbe943ec7b8c61549578176bff522d93dfb2f4b681de9f841c231" },
                @{ algorithm = "MD5";    hash = "2d70c2c2cf8ae23a1a86e64ffce2bbca" }
        }
        It "Should be able to get the correct hash from <algorithm> algorithm" -TestCases $testCases {
            param($algorithm, $hash)
            $algorithmResult = Get-FileHash $testDocument -Algorithm $algorithm
            $algorithmResult.Hash | Should -Be $hash
        }

        It "Should be throw for wrong algorithm name" {
            try {
                Get-FileHash Get-FileHash $testDocument -Algorithm wrongAlgorithm
                throw "No Exception!"
            }
            catch {
                $_.| Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetFileHashCommand"
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
                $_.| Should -Throw -ErrorId "FileNotFound,Microsoft.PowerShell.Commands.GetFileHashCommand"
            }
        }

        It "With '-LiteralPath': no file exist" {
            try {
                Get-FileHash -LiteralPath nofileexist.ttt -ErrorAction Stop
                throw "No Exception!"
            }
            catch {
                $_.| Should -Throw -ErrorId "FileNotFound,Microsoft.PowerShell.Commands.GetFileHashCommand"
            }
        }

        It "With '-Path': file exist" {
            $result = Get-FileHash -Path $testDocument
            $result.Path | Should -Be $testDocument
        }

        It "With '-LiteralPath': file exist" {
            $result = Get-FileHash -LiteralPath $testDocument
            $result.Path | Should -Be $testDocument
        }
    }
}
