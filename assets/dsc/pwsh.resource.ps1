[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Get', 'Set', 'Export')]
    [string]$Operation,
    [Parameter(ValueFromPipeline)]
    $stdinput
)

# catch any un-caught exception and write it to the error stream
trap {
    Write-Trace -Level Error -message $_.Exception.Message
    exit 1
}

function Write-Trace {
    param(
        [string]$message,
        [string]$level = 'Error'
    )

    $trace = [pscustomobject]@{
        $level.ToLower() = $message
    } | ConvertTo-Json -Compress

    $host.ui.WriteErrorLine($trace)
}

function GetConfigFilePath {
    param(
        [string]$Scope = 'CurrentUser'
    )

    if ($Scope -eq 'CurrentUser') {
        return Join-Path -Path ([System.Environment]::GetFolderPath('Personal')) -ChildPath "PowerShell" -AdditionalChildPath "powershell.config.json"
    } else {
        return Join-Path -Path $PSHOME -ChildPath "powershell.config.json"
    }
}

function GetOperation {
    $config = Get-Content (GetConfigFilePath -Scope 'CurrentUser') -Raw
    if ($config) {
        $config | ConvertFrom-Json | ConvertTo-Json
    } else {
        Write-Trace -Level Error -message "No configuration found."
        exit 1
    }
}

function SetOperation {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Configuration
    )

    $config = $Configuration | ConvertFrom-Json
    if ($config) {
        $config | ConvertTo-Json | Set-Content -Path (GetConfigFilePath -Scope 'CurrentUser') -Force
    } else {
        Write-Trace -Level Error -message "Invalid configuration format."
        exit 1
    }
}

$ProgressPreference = 'Ignore'
$WarningPreference = 'Ignore'
$VerbosePreference = 'Ignore'

if ($Operation -eq 'Get' -or $Operation -eq 'Export') {
    GetOperation
} elseif ($Operation -eq 'Set') {
    if ($stdinput) {
        SetOperation -Configuration $stdinput
    } else {
        Write-Trace -Level Error -message "No configuration provided for Set operation."
        exit 1
    }
} else {
    throw "Invalid operation: $Operation. Use 'Get', 'Set', or 'Export'."
}


