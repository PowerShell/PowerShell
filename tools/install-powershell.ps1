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
[CmdletBinding()]
param(
    [Parameter()]
    [string] $Destination,

    [Parameter()]
    [switch] $Daily,

    [Parameter()]
    [switch] $DoNotOverwrite,

    [Parameter()]
    [switch] $AddToPath
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$IsLinuxEnv = (Get-Variable -Name "IsLinux" -ErrorAction Ignore) -and $IsLinux
$IsMacOSEnv = (Get-Variable -Name "IsMacOS" -ErrorAction Ignore) -and $IsMacOS
$IsWinEnv   = !$IsLinuxEnv -and !$IsMacOSEnv

if (-not $Destination) {
    $Destination = if ($IsWinEnv) {
        "$env:LOCALAPPDATA\Microsoft\powershell"
    } else {
        "~/.powershell"
    }

    if ($Daily) {
        $Destination = "${Destination}-daily"
    }
}
$Destination = $PSCmdlet.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Destination)
Write-Verbose "Destination: $Destination" -Verbose

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

$architecture = if (-not $IsWinEnv) {
    "x64"
} else {
    switch ($env:PROCESSOR_ARCHITECTURE) {
        "AMD64" { "x64" }
        "x86"   { "x86" }
        default  { throw "PowerShell package for OS architecture '$_' is not supported." }
    }
}
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $tempDir -Force > $null
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
            Register-PackageSource -Name powershell-core-daily -Location $packageSource -ProviderName nuget -Trusted > $null
        }

        $packageName = if ($IsWinEnv) {
            "powershell-win-x64-win7-x64"
        } elseif ($IsLinuxEnv) {
            "powershell-linux-x64"
        } elseif ($IsMacOSEnv) {
            "powershell-osx-x64"
        }

        $package = Find-Package -Source powershell-core-daily -AllowPrereleaseVersions -Name $packageName
        Write-Verbose "Daily package found. Name: $packageName; Version: $($package.Version)" -Verbose

        Install-Package -InputObject $package -Destination $tempDir -ExcludeVersion > $null
        $contentPath = [System.IO.Path]::Combine($tempDir, $packageName, "content")
    } else {
        $metadata = Invoke-RestMethod https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/metadata.json
        $release = $metadata.ReleaseTag -replace '^v'

        $packageName = if ($IsWinEnv) {
            "PowerShell-${release}-win-${architecture}.zip"
        } elseif ($IsLinuxEnv) {
            "powershell-${release}-linux-${architecture}.tar.gz"
        } elseif ($IsMacOSEnv) {
            "powershell-${release}-osx-${architecture}.tar.gz"
        }

        $downloadURL = "https://github.com/PowerShell/PowerShell/releases/download/v${release}/${packageName}"
        Write-Verbose "About to download package from '$downloadURL'" -Verbose

        $packagePath = Join-Path -Path $tempDir -ChildPath $packageName
        Invoke-WebRequest -Uri $downloadURL -OutFile $packagePath
        $contentPath = Join-Path -Path $tempDir -ChildPath "new"

        New-Item -ItemType Directory -Path $contentPath > $null
        if ($IsWinEnv) {
            Expand-Archive -Path $packagePath -DestinationPath $contentPath
        } else {
            tar zxf $packagePath -C $contentPath
        }
    }
    Remove-Destination $Destination
    if (Test-Path $Destination) {
        Write-Verbose "Copying files" -Verbose
        # only copy files as folders will already exist at $Destination
        Get-ChildItem -Recurse -Path "$contentPath" -File | ForEach-Object {
            $DestinationFilePath = Join-Path $Destination $_.fullname.replace($contentPath,"")
            Copy-Item $_.fullname -Destination $DestinationFilePath
        }
    } else {
        $null = New-Item -Path (Split-Path -Path $Destination -Parent) -ItemType Directory -ErrorAction SilentlyContinue
        Move-Item -Path $contentPath -Destination $Destination
    }

    # Edit icon to disambiguate daily builds.
    if ($IsWinEnv -and $Daily.IsPresent) {
        if (-not (Test-Path "~/.rcedit/rcedit-x64.exe")) {
            Write-Verbose "Install RCEdit for modifying exe resources" -Verbose
            $rceditUrl = "https://github.com/electron/rcedit/releases/download/v1.0.0/rcedit-x64.exe"
            New-Item -Path "~/.rcedit" -Type Directory -Force > $null
            Invoke-WebRequest -OutFile "~/.rcedit/rcedit-x64.exe" -Uri $rceditUrl
        }

        Write-Verbose "Change icon to disambiguate it from a released installation" -Verbose
        & "~/.rcedit/rcedit-x64.exe" "$Destination\pwsh.exe" --set-icon "$Destination\assets\Powershell_av_colors.ico"
    }

    ## Change the mode of 'pwsh' to 'rwxr-xr-x' to allow execution
    if (-not $IsWinEnv) { chmod 755 $Destination/pwsh }

    if ($AddToPath) {
        if ($IsWinEnv -and (-not [System.Environment]::GetEnvironmentVariable("Path", "Machine").Contains($Destination))) {
            ## Add to the Machine scope 'Path' environment variable
            $machinePath = [System.Environment]::GetEnvironmentVariable("Path", "Machine")
            $machinePath = $Destination + [System.IO.Path]::PathSeparator + $machinePath
            [System.Environment]::SetEnvironmentVariable("Path", $machinePath, "Machine")
            Write-Verbose "'$Destination' is added to the Path" -Verbose
        }

        if (-not $IsWinEnv) {
            $targetPath = Join-Path -Path $Destination -ChildPath "pwsh"
            $symlink = if ($IsLinuxEnv) { "/usr/bin/pwsh" } elseif ($IsMacOSEnv) { "/usr/local/bin/pwsh" }
            $needNewSymlink = $true

            if (Test-Path -Path $symlink) {
                $linkItem = Get-Item -Path $symlink
                if ($linkItem.LinkType -ne "SymbolicLink") {
                    Write-Warning "'$symlink' already exists but it's not a symbolic link. Abort adding to PATH."
                    $needNewSymlink = $false
                }
                elseif ($linkItem.Target -contains $targetPath) {
                    ## The link already points to the target
                    Write-Verbose "'$symlink' already points to '$targetPath'" -Verbose
                    $needNewSymlink = $false
                }
            }

            if ($needNewSymlink) {
                $uid = id -u
                $SUDO = if ($uid -ne "0") { "sudo" } else { "" }

                Write-Verbose "Make symbolic link '$symlink' point to '$targetPath'..." -Verbose
                Invoke-Expression -Command "$SUDO ln -fs $targetPath $symlink"

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

    Write-Host "PowerShell Core has been installed at $Destination" -ForegroundColor Green
    if ($Destination -eq $PSHome) {
        Write-Host "Please restart pwsh" -ForegroundColor Magenta
    }
} finally {
    # Restore original value
    [Net.ServicePointManager]::SecurityProtocol = $originalValue

    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
