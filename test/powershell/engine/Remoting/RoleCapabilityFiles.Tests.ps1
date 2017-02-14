##
## PowerShell Remoting Endpoint Role Capability Files Tests
##

Import-Module $PSScriptRoot\..\..\Common\Test.Helpers.psm1

Describe "Remote session configuration RoleDefintion RoleCapabilityFiles key tests" -Tags "Feature" {

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

        $exc = {
            $iss = [initialsessionstate]::CreateFromSessionConfigurationFile($PSSessionConfigFile, { $true })
        } | ShouldBeErrorId "PSInvalidOperationException"
        $exc.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should Be "CouldNotFindRoleCapabilityFile"
    }

    It "Verifies incorrect role capability file extenstion error" {

        New-PSSessionConfigurationFile -Path $PSSessionConfigFile -RoleDefinitions @{
            Administrators = @{ RoleCapabilityFiles = "$BadRoleCapFile" }
        }

        $exc = {
            $iss = [initialsessionstate]::CreateFromSessionConfigurationFile($PSSessionConfigFile, { $true })
        } | ShouldBeErrorId "PSInvalidOperationException"
        $exc.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should Be "InvalidRoleCapabilityFileExtension"
    }

    It "Verifies restriction on good role capability file" {

        New-PSSessionConfigurationFile -Path $PSSessionConfigFile -RoleDefinitions @{
            Administrators = @{ RoleCapabilityFiles = "$GoodRoleCapFile" }
        }

        # 'Get-Service' is not included in the session.
        $iss = [initialsessionstate]::CreateFromSessionConfigurationFile($PSSessionConfigFile, { $true })
        [powershell] $ps = [powershell]::Create($iss)
        $null = $ps.AddCommand('Get-Service')

        $exc = {
            $ps.Invoke()
        } | ShouldBeErrorId "CommandNotFoundException"
        $exc.Exception.InnerException.CommandName | Should Be "Get-Service"
        $ps.Dispose()
    }
}
