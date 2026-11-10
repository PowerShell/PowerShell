[Diagnostics.CodeAnalysis.SuppressMessageAttribute(
    'PSAvoidUsingWriteHost',
    '',
    Justification = 'The script writes diagnostic data and Azure Pipelines logging commands to the task host.'
)]
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

function Write-DiagnosticTable {
    param(
        [Parameter(Mandatory)]
        [string]
        $Title,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]
        $InputObject
    )

    Write-Host $Title
    if ($InputObject.Count -eq 0) {
        Write-Host '  None'
        return
    }

    $InputObject | Format-Table -AutoSize | Out-String -Width 9999 | Write-Host
}

function Get-SanitizedUri {
    param(
        [AllowNull()]
        [string]
        $Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return '<not set>'
    }

    try {
        $uri = [uri]$Value
        return '{0}://{1}:{2}' -f $uri.Scheme, $uri.Host, $uri.Port
    } catch {
        return '<set but not a valid URI>'
    }
}

function Show-ArchiveDiagnostic {
    param(
        [Parameter(Mandatory)]
        [string]
        $Path,

        [Parameter(Mandatory)]
        [string]
        $Label
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        Write-Host "$Label file does not exist: $Path"
        return
    }

    $file = Get-Item -LiteralPath $Path
    $hash = Get-FileHash -LiteralPath $Path -Algorithm SHA256
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $header = [byte[]]::new([Math]::Min(4, $stream.Length))
        $null = $stream.Read($header, 0, $header.Length)
    } finally {
        $stream.Dispose()
    }

    $headerHex = ($header | ForEach-Object { $_.ToString('X2') }) -join ' '
    $zipIsValid = $false
    $zipError = $null
    try {
        $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
        $zipIsValid = $true
        $archive.Dispose()
    } catch {
        $zipError = $_.Exception.Message
    }

    Write-Host "$Label path: $Path"
    Write-Host "$Label length: $($file.Length)"
    Write-Host "$Label SHA-256: $($hash.Hash)"
    Write-Host "$Label first bytes: $headerHex"
    Write-Host "$Label is valid ZIP: $zipIsValid"
    if ($zipError) {
        Write-Host "$Label ZIP error: $zipError"
    }
}

function Invoke-PackageDownloadProbe {
    param(
        [Parameter(Mandatory)]
        [string]
        $Name,

        [Parameter(Mandatory)]
        [version]
        $Version,

        [Parameter(Mandatory)]
        [string]
        $RepositorySourceLocation,

        [Parameter(Mandatory)]
        [string]
        $DestinationDirectory
    )

    $packageUri = '{0}?id={1}&version={2}' -f $RepositorySourceLocation.TrimEnd('/'), $Name.ToLowerInvariant(), $Version
    $packagePath = Join-Path $DestinationDirectory "${Name}.${Version}.nupkg"

    Write-Host "Testing direct download for $Name $Version from host '$(([uri]$packageUri).Host)'."
    try {
        $response = Invoke-WebRequest -Uri $packageUri -OutFile $packagePath -PassThru -MaximumRedirection 10
        $finalUri = $response.BaseResponse.RequestMessage.RequestUri
        $contentType = $response.Headers.'Content-Type' -join ', '

        Write-Host "Direct download status: $([int]$response.StatusCode)"
        Write-Host "Direct download final host: $($finalUri.Host)"
        Write-Host "Direct download content type: $contentType"
        Show-ArchiveDiagnostic -Path $packagePath -Label "$Name $Version direct download"
    } catch {
        Write-Warning "Direct download probe for $Name $Version failed: $($_.Exception.Message)"
        if (Test-Path -LiteralPath $packagePath) {
            Show-ArchiveDiagnostic -Path $packagePath -Label "$Name $Version partial direct download"
        }
    }
}

function Show-RepositoryDiagnostic {
    param(
        [Parameter(Mandatory)]
        [string]
        $Phase
    )

    $repositories = @(
        Get-PSRepository -ErrorAction Continue |
            Select-Object Name, SourceLocation, ScriptSourceLocation, PackageManagementProvider, InstallationPolicy
    )
    Write-DiagnosticTable -Title "PowerShell repositories $Phase configuration:" -InputObject $repositories

    $packageSources = @(
        Get-PackageSource -ErrorAction Continue |
            Select-Object Name, Location, ProviderName, IsTrusted, IsRegistered
    )
    Write-DiagnosticTable -Title "PackageManagement sources $Phase configuration:" -InputObject $packageSources
}

Write-Host 'PowerShell runtime:'
$PSVersionTable | Format-List | Out-String -Width 9999 | Write-Host

$installModuleCommand = @(
    Get-Command Install-Module |
        Select-Object Name, Source, Version, ModuleName
)
Write-DiagnosticTable -Title 'Install-Module command:' -InputObject $installModuleCommand

$packageModules = @(
    Get-Module PowerShellGet, PackageManagement -ListAvailable |
        Sort-Object Name, Version -Descending |
        Select-Object Name, Version, ModuleBase
)
Write-DiagnosticTable -Title 'Available package-management modules:' -InputObject $packageModules

$packageProviders = @(
    Get-PackageProvider -ListAvailable |
        Sort-Object Name, Version -Descending |
        Select-Object Name, Version, ProviderPath
)
Write-DiagnosticTable -Title 'Available PackageManagement providers:' -InputObject $packageProviders

$availableAzModules = @(
    Get-Module Az.Accounts, Az.Storage -ListAvailable |
        Sort-Object Name, Version -Descending |
        Select-Object Name, Version, ModuleBase
)
Write-DiagnosticTable -Title 'Available Az.Accounts and Az.Storage modules before installation:' -InputObject $availableAzModules

