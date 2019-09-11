# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Function Install-ModuleIfMissing
{
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

    if(!$module -or $module.Version -lt $MinimumVersion)
    {
        Write-Verbose "Installing module '$Name' ..." -Verbose
        Install-Module -Name $Name -Force -SkipPublisherCheck:$SkipPublisherCheck.IsPresent
    }
}

Function Test-IsInvokeDscResourceEnable
{
    return [ExperimentalFeature]::IsEnabled("PSDesiredStateConfiguration.InvokeDscResource")
}

Describe "Test PSDesiredStateConfiguration" -tags CI {
    Context "Module loading" {
        BeforeAll {
            $commands = Get-Command -Module PSDesiredStateConfiguration
            $expectedCommandCount = 3
            if (Test-IsInvokeDscResourceEnable)
            {
                $expectedCommandCount ++
            }
        }
        BeforeEach {
        }
        AfterEach {
        }
        AfterAll {
        }

        It "The module should have $expectedCommandCount commands" {
            if ($commands.Count -ne $expectedCommandCount)
            {
                $modulePath = (Get-Module PSDesiredStateConfiguration).Path
                Write-Verbose -Verbose -Message "PSDesiredStateConfiguration Path: $modulePath"
                $commands | Out-String | Write-Verbose -Verbose
            }
            $commands.Count | Should -Be $expectedCommandCount
        }

        It "The module should have the Configuration Command" {
            $commands | Where-Object {$_.Name -eq 'Configuration'} | Should -Not -BeNullOrEmpty
        }
        It "The module should have the Get-DscResource Command" {
            $commands | Where-Object {$_.Name -eq 'Get-DscResource'} | Should -Not -BeNullOrEmpty
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
                    Name = 'groupset'
                    ModuleName = 'PSDscResources'
                }
                @{
                    TestCaseName = 'Both names have matching case'
                    Name = 'GroupSet'
                    ModuleName = 'PSDscResources'
                }
                @{
                    TestCaseName = 'case mismatch in module name'
                    Name = 'GroupSet'
                    ModuleName = 'psdscResources'
                }
            )
        }
        AfterAll {
            $Global:ProgressPreference = $origProgress
        }
        it "should be able to get <Name> - <TestCaseName>" -TestCases $testCases {
            param($Name)

            if($IsWindows)
            {
                Set-ItResult -Pending -Because "Will only find script from PSDesiredStateConfiguration without modulename"
            }

            if($IsLinux)
            {
                Set-ItResult -Pending -Because "https://github.com/PowerShell/PSDesiredStateConfiguration/issues/26"
            }

            $resource = Get-DscResource -Name $name
            $resource | Should -Not -BeNullOrEmpty
            $resource.Name | Should -Be $Name
            if (Test-IsInvokeDscResourceEnable)
            {
                $resource.ImplementationDetail | Should -BeNullOrEmpty
            }            else
            {
                $resource.ImplementationDetail | Should -BeNullOrEmpty
            }

        }

        it "should be able to get <Name> from <ModuleName> - <TestCaseName>" -TestCases $testCases {
            param($Name,$ModuleName, $PendingBecause)

            if($IsLinux)
            {
                Set-ItResult -Pending -Because "https://github.com/PowerShell/PSDesiredStateConfiguration/issues/26"
            }

            if($PendingBecause)
            {
                Set-ItResult -Pending -Because $PendingBecause
            }
            $resource = Get-DscResource -Name $Name -Module $ModuleName
            $resource | Should -Not -BeNullOrEmpty
            $resource.Name | Should -Be $Name
            if (Test-IsInvokeDscResourceEnable)
            {
                $resource.ImplementationDetail | Should -BeNullOrEmpty
            }
            else
            {
                $resource.ImplementationDetail | Should -BeNullOrEmpty
            }
        }
    }
    Context "Get-DscResource - ScriptResources" {
        BeforeAll {
            $origProgress = $global:ProgressPreference
            $global:ProgressPreference = 'SilentlyContinue'

            Install-ModuleIfMissing -Name PSDscResources -Force

            Install-ModuleIfMissing -Name PowerShellGet -MinimumVersion '2.2.1'
            $module = Get-Module PowerShellGet -ListAvailable | Sort-Object -Property Version -Descending | Select-Object -First 1

            $psGetModuleSpecification = @{ModuleName=$module.Name;ModuleVersion=$module.Version.ToString()}
            $psGetModuleCount = @(Get-Module PowerShellGet -ListAvailable).Count
            $testCases = @(
                @{
                    TestCaseName = 'case mismatch in resource name'
                    Name = 'script'
                    ModuleName = 'PSDscResources'
                }
                @{
                    TestCaseName = 'Both names have matching case'
                    Name = 'Script'
                    ModuleName = 'PSDscResources'
                }
                @{
                    TestCaseName = 'case mismatch in module name'
                    Name = 'Script'
                    ModuleName = 'psdscResources'
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

        it "should be able to get <Name> - <TestCaseName>" -TestCases $testCases {
            param($Name)

            if($IsWindows)
            {
                Set-ItResult -Pending -Because "Will only find script from PSDesiredStateConfiguration without modulename"
            }

            if($PendingBecause)
            {
                Set-ItResult -Pending -Because $PendingBecause
            }

            $resources = @(Get-DscResource -Name $name)
            $resources | Should -Not -BeNullOrEmpty
            foreach($resource in $resource)
            {
                $resource.Name | Should -Be $Name
                if (Test-IsInvokeDscResourceEnable)
                {
                    $resource.ImplementationDetail | Should -Be 'ScriptBased'
                }
                else
                {
                    $resource.ImplementationDetail | Should -BeNullOrEmpty
                }

            }
        }

        it "should be able to get <Name> from <ModuleName> - <TestCaseName>" -TestCases $testCases {
            param($Name,$ModuleName, $PendingBecause)

            if($IsLinux)
            {
                Set-ItResult -Pending -Because "https://github.com/PowerShell/PSDesiredStateConfiguration/issues/12 and https://github.com/PowerShell/PowerShellGet/pull/529"
            }

            if($PendingBecause)
            {
                Set-ItResult -Pending -Because $PendingBecause
            }

            $resources = @(Get-DscResource -Name $name -Module $ModuleName)
            $resources | Should -Not -BeNullOrEmpty
            foreach($resource in $resource)
            {
                $resource.Name | Should -Be $Name
                if (Test-IsInvokeDscResourceEnable)
                {
                    $resource.ImplementationDetail | Should -Be 'ScriptBased'
                }
                else
                {
                    $resource.ImplementationDetail | Should -BeNullOrEmpty
                }
            }
        }

        it "should throw when resource is not found" {
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
                    Name = 'XmlFileContentResource'
                    ModuleName = 'XmlContentDsc'
                }
                @{
                    TestCaseName = 'Module Name case mismatch'
                    Name = 'XmlFileContentResource'
                    ModuleName = 'xmlcontentdsc'
                }
                @{
                    TestCaseName = 'Resource name case mismatch'
                    Name = 'xmlfilecontentresource'
                    ModuleName = 'XmlContentDsc'
                }
            )
        }
        AfterAll {
            $Global:ProgressPreference = $origProgress
        }

        it "should be able to get class resource - <Name> from <ModuleName> - <TestCaseName>" -TestCases $classTestCases {
            param($Name,$ModuleName, $PendingBecause)

            if($PendingBecause)
            {
                Set-ItResult -Pending -Because $PendingBecause
            }

            $resource = Get-DscResource -Name $Name -Module $ModuleName
            $resource | Should -Not -BeNullOrEmpty
            $resource.Name | Should -Be $Name
            if (Test-IsInvokeDscResourceEnable)
            {
                $resource.ImplementationDetail | Should -Be 'ClassBased'
            }
            else
            {
                $resource.ImplementationDetail | Should -BeNullOrEmpty
            }
        }

        it "should be able to get class resource - <Name> - <TestCaseName>" -TestCases $classTestCases {
            param($Name,$ModuleName, $PendingBecause)
            if($IsWindows)
            {
                Set-ItResult -Pending -Because "https://github.com/PowerShell/PSDesiredStateConfiguration/issues/19"
            }
            if($PendingBecause)
            {
                Set-ItResult -Pending -Because $PendingBecause
            }
            $resource = Get-DscResource -Name $Name
            $resource | Should -Not -BeNullOrEmpty
            $resource.Name | Should -Be $Name
            if (Test-IsInvokeDscResourceEnable)
            {
                $resource.ImplementationDetail | Should -Be 'ClassBased'
            }
            else
            {
                $resource.ImplementationDetail | Should -BeNullOrEmpty
            }
        }
    }
    Context "Invoke-DscResource" {
        BeforeAll {
            $origProgress = $global:ProgressPreference
            $global:ProgressPreference = 'SilentlyContinue'
            $module = Get-InstalledModule -Name PsDscResources -ErrorAction Ignore
            if($module)
            {
                Write-Verbose "removing PSDscResources, tests will re-install..." -Verbose
                Uninstall-Module -Name PsDscResources -AllVersions -Force
            }
        }
        AfterAll {
            $Global:ProgressPreference = $origProgress
        }
        Context "mof resources"  {
            BeforeAll {
                $dscMachineStatusCases = @(
                    @{
                        value = '1'
                        expectedResult = $true
                    }
                    @{
                        value = '$true'
                        expectedResult = $true
                    }
                    @{
                        value = '0'
                        expectedResult = $false
                    }
                    @{
                        value = '$false'
                        expectedResult = $false
                    }
                )

                Install-ModuleIfMissing -Name PowerShellGet -Force -SkipPublisherCheck -MinimumVersion '2.2.1'
                $module = Get-Module PowerShellGet -ListAvailable | Sort-Object -Property Version -Descending | Select-Object -First 1

                $psGetModuleSpecification = @{ModuleName=$module.Name;ModuleVersion=$module.Version.ToString()}
            }
            it "Set method should work" -Skip:(!(Test-IsInvokeDscResourceEnable)) {
                if(!$IsLinux)
                {
                        $result  = Invoke-DscResource -Name PSModule -ModuleName $psGetModuleSpecification -Method set -Property @{
                        Name = 'PsDscResources'
                        InstallationPolicy = 'Trusted'
                    }
                }
                else
                {
                    # workraound because of https://github.com/PowerShell/PowerShellGet/pull/529
                    Install-ModuleIfMissing -Name PsDscResources -Force
                }

                $result.RebootRequired | Should -BeFalse
                $module = Get-module PsDscResources -ListAvailable
                $module | Should -Not -BeNullOrEmpty -Because "Resource should have installed module"
            }
            it 'Set method should return RebootRequired=<expectedResult> when $global:DSCMachineStatus = <value>'  -Skip:(!(Test-IsInvokeDscResourceEnable))  -TestCases $dscMachineStatusCases {
                param(
                    $value,
                    $ExpectedResult
                )

                # using create scriptBlock because $using:<variable> doesn't work with existing Invoke-DscResource
                # Verified in Windows PowerShell on 20190814
                $result  = Invoke-DscResource -Name Script -ModuleName PSDscResources -Method Set -Property @{TestScript = {Write-Output 'test';return $false};GetScript = {return @{}}; SetScript = [scriptblock]::Create("`$global:DSCMachineStatus = $value;return")}
                $result | Should -Not -BeNullOrEmpty
                $result.RebootRequired | Should -BeExactly $expectedResult
            }
            it "Test method should return false"  -Skip:(!(Test-IsInvokeDscResourceEnable))  {
                $result  = Invoke-DscResource -Name Script -ModuleName PSDscResources -Method Test -Property @{TestScript = {Write-Output 'test';return $false};GetScript = {return @{}}; SetScript = {return}}
                $result | Should -Not -BeNullOrEmpty
                $result.InDesiredState | Should -BeFalse -Because "Test method return false"
            }
            it "Test method should return true"  -Skip:(!(Test-IsInvokeDscResourceEnable))  {
                $result  = Invoke-DscResource -Name Script -ModuleName PSDscResources -Method Test -Property @{TestScript = {Write-Verbose 'test';return $true};GetScript = {return @{}}; SetScript = {return}}
                $result | Should -BeTrue -Because "Test method return true"
            }
            it "Test method should return true with moduleSpecification"  -Skip:(!(Test-IsInvokeDscResourceEnable))  {
                $module = get-module PsDscResources -ListAvailable
                $moduleSpecification = @{ModuleName=$module.Name;ModuleVersion=$module.Version.ToString()}
                $result  = Invoke-DscResource -Name Script -ModuleName $moduleSpecification -Method Test -Property @{TestScript = {Write-Verbose 'test';return $true};GetScript = {return @{}}; SetScript = {return}}
                $result | Should -BeTrue -Because "Test method return true"
            }

            it "Invalid moduleSpecification"  -Skip:(!(Test-IsInvokeDscResourceEnable))  {
                Set-ItResult -Pending -Because "https://github.com/PowerShell/PSDesiredStateConfiguration/issues/17"
                $moduleSpecification = @{ModuleName='PsDscResources';ModuleVersion='99.99.99.993'}
                {
                    Invoke-DscResource -Name Script -ModuleName $moduleSpecification -Method Test -Property @{TestScript = {Write-Host 'test';return $true};GetScript = {return @{}}; SetScript = {return}} -ErrorAction Stop
                } |
                    Should -Throw -ErrorId 'InvalidResourceSpecification,Invoke-DscResource' -ExpectedMessage 'Invalid Resource Name ''Script'' or module specification.'
            }

            # waiting on Get-DscResource to be fixed
            it "Invalid module name" -Skip:(!(Test-IsInvokeDscResourceEnable))  {
                Set-ItResult -Pending -Because "https://github.com/PowerShell/PSDesiredStateConfiguration/issues/17"
                {
                    Invoke-DscResource -Name Script -ModuleName santoheusnaasonteuhsantoheu -Method Test -Property @{TestScript = {Write-Host 'test';return $true};GetScript = {return @{}}; SetScript = {return}} -ErrorAction Stop
                } |
                    Should -Throw -ErrorId 'Microsoft.PowerShell.Commands.WriteErrorException,CheckResourceFound'
            }

            it "Invalid resource name" -Skip:(!(Test-IsInvokeDscResourceEnable))  {
                if ($IsWindows) {
                    Set-ItResult -Pending -Because "https://github.com/PowerShell/PSDesiredStateConfiguration/issues/17"
                }

                {
                    Invoke-DscResource -Name santoheusnaasonteuhsantoheu -Method Test -Property @{TestScript = {Write-Host 'test';return $true};GetScript = {return @{}}; SetScript = {return}} -ErrorAction Stop
                } |
                    Should -Throw -ErrorId 'Microsoft.PowerShell.Commands.WriteErrorException,CheckResourceFound'
            }

            it "Get method should work"  -Skip:(!(Test-IsInvokeDscResourceEnable))  {
                if($IsLinux)
                {
                    Set-ItResult -Pending -Because "https://github.com/PowerShell/PSDesiredStateConfiguration/issues/12 and https://github.com/PowerShell/PowerShellGet/pull/529"
                }

                $result  = Invoke-DscResource -Name PSModule -ModuleName $psGetModuleSpecification -Method Get -Property @{ Name = 'PsDscResources'}
                $result | Should -Not -BeNullOrEmpty
                $result.Author | Should -BeLike 'Microsoft*'
                $result.InstallationPolicy | Should -BeOfType [string]
                $result.Guid | Should -BeOfType [Guid]
                $result.Ensure | Should -Be 'Present'
                $result.Name | Should -be 'PsDscResources'
                $result.Description | Should -BeLike 'This*DSC*'
                $result.InstalledVersion | should -BeOfType [Version]
                $result.ModuleBase | Should -BeLike '*PSDscResources*'
                $result.Repository | should -BeOfType [string]
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
            it 'Set method should work'  -Skip:(!(Test-IsInvokeDscResourceEnable))  {
                param(
                    $value,
                    $ExpectedResult
                )

                $testString = '890574209347509120348'
                $result  = Invoke-DscResource -Name XmlFileContentResource -ModuleName XmlContentDsc -Property @{Path=$resolvedXmlPath; XPath = '/configuration/appSetting/Test1';Ensure='Present'; Attributes=@{ TestValue2 = $testString; Name = $testString } } -Method Set
                $result | Should -Not -BeNullOrEmpty
                $result.RebootRequired | Should -BeFalse
                $testXmlPath | Should -FileContentMatch $testString
            }
        }
    }
}

