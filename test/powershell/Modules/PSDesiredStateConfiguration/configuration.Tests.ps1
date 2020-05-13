# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "DSC MOF Compilation" -tags "CI" {
    BeforeAll {
        $module = Get-Module PowerShellGet -ListAvailable | Sort-Object -Property Version -Descending | Select-Object -First 1

        $psGetModuleVersion = $module.Version.ToString()
        if (!$env:DSC_HOME)
        {
            Import-Module PSDesiredStateConfiguration
        }
    }

    It "Should be able to compile a MOF using PSModule resource"  {
        if ($IsLinux) {
            Set-ItResult -Pending -Because "https://github.com/PowerShell/PowerShellGet/pull/529"
        }

        Write-Verbose "DSC_HOME: ${env:DSC_HOME}" -Verbose
        [Scriptblock]::Create(@"
        configuration DSCTestConfig
        {
            Import-DscResource -ModuleName PowerShellGet -ModuleVersion $psGetModuleVersion
            Node "localhost" {
                PSModule f1
                {
                    Name = 'PsDscResources'
                    InstallationPolicy = 'Trusted'
                }
            }
        }

        DSCTestConfig -OutputPath TestDrive:\DscTestConfig2
"@) | Should -Not -Throw

        "TestDrive:\DscTestConfig2\localhost.mof" | Should -Exist
    }
}
