# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param(
    [Parameter(Mandatory)]
    [string] $Directory,

    [Parameter(Mandatory)]
    [string] $ExpectedFeatureState,

    [Parameter(Mandatory)]
    [string] $Side,

    [Parameter(Mandatory)]
    [int] $ProcessRun,

    [Parameter(Mandatory)]
    [int] $FileCount,

    [int] $Iterations = 20,

    [int] $Repetitions = 3,

    [int] $WarmupIterations = 3
)

$ErrorActionPreference = 'Stop'
$feature = @(Get-ExperimentalFeature | Where-Object Name -eq 'PSOptimizedProviderObjects')
$featureAvailable = $feature.Count -gt 0
$enabled = $featureAvailable -and [bool]$feature.Enabled
if ($ExpectedFeatureState -eq 'enabled' -and (-not $featureAvailable -or -not $enabled)) {
    throw "Candidate must expose and enable PSOptimizedProviderObjects; available=$featureAvailable, enabled=$enabled."
}
if ($ExpectedFeatureState -eq 'disabled' -and $enabled) {
    throw 'Baseline must have PSOptimizedProviderObjects disabled.'
}
$observedFeatureState = if (-not $featureAvailable) { 'unavailable-off' } elseif ($enabled) { 'enabled' } else { 'disabled' }
$actualFileCount = @(Get-ChildItem -LiteralPath $Directory -File).Count
if ($actualFileCount -ne $FileCount) {
    throw "Expected $FileCount files in the benchmark dataset, found $actualFileCount."
}

$scenarios = [ordered]@{
    Emission = { $null = @(Get-ChildItem -LiteralPath $Directory -File) }
    DirectMetadata = {
        $items = @(Get-ChildItem -LiteralPath $Directory -File)
        [long] $totalPathLength = 0
        foreach ($item in $items) { $totalPathLength += $item.PSPath.Length }
        if ($totalPathLength -le 0) { throw 'DirectMetadata did not read any PSPath values.' }
    }
    RepeatedMetadata = {
        $items = @(Get-ChildItem -LiteralPath $Directory -File)
        [long] $totalPathLength = 0
        foreach ($item in $items) {
            for ($read = 0; $read -lt 10; $read++) { $totalPathLength += $item.PSPath.Length }
        }
        if ($totalPathLength -le 0) { throw 'RepeatedMetadata did not read any PSPath values.' }
    }
    ForEachFullName = { $null = Get-ChildItem -LiteralPath $Directory -File | ForEach-Object -MemberName FullName }
    ForEachParentPath = { $null = Get-ChildItem -LiteralPath $Directory -File | ForEach-Object -MemberName PSParentPath }
    DefaultFormatting = { $null = Get-ChildItem -LiteralPath $Directory -File | Out-String -Width 240 }
    SelectAll = { $null = Get-ChildItem -LiteralPath $Directory -File | Select-Object * }
}

$results = foreach ($scenario in $scenarios.GetEnumerator()) {
    for ($warmup = 0; $warmup -lt $WarmupIterations; $warmup++) {
        & $scenario.Value
    }
    $runs = for ($repetition = 0; $repetition -lt $Repetitions; $repetition++) {
        [GC]::Collect()
        [GC]::WaitForPendingFinalizers()
        [GC]::Collect()
        $retainedBefore = [GC]::GetTotalMemory($false)
        $allocatedBefore = [GC]::GetAllocatedBytesForCurrentThread()
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        for ($iteration = 0; $iteration -lt $Iterations; $iteration++) {
            & $scenario.Value
        }
        $stopwatch.Stop()
        $allocatedBytes = [GC]::GetAllocatedBytesForCurrentThread() - $allocatedBefore
        [GC]::Collect()
        [GC]::WaitForPendingFinalizers()
        [GC]::Collect()
        $retainedAfter = [GC]::GetTotalMemory($false)

        [pscustomobject]@{
            ElapsedMilliseconds = $stopwatch.Elapsed.TotalMilliseconds
            AllocatedBytes = $allocatedBytes
            RetainedBytesDelta = $retainedAfter - $retainedBefore
        }
    }

    [pscustomobject]@{
        Scenario = $scenario.Key
        Iterations = $Iterations
        Repetitions = $Repetitions
        WarmupIterations = $WarmupIterations
        EmittedItemCount = $FileCount * $Iterations
        MetadataReadsPerEmittedItem = switch ($scenario.Key) {
            'DirectMetadata' { 1 }
            'RepeatedMetadata' { 10 }
            default { 0 }
        }
        Results = @($runs)
    }
}

[GC]::Collect()
[GC]::WaitForPendingFinalizers()
[GC]::Collect()
$retainedBefore = [GC]::GetTotalMemory($false)
$retainedItems = @(Get-ChildItem -LiteralPath $Directory -File)
[GC]::Collect()
[GC]::WaitForPendingFinalizers()
[GC]::Collect()
$retainedAfter = [GC]::GetTotalMemory($false)
$retainedOutput = [pscustomobject]@{
    ObjectCount = $retainedItems.Count
    RetainedBytesDelta = $retainedAfter - $retainedBefore
    RetainedBytesPerObject = if ($retainedItems.Count -eq 0) { $null } else { ($retainedAfter - $retainedBefore) / $retainedItems.Count }
}
[GC]::KeepAlive($retainedItems)

[pscustomobject]@{
    Side = $Side
    ProcessRun = $ProcessRun
    ExpectedFeatureState = $ExpectedFeatureState
    ObservedFeatureState = $observedFeatureState
    FeatureAvailable = $featureAvailable
    FileCount = $actualFileCount
    Scenarios = @($results)
    RetainedEmission = $retainedOutput
} | ConvertTo-Json -Depth 6 -Compress
