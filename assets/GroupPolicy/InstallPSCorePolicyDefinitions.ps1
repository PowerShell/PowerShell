# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
.Synopsis
    Group Policy tools use administrative template files (.admx, .adml) to populate policy settings in the user interface.
    This allows administrators to manage registry-based policy settings.
    This script installes PowerShell Core Administrative Templates for Windows.
.Notes
    The PowerShellCoreExecutionPolicy.admx and PowerShellCoreExecutionPolicy.adml files are
    expected to be at the location specified by the Path parameter with default value of the location of this script.
#>
[CmdletBinding()]
param
(
    [ValidateNotNullOrEmpty()]
    [string] $Path = $PSScriptRoot
)
Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

function Test-Elevated
{
    [CmdletBinding()]
    [OutputType([bool])]
    Param()

    # if the current Powershell session was called with administrator privileges,
    # the Administrator Group's well-known SID will show up in the Groups for the current identity.
    # Note that the SID won't show up unless the process is elevated.
    return (([Security.Principal.WindowsIdentity]::GetCurrent()).Groups -contains "S-1-5-32-544")
}
$IsWindowsOs = $PSHOME.EndsWith('\WindowsPowerShell\v1.0', [System.StringComparison]::OrdinalIgnoreCase) -or $IsWindows

if (-not $IsWindowsOs)
{
    throw 'This script must be run on Windows.'
}

if (-not (Test-Elevated))
{
    throw 'This script must be run from an elevated process.'
}

if ([System.Management.Automation.Platform]::IsNanoServer)
{
    throw 'Group policy definitions are not supported on Nano Server.'
}

$admxName = 'PowerShellCoreExecutionPolicy.admx'
$admlName = 'PowerShellCoreExecutionPolicy.adml'
$admx = Get-Item -Path (Join-Path -Path $Path -ChildPath $admxName)
$adml = Get-Item -Path (Join-Path -Path $Path -ChildPath $admlName)
$admxTargetPath = Join-Path -Path $env:WINDIR -ChildPath "PolicyDefinitions"
$admlTargetPath = Join-Path -Path $admxTargetPath -ChildPath "en-US"

$files = @($admx, $adml)
foreach ($file in $files)
{
    if (-not (Test-Path -Path $file))
    {
        throw "Could not find $($file.Name) at $Path"
    }
}

Write-Verbose "Copying $admx to $admxTargetPath"
Copy-Item -Path $admx -Destination $admxTargetPath -Force
$admxTargetFullPath = Join-Path -Path $admxTargetPath -ChildPath $admxName
if (Test-Path -Path $admxTargetFullPath)
{
    Write-Verbose "$admxName was installed successfully"
}
else
{
    Write-Error "Could not install $admxName"
}

Write-Verbose "Copying $adml to $admlTargetPath"
Copy-Item -Path $adml -Destination $admlTargetPath -Force
$admlTargetFullPath = Join-Path -Path $admlTargetPath -ChildPath $admlName
if (Test-Path -Path $admlTargetFullPath)
{
    Write-Verbose "$admlName was installed successfully"
}
else
{
    Write-Error "Could not install $admlName"
}
