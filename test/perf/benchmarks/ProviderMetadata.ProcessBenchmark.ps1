# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param(
    [Parameter(Mandatory)]
    [string] $PwshPath,

    [string] $BaselinePwshPath = $PwshPath,

    [int] $FileCount = 100,

    [int] $Iterations = 20,

    [int] $Repetitions = 3,

    [int] $WarmupIterations = 3,

    [int] $ProcessRuns = 3,

    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'
foreach ($inputValue in @{
    FileCount = $FileCount
    Iterations = $Iterations
    Repetitions = $Repetitions
    WarmupIterations = $WarmupIterations
    ProcessRuns = $ProcessRuns
}.GetEnumerator()) {
    if ($inputValue.Value -le 0) {
        throw "-$($inputValue.Key) must be greater than zero."
    }
}

foreach ($executable in @(@{ Name = 'PwshPath'; Path = $PwshPath }, @{ Name = 'BaselinePwshPath'; Path = $BaselinePwshPath })) {
    if (-not (Test-Path -LiteralPath $executable.Path -PathType Leaf)) {
        throw "-$($executable.Name) does not point to a file: '$($executable.Path)'"
    }
}
$PwshPath = (Resolve-Path -LiteralPath $PwshPath).Path
$BaselinePwshPath = (Resolve-Path -LiteralPath $BaselinePwshPath).Path

$resolvedOutput = $null
if ($OutputPath) {
    $resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
    $parentDirectory = [System.IO.Path]::GetDirectoryName($resolvedOutput)
    if (-not (Test-Path -LiteralPath $parentDirectory -PathType Container)) {
        throw "Output directory does not exist: '$parentDirectory'."
    }
}

$tempRoot = [System.IO.Path]::GetFullPath((Join-Path ([System.IO.Path]::GetTempPath()) "PowerShell.ProviderMetadata.$([guid]::NewGuid().ToString('N'))"))
$dataDirectory = Join-Path $tempRoot 'files'
$measureScript = Join-Path $PSScriptRoot 'assets/ProviderMetadata.Measure.ps1'

