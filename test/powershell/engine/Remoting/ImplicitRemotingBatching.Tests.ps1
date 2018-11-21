# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "TestImplicitRemotingBatching hook should correctly batch simple remote command pipelines" -Tag 'Feature','RequireAdminOnWindows' {

    BeforeAll {

        if (! $isWindows) { return }

        function ThrowSetupError
        {
            param (
                [string] $errorMessage,
                [System.Management.Automation.ErrorRecord[]] $eRecords
            )

            $msg = @()
            foreach ($err in $powerShell.Streams.Error)
            {
                $msg += $err.ToString() + "`n"
            }

            throw "$errorMessage : '$msg'"
        }

        # Make sure we can create a remote session
        $remotePSSession = New-RemoteSession
        if ($remotePSSession -eq $null)
        {
            Write-Verbose "Unable to create a remote session in test."
        }
        else
        {
            Remove-PSSession $remotePSSession
        }

        [powershell] $powerShell = [powershell]::Create([System.Management.Automation.RunspaceMode]::NewRunspace)

        # Create remote session in new PowerShell session
        $powerShell.AddScript('Import-Module -Name HelpersRemoting; $remoteSession = New-RemoteSession').Invoke()
        if ($powerShell.Streams.Error.Count -gt 0)
        {
            ThrowSetupError -errorMessage "Unable to create remote session for test with error" -eRecords $powerShell.Streams.Error
        }

        # Import implicit commands from remote session
        $powerShell.Commands.Clear()
        $powerShell.AddScript('Import-PSSession -Session $remoteSession -CommandName Get-Process,Write-Output -AllowClobber').Invoke()
        if ($powerShell.Streams.Error.Count -gt 0)
        {
            ThrowSetupError -errorMessage "Unable to import pssession for test" -eRecords $powerShell.Streams.Error
        }

        # Define $filter variable in local session
        $powerShell.Commands.Clear()
        $powerShell.AddScript('$filter = "pwsh","powershell"').Invoke()
        $localRunspace = $powerShell.Runspace

        [powershell] $psInvoke = [powershell]::Create([System.Management.Automation.RunspaceMode]::NewRunspace)

        $testCases = @(
            @{
                Name = 'Two implicit commands should be successfully batched'
                CommandLine = 'Get-Process -Name "pwsh" | Write-Output'
                ExpectedOutput = $true
            },
            @{
                Name = 'Two implicit commands with Where-Object should be successfully batched'
                CommandLine = 'Get-Process | Write-Output | Where-Object { $_.Name -like "*pwsh*" }'
                ExpectedOutput = $true
            },
            @{
                Name = 'Two implicit commands with Where-Object alias (?) should be successfully batched'
                CommandLine = 'Get-Process | Write-Output | ? { $_.Name -like "*pwsh*" }'
                ExpectedOutput = $true
            },
            @{
                Name = 'Two implicit commands with Where-Object alias (where) should be successfully batched'
                CommandLine = 'Get-Process | Write-Output | where { $_.Name -like "*pwsh*" }'
                ExpectedOutput = $true
            },
            @{
                Name = 'Two implicit commands with Sort-Object should be successfully batched'
                CommandLine = 'Get-Process -Name "pwsh" | Sort-Object -Property Name | Write-Output'
                ExpectedOutput = $true
            },
            @{
                Name = 'Two implicit commands with Sort-Object alias (sort) should be successfully batched'
                CommandLine = 'Get-Process -Name "pwsh" | sort -Property Name | Write-Output'
                ExpectedOutput = $true
            },
            @{
                Name = 'Two implicit commands with ForEach-Object should be successfully batched'
                CommandLine = 'Get-Process -Name "pwsh" | Write-Output | ForEach-Object { $_ }'
                ExpectedOutput = $true
            },
            @{
                Name = 'Two implicit commands with ForEach-Object alias (%) should be successfully batched'
                CommandLine = 'Get-Process -Name "pwsh" | Write-Output | % { $_ }'
                ExpectedOutput = $true
            },
            @{
                Name = 'Two implicit commands with ForEach-Object alias (foreach) should be successfully batched'
                CommandLine = 'Get-Process -Name "pwsh" | Write-Output | foreach { $_ }'
                ExpectedOutput = $true
            },
            @{
                Name = 'Two implicit commands with Measure-Command should be successfully batched'
                CommandLine = 'Measure-Command { Get-Process | Write-Output }'
                ExpectedOutput = $true
            },
            @{
                Name = 'Two implicit commands with Measure-Object should be successfully batched'
                CommandLine = 'Get-Process | Write-Output | Measure-Object'
                ExpectedOutput = $true
            },
            @{
                Name = 'Two implicit commands with Measure-Object alias (measure) should be successfully batched'
                CommandLine = 'Get-Process | Write-Output | measure'
                ExpectedOutput = $true
            },
            @{
                Name = 'Implicit commands with variable arguments should be successfully batched'
                CommandLine = 'Get-Process -Name $filter | Write-Output'
                ExpectedOutput = $true
            },
            @{
                Name = 'Pipeline with non-implicit command should not be batched'
                CommandLine = 'Get-Process | Write-Output | Select-Object -Property Name'
                ExpectedOutput = $false
            },
            @{
                Name = 'Non-simple pipeline should not be batched'
                CommandLine = '1..2 | % { Get-Process pwsh | Write-Output }'
                ExpectedOutput = $false
            }
            @{
                Name = 'Pipeline with single command should not be batched'
                CommandLine = 'Get-Process pwsh'
                ExpectedOutput = $false
            },
            @{
                Name = 'Pipeline without any implicit commands should not be batched'
                CommandLine = 'Get-PSSession | Out-Default'
                ExpectedOutput = $false
            }
        )
    }

    AfterAll {

        if (! $isWindows) { return }

        if ($remoteSession -ne $null) { Remove-PSSession $remoteSession -ErrorAction Ignore }
        if ($powershell -ne $null) { $powershell.Dispose() }
        if ($psInvoke -ne $null) { $psInvoke.Dispose() }
    }

    It "<Name>" -TestCases $testCases -Skip:(! $IsWindows) {
        param ($CommandLine, $ExpectedOutput)

        $psInvoke.Commands.Clear()
        $psInvoke.Commands.AddScript('param ($cmdLine, $runspace) [System.Management.Automation.Internal.InternalTestHooks]::TestImplicitRemotingBatching($cmdLine, $runspace)').AddArgument($CommandLine).AddArgument($localRunspace)

        $result = $psInvoke.Invoke()
        $result | Should Be $ExpectedOutput
    }
}
