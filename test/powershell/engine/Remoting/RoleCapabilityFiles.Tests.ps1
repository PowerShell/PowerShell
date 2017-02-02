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

        $fullyQualifiedErrorId = ""
        try
        {
            $iss = [initialsessionstate]::CreateFromSessionConfigurationFile($PSSessionConfigFile, { $true })
            throw 'No Exception!'
        }
        catch
        {
            $psioe = [System.Management.Automation.PSInvalidOperationException] ($_.Exception).InnerException
            if ($psioe -ne $null)
            {
                $fullyQualifiedErrorId = $psioe.ErrorRecord.FullyQualifiedErrorId
            }
            $fullyQualifiedErrorId | Should Be 'CouldNotFindRoleCapabilityFile'
        }
    }

    It "Verifies incorrect role capability file extenstion error" {

        New-PSSessionConfigurationFile -Path $PSSessionConfigFile -RoleDefinitions @{
            Administrators = @{ RoleCapabilityFiles = "$BadRoleCapFile" }
        }

        $fullyQualifiedErrorId = ""
        try
        {
            $iss = [initialsessionstate]::CreateFromSessionConfigurationFile($PSSessionConfigFile, { $true })
            throw 'No Exception!'
        }
        catch
        {
            $psioe = [System.Management.Automation.PSInvalidOperationException] ($_.Exception).InnerException
            if ($psioe -ne $null)
            {
                $fullyQualifiedErrorId = $psioe.ErrorRecord.FullyQualifiedErrorId
            }
            $fullyQualifiedErrorId | Should Be 'InvalidRoleCapabilityFileExtension'
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

        $exceptionTypeName = ""
        try
        {
            $ps.Invoke()
            throw 'No Exception!'
        }
        catch
        {
            if ($_.Exception.InnerException -ne $null)
            {
                $exceptionTypeName = $_.Exception.InnerException.GetType().FullName
            }
            $exceptionTypeName | Should Be 'System.Management.Automation.CommandNotFoundException'
        }

        $ps.Dispose()
    }
}
