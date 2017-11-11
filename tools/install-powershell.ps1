<#
.Synopsis
    Install PowerShell Core on Windows.
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
    [ValidateNotNullOrEmpty()]
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

if (Test-Path -Path $Destination) {
    if ($DoNotOverwrite) {
        throw "Destination folder '$Destination' already exist. Use a different path or omit '-DoNotOverwrite' to overwrite."
    }
    Remove-Item -Path $Destination -Recurse -Force
}
New-Item -ItemType Directory -Path $Destination -Force > $null
$Destination = Resolve-Path -Path $Destination | ForEach-Object -MemberName Path
Write-Verbose "Destination: $Destination" -Verbose

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
    if ($Daily) {
        if (-not (Get-Module -Name PackageManagement -ListAvailable)) {
            throw "PackageManagement module is required to install daily PowerShell Core."
        }

        if ($architecture -ne "x64") {
            throw "The OS architecture is '$architecture'. However, we currently only support daily package for x64 Windows."
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
            "powershell-osx.10.12-x64"
        }

        $package = Find-Package -Source powershell-core-daily -AllowPrereleaseVersions -Name $packageName
        Write-Verbose "Daily package found. Name: $packageName; Version: $($package.Version)" -Verbose
        Install-Package -InputObject $package -Destination $tempDir -ExcludeVersion > $null

        $contentPath = [System.IO.Path]::Combine($tempDir, $packageName, "content")
        Copy-Item -Path $contentPath\* -Destination $Destination -Recurse -Force
    } else {
        $metadata = Invoke-RestMethod https://api.github.com/repos/powershell/powershell/releases/latest
        $release = $metadata.tag_name -replace '^v'

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

        if ($IsWinEnv) {
            Expand-Archive -Path $packagePath -DestinationPath $Destination
        } else {
            tar zxf $packagePath -C $Destination
        }
    }

    ## Change the mode of 'pwsh' to 'rwxr-xr-x' to allow execution
    if (-not $IsWinEnv) { chmod 755 "$Destination/pwsh" }

    if ($AddToPath) {
        if ($IsWinEnv -and (-not $env:Path.Contains($Destination))) {
            ## Add to the User scope 'Path' environment variable
            $userPath = [System.Environment]::GetEnvironmentVariable("Path", "User")
            $userPath = $Destination + [System.IO.Path]::PathSeparator + $userPath
            [System.Environment]::SetEnvironmentVariable("Path", $userPath, "User")
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
} finally {
    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
