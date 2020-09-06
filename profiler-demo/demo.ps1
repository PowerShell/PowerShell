function Get-Profile ($Path) {
    $files = $Path | foreach { (Resolve-Path $_).Path }

    $fileMap = @{}
    foreach ($file in $files) {
        $lines = Get-Content $file
        $lineProfiles = [Collections.Generic.List[object]]::new($lines.Length)
        $index = 0
        foreach ($line in $lines) {
            $lineProfile = [PSCustomObject] @{
                Line = ++$index # start from 1 as in file
                Duration = [TimeSpan]::Zero
                HitCount = 0
                Text = $line
                Hits = [Collections.Generic.List[object]]::new()
            }

            $lineProfiles.Add($lineProfile)
        }

        $fileMap.Add($file, $lineProfiles)
    }

    foreach ($hit in [System.Management.Automation.Profiler]::Hits) {
        if (-not $hit.InFile -or -not ($fileMap.Contains($hit.Source))) {
            continue
        }

        $lineProfiles = $fileMap[$hit.Source]
        $lineProfile = $lineProfiles[$hit.Line - 1] # array indexes from 0, but lines from 1
        $lineProfile.Duration += $hit.Duration
        $lineProfile.HitCount++
        $lineProfile.Hits.Add($hit)
    }

    foreach ($pair in $fileMap.GetEnumerator()) {
        [PSCustomObject]@{
            Path = $pair.Key
            Profile = $pair.Value
        }
    }
}

$files = "$PSScriptRoot/test1.ps1", "$PSScriptRoot/test2.ps1"

[System.Management.Automation.Profiler]::Clear()
Set-PSDebug -Trace 3
& $files[0]
& $files[1]
Set-PSDebug -Trace 0

$profiles = Get-Profile $files
">>>> file: $($profiles[0].Path)"
$profiles[0].Profile | Format-Table Line, Duration, HitCount, Text

">>>> file: $($profiles[1].Path)"
$profiles[1].Profile | Format-Table Line, Duration, HitCount, Text

