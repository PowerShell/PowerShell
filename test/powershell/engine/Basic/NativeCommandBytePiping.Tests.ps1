# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Functional tests to verify that output from native executables is not encoded
# and decoded when piping to another native executable.

Describe 'Native command byte piping tests' -Tags 'CI' {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()

        # Without this the test would otherwise be hard coded to a specific set
        # of [Console]::OutputEncoding/$OutputEncoding settings.
        $mangledFFByte = $OutputEncoding.GetBytes(
            [Console]::OutputEncoding.GetString(0xFF) + [Environment]::NewLine).
            ForEach{ '{0:X2}' -f [int]$_ }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It 'Bytes are retained between native executables' {
        testexe -writebytes FF | testexe -readbytes | Should -BeExactly FF
    }

    It 'Retains all <Length> bytes across transfer chunks and a partial final chunk' -TestCases @(
        @{ Length = 1 }
        @{ Length = 16384 }
        @{ Length = 65539 }
    ) {
        param($Length)

        $expected = for ($index = 0; $index -lt $Length; $index++) {
            '{0:X2}' -f ($index % 256)
        }

        testexe -writebytestream $Length | testexe -readbytes | Should -BeExactly $expected

        $path = Join-Path $TestDrive "bytes-$Length.bin"
        testexe -writebytestream $Length > $path
        $actual = [IO.File]::ReadAllBytes($path)
        $actual.Length | Should -Be $Length
        [Convert]::ToHexString($actual) | Should -BeExactly ($expected -join '')
    }

    It 'Byte literals are retained when piped directly' {
        0xFFuy | testexe -readbytes | Should -BeExactly FF
        0xBEuy, 0xEFuy | testexe -readbytes | Should -BeExactly BE, EF
        ,[byte[]](0xBEuy, 0xEFuy) | testexe -readbytes | Should -BeExactly BE, EF
    }

    It 'Output behavior falls back when stderr is redirected to stdout' {
        testexe -writebytes FF 2>&1 | testexe -readbytes | Should -BeExactly $mangledFFByte
    }

    It 'Bytes are retained when using SMA.PowerShell' {
        $ps = $null
        try {
            $ps = [powershell]::Create().
                AddCommand('testexe').AddArgument('-writebytes').AddArgument('FF').
                AddCommand('testexe').AddArgument('-readbytes')

            $ps.Invoke() | Should -BeExactly 'FF'
        } finally {
            ($ps)?.Dispose()
        }
    }

    It 'Bytes are retained when using SteppablePipeline' {
        $pipe = $null
        try {
            $pipe = { testexe -writebytes FF | testexe -readbytes }.GetSteppablePipeline('Internal')
            $pipe.Begin($false)
            $pipe.Process()
            $pipe.End() | Should -BeExactly 'FF'
        } finally {
            ($pipe)?.Dispose()
        }
    }

    It 'Bytes are retained when redirecting to a file' {
        testexe -writebytes FF > $TestDrive/content.bin | Should -BeNullOrEmpty
        Get-Content -LiteralPath $TestDrive/content.bin -AsByteStream | Should -Be 0xFFuy
    }

    It 'Bytes are retained when redirecting to a file and Out-Default is downstream' {
        testexe -writebytes FF > $TestDrive/content2.bin | Out-Default
        Get-Content -LiteralPath $TestDrive/content2.bin -AsByteStream | Should -Be 0xFFuy
    }

    It 'Redirecting to $null should emit no output' {
        testexe -writebytes FF > $null | Should -BeNullOrEmpty
    }
}
