#Requires -Version 7.0

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
.SYNOPSIS
    Registers or unregisters the PowerShell ETW manifest
.PARAMETER Path
    The fully qualified path to the PowerShell.Core.Instrumentation.man manifest file.
    The default value is the location of this script.

.PARAMETER Unregister
    Specify to unregister the manifest.

.NOTES
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

# Timeout for wevtutil operations (seconds)
$wevtutilTimeoutSeconds = 60

# Publisher GUID from the manifest - used for idempotent checks
$publisherGuid = '{f90714a8-5509-434a-bf6d-b1624c8a19a2}'

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

function Invoke-NativeProcess {
    <#
    .SYNOPSIS
        Runs a native process and returns the exit code.
    .PARAMETER FilePath
        The path to the executable to run.
    .PARAMETER Arguments
        The arguments to pass to the executable.
    .OUTPUTS
        Returns the process exit code.
    #>
    param(
        [string]$FilePath,
        [string]$Arguments
    )

    $tempDir = [System.IO.Path]::GetTempPath()
    $outFile = Join-Path $tempDir "wevtutil-stdout-$([System.IO.Path]::GetRandomFileName())"
    $errFile = Join-Path $tempDir "wevtutil-stderr-$([System.IO.Path]::GetRandomFileName())"
    $exitCode = $null
    try {
        $process = Start-Process -FilePath $FilePath -ArgumentList $Arguments -NoNewWindow -PassThru -RedirectStandardOutput $outFile -RedirectStandardError $errFile
        $process.WaitForExit()
        $exitCode = $process.ExitCode

        # Only clean up temp files on success; keep them for debugging on failure
        if ($exitCode -eq 0) {
            Remove-Item $outFile, $errFile -Force -ErrorAction SilentlyContinue
        }
    }
    finally {
        # Ensure temp files are not leaked when the process is interrupted (e.g. job timeout)
        if ($null -eq $exitCode) {
            Remove-Item $outFile, $errFile -Force -ErrorAction SilentlyContinue
        }
    }

    return $exitCode
}

function Invoke-WevtutilWithTimeout {
    <#
    .SYNOPSIS
        Runs wevtutil with a timeout to prevent hangs.
    .PARAMETER Arguments
        The arguments to pass to wevtutil.
    .PARAMETER TimeoutSeconds
        Maximum time to wait for wevtutil to complete.
    .PARAMETER IgnoreExitCode
        If set, non-zero exit codes are not treated as errors.
    .OUTPUTS
        Returns $true if completed successfully, $false otherwise.
    #>
    param(
        [string]$Arguments,
        [int]$TimeoutSeconds = 60,
        [switch]$IgnoreExitCode
    )

    $wevtutilPath = Join-Path $env:SystemRoot 'System32\wevtutil.exe'
    if (-not (Test-Path $wevtutilPath)) {
        Write-Verbose "EventManifest: wevtutil.exe not found at $wevtutilPath" -Verbose
        return $false
    }

    try {
        Write-Verbose "EventManifest: running wevtutil.exe $Arguments (timeout: ${TimeoutSeconds}s)" -Verbose

        # Use Start-ThreadJob with timeout for idiomatic PowerShell with timeout support
        $job = Start-ThreadJob -ScriptBlock ${function:Invoke-NativeProcess} -ArgumentList $wevtutilPath, $Arguments

        $completed = Wait-Job -Job $job -Timeout $TimeoutSeconds

        if ($completed) {
            $exitCode = Receive-Job -Job $job
            Remove-Job -Job $job -Force

            if ($exitCode -ne 0 -and -not $IgnoreExitCode) {
                Write-Verbose "EventManifest: wevtutil failed with exit code $exitCode" -Verbose
                return $false
            }
            return $true
        }
        else {
            Write-Verbose "EventManifest: wevtutil timed out after $TimeoutSeconds seconds" -Verbose
            Stop-Job -Job $job
            Remove-Job -Job $job -Force
            return $false
        }
    }
    catch {
        Write-Verbose "EventManifest: error running wevtutil - $_" -Verbose
        return $false
    }
}

