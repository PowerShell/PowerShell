# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

$repoRoot = git rev-parse --show-toplevel
Import-Module "$repoRoot/build.psm1"

function Start-Benchmarking
{
    <#
    .SYNOPSIS
    Start a benchmark run.

    .PARAMETER TargetPSVersion
    The version of 'Microsoft.PowerShell.SDK' package that we want the benchmark to target.
    The supported versions are 7.0.x and above, including preview versions.

    .PARAMETER List
    List the available benchmarks, in either 'flat' or 'tree' views.

    .PARAMETER Filter
    One or more wildcard patterns to filter the benchmarks to be executed or to be listed.

    .PARAMETER Artifacts
    Path to the folder where you want to store the artifacts produced from running benchmarks.

    .PARAMETER KeepFiles
    Indicates to keep all temporary files produced for running benchmarks.
    #>
    [CmdletBinding()]
    param(
        [ValidatePattern(
            '^7\.(0|1|2)\.\d+(-preview\.\d{1,2})?$',
            ErrorMessage = 'The package version is invalid or not supported')]
        [string] $TargetPSVersion,

        [ValidateSet('flat', 'tree')]
        [string] $List,

        [string[]] $Filter = '*',
        [string] $Artifacts,
        [switch] $KeepFiles
    )

    Begin {
        Find-Dotnet

        if ($Artifacts) {
            $Artifacts = $PSCmdlet.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Artifacts)
        } else {
            $Artifacts = Join-Path $PSScriptRoot 'BenchmarkDotNet.Artifacts'
        }

        if (Test-Path -Path $Artifacts) {
            Remove-Item -Path $Artifacts -Recurse -Force -ErrorAction Stop
        }
    }

    End {
        try {
            Push-Location -Path "$PSScriptRoot/benchmarks"
            $savedOFS = $OFS; $OFS = $null

            if ($TargetPSVersion) {
                Write-Log -message "Run benchmarks targeting the 'Microsoft.PowerShell.SDK' version $TargetPSVersion"
                $env:PERF_TARGET_VERSION = $TargetPSVersion
            } else {
                Write-Log -message "Run benchmarks targeting the current PowerShell code base"
            }

            $runArgs = @()
            if ($List) { $runArgs += '--list', $List }
            if ($KeepFiles) { $runArgs += "--keepFiles" }

            dotnet run -c release --filter $Filter --artifacts $Artifacts $runArgs

            if (Test-Path $Artifacts) {
                Write-Log -message "`nBenchmark artifacts can be found at $Artifacts"
            }
        }
        finally {
            $OFS = $savedOFS
            $env:PERF_TARGET_VERSION = $null
            Pop-Location
        }
    }
}

function Compare-BenchmarkResult
{
    <#
    .SYNOPSIS
    Compare two benchmark run results to find possible regressions.

    When running benchmarks with 'Start-Benchmarking', you can define the result folder
    where to save the artifacts by specifying '-Artifacts'.

    To compare two benchmark run results, you need to specify the result folder paths
    for both runs, one as the base and one as the diff.

    .PARAMETER BaseResultPath
    Path to the benchmark result used as baseline.

    .PARAMETER DiffResultPath
    Path to the benchmark result to be compared with the baseline.

    .PARAMETER Threshold
    Threshold for Statistical Test. Examples: 5%, 10ms, 100ns, 1s

    .PARAMETER Noise
    Noise threshold for Statistical Test.
    The difference for 1.0ns and 1.1ns is 10%, but it's really just noise. Examples: 0.5ns 1ns.
    The default value is 0.3ns.

    .PARAMETER Top
    Filter the diff to top `N` results
    #>
    param(
        [Parameter(Mandatory)]
        [string] $BaseResultPath,

        [Parameter(Mandatory)]
        [string] $DiffResultPath,

        [Parameter(Mandatory)]
        [ValidatePattern('^\d{1,2}%$|^\d+(ms|ns|s)$')]
        [string] $Threshold,

        [ValidatePattern('^(\d\.)?\d+(ms|ns|s)$')]
        [string] $Noise,

        [ValidateRange(1, 100)]
        [int] $Top
    )

    Find-Dotnet

    try {
        Push-Location -Path "$PSScriptRoot/dotnet-tools/ResultsComparer"
        $savedOFS = $OFS; $OFS = $null

        $runArgs = @()
        if ($Noise) { $runArgs += "--noise $Noise" }
        if ($Top -gt 0) { $runArgs += "--top $Top" }

        dotnet run -c release --base $BaseResultPath --diff $DiffResultPath --threshold $Threshold "$runArgs"
    }
    finally {
        $OFS = $savedOFS
        Pop-Location
    }
}