function Invoke-IsolatedMeasurement {
    param(
        [string] $Side,
        [string] $ExecutablePath,
        [int] $ProcessRun
    )

    $enableFeature = $Side -eq 'Candidate'
    $settingsFile = Join-Path $tempRoot "$($Side.ToLowerInvariant()).$ProcessRun.settings.json"
    if ($enableFeature) {
        [string[]] $features = @('PSOptimizedProviderObjects')
    }
    else {
        [string[]] $features = @()
    }
    @{ ExperimentalFeatures = $features } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $settingsFile

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $ExecutablePath
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($argument in @('-SettingsFile', $settingsFile, '-NoLogo', '-NoProfile', '-File', $measureScript,
            '-Directory', $dataDirectory, '-ExpectedFeatureState', $(if ($enableFeature) { 'enabled' } else { 'disabled' }),
            '-Side', $Side, '-ProcessRun', $ProcessRun.ToString(), '-FileCount', $FileCount.ToString(),
            '-Iterations', $Iterations.ToString(), '-Repetitions', $Repetitions.ToString(),
            '-WarmupIterations', $WarmupIterations.ToString())) {
        $startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $stdout = $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()
    $exitCode = $process.ExitCode
    $process.Dispose()
    if ($exitCode -ne 0) {
        throw "Benchmark child failed ($Side, process run $ProcessRun, executable '$ExecutablePath'): $stderr"
    }

    $jsonLine = @($stdout -split "`r?`n" | Where-Object { $_.TrimStart().StartsWith('{') } | Select-Object -Last 1)
    if ($jsonLine.Count -ne 1) {
        throw "Benchmark child did not return a JSON result: $stdout`n$stderr"
    }

    $jsonLine[0] | ConvertFrom-Json
}

function Get-Median {
    param([double[]] $Values)
    $orderedValues = @($Values | Sort-Object)
    if ($orderedValues.Count -eq 0) { return $null }
    $middle = [int][math]::Floor($orderedValues.Count / 2)
    if ($orderedValues.Count % 2) { return [double]$orderedValues[$middle] }
    return ([double]$orderedValues[$middle - 1] + [double]$orderedValues[$middle]) / 2
}

function Get-ExecutableMetadata {
    param([string] $Path)
    $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $resolvedPath
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($argument in @('-NoLogo', '-NoProfile', '-Command', '$ErrorActionPreference = ''Stop''; $debug = [System.Attribute]::GetCustomAttribute([System.Management.Automation.PSObject].Assembly, [System.Diagnostics.DebuggableAttribute]); [pscustomobject]@{ Version=$PSVersionTable.PSVersion.ToString(); ClrVersion=[System.Environment]::Version.ToString(); OS=[System.Runtime.InteropServices.RuntimeInformation]::OSDescription; Architecture=[System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString(); DebugStatus=if ($null -eq $debug) { ''unknown'' } elseif ($debug.IsJITOptimizerDisabled) { ''JIT optimizer disabled'' } elseif ($debug.IsJITTrackingEnabled) { ''JIT tracking enabled'' } else { ''release-like'' } } | ConvertTo-Json -Compress')) {
        $startInfo.ArgumentList.Add($argument)
    }
    $process = [System.Diagnostics.Process]::Start($startInfo)
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $stdout = $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()
    $exitCode = $process.ExitCode
    $process.Dispose()
    if ($exitCode -ne 0) { throw "Could not inspect executable '$resolvedPath': $stderr" }
    $metadata = $stdout.Trim() | ConvertFrom-Json
    [pscustomobject]@{
        Path = $resolvedPath
        Version = $metadata.Version
        ClrVersion = $metadata.ClrVersion
        OS = $metadata.OS
        Architecture = $metadata.Architecture
        DebugStatus = $metadata.DebugStatus
    }
}

try {
    New-Item -ItemType Directory -Path $dataDirectory -Force | Out-Null
    for ($i = 0; $i -lt $FileCount; $i++) {
        Set-Content -LiteralPath (Join-Path $dataDirectory "item-$i.txt") -Value 'metadata benchmark'
    }

    $baselineMetadata = Get-ExecutableMetadata -Path $BaselinePwshPath
    $candidateMetadata = Get-ExecutableMetadata -Path $PwshPath
    $samples = [System.Collections.Generic.List[object]]::new()
    $processOrder = [System.Collections.Generic.List[object]]::new()
    for ($processRun = 1; $processRun -le $ProcessRuns; $processRun++) {
        $order = if ($processRun % 2) { @('Baseline', 'Candidate') } else { @('Candidate', 'Baseline') }
        foreach ($side in $order) {
            $executable = if ($side -eq 'Baseline') { $BaselinePwshPath } else { $PwshPath }
            $result = Invoke-IsolatedMeasurement -Side $side -ExecutablePath $executable -ProcessRun $processRun
            $samples.Add([pscustomobject]@{ Side = $side; ProcessRun = $processRun; Result = $result })
            $processOrder.Add([pscustomobject]@{ ProcessRun = $processRun; Side = $side })
        }
    }

    $scenarioNames = @($samples[0].Result.Scenarios | ForEach-Object Scenario)
    $comparisons = foreach ($scenarioName in $scenarioNames) {
        $bySide = @{}
        foreach ($side in @('Baseline', 'Candidate')) {
            $measurements = foreach ($sample in @($samples | Where-Object Side -eq $side)) {
                $scenario = @($sample.Result.Scenarios | Where-Object Scenario -eq $scenarioName)[0]
                foreach ($measurement in $scenario.Results) {
                    [pscustomobject]@{
                        ElapsedMilliseconds = [double]$measurement.ElapsedMilliseconds
                        AllocatedBytesPerEmittedItem = [double]$measurement.AllocatedBytes / [double]$scenario.EmittedItemCount
                    }
                }
            }
            $bySide[$side] = [pscustomobject]@{
                MedianElapsedMilliseconds = Get-Median -Values @($measurements | ForEach-Object ElapsedMilliseconds)
                MedianAllocatedBytesPerEmittedItem = Get-Median -Values @($measurements | ForEach-Object AllocatedBytesPerEmittedItem)
            }
        }
        $elapsedRatio = if ($bySide.Baseline.MedianElapsedMilliseconds -eq 0) { $null } else { $bySide.Candidate.MedianElapsedMilliseconds / $bySide.Baseline.MedianElapsedMilliseconds }
        $allocationRatio = if ($bySide.Baseline.MedianAllocatedBytesPerEmittedItem -eq 0) { $null } else { $bySide.Candidate.MedianAllocatedBytesPerEmittedItem / $bySide.Baseline.MedianAllocatedBytesPerEmittedItem }
        [pscustomobject]@{
            Scenario = $scenarioName
            Baseline = $bySide.Baseline
            Candidate = $bySide.Candidate
            ElapsedCandidateOverBaselineRatio = $elapsedRatio
            ElapsedPercentChange = if ($null -eq $elapsedRatio) { $null } else { ($elapsedRatio - 1) * 100 }
            AllocatedBytesPerEmittedItemCandidateOverBaselineRatio = $allocationRatio
            AllocatedBytesPerEmittedItemPercentChange = if ($null -eq $allocationRatio) { $null } else { ($allocationRatio - 1) * 100 }
        }
    }
    $retainedComparisons = foreach ($side in @('Baseline', 'Candidate')) {
        $values = @($samples | Where-Object Side -eq $side | ForEach-Object { [double]$_.Result.RetainedEmission.RetainedBytesPerObject })
        [pscustomobject]@{ Side = $side; MedianApproximateRetainedBytesPerObject = Get-Median -Values $values }
    }
    $baselineRetained = $retainedComparisons[0].MedianApproximateRetainedBytesPerObject
    $candidateRetained = $retainedComparisons[1].MedianApproximateRetainedBytesPerObject
    $retainedRatio = if ($baselineRetained -eq 0) { $null } else { $candidateRetained / $baselineRetained }
    $report = [pscustomobject]@{
        SchemaVersion = 1
        Environment = [pscustomobject]@{ Baseline = $baselineMetadata; Candidate = $candidateMetadata }
        Settings = [pscustomobject]@{ FileCount = $FileCount; Iterations = $Iterations; Repetitions = $Repetitions; WarmupIterations = $WarmupIterations; ProcessRuns = $ProcessRuns }
        Dataset = [pscustomobject]@{ Directory = $dataDirectory; FileCount = $FileCount }
        ObservedFeatureState = [pscustomobject]@{
            Baseline = @($samples | Where-Object Side -eq 'Baseline' | ForEach-Object { $_.Result.ObservedFeatureState } | Select-Object -Unique)
            Candidate = @($samples | Where-Object Side -eq 'Candidate' | ForEach-Object { $_.Result.ObservedFeatureState } | Select-Object -Unique)
        }
        ProcessOrder = @($processOrder)
        RawSamples = @($samples)
        Comparisons = @($comparisons)
        RetainedEmissionComparison = [pscustomobject]@{
            Measurements = @($retainedComparisons)
            CandidateOverBaselineRatio = $retainedRatio
            PercentChange = if ($null -eq $retainedRatio) { $null } else { ($retainedRatio - 1) * 100 }
            Interpretation = 'Approximate retained managed heap per emitted object; not process RSS or a significance estimate.'
        }
    }
    $json = $report | ConvertTo-Json -Depth 16
    if ($resolvedOutput) {
        Set-Content -LiteralPath $resolvedOutput -Value $json -Encoding utf8
    }
    $json
}
finally {
    $tempPath = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $resolvedRoot = [System.IO.Path]::GetFullPath($tempRoot)
    $resolvedLeaf = [System.IO.Path]::GetFileName($resolvedRoot)
    if (-not $resolvedRoot.StartsWith($tempPath, [System.StringComparison]::OrdinalIgnoreCase) -or -not $resolvedLeaf.StartsWith('PowerShell.ProviderMetadata.', [System.StringComparison]::Ordinal)) {
        throw "Refusing to remove unexpected benchmark temporary path '$resolvedRoot'."
    }

    if (Test-Path -LiteralPath $resolvedRoot) {
        Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
    }
}
