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
    Add the absolute destination path to the 'User' scope environment variable 'Path'
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

if (-not $Destination) {
    $Destination = "$env:LOCALAPPDATA\Microsoft\powershell"
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

$architecture = switch ($env:PROCESSOR_ARCHITECTURE) {
                    "AMD64" { "x64" }
                    "x86"   { "x86" }
                    default  { throw "PowerShell package for OS architecture '$_' is not supported." }
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

        $packageName = "powershell-win-x64-win7-x64"
        $package = Find-Package -Source powershell-core-daily -AllowPrereleaseVersions -Name $packageName
        Write-Verbose "Daily package found. Name: $packageName; Version: $($package.Version)" -Verbose
        Install-Package -InputObject $package -Destination $tempDir -ExcludeVersion > $null

        $contentPath = [System.IO.Path]::Combine($tempDir, $packageName, "content")
        Copy-Item -Path $contentPath\* -Destination $Destination -Recurse -Force
    } else {
        $metadata = Invoke-RestMethod https://api.github.com/repos/powershell/powershell/releases/latest
        $release = $metadata.tag_name -replace '^v'

        $packageName = "PowerShell-${release}-win-${architecture}.zip"
        $downloadURL = "https://github.com/PowerShell/PowerShell/releases/download/v${release}/${packageName}"
        Write-Verbose "About to download package from '$downloadURL'" -Verbose

        $packagePath = Join-Path -Path $tempDir -ChildPath $packageName
        Invoke-WebRequest -Uri $downloadURL -OutFile $packagePath

        Expand-Archive -Path $packagePath -DestinationPath $Destination
    }

    if ($AddToPath -and (-not $env:Path.Contains($Destination))) {
        ## Add to the User scope 'Path' environment variable
        $userPath = [System.Environment]::GetEnvironmentVariable("Path", "User")
        $userPath = $Destination + [System.IO.Path]::PathSeparator + $userPath
        [System.Environment]::SetEnvironmentVariable("Path", $userPath, "User")

        ## Add to the 'Path' for the current process
        $env:Path = $Destination + [System.IO.Path]::PathSeparator + $env:Path
        Write-Verbose "'$Destination' is added to Path" -Verbose
    }

    Write-Host "PowerShell Core has been installed at $Destination" -ForegroundColor Green
} finally {
    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
