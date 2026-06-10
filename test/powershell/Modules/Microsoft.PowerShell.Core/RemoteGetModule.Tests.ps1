# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

$script:skipRemoteGetModule = (-not $IsWindows) -or (Test-IsWinWow64)

Describe "Remote module tests" -Tags 'Feature','RequireAdminOnWindows' -Skip:$script:skipRemoteGetModule {

    BeforeAll {
        $pssession = New-RemoteSession
        # pending https://github.com/PowerShell/PowerShell/issues/4819
        # $cimsession = New-RemoteSession -CimSession
    }

    AfterAll {
        if ($null -ne $pssession) { Remove-PSSession $pssession -ErrorAction SilentlyContinue }
    }

    It "Get-Module fails if not using -ListAvailable with '<parameter>'" -TestCases @(
        @{parameter="PSSession"; usePSSession=$true}
        # @{parameter="CIMSession"; value=$cimsession}
    ) {
        param($parameter, $usePSSession)
        $value = if ($usePSSession) { $pssession } else { $null }
        $parameters = @{$parameter=$value}
        { Get-Module @parameters -ErrorAction Stop } | Should -Throw -ErrorId "RemoteDiscoveryWorksOnlyInListAvailableMode,Microsoft.PowerShell.Commands.GetModuleCommand"
    }

    It "Get-Module succeeds using -ListAvailable with '<parameter>' name=<name>" -TestCases @(
        @{parameter="PSSession"; usePSSession=$true; name=$null},
        @{parameter="PSSession"; usePSSession=$true; name="Pester"}
        # @{parameter="CIMSession"; value=$cimsession},
        # @{parameter="CIMSession"; value=$cimsession; name="pESTER"}
    ) {
        param($parameter, $usePSSession, $name)
        $value = if ($usePSSession) { $pssession } else { $null }
        $parameters = @{$parameter=$value; ListAvailable=$true; Refresh=$true}
        if ($name) {
            $parameters += @{name=$name}
        }
        $modules = Get-Module @parameters
        $modules | Should -Not -BeNullOrEmpty
        $modules[0] | Should -BeOfType System.Management.Automation.PSModuleInfo
    }

    It "Get-Module can be called as an API with '<parameter>' = '<value>'" -TestCases @(
        @{parameter = "Name"              ; value = "foo"},
        @{parameter = "FullyQualifiedName"; value = @{ModuleName="foo";ModuleVersion="1.2.3"}},
        @{parameter = "All"               ; value = $true},
        @{parameter = "All"               ; value = $false},
        @{parameter = "ListAvailable"     ; value = $true},
        @{parameter = "ListAvailable"     ; value = $false},
        @{parameter = "PSEdition"         ; value = "foo"},
        @{parameter = "Refresh"           ; value = $true},
        @{parameter = "Refresh"           ; value = $false},
        @{parameter = "PSSession"         ; usePSSession = $true},
        # @{parameter = "CimSession"        ; value = $cimsession},
        @{parameter = "CimResourceUri"    ; value = "http://foo/"},
        @{parameter = "CimNamespace"      ; value = "foo"},
        @{parameter = "PSEdition"         ; value = "Core"},
        @{parameter = "PSEdition"         ; value = "Desktop"}
        ) {
        param($parameter, $value, $usePSSession)

        if ($usePSSession) { $value = $pssession }
        $getModuleCommand = [Microsoft.PowerShell.Commands.GetModuleCommand]::new()
        $getModuleCommand.$parameter = $value
        if ($parameter -eq "FullyQualifiedName") {
            $getModuleCommand.FullyQualifiedName | Should -BeOfType Microsoft.PowerShell.Commands.ModuleSpecification
            $getModuleCommand.FullyQualifiedName.Name | Should -BeExactly "foo"
            $getModuleCommand.FullyQualifiedName.Version | Should -Be "1.2.3"
        } else {
            $getModuleCommand.$parameter | Should -Be $value
        }
    }

    It "Failure if -Name and -FullyQualifiedName are both specified" {
        { Get-Module -Name foo -FullyQualifiedName @{ModuleName='foo'} -ErrorAction Stop } | Should -Throw -ErrorId "CannotConvertArgumentNoMessage,Microsoft.PowerShell.Commands.GetModuleCommand"
    }

    It "Get-Module supports pipeline" {
        $module = Get-Module -Name Microsoft.PowerShell.Utility
        Compare-Object $module ($module | Get-Module) | Should -BeNullOrEmpty
    }

    It "New-CimSession works" -Pending {
        # enable all CimSession tests once https://github.com/PowerShell/PowerShell/issues/4819 is fixed
    }
}
