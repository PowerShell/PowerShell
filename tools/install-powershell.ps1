# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
<#
.Synopsis
    Install PowerShell Core on Windows, Linux or macOS.
.DESCRIPTION
    By default, the latest PowerShell Core release package will be installed.
    If '-Daily' is specified, then the latest PowerShell Core daily package will be installed.
.Parameter Destination
    The destination path to install PowerShell Core to.
.Parameter Daily
    Install PowerShell Core from the daily build.
    Note that the 'PackageManagement' module is required to install a daily package.
.Parameter DoNotOverwrite
    Do not overwrite the destination folder if it already exists.
.Parameter AddToPath
    On Windows, add the absolute destination path to the 'User' scope environment variable 'Path';
    On Linux, make the symlink '/usr/bin/pwsh' points to "$Destination/pwsh";
    On MacOS, make the symlink '/usr/local/bin/pwsh' points to "$Destination/pwsh".
#>
[CmdletBinding(DefaultParameterSetName = "Daily")]
param(
    [Parameter(ParameterSetName = "Daily")]
    [string] $Destination,

    [Parameter(ParameterSetName = "Daily")]
    [switch] $Daily,

    [Parameter(ParameterSetName = "Daily")]
    [switch] $DoNotOverwrite,

    [Parameter(ParameterSetName = "Daily")]
    [switch] $AddToPath,

    [Parameter(ParameterSetName = "MSI")]
    [switch] $UseMSI,

    [Parameter(ParameterSetName = "MSI")]
    [switch] $Quiet,

    [Parameter()]
    [switch] $Preview
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$IsLinuxEnv = (Get-Variable -Name "IsLinux" -ErrorAction Ignore) -and $IsLinux
$IsMacOSEnv = (Get-Variable -Name "IsMacOS" -ErrorAction Ignore) -and $IsMacOS
$IsWinEnv = !$IsLinuxEnv -and !$IsMacOSEnv

if (-not $Destination) {
    if ($IsWinEnv) {
        $Destination = "$env:LOCALAPPDATA\Microsoft\powershell"
    } else {
        $Destination = "~/.powershell"
    }

    if ($Daily) {
        $Destination = "${Destination}-daily"
    }
}

$Destination = $PSCmdlet.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Destination)

if (-not $UseMSI) {
    Write-Verbose "Destination: $Destination" -Verbose
} else {
    if (-not $IsWinEnv) {
        throw "-UseMSI is only supported on Windows"
    }
}

# Expand an archive using Expand-archive when available
# and the DotNet API when it is not
function Expand-ArchiveInternal {
    [CmdletBinding()]
    param(
        $Path,
        $DestinationPath
    )

    if((Get-Command -Name Expand-Archive -ErrorAction Ignore))
    {
        Expand-Archive -Path $Path -DestinationPath $DestinationPath
    }
    else
    {
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $resolvedPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
        $resolvedDestinationPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($DestinationPath)
        [System.IO.Compression.ZipFile]::ExtractToDirectory($resolvedPath,$resolvedDestinationPath)
    }
}

Function Remove-Destination([string] $Destination) {
    if (Test-Path -Path $Destination) {
        if ($DoNotOverwrite) {
            throw "Destination folder '$Destination' already exist. Use a different path or omit '-DoNotOverwrite' to overwrite."
        }
        Write-Verbose "Removing old installation: $Destination" -Verbose
        if (Test-Path -Path "$Destination.old") {
            Remove-Item "$Destination.old" -Recurse -Force
        }
        if ($IsWinEnv -and ($Destination -eq $PSHome)) {
            # handle the case where the updated folder is currently in use
            Get-ChildItem -Recurse -File -Path $PSHome | ForEach-Object {
                if ($_.extension -eq "old") {
                    Remove-Item $_
                } else {
                    Move-Item $_.fullname "$($_.fullname).old"
                }
            }
        } else {
            # Unix systems don't keep open file handles so you can just move files/folders even if in use
            Move-Item "$Destination" "$Destination.old"
        }
    }
}

