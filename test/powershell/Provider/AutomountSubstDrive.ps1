# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Precondition: start from fresh PS session, do not have the media mounted
param([switch]$useModule, [string]$FullPath)

$global:CoreScriptPath = Join-Path $PSScriptRoot 'AutomountSubstDriveCore.ps1'

if ($useModule)
{
    $m = New-Module {
        function Test-DrivePresenceFromModule
        {
            param ([String]$Path)

            & $global:CoreScriptPath -Path $Path
        }

        Export-ModuleMember -Function Test-DrivePresenceFromModule
    }
}

try
{
    if ($useModule)
    {
        Import-Module $m -Force
        Test-DrivePresenceFromModule -Path $FullPath
    }
    else
    {
        & $global:CoreScriptPath -Path $FullPath
    }
}
finally
{
    if ($useModule)
    {
        Remove-Module $m
    }
}
