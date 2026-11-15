# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Container })]
    [string] $Path,

    [Parameter(Mandatory)]
    [bool] $OfficialBuild,

    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string] $EntitlementsPath
)

$binaries = @(
    Get-ChildItem -LiteralPath $Path -Recurse -File |
        Where-Object { $_.Name -eq 'pwsh' -or $_.Extension -eq '.dylib' }
)

if ($binaries.Count -eq 0) {
    throw "No Mach-O binaries were found in '$Path'."
}

foreach ($binary in $binaries) {
    & codesign --verify --deep --strict --verbose=4 $binary.FullName
    if ($LASTEXITCODE -eq 0) {
        continue
    }

    if ($OfficialBuild) {
        throw "codesign verification failed for '$($binary.FullName)'."
    }

    # Nonofficial signing can leave only one slice of a universal binary signed.
    # Re-signing on macOS applies an ad-hoc signature across every architecture.
    Write-Verbose -Message "Applying an ad-hoc signature to '$($binary.FullName)' for the nonofficial package." -Verbose
    $signArguments = @('--sign', '-', '--force', '--options', 'runtime')
    if ($binary.Name -eq 'pwsh') {
        $signArguments += @('--entitlements', $EntitlementsPath)
    }
    $signArguments += $binary.FullName

    & codesign @signArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Ad-hoc codesign failed for '$($binary.FullName)'."
    }

    & codesign --verify --deep --strict --verbose=4 $binary.FullName
    if ($LASTEXITCODE -ne 0) {
        throw "codesign verification failed for '$($binary.FullName)' after applying an ad-hoc signature."
    }
}
