# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Function Install-ModuleIfMissing {
    param(
        [parameter(Mandatory)]
        [String]
        $Name,
        [version]
        $MinimumVersion,
        [switch]
        $SkipPublisherCheck,
        [switch]
        $Force
    )

    $module = Get-Module -Name $Name -ListAvailable -ErrorAction Ignore | Sort-Object -Property Version -Descending | Select-Object -First 1

    if (!$module -or $module.Version -lt $MinimumVersion) {
        Write-Verbose "Installing module '$Name' ..." -Verbose
        Install-Module -Name $Name -Force -SkipPublisherCheck:$SkipPublisherCheck.IsPresent
    }
}

Function Test-IsInvokeDscResourceEnable {
    return [ExperimentalFeature]::IsEnabled("PSDesiredStateConfiguration.InvokeDscResource")
}

Describe "Test PSDesiredStateConfiguration" -tags CI {
    BeforeAll {
        $MissingLibmi = $false
        $platformInfo = Get-PlatformInfo
        if (
            ($platformInfo.Platform -match "alpine|raspbian") -or
            ($platformInfo.Platform -eq "debian" -and ($platformInfo.Version -eq '10' -or $platformInfo.Version -eq '')) -or # debian 11 has empty Version ID
            ($platformInfo.Platform -eq 'centos' -and $platformInfo.Version -eq '8')
        ) {
            $MissingLibmi = $true
        }
    }

    Context "Module loading" {
        BeforeAll {
            Function BeCommand {
                [CmdletBinding()]
                Param(
                    [object[]] $ActualValue,
                    [string] $CommandName,
                    [string] $ModuleName,
                    [switch]$Negate
                )

                $failure = if ($Negate) {
                    "Expected: Command $CommandName should not exist in module $ModuleName"
                }
                else {
                    "Expected: Command $CommandName should exist in module $ModuleName"
                }

                $succeeded = if ($Negate) {
                    ($ActualValue | Where-Object { $_.Name -eq $CommandName }).count -eq 0
                }
                else {
                    ($ActualValue | Where-Object { $_.Name -eq $CommandName }).count -gt 0
                }

                return [PSCustomObject]@{
                    Succeeded = $succeeded
                    FailureMessage = $failure
                }
            }

            Add-AssertionOperator -Name 'HaveCommand' -Test $Function:BeCommand -SupportsArrayInput

            $commands = Get-Command -Module PSDesiredStateConfiguration
        }

        It "The module should have the Configuration Command" {
            $commands | Should -HaveCommand -CommandName 'Configuration' -ModuleName PSDesiredStateConfiguration
        }

        It "The module should have the Configuration Command" {
            $commands | Should -HaveCommand -CommandName 'New-DscChecksum' -ModuleName PSDesiredStateConfiguration
        }

        It "The module should have the Get-DscResource Command" {
            $commands | Should -HaveCommand -CommandName 'Get-DscResource' -ModuleName PSDesiredStateConfiguration
        }

        It "The module should have the Invoke-DscResource Command" -Skip:(!(Test-IsInvokeDscResourceEnable)) {
            $commands | Should -HaveCommand -CommandName 'Invoke-DscResource' -ModuleName PSDesiredStateConfiguration
        }
    }
    Context "Get-DscResource - Composite Resources" {
        BeforeAll {
            $origProgress = $global:ProgressPreference
            $global:ProgressPreference = 'SilentlyContinue'
            Install-ModuleIfMissing -Name PSDscResources
            $testCases = @(
                @{
                    TestCaseName = 'case mismatch in resource name'
                    Name         = 'groupset'
                    ModuleName   = 'PSDscResources'
                }
                @{
                    TestCaseName = 'Both names have matching case'
                    Name         = 'GroupSet'
                    ModuleName   = 'PSDscResources'
                }
                @{
                    TestCaseName = 'case mismatch in module name'
                    Name         = 'GroupSet'
                    ModuleName   = 'psdscResources'
                }
            )
        }

        AfterAll {
            $Global:ProgressPreference = $origProgress
        }

        It "should be able to get <Name> - <TestCaseName>" -TestCases $testCases {
            param($Name)

            if ($IsWindows) {
                Set-ItResult -Pending -Because "Will only find script from PSDesiredStateConfiguration without modulename"
            }

            if ($IsLinux) {
                Set-ItResult -Pending -Because "https://github.com/PowerShell/PSDesiredStateConfiguration/issues/26"
            }

            $resource = Get-DscResource -Name $name
            $resource | Should -Not -BeNullOrEmpty
            $resource.Name | Should -Be $Name
            if (Test-IsInvokeDscResourceEnable) {
                $resource.ImplementationDetail | Should -BeNullOrEmpty
            }
            else {
                $resource.ImplementationDetail | Should -BeNullOrEmpty
            }

        }

        It "should be able to get <Name> from <ModuleName> - <TestCaseName>" -TestCases $testCases {
            param($Name, $ModuleName, $PendingBecause)

            if ($IsLinux) {
                Set-ItResult -Pending -Because "https://github.com/PowerShell/PSDesiredStateConfiguration/issues/26"
            }

            if ($PendingBecause) {
                Set-ItResult -Pending -Because $PendingBecause
            }

            $resource = Get-DscResource -Name $Name -Module $ModuleName
            $resource | Should -Not -BeNullOrEmpty
            $resource.Name | Should -Be $Name
            if (Test-IsInvokeDscResourceEnable) {
                $resource.ImplementationDetail | Should -BeNullOrEmpty
            }
            else {
                $resource.ImplementationDetail | Should -BeNullOrEmpty
            }
        }
    }

    Context "Get-DscResource - ScriptResources" {
        BeforeAll {
            $origProgress = $global:ProgressPreference
            $global:ProgressPreference = 'SilentlyContinue'

            Install-ModuleIfMissing -Name PSDscResources -Force

            # Install PowerShellGet only if PowerShellGet 2.2.1 or newer does not exist
            Install-ModuleIfMissing -Name PowerShellGet -MinimumVersion '2.2.1'
            $module = Get-Module PowerShellGet -ListAvailable | Sort-Object -Property Version -Descending | Select-Object -First 1

            $psGetModuleSpecification = @{ModuleName = $module.Name; ModuleVersion = $module.Version.ToString() }
            $psGetModuleCount = @(Get-Module PowerShellGet -ListAvailable).Count
            $testCases = @(
                @{
                    TestCaseName = 'case mismatch in resource name'
                    Name         = 'script'
                    ModuleName   = 'PSDscResources'
                }
                @{
                    TestCaseName = 'Both names have matching case'
                    Name         = 'Script'
                    ModuleName   = 'PSDscResources'
                }
                @{
                    TestCaseName = 'case mismatch in module name'
                    Name         = 'Script'
                    ModuleName   = 'psdscResources'
                }
                <#
                Add these back when PowerShellGet is fixed https://github.com/PowerShell/PowerShellGet/pull/529
                @{
                    TestCaseName = 'case mismatch in resource name'
                    Name = 'PsModule'
                    ModuleName = 'PowerShellGet'
                }
                @{
                    TestCaseName = 'Both names have matching case'
                    Name = 'PSModule'
                    ModuleName = 'PowerShellGet'
                }
                @{
                    TestCaseName = 'case mismatch in module name'
                    Name = 'PSModule'
                    ModuleName = 'powershellget'
                }
                #>
            )
        }

        AfterAll {
            $Global:ProgressPreference = $origProgress
        }

        It "should be able to get <Name> - <TestCaseName>" -TestCases $testCases {
            param($Name)

            if ($IsWindows) {
                Set-ItResult -Pending -Because "Will only find script from PSDesiredStateConfiguration without modulename"
            }

            if ($MissingLibmi) {
                Set-ItResult -Pending -Because "Libmi not available for this platform"
            }

            if ($PendingBecause) {
                Set-ItResult -Pending -Because $PendingBecause
            }

            $resources = @(Get-DscResource -Name $name)
            $resources | Should -Not -BeNullOrEmpty
            foreach ($resource in $resource) {
                $resource.Name | Should -Be $Name
                if (Test-IsInvokeDscResourceEnable) {
                    $resource.ImplementationDetail | Should -Be 'ScriptBased'
                }
                else {
                    $resource.ImplementationDetail | Should -BeNullOrEmpty
                }

            }
        }

        It "should be able to get <Name> from <ModuleName> - <TestCaseName>" -TestCases $testCases {
            param($Name, $ModuleName, $PendingBecause)

            if ($IsLinux) {
                Set-ItResult -Pending -Because "https://github.com/PowerShell/PSDesiredStateConfiguration/issues/12 and https://github.com/PowerShell/PowerShellGet/pull/529"
            }

            if ($PendingBecause) {
                Set-ItResult -Pending -Because $PendingBecause
            }

            $resources = @(Get-DscResource -Name $name -Module $ModuleName)
            $resources | Should -Not -BeNullOrEmpty
            foreach ($resource in $resource) {
                $resource.Name | Should -Be $Name
                if (Test-IsInvokeDscResourceEnable) {
                    $resource.ImplementationDetail | Should -Be 'ScriptBased'
                }
                else {
                    $resource.ImplementationDetail | Should -BeNullOrEmpty
                }
            }
        }

        It "should throw when resource is not found" {
            Set-ItResult -Pending -Because "https://github.com/PowerShell/PSDesiredStateConfiguration/issues/17"
            {
                Get-DscResource -Name antoehusatnoheusntahoesnuthao -Module tanshoeusnthaosnetuhasntoheusnathoseun
            } |
            Should -Throw -ErrorId 'Microsoft.PowerShell.Commands.WriteErrorException,CheckResourceFound'
        }
    }
    Context "Get-DscResource - Class base Resources" {

        BeforeAll {
            $origProgress = $global:ProgressPreference
            $global:ProgressPreference = 'SilentlyContinue'
            Install-ModuleIfMissing -Name XmlContentDsc -Force
            $classTestCases = @(
                @{
                    TestCaseName = 'Good case'
                    Name         = 'XmlFileContentResource'
                    ModuleName   = 'XmlContentDsc'
                }
                @{
                    TestCaseName = 'Module Name case mismatch'
                    Name         = 'XmlFileContentResource'
                    ModuleName   = 'xmlcontentdsc'
                }
                @{
                    TestCaseName = 'Resource name case mismatch'
                    Name         = 'xmlfilecontentresource'
                    ModuleName   = 'XmlContentDsc'
                }
            )
        }

        AfterAll {
            $global:ProgressPreference = $origProgress
        }

        It "should be able to get class resource - <Name> from <ModuleName> - <TestCaseName>" -TestCases $classTestCases {
            param($Name, $ModuleName, $PendingBecause)

            if ($MissingLibmi) {
                Set-ItResult -Pending -Because "Libmi not available for this platform"
            }

            if ($PendingBecause) {
                Set-ItResult -Pending -Because $PendingBecause
            }

            $resource = Get-DscResource -Name $Name -Module $ModuleName
            $resource | Should -Not -BeNullOrEmpty
            $resource.Name | Should -Be $Name
            if (Test-IsInvokeDscResourceEnable) {
                $resource.ImplementationDetail | Should -Be 'ClassBased'
            }
            else {
                $resource.ImplementationDetail | Should -BeNullOrEmpty
            }
        }

        It "should be able to get class resource - <Name> - <TestCaseName>" -TestCases $classTestCases {
            param($Name, $ModuleName, $PendingBecause)
            if ($IsWindows) {
                Set-ItResult -Pending -Because "https://github.com/PowerShell/PSDesiredStateConfiguration/issues/19"
            }

            if ($MissingLibmi) {
                Set-ItResult -Pending -Because "Libmi not available for this platform"
            }

            if ($PendingBecause) {
                Set-ItResult -Pending -Because $PendingBecause
            }

            $resource = Get-DscResource -Name $Name
            $resource | Should -Not -BeNullOrEmpty
            $resource.Name | Should -Be $Name
            if (Test-IsInvokeDscResourceEnable) {
                $resource.ImplementationDetail | Should -Be 'ClassBased'
            }
            else {
                $resource.ImplementationDetail | Should -BeNullOrEmpty
            }
        }
    }
    Context "Invoke-DscResource" {
        BeforeAll {
            $origProgress = $global:ProgressPreference
            $global:ProgressPreference = 'SilentlyContinue'
            $module = Get-InstalledModule -Name PsDscResources -ErrorAction Ignore
            if ($module) {
                Write-Verbose "removing PSDscResources, tests will re-install..." -Verbose
                Uninstall-Module -Name PsDscResources -AllVersions -Force
            }
        }

        AfterAll {
            $Global:ProgressPreference = $origProgress
        }

        Context "mof resources" {
            BeforeAll {
                $dscMachineStatusCases = @(
                    @{
                        value          = '1'
                        expectedResult = $true
                    }
                    @{
                        value          = '$true'
                        expectedResult = $true
                    }
                    @{
                        value          = '0'
                        expectedResult = $false
                    }
                    @{
                        value          = '$false'
                        expectedResult = $false
                    }
                )

                Install-ModuleIfMissing -Name PowerShellGet -Force -SkipPublisherCheck -MinimumVersion '2.2.1'
                Install-ModuleIfMissing -Name xWebAdministration
                $module = Get-Module PowerShellGet -ListAvailable | Sort-Object -Property Version -Descending | Select-Object -First 1

                $psGetModuleSpecification = @{ModuleName = $module.Name; ModuleVersion = $module.Version.ToString() }
            }
            It "Set method should work" -Skip:(!(Test-IsInvokeDscResourceEnable)) {
                if ($MissingLibmi) {
                    Set-ItResult -Pending -Because "Libmi not available for this platform"
                }

                if (!$IsLinux) {
                    $result = Invoke-DscResource -Name PSModule -ModuleName $psGetModuleSpecification -Method set -Property @{
                        Name               = 'PsDscResources'
                        InstallationPolicy = 'Trusted'
                    }
                }
                else {
                    # workraound because of https://github.com/PowerShell/PowerShellGet/pull/529
                    Install-ModuleIfMissing -Name PsDscResources -Force
                }

                $result.RebootRequired | Should -BeFalse
                $module = Get-Module PsDscResources -ListAvailable
                $module | Should -Not -BeNullOrEmpty -Because "Resource should have installed module"
            }
            It 'Set method should return RebootRequired=<expectedResult> when $global:DSCMachineStatus = <value>'  -Skip:(!(Test-IsInvokeDscResourceEnable))  -TestCases $dscMachineStatusCases {
                param(
                    $value,
                    $ExpectedResult
                )

                if ($MissingLibmi) {
                    Set-ItResult -Pending -Because "Libmi not available for this platform"
                }

                # using create scriptBlock because $using:<variable> doesn't work with existing Invoke-DscResource
                # Verified in Windows PowerShell on 20190814
                $result = Invoke-DscResource -Name Script -ModuleName PSDscResources -Method Set -Property @{TestScript = { Write-Output 'test'; return $false }; GetScript = { return @{ } }; SetScript = [scriptblock]::Create("`$global:DSCMachineStatus = $value;return") }
                $result | Should -Not -BeNullOrEmpty
                $result.RebootRequired | Should -BeExactly $expectedResult
            }

            It "Test method should return false"  -Skip:(!(Test-IsInvokeDscResourceEnable)) {
                if ($MissingLibmi) {
                    Set-ItResult -Pending -Because "Libmi not available for this platform"
                }

                $result = Invoke-DscResource -Name Script -ModuleName PSDscResources -Method Test -Property @{TestScript = { Write-Output 'test'; return $false }; GetScript = { return @{ } }; SetScript = { return } }
                $result | Should -Not -BeNullOrEmpty
                $result.InDesiredState | Should -BeFalse -Because "Test method return false"
            }

            It "Test method should return true"  -Skip:(!(Test-IsInvokeDscResourceEnable)) {
                if ($MissingLibmi) {
                    Set-ItResult -Pending -Because "Libmi not available for this platform"
                }

                $result = Invoke-DscResource -Name Script -ModuleName PSDscResources -Method Test -Property @{TestScript = { Write-Verbose 'test'; return $true }; GetScript = { return @{ } }; SetScript = { return } }
                $result | Should -BeTrue -Because "Test method return true"
            }

            It "Test method should return true with moduleSpecification"  -Skip:(!(Test-IsInvokeDscResourceEnable)) {
                if ($MissingLibmi) {
                    Set-ItResult -Pending -Because "Libmi not available for this platform"
                }

                $module = Get-Module PsDscResources -ListAvailable
                $moduleSpecification = @{ModuleName = $module.Name; ModuleVersion = $module.Version.ToString() }
                $result = Invoke-DscResource -Name Script -ModuleName $moduleSpecification -Method Test -Property @{TestScript = { Write-Verbose 'test'; return $true }; GetScript = { return @{ } }; SetScript = { return } }
                $result | Should -BeTrue -Because "Test method return true"
            }

            It "Invalid moduleSpecification"  -Skip:(!(Test-IsInvokeDscResourceEnable)) {
                Set-ItResult -Pending -Because "https://github.com/PowerShell/PSDesiredStateConfiguration/issues/17"
                $moduleSpecification = @{ModuleName = 'PsDscResources'; ModuleVersion = '99.99.99.993' }
                {
                    Invoke-DscResource -Name Script -ModuleName $moduleSpecification -Method Test -Property @{TestScript = { Write-Host 'test'; return $true }; GetScript = { return @{ } }; SetScript = { return } } -ErrorAction Stop
                } |
                Should -Throw -ErrorId 'InvalidResourceSpecification,Invoke-DscResource' -ExpectedMessage 'Invalid Resource Name ''Script'' or module specification.'
            }

            It "Resource with embedded resource not supported and a warning should be produced"  {

                Set-ItResult -Pending -Because "Test is unreliable in release automation."

                if (!(Test-IsInvokeDscResourceEnable)) {
                    Set-ItResult -Skipped -Because "Feature not enabled"
                }

                if (!$IsMacOS) {
                    Set-ItResult -Skipped -Because "Not applicable on Windows and xWebAdministration resources don't load on linux"
                }

                try {
                    Invoke-DscResource -Name xWebSite -ModuleName 'xWebAdministration' -Method Test -Property @{TestScript = 'foodbar' } -ErrorAction Stop -WarningVariable warnings
                }
                catch{
                    #this will fail too, but that is nat what we are testing...
                }

                $warnings.Count | Should -Be 1 -Because "There should be 1 warning on macOS and Linux"
                $warnings[0] | Should -Match 'embedded resources.*not support'
            }

            It "Using PsDscRunAsCredential should say not supported" -Skip:(!(Test-IsInvokeDscResourceEnable)) {
                {
                    Invoke-DscResource -Name Script -ModuleName PSDscResources -Method Set -Property @{TestScript = { Write-Output 'test'; return $false }; GetScript = { return @{ } }; SetScript = {return}; PsDscRunAsCredential='natoheu'}  -ErrorAction Stop
                } |
                Should -Throw -ErrorId 'PsDscRunAsCredentialNotSupport,Invoke-DscResource'
            }

            # waiting on Get-DscResource to be fixed
            It "Invalid module name" -Skip:(!(Test-IsInvokeDscResourceEnable)) {
                Set-ItResult -Pending -Because "https://github.com/PowerShell/PSDesiredStateConfiguration/issues/17"
                {
                    Invoke-DscResource -Name Script -ModuleName santoheusnaasonteuhsantoheu -Method Test -Property @{TestScript = { Write-Host 'test'; return $true }; GetScript = { return @{ } }; SetScript = { return } } -ErrorAction Stop
                } |
                Should -Throw -ErrorId 'Microsoft.PowerShell.Commands.WriteErrorException,CheckResourceFound'
            }

            It "Invalid resource name" -Skip:(!(Test-IsInvokeDscResourceEnable)) {
                if ($IsWindows) {
                    Set-ItResult -Pending -Because "https://github.com/PowerShell/PSDesiredStateConfiguration/issues/17"
                }

                if ($MissingLibmi) {
                    Set-ItResult -Pending -Because "Libmi not available for this platform"
                }

                {
                    Invoke-DscResource -Name santoheusnaasonteuhsantoheu -Method Test -Property @{TestScript = { Write-Host 'test'; return $true }; GetScript = { return @{ } }; SetScript = { return } } -ErrorAction Stop
                } |
                Should -Throw -ErrorId 'Microsoft.PowerShell.Commands.WriteErrorException,CheckResourceFound'
            }

            It "Get method should work"  -Skip:(!(Test-IsInvokeDscResourceEnable)) {
                if ($IsLinux) {
                    Set-ItResult -Pending -Because "https://github.com/PowerShell/PSDesiredStateConfiguration/issues/12 and https://github.com/PowerShell/PowerShellGet/pull/529"
                }

                $result = Invoke-DscResource -Name PSModule -ModuleName $psGetModuleSpecification -Method Get -Property @{ Name = 'PsDscResources' }
                $result | Should -Not -BeNullOrEmpty
                $result.Author | Should -BeLike 'Microsoft*'
                $result.InstallationPolicy | Should -BeOfType string
                $result.Guid | Should -BeOfType Guid
                $result.Ensure | Should -Be 'Present'
                $result.Name | Should -Be 'PsDscResources'
                $result.Description | Should -BeLike 'This*DSC*'
                $result.InstalledVersion | Should -BeOfType Version
                $result.ModuleBase | Should -BeLike '*PSDscResources*'
                $result.Repository | Should -BeOfType string
                $result.ModuleType | Should -Be 'Manifest'
            }
        }

        Context "Class Based Resources" {
            BeforeAll {
                Install-ModuleIfMissing -Name XmlContentDsc -Force
            }

            AfterAll {
                $Global:ProgressPreference = $origProgress
            }

            BeforeEach {
                $testXmlPath = 'TestDrive:\test.xml'
                @'
<configuration>
<appSetting>
    <Test1/>
</appSetting>
</configuration>
'@ | Out-File -FilePath $testXmlPath -Encoding utf8NoBOM
                $resolvedXmlPath = (Resolve-Path -Path $testXmlPath).ProviderPath
            }

            It 'Set method should work'  -Skip:(!(Test-IsInvokeDscResourceEnable)) {
                param(
                    $value,
                    $ExpectedResult
                )

                if ($MissingLibmi) {
                    Set-ItResult -Pending -Because "Libmi not available for this platform"
                }

                $testString = '890574209347509120348'
                $result = Invoke-DscResource -Name XmlFileContentResource -ModuleName XmlContentDsc -Property @{Path = $resolvedXmlPath; XPath = '/configuration/appSetting/Test1'; Ensure = 'Present'; Attributes = @{ TestValue2 = $testString; Name = $testString } } -Method Set
                $result | Should -Not -BeNullOrEmpty
                $result.RebootRequired | Should -BeFalse
                $testXmlPath | Should -FileContentMatch $testString
            }
        }
    }
}
