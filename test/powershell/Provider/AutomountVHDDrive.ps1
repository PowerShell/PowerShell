# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Precondition: start from fresh PS session, do not have the media mounted
param([switch]$useModule, [string]$VHDPath)

function CreateVHD ($VHDPath, $Size)
{
  $drive = (New-VHD -Path $vhdpath -SizeBytes $size -Dynamic   | `
              Mount-VHD -Passthru |  `
              Get-Disk -Number {$_.DiskNumber} | `
              Initialize-Disk -PartitionStyle MBR -PassThru | `
              New-Partition -UseMaximumSize -AssignDriveLetter:$false -MbrType IFS | `
              Format-Volume -Confirm:$false -FileSystem NTFS -Force | `
              Get-Partition | `
              Add-PartitionAccessPath -AssignDriveLetter -PassThru | `
              Get-Volume).DriveLetter

    $drive
}

if ($useModule)
{
    $m = New-Module {
        function Test-DrivePresenceFromModule
        {
            param ([string]$Path)

            if (Test-Path $Path)
            {
                "Drive found"
                if (-not (Get-PSDrive -Name $Path[0] -Scope Global -ErrorAction SilentlyContinue))
                {
                    Write-Error "Drive is NOT in Global scope"
                }
            }
            else { Write-Error "$Path not found" }
        }

        Export-ModuleMember -Function Test-DrivePresenceFromModule
    }
}

try
{
    if ($useModule)
    {
        Import-Module $m -Force
    }

    $drive = CreateVHD -VHDPath $VHDPath -Size 5mb
    $pathToCheck = "${drive}:"

    if ($useModule)
    {
        Test-DrivePresenceFromModule -Path $pathToCheck
    }
    else
    {
        if (Test-Path $pathToCheck)
        {
            "Drive found"
            if (-not (Get-PSDrive -Name $drive -Scope Global -ErrorAction SilentlyContinue))
            {
                Write-Error "Drive is NOT in Global scope"
            }
        }
        else { Write-Error "$pathToCheck not found" }
    }
}
finally
{
    if ($useModule)
    {
        Remove-Module $m
    }
    Dismount-VHD $VHDPath
    Remove-Item $VHDPath
}
