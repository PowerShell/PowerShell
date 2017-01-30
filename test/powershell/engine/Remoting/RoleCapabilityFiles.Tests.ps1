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
            throw 'No Exception!'
        }
        catch
        {
            [System.Management.Automation.MethodInvocationException] $expectedException = [System.Management.Automation.MethodInvocationException] $_.Exception
            if ($expectedException -ne $null)
            {
                ([System.Management.Automation.PSInvalidOperationException] $expectedException.InnerException).ErrorRecord.FullyQualifiedErrorId | Should Be 'CouldNotFindRoleCapabilityFile'
            }
            else
            {
                throw 'Unexpected Exception'
            }
        }
    }

    It "Verifies incorrect role capability file extenstion error" {

        New-PSSessionConfigurationFile -Path $PSSessionConfigFile -RoleDefinitions @{
            Administrators = @{ RoleCapabilityFiles = "$BadRoleCapFile" }
        }

        try
        {
            $iss = [initialsessionstate]::CreateFromSessionConfigurationFile($PSSessionConfigFile, { $true })
            throw 'No Exception!'
        }
        catch
        {
            [System.Management.Automation.MethodInvocationException] $expectedException = [System.Management.Automation.MethodInvocationException] $_.Exception
            if ($expectedException -ne $null)
            {
                ([System.Management.Automation.PSInvalidOperationException] $expectedException.InnerException).ErrorRecord.FullyQualifiedErrorId | Should Be 'InvalidRoleCapabilityFileExtension'
            }
            else
            {
                throw 'Unexpected Exception'
            }
        }
    }

    It "Verifies restriction on good role capability file" {

        New-PSSessionConfigurationFile -Path $PSSessionConfigFile -RoleDefinitions @{
            Administrators = @{ RoleCapabilityFiles = "$GoodRoleCapFile" }
        }

        # 'Get-Service' is not included in the session.
        $iss = [initialsessionstate]::CreateFromSessionConfigurationFile($PSSessionConfigFile, { $true })
        [powershell] $ps = [powershell]::Create($iss)
        $null = $ps.AddCommand('Get-Service')

        try
        {
            $ps.Invoke()
            throw 'No Exception!'
        }
        catch
        {
            [System.Management.Automation.MethodInvocationException] $expectedException = [System.Management.Automation.MethodInvocationException] $_.Exception
            if ($expectedException -ne $null)
            {
                ($expectedException.InnerException.GetType().FullName) | Should Be 'System.Management.Automation.CommandNotFoundException'
            }
            else
            {
                throw 'Unexpected Exception'
            }
        }

        $ps.Dispose()
    }
}
