# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Get-FileHash" -Tags "CI" {

    BeforeAll {
        $testDocument = Join-Path -Path $TestDrive -ChildPath "hashable.txt"
        $utf8NoBOM = [System.Text.UTF8Encoding]::new($false)
        [System.IO.File]::WriteAllText($testDocument, "Get-Module`n", $utf8NoBOM)
    }

    Context "Validate platform encoding" {
        # If this test fails, then the problem lies outside the Get-FileHash implementation
        It "Should have the bytes in the file as expected" {
            [System.IO.File]::ReadAllBytes($testDocument) | Should -BeExactly @(0x47, 0x65, 0x74, 0x2d, 0x4d, 0x6f, 0x64, 0x75, 0x6c, 0x65, 0x0a)
        }
    }

    Context "Default result tests" {
        It "Should default to correct algorithm, hash and path" {
            $result = Get-FileHash $testDocument
            $result.Algorithm | Should -Be "SHA256"
            $result.Hash | Should -Be "41620f6c9f3531722efe90aed9abbc1d1b31788aa9141982030d3dde199f770c"
            $result.Path | Should -Be $testDocument
        }

        It "Should write non-terminating error if argument is a folder" {
            $result = $PSHOME, "${pshome}\pwsh.dll" | Get-FileHash -ErrorVariable errorVariable -ErrorAction SilentlyContinue
            $result.Count | Should -Be 1
            $errorVariable.FullyQualifiedErrorId | Should -BeExactly "UnauthorizedAccessError,Microsoft.PowerShell.Commands.GetFileHashCommand"
        }

        It "Should write non-terminating error if a file is locked" -Skip:(-not $IsWindows) {
            $pagefilePath = (Get-CimInstance -ClassName Win32_PageFileusage).Name
            $result = $pagefilePath, "${pshome}\pwsh.dll" | Get-FileHash -ErrorVariable errorVariable -ErrorAction SilentlyContinue
            $result.Count | Should -Be 1
            $errorVariable.FullyQualifiedErrorId | Should -BeExactly "FileReadError,Microsoft.PowerShell.Commands.GetFileHashCommand"
        }
    }

    Context "Algorithm tests" {
        BeforeAll {
            # Keep "sHA1" below! It is for testing that the cmdlet accept a hash algorithm name in any case!
            $testcases =
                @{ algorithm = "sHA1";   hash = "0c483659b1f2d5a8f116211de8f58bf45893cffb" },
                @{ algorithm = "SHA256"; hash = "41620f6c9f3531722efe90aed9abbc1d1b31788aa9141982030d3dde199f770c" },
                @{ algorithm = "SHA384"; hash = "ec4c4d4f0b2a79f216118c5a5059b8ce061097ba9161be5890c098aaeb5db169c13dae0a6f855c9a589cd11df47d0c87" },
                @{ algorithm = "SHA512"; hash = "6aba8ba8b619100a6829beb940d9d77e4a8197fdcac2d0fe5ad6c2758dacc5a59774195fd8a7a92780b7582a966b81ca0c1576c0044c5af7be20f5ccf425bd76" },
                @{ algorithm = "MD5";    hash = "f9d78bd059ab162bea21eb02badde001" }
        }
        It "Should be able to get the correct hash from <algorithm> algorithm" -TestCases $testCases {
            param($algorithm, $hash)
            $algorithmResult = Get-FileHash $testDocument -Algorithm $algorithm
            $algorithmResult.Hash | Should -Be $hash
        }

        It "Should be throw for wrong algorithm name" {
            { Get-FileHash Get-FileHash $testDocument -Algorithm wrongAlgorithm } | Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetFileHashCommand"
        }
    }

    Context "Paths tests" {
        It "With '-Path': no file exist" {
            { Get-FileHash -Path nofileexist.ttt -ErrorAction Stop } | Should -Throw -ErrorId "FileNotFound,Microsoft.PowerShell.Commands.GetFileHashCommand"
        }

        It "With '-LiteralPath': no file exist" {
            { Get-FileHash -LiteralPath nofileexist.ttt -ErrorAction Stop } | Should -Throw -ErrorId "FileNotFound,Microsoft.PowerShell.Commands.GetFileHashCommand"
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

    Context "File should be closed before Get-FileHash writes pipeline output" {
        It "Should be able to edit the file without 'file is in use' exceptions" {
            # This test runs against a copy of the document
            # because it involves renaming it,
            # and that might break tests added later on.
            $testDocumentCopy = "${testDocument}-copy"
            Copy-Item -Path $testdocument -Destination $testDocumentCopy

            $newPath = Get-FileHash -Path $testDocumentCopy | Rename-Item -NewName {$_.Hash} -PassThru
            $newPath.FullName | Should -Exist

            Remove-Item -Path $testDocumentCopy -Force -ErrorAction SilentlyContinue
        }
    }
}