function Test-ManifestRegistered {
    <#
    .SYNOPSIS
        Checks if the PowerShell event manifest is already registered.
    .DESCRIPTION
        Queries the registry for the publisher GUID to determine if registration is needed.
    .OUTPUTS
        Returns $true if registered, $false if not, $null if unable to determine.
    #>
    try {
        # Check if the publisher is registered in the event log registry
        $publisherKey = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WINEVT\Publishers\$publisherGuid"
        if (Test-Path $publisherKey) {
            Write-Verbose "EventManifest: publisher $publisherGuid found in registry" -Verbose
            return $true
        }
        return $false
    }
    catch {
        Write-Verbose "EventManifest: unable to check registry - $_" -Verbose
        return $null
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

# Resolve path for security validation - prevents directory traversal attacks
# If path doesn't exist, we'll handle it gracefully when checking for files
$resolvedPath = $Path
if (Test-Path -Path $Path -PathType Leaf) {
    throw "Path parameter must be a directory, not a file: $Path"
}
elseif (Test-Path -Path $Path -PathType Container) {
    # Path is a directory; resolve to its canonical form
    $resolvedPath = (Resolve-Path -Path $Path).Path
}

$manifest = Join-Path -Path $resolvedPath -ChildPath 'PowerShell.Core.Instrumentation.man'
$binary = Join-Path -Path $resolvedPath -ChildPath 'PowerShell.Core.Instrumentation.dll'

$files = @($manifest, $binary)
foreach ($file in $files)
{
    if (-not (Test-Path -Path $file))
    {
        Write-Verbose "EventManifest: could not find $file, skipping registration" -Verbose
        exit 0
    }
}

if ($Unregister)
{
    # During uninstall, attempt to unregister but don't block if it fails
    # An orphaned registration is better than a hung uninstall
    Write-Verbose "EventManifest: attempting to unregister manifest" -Verbose
    $unregisterArgs = 'um "{0}"' -f $manifest
    $result = Invoke-WevtutilWithTimeout -Arguments $unregisterArgs -TimeoutSeconds $wevtutilTimeoutSeconds -IgnoreExitCode
    if ($result) {
        Write-Verbose "EventManifest: unregistration completed" -Verbose
    }
    else {
        Write-Verbose "EventManifest: unregistration failed or timed out, continuing" -Verbose
    }
    exit 0
}

# Installation path: check if already registered (idempotent)
$isRegistered = Test-ManifestRegistered
if ($isRegistered -eq $true) {
    Write-Verbose "EventManifest: already registered, skipping" -Verbose
    exit 0
}
elseif ($null -eq $isRegistered) {
    Write-Verbose "EventManifest: unable to determine registration status, proceeding with registration" -Verbose
}

# Attempt to unregister first to avoid warnings during registration
# This is best-effort; we continue even if it fails
Write-Verbose "EventManifest: unregistering any existing manifest" -Verbose
$unregisterArgs = 'um "{0}"' -f $manifest
$null = Invoke-WevtutilWithTimeout -Arguments $unregisterArgs -TimeoutSeconds $wevtutilTimeoutSeconds -IgnoreExitCode

# Now attempt to register the manifest
Write-Verbose "EventManifest: registering manifest" -Verbose
$registerArgs = 'im "{0}" /rf:"{1}" /mf:"{1}"' -f $manifest, $binary
$result = Invoke-WevtutilWithTimeout -Arguments $registerArgs -TimeoutSeconds $wevtutilTimeoutSeconds

if ($result) {
    Write-Verbose "EventManifest: registration completed successfully" -Verbose
}
else {
    Write-Verbose "EventManifest: registration failed or timed out, continuing installation" -Verbose
}

# Always exit 0 so the MSI can complete
exit 0
