# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $ResultPath,

    [ValidateRange(1, [int]::MaxValue)]
    [int[]] $RowCount = @(5000, 20000),

    [ValidateRange(1, [int]::MaxValue)]
    [int] $RepeatCount = 11,

    [ValidateSet('Enumerate', 'TablePackets', 'TableForcePackets', 'TableForceTextStream',
        'ViewPackets', 'ViewTextStream', 'TableTextStream', 'TableTextBuffered',
        'TableAutoSize', 'TableWrap', 'TableCalculated', 'ListTextStream', 'WideTextStream')]
    [string[]] $ScenarioName = @()
)

$ErrorActionPreference = 'Stop'
$PSStyle.OutputRendering = 'PlainText'
Update-FormatData -PrependPath (Join-Path $PSScriptRoot 'Formatting.format.ps1xml')
$random = [Random]::new(1729)
$samples = [System.Collections.Generic.List[object]]::new()
$metadata = [ordered]@{
    PowerShell = $PSVersionTable.PSVersion.ToString()
    Runtime = [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
    Architecture = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
    Assembly = [System.Management.Automation.PSObject].Assembly.Location
    RepeatCount = $RepeatCount
    WarmupCount = 3
    Seed = 1729
    Width = 120
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
}

foreach ($count in $RowCount) {
    foreach ($kind in @('Plain', 'Ansi', 'Long')) {
        $data = @(for ($index = 0; $index -lt $count; $index++) {
            $name = 'item-{0:D6}' -f $index
            $description = 'A short description'
            if ($kind -eq 'Ansi') {
                $name = "`e[32m$name`e[0m"
                $description = "`e[36m$description`e[0m"
            } elseif ($kind -eq 'Long') {
                $description = ('word ' * 32).TrimEnd()
            }
            [pscustomobject]@{ PSTypeName = 'FormatProfile.Row'; Name = $name; Count = $index; Description = $description }
        })
        $selected = @($scenario.Keys)
        if ($kind -ne 'Plain') {
            $selected = @('TablePackets', 'TableTextStream', 'TableAutoSize', 'TableWrap')
        }
        if ($ScenarioName.Count -gt 0) {
            $selected = @($selected | Where-Object { $_ -in $ScenarioName })
        }
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
                    InputKind = $kind
                    Scenario = $name
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
