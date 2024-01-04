
# PowerShell Performance Analysis

This directory contains useful scripts and related files for analyzing PowerShell performance.

If you use the [Windows Performance Toolkit](https://learn.microsoft.com/en-us/windows-hardware/test/wpt/), you can use the following to collect data and analyze a trace.

```PowerShell
$PowerShellGitRepo = "D:\PowerShell"
wpr -start $PowerShellGitRepo\tools\performance\PowerShell.wprp -filemode
pwsh.exe -NoProfile -Command "echo 1"
wpr -stop PowerShellTrace.etl
wpa -i wpa://.\PowerShellTrace.etl?profile=$PowerShellGitRepo\tools\performance\PowerShell.wpaProfile
```

When wpa opens, under System Activity, you'll find a section "Regions of Interest".
With the above wpaProfile, you should see a bunch of PowerShell related regions as well as GC and JIT activity.

If you use [PerfView](https://github.com/microsoft/perfview), you can collect a trace by running

```PowerShell
Invoke-PerfviewPS.ps1 -scenario { echo 1 }
perfview .\perfviewdata.etl
```

The etl files collected with perfview or wpr should contain roughly the same events.

Also note that you can collect the trace with one tool and analyze with the other.

## Symbols

PDB files are not published for PowerShell Core,
so the current recommendation is to build PowerShell yourself passing `-CrossGen` to `Start-Build`.

If profiling Windows PowerShell, symbols are generated from GAC.
wprui.exe and perfview.exe will both generate the PDB files needed.

## Files

| File | Description |
| ---- | ----------- |
| GC.Regions.xml | WPA regions of interest for GC |
| JIT.Regions.xml | WPA regions of interest for JIT |
| PowerShell.Regions.xml | WPA regions of interest for PowerShell |
| PowerShell.stacktags | PowerShell stack tags |
| PowerShell.wpaProfile | WPA profile to load regions of interest and stack tags |
| PowerShell.wprp | WPR profile to enable CLR and PowerShell ETW events |
| Invoke-PerfviewPS.ps1 | Script to run perfview and with PowerShell ETW events enabled |

