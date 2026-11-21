# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Tests for human-readable rendering of file sizes and timestamps' -Tag 'CI' {
    BeforeAll {
        $root = Join-Path $TestDrive 'HumanReadable'
        $null = New-Item -ItemType Directory -Path $root -Force

        # Files with known sizes.
        $script:bytesFile = Join-Path $root 'bytes.bin'
        $script:kiloFile = Join-Path $root 'kilo.bin'
        $script:megaFile = Join-Path $root 'mega.bin'

        [System.IO.File]::WriteAllBytes($bytesFile, [byte[]]::new(745))
        [System.IO.File]::WriteAllBytes($kiloFile, [byte[]]::new(1500))
        [System.IO.File]::WriteAllBytes($megaFile, [byte[]]::new(25MB))

        # The expected value for 'kilo.bin' is computed the same way the implementation computes it,
        # so the test is resilient against the current culture's decimal separator.
        $script:expectedKilo = '{0:0.#} KB' -f ([math]::Truncate(1500 / 1KB * 10) / 10)

        # A file used to exercise the 'LastWriteTimeString' rendering.
        $script:timeFile = Join-Path $root 'time.bin'
        [System.IO.File]::WriteAllBytes($timeFile, [byte[]]::new(1))

        # Save the settings so they can be restored for subsequent tests, as '$PSStyle' is a
        # process-wide singleton shared by all tests.
        $script:oldSize = $PSStyle.HumanReadable.Size
        $script:oldDatetime = $PSStyle.HumanReadable.Datetime
    }

    AfterAll {
        $PSStyle.HumanReadable.Size = $script:oldSize
        $PSStyle.HumanReadable.Datetime = $script:oldDatetime
    }

    AfterEach {
        $PSStyle.HumanReadable.Size = $true
        $PSStyle.HumanReadable.Datetime = $false
    }

    Context 'Size' {
        It 'LengthString is humanized by default for <fileName>' -TestCases @(
            @{ FileName = 'bytes.bin'; FileSize = 745; Expected = '745 B' }
            @{ FileName = 'mega.bin'; FileSize = 25MB; Expected = '25 MB' }
        ) {
            param($FileName, $FileSize, $Expected)

            $item = Get-Item (Join-Path $root $FileName)
            $item.Length | Should -Be $FileSize
            $item.LengthString | Should -BeExactly $Expected
        }

        It 'LengthString uses one truncated decimal and a binary unit for sizes above 1 KB' {
            # The expected value is computed the same way the implementation computes it,
            # so the test is resilient against the current culture's decimal separator.
            $expected = '{0:0.#} KB' -f ([math]::Truncate(1500 / 1KB * 10) / 10)
            (Get-Item $kiloFile).LengthString | Should -BeExactly $expected
        }

        It 'Zero-length file renders as "0 B"' {
            $path = Join-Path $root 'empty.bin'
            $null = New-Item -ItemType File -Path $path -Force
            (Get-Item $path).LengthString | Should -BeExactly '0 B'
        }

        It 'LengthString shows the raw byte count when $PSStyle.HumanReadable.Size is disabled' {
            $PSStyle.HumanReadable.Size = $false
            (Get-Item $kiloFile).LengthString | Should -BeExactly '1500'
        }

        It 'Directory listing shows humanized lengths by default' {
            # The 'children' view is selected explicitly because on Unix platforms the default
            # view for FileSystemInfo is 'childrenWithUnixStat', which renders the raw UnixStat
            # size and formats LastWriteTime directly instead of using the LengthString and
            # LastWriteTimeString properties exercised here.
            $out = Get-Item $kiloFile | Format-Table -View children | Out-String
            $out.Contains($script:expectedKilo) | Should -BeTrue
        }

        It 'Directory listing shows raw lengths when $PSStyle.HumanReadable.Size is disabled' {
            $PSStyle.HumanReadable.Size = $false
            $out = Get-Item $kiloFile | Format-Table -View children | Out-String
            $out.Contains($script:expectedKilo) | Should -BeFalse
            $out.Contains('1500') | Should -BeTrue
        }

        It 'Offline file length is wrapped in parentheses when humanized' -Skip:(-not $IsWindows) {
            $path = Join-Path $root 'offline.bin'
            [System.IO.File]::WriteAllBytes($path, [byte[]]::new(2048))
            [System.IO.File]::SetAttributes($path, [System.IO.FileAttributes]::Offline)

            try {
                $item = Get-Item $path
                if (-not $item.Attributes.HasFlag([System.IO.FileAttributes]::Offline)) {
                    # The file system did not retain the attribute.
                    Set-ItResult -Skipped -Because "the 'Offline' file attribute could not be set"
                    return
                }

                $item.LengthString | Should -BeExactly '(2 KB)'

                $PSStyle.HumanReadable.Size = $false
                (Get-Item $path).LengthString | Should -BeExactly '(2048)'
            }
            finally {
                [System.IO.File]::SetAttributes($path, [System.IO.FileAttributes]::Normal)
            }
        }
    }

    Context 'Datetime' {
        AfterEach {
            $PSStyle.HumanReadable.Datetime = $false
        }

        It 'LastWriteTimeString is culture-aware absolute by default' {
            $item = Get-Item $timeFile
            $item.LastWriteTimeString | Should -BeExactly ('{0,10:d} {0,8:t}' -f $item.LastWriteTime)
        }

        It 'LastWriteTimeString renders "just now" for a recently written file when enabled' {
            $PSStyle.HumanReadable.Datetime = $true
            # Rewrite the file so its LastWriteTime is guaranteed to be within the last minute,
            # even when the test agent is slow to get to this test.
            [System.IO.File]::WriteAllBytes($timeFile, [byte[]]::new(1))
            (Get-Item $timeFile).LastWriteTimeString | Should -BeExactly 'just now'
        }

        It 'LastWriteTimeString renders "<Expected>" when enabled' -TestCases @(
            @{ Delta = [TimeSpan]::FromMinutes(5.5); Expected = '5 min ago' }
            @{ Delta = [TimeSpan]::FromHours(1.05); Expected = '1 hour ago' }
            @{ Delta = [TimeSpan]::FromHours(3.05); Expected = '3 hours ago' }
            @{ Delta = [TimeSpan]::FromDays(1.05); Expected = '1 day ago' }
            @{ Delta = [TimeSpan]::FromDays(2.05); Expected = '2 days ago' }
        ) {
            param($Delta, $Expected)

            $PSStyle.HumanReadable.Datetime = $true
            $item = Get-Item $timeFile
            $item.LastWriteTime = (Get-Date) - $Delta

            (Get-Item $timeFile).LastWriteTimeString | Should -BeExactly $Expected
        }

        It 'LastWriteTimeString falls back to the absolute format for values older than 7 days or in the future' -TestCases @(
            @{ Delta = [TimeSpan]::FromDays(40) }
            @{ Delta = [TimeSpan]::FromDays(-1) }
        ) {
            param($Delta)

            $PSStyle.HumanReadable.Datetime = $true
            $item = Get-Item $timeFile
            $item.LastWriteTime = (Get-Date) - $Delta

            $item = Get-Item $timeFile
            $item.LastWriteTimeString | Should -BeExactly ('{0,10:d} {0,8:t}' -f $item.LastWriteTime)
        }

        It 'Directory listing shows a relative timestamp when enabled' {
            $PSStyle.HumanReadable.Datetime = $true
            $item = Get-Item $timeFile
            $item.LastWriteTime = (Get-Date) - [TimeSpan]::FromMinutes(5.5)

            $out = Get-Item $timeFile | Format-Table -View children | Out-String
            $out.Contains('5 min ago') | Should -BeTrue
        }
    }
}
