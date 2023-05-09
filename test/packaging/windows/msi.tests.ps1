# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe -Name "Windows MSI" -Fixture {
    BeforeAll {
        Set-StrictMode -Off
        function Test-Elevated {
            [CmdletBinding()]
            [OutputType([bool])]
            Param()

            # if the current Powershell session was called with administrator privileges,
            # the Administrator Group's well-known SID will show up in the Groups for the current identity.
            # Note that the SID won't show up unless the process is elevated.
            return (([Security.Principal.WindowsIdentity]::GetCurrent()).Groups -contains "S-1-5-32-544")
        }

        function Test-IsMuEnabled {
            $sm = (New-Object -ComObject Microsoft.Update.ServiceManager)
            $mu = $sm.Services | Where-Object { $_.ServiceId -eq '7971f918-a847-4430-9279-4a52d1efe18d' }
            if ($mu) {
                return $true
            }
            return $false
        }

        function Invoke-TestAndUploadLogOnFailure {
            param (
                [scriptblock] $Test
            )

            try {
                & $Test
            }
            catch {
                Send-VstsLogFile -Path $msiLog
                throw
            }
        }

        function Get-UseMU {
            $useMu = $null
            $key = 'HKLM:\SOFTWARE\Microsoft\PowerShellCore\'
            if ($runtime -like '*x86*') {
                $key = 'HKLM:\SOFTWARE\Wow6432Node\Microsoft\PowerShellCore\'
            }

            try {
                $useMu = Get-ItemPropertyValue -Path $key -Name UseMU -ErrorAction SilentlyContinue
            } catch {}

            if (!$useMu) {
                $useMu = 0
            }

            return $useMu
        }

        function Set-UseMU {
            param(
                [int]
                $Value
            )
            $key = 'HKLM:\SOFTWARE\Microsoft\PowerShellCore\'
            if ($runtime -like '*x86*') {
                $key = 'HKLM:\SOFTWARE\Wow6432Node\Microsoft\PowerShellCore\'
            }

            Set-ItemProperty -Path $key -Name UseMU -Value $Value -Type DWord

            return $useMu
        }

        function Invoke-Msiexec {
            param(
                [Parameter(ParameterSetName = 'Install', Mandatory)]
                [Switch]$Install,

                [Parameter(ParameterSetName = 'Uninstall', Mandatory)]
                [Switch]$Uninstall,

                [Parameter(Mandatory)]
                [ValidateScript({Test-Path -Path $_})]
                [String]$MsiPath,

                [Parameter(ParameterSetName = 'Install')]
                [HashTable] $Properties

            )
            $action = "$($PSCmdlet.ParameterSetName)ing"
            if ($Install.IsPresent) {
                $switch = '/I'
            } else {
                $switch = '/x'
            }

            $additionalOptions = @()
            if ($Properties) {
                foreach ($key in $Properties.Keys) {
                    $additionalOptions += "$key=$($Properties.$key)"
                }
            }

            $argumentList = "$switch $MsiPath /quiet /l*vx $msiLog $additionalOptions"
            Write-Verbose -Message "running msiexec $argumentList"
            $msiExecProcess = Start-Process msiexec.exe -Wait -ArgumentList $argumentList -NoNewWindow -PassThru
            if ($msiExecProcess.ExitCode -ne 0) {
                $exitCode = $msiExecProcess.ExitCode
                throw "$action MSI failed and returned error code $exitCode."
            }
        }

        $msiX64Path = $env:PsMsiX64Path
        $channel = $env:PSMsiChannel
        $runtime = $env:PSMsiRuntime
        $muEnabled = Test-IsMuEnabled

        # Get any existing powershell in the path
        $beforePath = @(([System.Environment]::GetEnvironmentVariable('PATH', 'MACHINE')) -split ';' |
                Where-Object {$_ -like '*files\powershell*'})

        $msiLog = Join-Path -Path $TestDrive -ChildPath 'msilog.txt'

        foreach ($pathPart in $beforePath) {
            Write-Warning "Found existing PowerShell path: $pathPart"
        }

        if (!(Test-Elevated)) {
            Write-Warning "Tests must be elevated"
        }
        $uploadedLog = $false
    }

    AfterAll {
        Set-StrictMode -Version 3.0
    }

    BeforeEach {
        $error.Clear()
    }

    Context "Upgrade code" {
        BeforeAll {
            Write-Verbose "cr-$channel-$runtime" -Verbose
            $pwshPath = Join-Path $env:ProgramFiles -ChildPath "PowerShell"
            $pwshx86Path = Join-Path ${env:ProgramFiles(x86)} -ChildPath "PowerShell"
            $regKeyPath = "HKLM:\SOFTWARE\Microsoft\PowerShellCore\InstalledVersions"

            switch ("$channel-$runtime") {
                "preview-win7-x64" {
                    $versionPath = Join-Path -Path $pwshPath -ChildPath '7-preview'
                    $revisionRange = 0, 99
                    $msiUpgradeCode = '39243d76-adaf-42b1-94fb-16ecf83237c8'
                    $regKeyPath = Join-Path $regKeyPath -ChildPath $msiUpgradeCode
                }
                "stable-win7-x64" {
                    $versionPath = Join-Path -Path $pwshPath -ChildPath '7'
                    $revisionRange = 500, 500
                    $msiUpgradeCode = '31ab5147-9a97-4452-8443-d9709f0516e1'
                    $regKeyPath = Join-Path $regKeyPath -ChildPath $msiUpgradeCode
                }
                "preview-win7-x86" {
                    $versionPath = Join-Path -Path $pwshx86Path -ChildPath '7-preview'
                    $revisionRange = 0, 99
                    $msiUpgradeCode = '86abcfbd-1ccc-4a88-b8b2-0facfde29094'
                    $regKeyPath = Join-Path $regKeyPath -ChildPath $msiUpgradeCode
                }
                "stable-win7-x86" {
                    $versionPath = Join-Path -Path $pwshx86Path -ChildPath '7'
                    $revisionRange = 500, 500
                    $msiUpgradeCode = '1d00683b-0f84-4db8-a64f-2f98ad42fe06'
                    $regKeyPath = Join-Path $regKeyPath -ChildPath $msiUpgradeCode
                }
                default {
                    throw "'$_' not a valid channel runtime combination"
                }
            }
        }

        It "$Channel MSI should not be installed before test" -Skip:(!(Test-Elevated)) {
            $result = @(Get-CimInstance -Query "SELECT Value FROM Win32_Property WHERE Property='UpgradeCode' and Value = '{$msiUpgradeCode}'")
            $result.Count | Should -Be 0 -Because "Query should return nothing if $channel $runtime is not installed"
        }

        It "MSI should install without error" -Skip:(!(Test-Elevated)) {
            {
                Invoke-MsiExec -Install -MsiPath $msiX64Path -Properties @{ADD_PATH = 1}
            } | Should -Not -Throw
        }

        It "Upgrade code should be correct" -Skip:(!(Test-Elevated)) {
            $result = @(Get-CimInstance -Query "SELECT Value FROM Win32_Property WHERE Property='UpgradeCode' and Value = '{$msiUpgradeCode}'")
            $result.Count | Should -Be 1 -Because "Query should return 1 result if Upgrade code is for $runtime $channel"
        }

        It "Revision should be in correct range" -Skip:(!(Test-Elevated)) {
            $pwshDllPath = Join-Path -Path $versionPath -ChildPath "pwsh.dll"
            [version] $version = (Get-ChildItem $pwshDllPath).VersionInfo.FileVersion
            Write-Verbose "pwsh.dll version: $version" -Verbose
            $version.Revision | Should -BeGreaterOrEqual $revisionRange[0] -Because "$channel revision should between $($revisionRange[0]) and $($revisionRange[1])"
            $version.Revision | Should -BeLessOrEqual $revisionRange[1] -Because "$channel revision should between $($revisionRange[0]) and $($revisionRange[1])"
        }

        It 'MSI should add ProductCode in registry' -Skip:(!(Test-Elevated)) {

            $productCode = if ($msiUpgradeCode -eq '39243d76-adaf-42b1-94fb-16ecf83237c8' -or
                $msi -eq '31ab5147-9a97-4452-8443-d9709f0516e1') {
                # x64
                $regKeyPath | Should -Exist
                $productCode = Get-ItemPropertyValue -Path $regKeyPath -Name 'ProductCode'
            } elseif ($msiUpgradeCode -eq '39243d76-adaf-42b1-94fb-16ecf83237c8' -or
                $msi -eq '31ab5147-9a97-4452-8443-d9709f0516e1') {
                # x86 - need to open the 32bit reghive
                $wow32RegKey = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, [Microsoft.Win32.RegistryView]::Registry32)
                $subKey = $wow32RegKey.OpenSubKey("Software\Microsoft\PowerShellCore\InstalledVersions\$msiUpgradeCode")
                $subKey.GetValue("ProductCode")
            }

            $productCode | Should -Not -BeNullOrEmpty
            $productCodeGuid = [Guid]$productCode
            $productCodeGuid | Should -BeOfType "Guid"
            $productCodeGuid.Guid | Should -Not -Be $msiUpgradeCode
        }

        It "MSI should uninstall without error" -Skip:(!(Test-Elevated)) {
            {
                Invoke-MsiExec -Uninstall -MsiPath $msiX64Path
            } | Should -Not -Throw
        }
    }

    Context "Add Path disabled" {
        BeforeAll {
            Set-UseMU -Value 0
        }

        It "UseMU should be 0 before install" -Skip:(!(Test-Elevated)) {
            $useMu = Get-UseMU
            $useMu | Should -Be 0
        }

        It "MSI should install without error" -Skip:(!(Test-Elevated)) {
            {
                Invoke-MsiExec -Install -MsiPath $msiX64Path -Properties @{ADD_PATH = 0; USE_MU = 1; ENABLE_MU = 1}
            } | Should -Not -Throw
        }

        It "MSI should have not be updated path" -Skip:(!(Test-Elevated)) {
            $psPath = ([System.Environment]::GetEnvironmentVariable('PATH', 'MACHINE')) -split ';' |
                Where-Object { $_ -like '*files\powershell*' -and $_ -notin $beforePath }

            $psPath | Should -BeNullOrEmpty
        }

        It "UseMU should be 1" -Skip:(!(Test-Elevated)) {
            Invoke-TestAndUploadLogOnFailure -Test {
                $useMu = Get-UseMU
                $useMu | Should -Be 1
            }
        }

        It "MSI should uninstall without error" -Skip:(!(Test-Elevated)) {
            {
                Invoke-MsiExec -Uninstall -MsiPath $msiX64Path
            } | Should -Not -Throw
        }
    }

    Context "USE_MU disabled" {
        BeforeAll {
            Set-UseMU -Value 0
        }

        It "UseMU should be 0 before install" -Skip:(!(Test-Elevated)) {
            $useMu = Get-UseMU
            $useMu | Should -Be 0
        }

        It "MSI should install without error" -Skip:(!(Test-Elevated)) {
            {
                Invoke-MsiExec -Install -MsiPath $msiX64Path -Properties @{USE_MU = 0}
            } | Should -Not -Throw
        }

        It "UseMU should be 0" -Skip:(!(Test-Elevated)) {
            Invoke-TestAndUploadLogOnFailure -Test {
                $useMu = Get-UseMU
                $useMu | Should -Be 0
            }
        }

        It "MSI should uninstall without error" -Skip:(!(Test-Elevated)) {
            {
                Invoke-MsiExec -Uninstall -MsiPath $msiX64Path
            } | Should -Not -Throw
        }
    }

    Context "Add Path enabled" {
        It "MSI should install without error" -Skip:(!(Test-Elevated)) {
            {
                Invoke-MsiExec -Install -MsiPath $msiX64Path -Properties @{ADD_PATH = 1}
            } | Should -Not -Throw
        }

        It "MSI should have updated path" -Skip:(!(Test-Elevated)) {
            if ($channel -eq 'preview') {
                $pattern = '*files*\powershell*\preview*'
            } else {
                $pattern = '*files*\powershell*'
            }

            $psPath = ([System.Environment]::GetEnvironmentVariable('PATH', 'MACHINE')) -split ';' |
            Where-Object { $_ -like $pattern -and $_ -notin $beforePath }

            if (!$psPath) {
                ([System.Environment]::GetEnvironmentVariable('PATH', 'MACHINE')) -split ';' |
                Where-Object { $_ -notin $beforePath } |
                ForEach-Object { Write-Verbose -Verbose $_ }
            }

            $psPath | Should -Not -BeNullOrEmpty
        }

        It "MSI should uninstall without error" -Skip:(!(Test-Elevated)) {
            {
                Invoke-MsiExec -Uninstall -MsiPath $msiX64Path
            } | Should -Not -Throw
        }

        Context "Disable Telemetry" {
            It "MSI should set POWERSHELL_TELEMETRY_OPTOUT env variable when MSI property DISABLE_TELEMETRY is set to 1" -Skip:(!(Test-Elevated)) {
                try {
                    $originalValue = [System.Environment]::GetEnvironmentVariable('POWERSHELL_TELEMETRY_OPTOUT', [System.EnvironmentVariableTarget]::Machine)
                    [System.Environment]::SetEnvironmentVariable('POWERSHELL_TELEMETRY_OPTOUT', '0', [System.EnvironmentVariableTarget]::Machine)
                    {
                        Invoke-MsiExec -Install -MsiPath $msiX64Path -Properties @{DISABLE_TELEMETRY = 1 }
                    } | Should -Not -Throw
                    [System.Environment]::GetEnvironmentVariable('POWERSHELL_TELEMETRY_OPTOUT', [System.EnvironmentVariableTarget]::Machine) |
                        Should -Be 1
                }
                finally {
                    [System.Environment]::SetEnvironmentVariable('POWERSHELL_TELEMETRY_OPTOUT', $originalValue, [System.EnvironmentVariableTarget]::Machine)
                    {
                        Invoke-MsiExec -Uninstall -MsiPath $msiX64Path
                    } | Should -Not -Throw
                }
            }

            It "MSI should not change POWERSHELL_TELEMETRY_OPTOUT env variable when MSI property DISABLE_TELEMETRY not set" -Skip:(!(Test-Elevated)) {
                try {
                    $originalValue = [System.Environment]::GetEnvironmentVariable('POWERSHELL_TELEMETRY_OPTOUT', [System.EnvironmentVariableTarget]::Machine)
                    [System.Environment]::SetEnvironmentVariable('POWERSHELL_TELEMETRY_OPTOUT', 'untouched', [System.EnvironmentVariableTarget]::Machine)
                    {
                        Invoke-MsiExec -Install -MsiPath $msiX64Path
                    } | Should -Not -Throw
                    [System.Environment]::GetEnvironmentVariable('POWERSHELL_TELEMETRY_OPTOUT', [System.EnvironmentVariableTarget]::Machine) |
                        Should -Be 'untouched'
                }
                finally {
                    [System.Environment]::SetEnvironmentVariable('POWERSHELL_TELEMETRY_OPTOUT', $originalValue, [System.EnvironmentVariableTarget]::Machine)
                    {
                        Invoke-MsiExec -Uninstall -MsiPath $msiX64Path
                    } | Should -Not -Throw
                }
            }
        }
    }
}
