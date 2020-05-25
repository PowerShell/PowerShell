$GetCI = Read-Host -Prompt "CI"
$os = Get-Ciminstance Win32_OperatingSystem -ComputerName $GetCI
$pctFree = [System.Math]::Round(($os.FreePhysicalMemory/$os.TotalVisibleMemorySize)*100,2)
$os | Select-Object @{Name = "PctFree"; Expression = {$pctFree}},
@{Name = "FreeGB";Expression = {[math]::Round($_.FreePhysicalMemory/1mb,2)}},
@{Name = "TotalGB";Expression = {[int]($_.TotalVisibleMemorySize/1mb)}}