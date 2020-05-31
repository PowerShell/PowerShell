# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

using namespace System

Describe 'Verify file encoding' {
    BeforeAll {
        Push-Location $PSScriptRoot\..\..\..
        $nonBinaryFiles = (git grep -I --files-with-matches -e $'')
    }

    AfterAll {
        Pop-Location
    }

    It 'No UTF-8 BOM' {
        filter hasUtf8Bom {
            $_.Where{
                $bom = [Text.UnicodeEncoding]::UTF8.GetPreamble()
                $bytes = [byte[]]::new($bom.Length)
                $stream = [IO.File]::OpenRead($_)
                $stream.Read($bytes, 0, $bytes.Length) > $null
                $stream.Close()
                [Linq.Enumerable]::SequenceEqual($bom, $bytes)
            }
        }

        $nonBinaryFiles | hasUtf8Bom | Should -BeNullOrEmpty
    }
}
