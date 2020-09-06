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
                # for demo
                CalledFrom = [Collections.Generic.List[object]]::new()
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
        # Hit has it's own index so we can refer back to it in the whole timeline
        # to get for example who called us
        if (0 -lt $hit.Index) {
            $caller = [System.Management.Automation.Profiler]::Hits[$hit.Index - 1]
            
            $callerSource = $fileMap[$caller.Source]
            if (-not $callerSource) { 
                $callerText = "<unknown>:$($caller.Source):$($caller.Line)"
            }
            else { 
                $callerText = "$($callerSource.Text):$($caller.Source):$($caller.Line):"
            }
            
            $lineProfile.CalledFrom.Add($callerText)
        }
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
$profiles[1].Profile | Format-Table Line, Duration, HitCount, Text, CalledFrom