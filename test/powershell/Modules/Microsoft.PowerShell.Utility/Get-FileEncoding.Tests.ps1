# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-FileEncoding" -Tags "CI" {
    BeforeAll {
        $testFilePath = Join-Path -Path $TestDrive -ChildPath test.txt
        $testFileLiteralPath = Join-Path -Path $TestDrive -ChildPath "[test].txt"
        $content = 'abc'

        $testCases = @(
            @{ EncodingName = 'ansi'; ExpectedEncoding = 'utf-8' }
            @{ EncodingName = 'ascii'; ExpectedEncoding = 'utf-8' }
            @{ EncodingName = 'bigendianunicode'; ExpectedEncoding = 'utf-16BE' }
            @{ EncodingName = 'bigendianutf32'; ExpectedEncoding = 'utf-32BE' }
            @{ EncodingName = 'oem'; ExpectedEncoding = 'utf-8' }
            @{ EncodingName = 'unicode'; ExpectedEncoding = 'utf-16' }
            @{ EncodingName = 'utf8'; ExpectedEncoding = 'utf-8' }
            @{ EncodingName = 'utf8BOM'; ExpectedEncoding = 'utf-8' }
            @{ EncodingName = 'utf8NoBOM'; ExpectedEncoding = 'utf-8' }
            @{ EncodingName = 'utf32'; ExpectedEncoding = 'utf-32' }
        )
    }

    It "Validate Get-FileEncoding using -Path returns file encoding for '<EncodingName>'" -TestCases $testCases {
        param($EncodingName, $ExpectedEncoding)
        Set-Content -Path $testFilePath -Encoding $EncodingName -Value $content -Force
        (Get-FileEncoding -Path $testFilePath).BodyName | Should -Be $ExpectedEncoding
    }

    It "Validate Get-FileEncoding using -LiteralPath returns file encoding for '<EncodingName>'" -TestCases $testCases {
        param($EncodingName, $ExpectedEncoding)
        Set-Content -LiteralPath $testFileLiteralPath -Encoding $EncodingName -Value $content -Force
        (Get-FileEncoding -LiteralPath $testFileLiteralPath).BodyName | Should -Be $ExpectedEncoding
    }

    It "Should throw exception if path is not found using -Path" {
        { Get-FileEncoding -Path nonexistentpath } | Should -Throw -ErrorId 'PathNotFound,Microsoft.PowerShell.Commands.GetFileEncodingCommand'
    }

    It "Should throw exception if path is not found using -Path" {
        { Get-FileEncoding -Literal nonexistentpath } | Should -Throw -ErrorId 'PathNotFound,Microsoft.PowerShell.Commands.GetFileEncodingCommand'
    }
}
