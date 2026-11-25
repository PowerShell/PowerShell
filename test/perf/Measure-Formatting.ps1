# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $ResultPath,

    [ValidateRange(1, [int]::MaxValue)]
    [int[]] $RowCount = @(1, 10, 5000, 20000),

    [ValidateRange(1, [int]::MaxValue)]
    [int] $RepeatCount = 11,

    [ValidateSet('Enumerate', 'TablePackets', 'TableForcePackets', 'TableForceTextStream',
        'ViewPackets', 'ViewTextStream', 'TableTextStream', 'TableTextBuffered',
        'TableAutoSize', 'TableWrap', 'TableCalculated', 'ListTextStream', 'WideTextStream',
        'TableTextNarrow', 'TableWrapNarrow')]
    [string[]] $ScenarioName = @(),

    [ValidateSet('Plain', 'Ansi', 'Long', 'AnsiLong', 'Alternating', 'Grouped', 'OutOfBand')]
    [string[]] $InputKind = @('Plain', 'Ansi', 'Long', 'AnsiLong', 'Alternating', 'Grouped', 'OutOfBand')
)

$ErrorActionPreference = 'Stop'
$PSStyle.OutputRendering = 'PlainText'
Update-FormatData -PrependPath (Join-Path $PSScriptRoot 'Formatting.format.ps1xml')
$random = [Random]::new(1729)
$samples = [System.Collections.Generic.List[object]]::new()
$assemblyPath = [System.Management.Automation.PSObject].Assembly.Location
$metadata = [ordered]@{
    PowerShell = $PSVersionTable.PSVersion.ToString()
    Runtime = [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
    Architecture = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
    Assembly = $assemblyPath
    AssemblySha256 = (Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash
    RequestedScenarios = @($ScenarioName)
    InputKinds = @($InputKind)
    RowCounts = @($RowCount)
    Workloads = [System.Collections.Generic.List[object]]::new()
    RepeatCount = $RepeatCount
    WarmupCount = 3
    Seed = 1729
    Width = 120
    NarrowWidth = 40
    StartedUtc = [DateTime]::UtcNow
    AllocationMetric = 'GC.GetTotalAllocatedBytes(true), process-wide; not retained/peak memory'
}
$scenario = [ordered]@{
    'Enumerate' = { param($Data) $Data | ForEach-Object { $null = $_ } }
    'TablePackets' = { param($Data) $Data | Format-Table Name, Count, Description | Out-Null }
    'TableForcePackets' = { param($Data) $Data | Format-Table Name, Count, Description -Force | Out-Null }
    'TableForceTextStream' = { param($Data) $Data | Format-Table Name, Count, Description -Force | Out-String -Stream -Width 120 | Out-Null }
    'ViewPackets' = { param($Data) $Data | Format-Table -View FormatProfileRow | Out-Null }
    'ViewTextStream' = { param($Data) $Data | Format-Table -View FormatProfileRow | Out-String -Stream -Width 120 | Out-Null }
    'TableTextStream' = { param($Data) $Data | Format-Table Name, Count, Description | Out-String -Stream -Width 120 | Out-Null }
    'TableTextBuffered' = { param($Data) $Data | Format-Table Name, Count, Description | Out-String -Width 120 | Out-Null }
    'TableAutoSize' = { param($Data) $Data | Format-Table Name, Count, Description -AutoSize | Out-String -Stream -Width 120 | Out-Null }
    'TableWrap' = { param($Data) $Data | Format-Table Name, Count, Description -Wrap | Out-String -Stream -Width 120 | Out-Null }
    'TableCalculated' = { param($Data) $Data | Format-Table @{n='Name';e={$_.Name}}, @{n='Count';e={$_.Count}}, @{n='Description';e={$_.Description}} | Out-String -Stream -Width 120 | Out-Null }
    'ListTextStream' = { param($Data) $Data | Format-List Name, Count, Description | Out-String -Stream -Width 120 | Out-Null }
    'WideTextStream' = { param($Data) $Data | Format-Wide Name -Column 3 | Out-String -Stream -Width 120 | Out-Null }
    'TableTextNarrow' = { param($Data) $Data | Format-Table Name, Count, Description | Out-String -Stream -Width 40 | Out-Null }
    'TableWrapNarrow' = { param($Data) $Data | Format-Table Name, Count, Description -Wrap | Out-String -Stream -Width 40 | Out-Null }
}

foreach ($count in $RowCount) {
    foreach ($kind in $InputKind) {
        $data = @(for ($index = 0; $index -lt $count; $index++) {
            $name = 'item-{0:D6}' -f $index
            $description = 'A short description'
            if ($kind -in 'Long', 'AnsiLong') {
                $description = ('word ' * 32).TrimEnd()
            }
            if ($kind -in 'Ansi', 'AnsiLong') {
                $name = "`e[32m$name`e[0m"
                $description = "`e[36m$description`e[0m"
            }
            $row = [pscustomobject]@{ PSTypeName = 'FormatProfile.Row'; Name = $name; Count = $index; Description = $description }
            if ($kind -in 'Alternating', 'Grouped') {
                $typeSuffix = if ($kind -eq 'Alternating') { $index % 2 } else { [int]($index -ge [Math]::Ceiling($count / 2)) }
                $row.PSObject.TypeNames.Insert(0, "FormatProfile.Row$typeSuffix")
            }
            $row
            if ($kind -eq 'OutOfBand' -and ($index + 1) % 100 -eq 0) {
                [pscustomobject]@{ PSTypeName = 'FormatProfile.Notice'; Message = "Notice after row $($index + 1)" }
            }
        })
        $selected = switch ($kind) {
            'Plain' { @($scenario.Keys | Where-Object { $_ -notlike '*Narrow' }) }
            'AnsiLong' { @('TableTextNarrow', 'TableWrapNarrow') }
            { $_ -in 'Alternating', 'Grouped', 'OutOfBand' } { @('ViewPackets', 'ViewTextStream') }
            default { @('TablePackets', 'TableTextStream', 'TableAutoSize', 'TableWrap') }
        }
        if ($ScenarioName.Count -gt 0) {
            $selected = @($selected | Where-Object { $_ -in $ScenarioName })
        }
        if ($selected.Count -eq 0) {
            continue
        }
        $metadata.Workloads.Add([pscustomobject]@{
            Rows = $count
            InputKind = $kind
            InputCount = $data.Count
            Scenarios = @($selected)
        })
        foreach ($name in $selected) {
            for ($warmup = 0; $warmup -lt 3; $warmup++) {
                & $scenario[$name] $data
            }
        }
        for ($repeat = 0; $repeat -lt $RepeatCount; $repeat++) {
            $order = @($selected)
            for ($index = $order.Length - 1; $index -gt 0; $index--) {
                $other = $random.Next($index + 1)
                $order[$index], $order[$other] = $order[$other], $order[$index]
            }
            foreach ($name in $order) {
                [GC]::Collect()
                [GC]::WaitForPendingFinalizers()
                [GC]::Collect()
                $before = [GC]::GetTotalAllocatedBytes($true)
                $timer = [Diagnostics.Stopwatch]::StartNew()
                & $scenario[$name] $data
                $timer.Stop()
                $allocated = [GC]::GetTotalAllocatedBytes($true) - $before
                $samples.Add([pscustomobject]@{
                    Rows = $count
                    InputCount = $data.Count
                    InputKind = $kind
                    Scenario = $name
                    Width = if ($name -like '*Narrow') { 40 } else { 120 }
                    Repeat = $repeat
                    Milliseconds = $timer.Elapsed.TotalMilliseconds
                    AllocatedBytes = $allocated
                })
            }
        }
        Write-Verbose "Measured $count rows, $kind"
    }
}

$metadata.CompletedUtc = [DateTime]::UtcNow
[pscustomobject]@{ Metadata = $metadata; Samples = $samples } |
    ConvertTo-Json -Depth 6 |
    Set-Content -LiteralPath $ResultPath
$summary = $samples | Group-Object Rows, InputKind, Scenario | ForEach-Object {
    $times = @($_.Group.Milliseconds | Sort-Object)
    $bytes = @($_.Group.AllocatedBytes | Sort-Object)
    $middle = [int][Math]::Floor($times.Count / 2)
    $medianTime = $times[$middle]
    $medianBytes = $bytes[$middle]
    if ($times.Count % 2 -eq 0) {
        $medianTime = ($medianTime + $times[$middle - 1]) / 2
        $medianBytes = ($medianBytes + $bytes[$middle - 1]) / 2
    }
    [pscustomobject]@{
        Rows = $_.Group[0].Rows
        Kind = $_.Group[0].InputKind
        Scenario = $_.Group[0].Scenario
        MedianMs = [Math]::Round($medianTime, 2)
        MinMs = [Math]::Round($times[0], 2)
        MaxMs = [Math]::Round($times[-1], 2)
        MiB = [Math]::Round($medianBytes / 1MB, 2)
        BytesPerRow = [Math]::Round($medianBytes / $_.Group[0].Rows, 1)
    }
}
$summary | Export-Csv -LiteralPath ([IO.Path]::ChangeExtension($ResultPath, '.summary.csv'))
$summary
