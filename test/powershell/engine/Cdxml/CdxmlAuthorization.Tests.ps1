# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Local CDXML authorization" -Tags "CI" {
    BeforeAll {
        . "$PSScriptRoot\..\..\..\..\tools\buildCommon\startNativeExecution.ps1"
    }

    It "Honors <Policy> for zone <Zone> (<Extension>)" -Skip:(-not $IsWindows) -TestCases @(
        @{ Policy = 'Unrestricted'; Zone = 0; Extension = '.cdxml'; ExitCode = 0 }
        @{ Policy = 'Unrestricted'; Zone = 2; Extension = '.CDXML'; ExitCode = 0 }
        @{ Policy = 'Unrestricted'; Zone = 3; Extension = '.cdxml'; ExitCode = 1 }
        @{ Policy = 'Unrestricted'; Zone = 4; Extension = '.cdxml'; ExitCode = 1 }
        @{ Policy = 'RemoteSigned'; Zone = 0; Extension = '.cdxml'; ExitCode = 0 }
        @{ Policy = 'RemoteSigned'; Zone = 3; Extension = '.cdxml'; ExitCode = 1 }
        @{ Policy = 'AllSigned'; Zone = 0; Extension = '.cdxml'; ExitCode = 1 }
        @{ Policy = 'Restricted'; Zone = 0; Extension = '.cdxml'; ExitCode = 1 }
    ) {
        param($Policy, $Zone, $Extension, $ExitCode)

        # Exercise module authorization without creating a WMI class or invoking its generated commands.
        $path = Join-Path $TestDrive "authorization$Extension"
        Copy-Item -LiteralPath "$PSScriptRoot\assets\CimTest\CimTest.cdxml" -Destination $path
        Set-Content -LiteralPath $path -Stream Zone.Identifier -Value "[ZoneTransfer]`r`nZoneId=$Zone"
        $command = @"
try {
    Import-Module '$($path.Replace("'", "''"))' -ErrorAction Stop
    (Get-Command -Module authorization | Sort-Object Name).Name -join ','
} catch {
    `$_.FullyQualifiedErrorId
    exit 1
}
"@
        $encodedCommand = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($command))
        $commandArguments = '-NoProfile', '-NonInteractive', '-ExecutionPolicy', $Policy, '-EncodedCommand', $encodedCommand
        $result = Start-NativeExecution -IgnoreExitcode {
            & "$PSHOME\pwsh.exe" @commandArguments
        }
        $LASTEXITCODE | Should -Be $ExitCode
        if ($ExitCode -eq 0) {
            $result | Should -BeExactly 'Get-CimTest,New-CimTest,Remove-CimTest,Set-CimTest'
        } else {
            $result | Should -BeExactly 'UnauthorizedAccess'
        }
    }
}
