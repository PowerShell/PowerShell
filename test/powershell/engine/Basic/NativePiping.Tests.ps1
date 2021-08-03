# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Piping with native commands' {
    BeforeAll {
        $tmpFile = Join-Path $TestDrive 'tmp.bin'
    }

    BeforeEach {
        Remove-Item -LiteralPath $tmpFile -Force -ErrorAction Ignore
    }

    It 'Pipes bytes exactly from one native command to another' {
        testexe -writebytes 'deadbeef' | textexe -writetofile $tmpFile
        Get-Content -LiteralPath $tmpFile -AsByteStream | Should -BeExactly ([byte[]](0xde, 0xad, 0xbe, 0xef))
    }

    It 'Pipes redirected stderr as exact bytes' {
        testexe -errbytes 'deadbeef' 2>&1 | testexe -writetofile $tmpFile
        Get-Content -LiteralPath $tmpFile -AsByteStream | Should -BeExactly ([byte[]](0xde, 0xad, 0xbe, 0xef))
    }

    It 'Pipes combined stdout and stderr as exact bytes' {
        testexe -writebytes 'deadbeef' -errbytes 'cafebabe' 2>&1 | testexe -writetofile $tmpFile
        Get-Content -LiteralPath $tmpFile -AsByteStream | Should -BeExactly ([byte[]](0xde, 0xad, 0xbe, 0xef, 0xca, 0xfe, 0xba, 0xbe))
    }

    It 'Redirects bytes from stdout exactly' {
        testexe -writebytes 'deadbeef' > $tmpFile
        Get-Content -LiteralPath $tmpFile -AsByteStream | Should -BeExactly ([byte[]](0xde, 0xad, 0xbe, 0xef))
    }

    It 'Redirects bytes from stderr exactly' {
        testexe -errbytes 'deadbeef' 2>$tmpFile
        Get-Content -LiteralPath $tmpFile -AsByteStream | Should -BeExactly ([byte[]](0xde, 0xad, 0xbe, 0xef))
    }

    It 'Combines native stdout pipe with normal error output' {
        $errs = . { testexe -writebytes 'deadbeef' -errbytes '68656c6c6f' | testexe -writetofile $tmpFile } 2>&1
        Get-Content -LiteralPath $tmpFile -AsByteStream | Should -BeExactly ([byte[]](0xde, 0xad, 0xbe, 0xef))
        $errs | Should -BeExactly 'hello'
    }

    It 'Pipes bytes as bytes to a cmdlet' {
        $output = testexe -writebytes 'deadbeef' | Write-Output
        $output | Should -BeExactly ([byte[]](0xde, 0xad, 0xbe, 0xef))
    }
}
