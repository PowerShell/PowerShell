# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module HelpersRemoting
Import-Module HelpersCommon

Describe "Remote runspace pool should expose commands in endpoint configuration" -Tags 'Feature','RequireAdminOnWindows' {

    BeforeAll {

        $pendingTest = (Test-IsWinWow64)
        $skipTest = !$IsWindows -or !(Test-CanWriteToPsHome)

        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()

        if ($pendingTest) {
            $PSDefaultParameterValues["it:pending"] = $true
            return
        }

        if ($skipTest) {
            $PSDefaultParameterValues["it:skip"] = $true
            return
        }

        $configName = "restrictedV"
        $configPath = Join-Path $TestDrive ($configName + ".pssc")

        New-PSSessionConfigurationFile -Path $configPath -SessionType RestrictedRemoteServer -VisibleCmdlets 'Get-CimInstance'

        $null = Register-PSSessionConfiguration -Name $configName -Path $configPath -Force -ErrorAction SilentlyContinue

        $remoteRunspacePool = New-RemoteRunspacePool -ConfigurationName $configName
    }

    AfterAll {

        if ($pendingTest -or $skipTest) {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
            return
        }

        if ($remoteRunspacePool -ne $null)
        {
            $remoteRunspacePool.Dispose()
        }

        Unregister-PSSessionConfiguration -Name $configName -Force -ErrorAction SilentlyContinue
    }

    It "Verifies that the configured endpoint cmdlet is available in all runspace pool instances" -Skip:(!$IsWindows -or !(Test-CanWriteToPsHome)) {

        [powershell] $ps1 = [powershell]::Create()
        $ps1.RunspacePool = $remoteRunspacePool
        $null = $ps1.AddCommand('Get-Command').AddParameter('Name','Get-CimInstance')

        [powershell] $ps2 = [powershell]::Create()
        $ps2.RunspacePool = $remoteRunspacePool
        $null = $ps2.AddCommand('Get-Command').AddParameter('Name','Get-CimInstance')

        [powershell] $ps3 = [powershell]::Create()
        $ps3.RunspacePool = $remoteRunspacePool
        $null = $ps3.AddCommand('Get-Command').AddParameter('Name','Get-CimInstance')

        [powershell] $ps4 = [powershell]::Create()
        $ps4.RunspacePool = $remoteRunspacePool
        $null = $ps4.AddCommand('Get-Command').AddParameter('Name','Get-CimInstance')

        # Invoke all four simultaneously
        $a1 = $ps1.BeginInvoke()
        $a2 = $ps2.BeginInvoke()
        $a3 = $ps3.BeginInvoke()
        $a4 = $ps4.BeginInvoke()

        # Wait for completion
        $r1 = $ps1.EndInvoke($a1)
        $r2 = $ps2.EndInvoke($a2)
        $r3 = $ps3.EndInvoke($a3)
        $r4 = $ps4.EndInvoke($a4)

        $r1.Name | Should -BeExactly 'Get-CimInstance'
        $r2.Name | Should -BeExactly 'Get-CimInstance'
        $r3.Name | Should -BeExactly 'Get-CimInstance'
        $r4.Name | Should -BeExactly 'Get-CimInstance'
    }
}
