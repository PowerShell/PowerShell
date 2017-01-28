##
## PowerShell Remoting Endpoint Role Capability Files Tests
##

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

        try
        {
            $iss = [initialsessionstate]::CreateFromSessionConfigurationFile($PSSessionConfigFile, { $true })
            throw 'Should have thrown CouldNotFindRoleCapabilityFile exception'
        }
        catch [System.Management.Automation.MethodInvocationException]
        {
            ([System.Management.Automation.PSInvalidOperationException] $_.Exception.InnerException).ErrorRecord.FullyQualifiedErrorId | Should Be 'CouldNotFindRoleCapabilityFile'
        }
    }

    It "Verifies incorrect role capability file extenstion error" {

        New-PSSessionConfigurationFile -Path $PSSessionConfigFile -RoleDefinitions @{
            Administrators = @{ RoleCapabilityFiles = "$BadRoleCapFile" }
        }

        try
        {
            $iss = [initialsessionstate]::CreateFromSessionConfigurationFile($PSSessionConfigFile, { $true })
            throw 'Should have thrown InvalidRoleCapabilityFileExtension exception'
        }
        catch [System.Management.Automation.MethodInvocationException]
        {
            ([System.Management.Automation.PSInvalidOperationException] $_.Exception.InnerException).ErrorRecord.FullyQualifiedErrorId | Should Be 'InvalidRoleCapabilityFileExtension'
        }
    }

    It "Verifies good role capability file" {

        New-PSSessionConfigurationFile -Path $PSSessionConfigFile -RoleDefinitions @{
            Administrators = @{ RoleCapabilityFiles = "$GoodRoleCapFile" }
        }

        $iss = [initialsessionstate]::CreateFromSessionConfigurationFile($PSSessionConfigFile, { $true })
        [powershell] $ps = [powershell]::Create($iss)
        $result = $ps.AddCommand('Get-Process').AddParameter('Name', 'PowerShell*').Invoke()

        $result.Count | Should Not Be 0
        $ps.Dispose()
    }
}