Show-RepositoryDiagnostic -Phase 'before'

Write-Host 'Network proxy environment:'
Write-Host "HTTP_PROXY: $(Get-SanitizedUri -Value $env:HTTP_PROXY)"
Write-Host "HTTPS_PROXY: $(Get-SanitizedUri -Value $env:HTTPS_PROXY)"
Write-Host "ALL_PROXY: $(Get-SanitizedUri -Value $env:ALL_PROXY)"
Write-Host "NO_PROXY: $($env:NO_PROXY)"
Write-Host "1ESNI_CONFIG_PATH: $($env:1ESNI_CONFIG_PATH)"

$repositoryName = 'PowerShellGalleryMirror'
$repositorySourceLocation = 'https://pkgs.dev.azure.com/powershell/PowerShell/_packaging/PowerShellGalleryMirror/nuget/v2'
$repositoryUri = [uri]$repositorySourceLocation
$defaultProxy = [System.Net.WebRequest]::DefaultWebProxy
if ($null -eq $defaultProxy) {
    Write-Host 'DefaultWebProxy: <not configured>'
} else {
    $resolvedProxy = $defaultProxy.GetProxy($repositoryUri)
    Write-Host "DefaultWebProxy bypasses CFS: $($defaultProxy.IsBypassed($repositoryUri))"
    Write-Host "DefaultWebProxy for CFS: $(Get-SanitizedUri -Value $resolvedProxy.AbsoluteUri)"
}

Unregister-PSRepository -Name $repositoryName -ErrorAction Ignore
Register-PSRepository -Name $repositoryName -SourceLocation $repositorySourceLocation -InstallationPolicy Trusted -ErrorAction Stop

$registeredSourceLocation = (Get-PSRepository -Name $repositoryName -ErrorAction Stop).SourceLocation
if ($registeredSourceLocation.TrimEnd('/') -ne $repositorySourceLocation.TrimEnd('/')) {
    throw "Repository '$repositoryName' has unexpected source location '$registeredSourceLocation'."
}

Show-RepositoryDiagnostic -Phase 'after'

$diagnosticDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "az-module-diagnostic-$PID-$([guid]::NewGuid().ToString('N'))"
$null = New-Item -Path $diagnosticDirectory -ItemType Directory -Force
try {
    Invoke-PackageDownloadProbe -Name Az.Accounts -Version 5.5.2 -RepositorySourceLocation $repositorySourceLocation -DestinationDirectory $diagnosticDirectory
    Invoke-PackageDownloadProbe -Name Az.Storage -Version 9.7.2 -RepositorySourceLocation $repositorySourceLocation -DestinationDirectory $diagnosticDirectory
} finally {
    Remove-Item -LiteralPath $diagnosticDirectory -Recurse -Force -ErrorAction Ignore
}

$traceDirectory = if ($env:AGENT_TEMPDIRECTORY) { $env:AGENT_TEMPDIRECTORY } else { [System.IO.Path]::GetTempPath() }
$tracePath = Join-Path $traceDirectory "PackageManagement-$PID-$([guid]::NewGuid().ToString('N')).log"
$sanitizedTracePath = "$tracePath.sanitized"
$installStartTime = [datetime]::UtcNow
try {
    Trace-Command -Name PackageManagement -Option All -FilePath $tracePath -Expression {
        Install-Module -Repository $repositoryName -Name Az.Accounts -RequiredVersion 5.5.2 -Scope CurrentUser -Force -Verbose -ErrorAction Stop
        Install-Module -Repository $repositoryName -Name Az.Storage -RequiredVersion 9.7.2 -Scope CurrentUser -Force -Verbose -ErrorAction Stop
    }
} catch {
    Write-Host 'Recently created NuGet package files after installation failure:'
    $recentPackages = @(
        Get-ChildItem -Path ([System.IO.Path]::GetTempPath()) -Filter '*.nupkg' -File -Recurse -ErrorAction Ignore |
            Where-Object LastWriteTimeUtc -ge $installStartTime
    )
    if ($recentPackages.Count -eq 0) {
        Write-Host '  None'
    } else {
        foreach ($package in $recentPackages) {
            Show-ArchiveDiagnostic -Path $package.FullName -Label 'PackageManagement download'
        }
    }

    if (Test-Path -LiteralPath $tracePath) {
        Get-Content -LiteralPath $tracePath |
            ForEach-Object { $_ -replace '(https?://[^?\s''"]+)\?[^\s''"]+', '$1?<query-redacted>' } |
            Set-Content -LiteralPath $sanitizedTracePath
    }
    if (Test-Path -LiteralPath $sanitizedTracePath) {
        Write-Host 'Last 200 lines of the PackageManagement trace:'
        Get-Content -LiteralPath $sanitizedTracePath -Tail 200 | Write-Host
    }
    throw
} finally {
    if (Test-Path -LiteralPath $tracePath) {
        if (-not (Test-Path -LiteralPath $sanitizedTracePath)) {
            Get-Content -LiteralPath $tracePath |
                ForEach-Object { $_ -replace '(https?://[^?\s''"]+)\?[^\s''"]+', '$1?<query-redacted>' } |
                Set-Content -LiteralPath $sanitizedTracePath
        }
        Remove-Item -LiteralPath $tracePath -Force

        Write-Host "Sanitized PackageManagement trace: $sanitizedTracePath"
        Write-Host "##vso[task.uploadfile]$sanitizedTracePath"
    }
}
