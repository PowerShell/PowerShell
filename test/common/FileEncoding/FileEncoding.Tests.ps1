# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

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
                $bytes = [byte[]]::new(3)
                $stream = [System.IO.File]::OpenRead($_)
                $stream.Read($bytes, 0, 3) > $null
                $stream.Close()
                $bytes[0] -eq 0xef -and $bytes[1] -eq 0xbb -and $bytes[2] -eq 0xbf
            }
        }

        $nonBinaryFiles | hasUtf8Bom | Should -BeNullOrEmpty
    }
}
