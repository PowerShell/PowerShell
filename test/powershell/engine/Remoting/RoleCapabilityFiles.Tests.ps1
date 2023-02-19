# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
##
## PowerShell Remoting Endpoint Role Capability Files Tests
##

Describe "Remote session configuration RoleDefinition RoleCapabilityFiles key tests" -Tags "Feature" {

    BeforeAll {

        if (!$IsWindows)
        {
            $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = $true
        }
        else
        {
            [string] $RoleCapDirectory = (New-Item -Path "$TestDrive\RoleCapability" -ItemType Directory -Force).FullName

            [string] $GoodRoleCapFile = "$RoleCapDirectory\TestGoodRoleCap.psrc"
            New-PSRoleCapabilityFile -Path $GoodRoleCapFile -VisibleCmdlets 'Get-Command','Get-Process','Clear-Host','Out-Default','Select-Object','Get-FormatData','Get-Help'

            [string] $BadRoleCapFile = "$RoleCapDirectory\TestBadRoleCap.psrc"
            New-PSRoleCapabilityFile -Path $BadRoleCapFile -VisibleCmdlets *
            [string] $BadRoleCapFile = $BadRoleCapFile.Replace('.psrc', 'psbad')

            [string] $PSSessionConfigFile = "$RoleCapDirectory\TestConfig.pssc"
        }
    }

    AfterAll {

        if (!$IsWindows)
        {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
        }
    }

    It "Verifies missing role capability file error" {

        New-PSSessionConfigurationFile -Path $PSSessionConfigFile -RoleDefinitions @{
            Administrators = @{ RoleCapabilityFiles = "$RoleCapDirectory\NoFile.psrc" }
        }

        $e = {
            $iss = [initialsessionstate]::CreateFromSessionConfigurationFile($PSSessionConfigFile, { $true })
        } | Should -Throw -PassThru

        $e.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly 'CouldNotFindRoleCapabilityFile'
    }

    It "Verifies incorrect role capability file extension error" {

        New-PSSessionConfigurationFile -Path $PSSessionConfigFile -RoleDefinitions @{
            Administrators = @{ RoleCapabilityFiles = "$BadRoleCapFile" }
        }

        $e = {
            $iss = [initialsessionstate]::CreateFromSessionConfigurationFile($PSSessionConfigFile, { $true })
        } | Should -Throw -PassThru
        $e.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly 'InvalidRoleCapabilityFileExtension'
    }

    It "Verifies restriction on good role capability file" {

        New-PSSessionConfigurationFile -Path $PSSessionConfigFile -RoleDefinitions @{
            Administrators = @{ RoleCapabilityFiles = "$GoodRoleCapFile" }
        }

        # 'Get-Service' is not included in the session.
        $iss = [initialsessionstate]::CreateFromSessionConfigurationFile($PSSessionConfigFile, { $true })
        [powershell] $ps = [powershell]::Create($iss)
        $null = $ps.AddCommand('Get-Service')

        { $ps.Invoke() } | Should -Throw -ErrorId 'CommandNotFoundException'

        $ps.Dispose()
    }
}
