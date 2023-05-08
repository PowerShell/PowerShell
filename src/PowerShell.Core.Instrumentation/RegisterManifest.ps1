# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
.Synopsis
    Registers or unregisters the PowerShell ETW manifest
.Parameter Path
    The fully qualified path to the PowerShell.Core.Instrumentation.man manifest file.
    The default value is the location of this script.

.Parameter Unregister
    Specify to unregister the manifest.
.Notes
    The PowerShell.Core.Instrumentation.man and PowerShell.Core.Instrumentation.dll files are
    expected to be at the location specified by the Path parameter.
    When registered, PowerShell.Core.Instrumentation.dll is locked to prevent deleting or changing.
    To update the binary, first unregister the manifest using the -Unregister switch.
#>
[CmdletBinding()]
param
(
    [ValidateNotNullOrEmpty()]
    [string] $Path = $PSScriptRoot,

    [switch] $Unregister
)
Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

function Start-NativeExecution([scriptblock]$sb, [switch]$IgnoreExitcode)
{
    $backupEAP = $script:ErrorActionPreference
    $script:ErrorActionPreference = "Continue"
    try
    {
        & $sb
        # note, if $sb doesn't have a native invocation, $LASTEXITCODE will
        # point to the obsolete value
        if ($LASTEXITCODE -ne 0 -and -not $IgnoreExitcode)
        {
            throw "Execution of {$sb} failed with exit code $LASTEXITCODE"
        }
    }
    finally
    {
        $script:ErrorActionPreference = $backupEAP
    }
}

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

$manifest = Get-Item -Path (Join-Path -Path $Path -ChildPath 'PowerShell.Core.Instrumentation.man')
$binary = Get-Item -Path (Join-Path -Path $Path -ChildPath 'PowerShell.Core.Instrumentation.dll')

$files = @($manifest, $binary)
foreach ($file in $files)
{
    if (-not (Test-Path -Path $file))
    {
        throw "Could not find $($file.Name) at $Path"
    }
}

[string] $command = 'wevtutil um "{0}"' -f $manifest.FullName

# Unregister if present. Avoids warnings when registering the manifest
# and it is already registered.
Write-Verbose "unregister the manifest, if present: $command"
Start-NativeExecution {Invoke-Expression $command} $true

if (-not $Unregister)
{
    $command = 'wevtutil.exe im "{0}" /rf:"{1}" /mf:"{1}"' -f $manifest.FullName, $binary.FullName
    Write-Verbose -Message "Register the manifest: $command"
    Start-NativeExecution { Invoke-Expression $command }
}
