
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Functional tests to verify that output from native executables is not encoded
# and decoded when piping to another native executable.

Describe 'Native command byte piping tests' -Tags 'CI' {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if (-not [ExperimentalFeature]::IsEnabled('PSNativeCommandPreserveBytePipe'))
        {
            $PSDefaultParameterValues['It:Skip'] = $true
            return
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It 'Bytes are retained between native executables' {
        testexe -writebytes FF | testexe -readbytes | Should -BeExactly FF
    }

    It 'Byte literals are retained when piped directly' {
        0xFFuy | testexe -readbytes | Should -BeExactly FF
        0xBEuy, 0xEFuy | testexe -readbytes | Should -BeExactly BE, EF
        ,[byte[]](0xBEuy, 0xEFuy) | testexe -readbytes | Should -BeExactly BE, EF
    }

    It 'Output behavior falls back when stderr is redirected to stdout' {
        $newLine = [Environment]::NewLine.ToCharArray().ForEach{ '{0:X2}' -f [int]$_ }
        testexe -writebytes FF 2>&1 | testexe -readbytes | Should -BeExactly @('EF', 'BF', 'BD'; $newLine)
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
}