<#
.Synopsis
    Adds a Path to settings (Supports Windows Only)
.DESCRIPTION
    Adds the target path to the target registry.
.Parameter Path
    The path to add to the registry. It is validated with ValidatePathNotInSettings which ensures that:
    -The path exists
    -Is a directory
    -Is not in the registry (HKCU or HKLM)
.Parameter Target
    The target hive to install the Path to.
    Must be either User or Machine
    Defaults to User
#>
Function Add-PathTToSettings {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [ValidateNotNullOrEmpty()]
        [ValidatePathNotInSettings()]
        [string] $Path,

        [Parameter(ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [ValidateNotNullOrEmpty()]
        [ValidateSet([System.EnvironmentVariableTarget]::User, [System.EnvironmentVariableTarget]::Machine)]
        [System.EnvironmentVariableTarget] $Target = ([System.EnvironmentVariableTarget]::User)
    )

    if (-not $IsWinEnv) {
        return
    }

    if ($Target -eq [System.EnvironmentVariableTarget]::User) {
        [string] $Environment = 'Environment'
        [Microsoft.Win32.RegistryKey] $Key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($Environment, [Microsoft.Win32.RegistryKeyPermissionCheck]::ReadWriteSubTree)
    } else {
        [string] $Environment = 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment'
        [Microsoft.Win32.RegistryKey] $Key = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey($Environment, [Microsoft.Win32.RegistryKeyPermissionCheck]::ReadWriteSubTree)
    }

    # $key is null here if it the user was unable to get ReadWriteSubTree access.
    if ($null -eq $Key) {
        throw (new-object -typeName 'System.Security.SecurityException' -ArgumentList "Unable to access the target registry")
    }

    # Get current unexpanded value
    [string] $CurrentUnexpandedValue = $Key.GetValue('PATH', '', [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)

    # Keep current PathValueKind if possible/appropriate
    try {
        [Microsoft.Win32.RegistryValueKind] $PathValueKind = $Key.GetValueKind('PATH')
    } catch {
        [Microsoft.Win32.RegistryValueKind] $PathValueKind = [Microsoft.Win32.RegistryValueKind]::ExpandString
    }

    # Evaluate new path
    $NewPathValue = [string]::Concat($CurrentUnexpandedValue.TrimEnd([System.IO.Path]::PathSeparator), [System.IO.Path]::PathSeparator, $Path)

    # Upgrade PathValueKind to [Microsoft.Win32.RegistryValueKind]::ExpandString if appropriate
    if ($NewPathValue.Contains('%')) { $PathValueKind = [Microsoft.Win32.RegistryValueKind]::ExpandString }

    $Key.SetValue("PATH", $NewPathValue, $PathValueKind)
}

if (-not $IsWinEnv) {
    $architecture = "x64"
} else {
    switch ($env:PROCESSOR_ARCHITECTURE) {
        "AMD64" { $architecture = "x64" }
        "x86" { $architecture = "x86" }
        default { throw "PowerShell package for OS architecture '$_' is not supported." }
    }
}
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $tempDir -Force -ErrorAction SilentlyContinue
try {
    # Setting Tls to 12 to prevent the Invoke-WebRequest : The request was
    # aborted: Could not create SSL/TLS secure channel. error.
    $originalValue = [Net.ServicePointManager]::SecurityProtocol
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    if ($Daily) {
        if (-not (Get-Module -Name PackageManagement -ListAvailable)) {
            throw "PackageManagement module is required to install daily PowerShell Core."
        }

        if ($architecture -ne "x64") {
            throw "The OS architecture is '$architecture'. However, we currently only support daily package for x64."
        }

        ## Register source if not yet
        if (-not (Get-PackageSource -Name powershell-core-daily -ErrorAction SilentlyContinue)) {
            $packageSource = "https://powershell.myget.org/F/powershell-core-daily"
            Write-Verbose "Register powershell-core-daily package source '$packageSource' with PackageManagement" -Verbose
            Register-PackageSource -Name powershell-core-daily -Location $packageSource -ProviderName nuget -Trusted -ErrorAction SilentlyContinue
        }

        if ($IsWinEnv) {
            $packageName = "powershell-win-x64-win7-x64"
        } elseif ($IsLinuxEnv) {
            $packageName = "powershell-linux-x64"
        } elseif ($IsMacOSEnv) {
            $packageName = "powershell-osx-x64"
        }

        $package = Find-Package -Source powershell-core-daily -AllowPrereleaseVersions -Name $packageName
        Write-Verbose "Daily package found. Name: $packageName; Version: $($package.Version)" -Verbose

        Install-Package -InputObject $package -Destination $tempDir -ExcludeVersion -ErrorAction SilentlyContinue
        $contentPath = [System.IO.Path]::Combine($tempDir, $packageName, "content")
    } else {
        $metadata = Invoke-RestMethod https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/metadata.json
        if ($Preview) {
            $release = $metadata.PreviewReleaseTag -replace '^v'
        } else {
            $release = $metadata.ReleaseTag -replace '^v'
        }

        if ($IsWinEnv) {
            if ($UseMSI) {
                $packageName = "PowerShell-${release}-win-${architecture}.msi"
            } else {
                $packageName = "PowerShell-${release}-win-${architecture}.zip"
            }
        } elseif ($IsLinuxEnv) {
            $packageName = "powershell-${release}-linux-${architecture}.tar.gz"
        } elseif ($IsMacOSEnv) {
            $packageName = "powershell-${release}-osx-${architecture}.tar.gz"
        }

        $downloadURL = "https://github.com/PowerShell/PowerShell/releases/download/v${release}/${packageName}"
        Write-Verbose "About to download package from '$downloadURL'" -Verbose

        $packagePath = Join-Path -Path $tempDir -ChildPath $packageName
        if (!$PSVersionTable.ContainsKey('PSEdition') -or $PSVersionTable.PSEdition -eq "Desktop") {
            # On Windows PowerShell, progress can make the download significantly slower
            $oldProgressPreference = $ProgressPreference
            $ProgressPreference = "SilentlyContinue"
        }

        try {
            Invoke-WebRequest -Uri $downloadURL -OutFile $packagePath
        } finally {
            if (!$PSVersionTable.ContainsKey('PSEdition') -or $PSVersionTable.PSEdition -eq "Desktop") {
                $ProgressPreference = $oldProgressPreference
            }
        }

        $contentPath = Join-Path -Path $tempDir -ChildPath "new"

        New-Item -ItemType Directory -Path $contentPath -ErrorAction SilentlyContinue
        if ($IsWinEnv) {
            if ($UseMSI -and $Quiet) {
                Write-Verbose "Performing quiet install"
                $process = Start-Process msiexec -ArgumentList "/i", $packagePath, "/quiet" -Wait -PassThru
                if ($process.exitcode -ne 0) {
                    throw "Quiet install failed, please rerun install without -Quiet switch or ensure you have administrator rights"
                }
            } elseif ($UseMSI) {
                Start-Process $packagePath -Wait
            } else {
                Expand-ArchiveInternal -Path $packagePath -DestinationPath $contentPath
            }
        } else {
            tar zxf $packagePath -C $contentPath
        }
    }

    if (-not $UseMSI) {
        Remove-Destination $Destination
        if (Test-Path $Destination) {
            Write-Verbose "Copying files" -Verbose
            # only copy files as folders will already exist at $Destination
            Get-ChildItem -Recurse -Path "$contentPath" -File | ForEach-Object {
                $DestinationFilePath = Join-Path $Destination $_.fullname.replace($contentPath, "")
                Copy-Item $_.fullname -Destination $DestinationFilePath
            }
        } else {
            $null = New-Item -Path (Split-Path -Path $Destination -Parent) -ItemType Directory -ErrorAction SilentlyContinue
            Move-Item -Path $contentPath -Destination $Destination
        }
    }

    # Edit icon to disambiguate daily builds.
    if ($IsWinEnv -and $Daily.IsPresent) {
        if (-not (Test-Path "~/.rcedit/rcedit-x64.exe")) {
            Write-Verbose "Install RCEdit for modifying exe resources" -Verbose
            $rceditUrl = "https://github.com/electron/rcedit/releases/download/v1.0.0/rcedit-x64.exe"
            New-Item -Path "~/.rcedit" -Type Directory -Force -ErrorAction SilentlyContinue
            Invoke-WebRequest -OutFile "~/.rcedit/rcedit-x64.exe" -Uri $rceditUrl
        }

        Write-Verbose "Change icon to disambiguate it from a released installation" -Verbose
        & "~/.rcedit/rcedit-x64.exe" "$Destination\pwsh.exe" --set-icon "$Destination\assets\Powershell_avatar.ico"
    }

    ## Change the mode of 'pwsh' to 'rwxr-xr-x' to allow execution
    if (-not $IsWinEnv) { chmod 755 $Destination/pwsh }

    if ($AddToPath -and -not $UseMSI) {
        if ($IsWinEnv) {
            if ((-not ($Destination.StartsWith($ENV:USERPROFILE))) -and
                (-not ($Destination.StartsWith($ENV:APPDATA))) -and
                (-not ($Destination.StartsWith($env:LOCALAPPDATA)))) {
                $TargetRegistry = [System.EnvironmentVariableTarget]::Machine
                try {
                    Add-PathTToSettings -Path $Destination -Target $TargetRegistry
                } catch {
                    Write-Verbose -Message "Unable to save the new path in the machine wide registry."
                    $TargetRegistry = [System.EnvironmentVariableTarget]::User
                }
            } else {
                $TargetRegistry = [System.EnvironmentVariableTarget]::User
            }

            # If failed to install to machine wide path or path was not appropriate for machine wide path
            if ($TargetRegistry -eq [System.EnvironmentVariableTarget]::User) {
                try {
                    Add-PathTToSettings -Path $Destination -Target $TargetRegistry
                } catch {
                    Write-Verbose -Message "Unable to save the new path in the registry for the current user"
                }
            }
        } else {
            $targetPath = Join-Path -Path $Destination -ChildPath "pwsh"
            if ($IsLinuxEnv) { $symlink = "/usr/bin/pwsh" } elseif ($IsMacOSEnv) { $symlink = "/usr/local/bin/pwsh" }
            $needNewSymlink = $true

            if (Test-Path -Path $symlink) {
                $linkItem = Get-Item -Path $symlink
                if ($linkItem.LinkType -ne "SymbolicLink") {
                    Write-Warning "'$symlink' already exists but it's not a symbolic link. Abort adding to PATH."
                    $needNewSymlink = $false
                } elseif ($linkItem.Target -contains $targetPath) {
                    ## The link already points to the target
                    Write-Verbose "'$symlink' already points to '$targetPath'" -Verbose
                    $needNewSymlink = $false
                }
            }

            if ($needNewSymlink) {
                $uid = id -u
                if ($uid -ne "0") { $SUDO = "sudo" } else { $SUDO = "" }

                Write-Verbose "Make symbolic link '$symlink' point to '$targetPath'..." -Verbose
                & $SUDO ln -fs $targetPath $symlink

                if ($LASTEXITCODE -ne 0) {
                    Write-Error "Could not add to PATH: failed to make '$symlink' point to '$targetPath'."
                }
            }
        }

        ## Add to the current process 'Path' if the process is not 'pwsh'
        $runningProcessName = (Get-Process -Id $PID).ProcessName
        if ($runningProcessName -ne 'pwsh') {
            $env:Path = $Destination + [System.IO.Path]::PathSeparator + $env:Path
        }
    }

    if (-not $UseMSI) {
        Write-Host "PowerShell Core has been installed at $Destination" -ForegroundColor Green
        if ($Destination -eq $PSHome) {
            Write-Host "Please restart pwsh" -ForegroundColor Magenta
        }
    }
} finally {
    # Restore original value
    [Net.ServicePointManager]::SecurityProtocol = $originalValue

    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
