<# 
Start x instances Processbooks for x Displays - MIT Build
+ KEPInfo - copy2Local + start

last Build: 19.11.2018
by: UI221223
#>

$PBShell = "C:\Program Files (x86)\PIPC\Procbook\Pbshell.exe"

$ShiOv = "\\Group.rwe.com\trading\RWE-Trading\IPM\MIT\Power\PI\Processbook\Shiba Processbook\SHIBA_Overview.pdi"
$MonOv = "\\Group.rwe.com\trading\RWE-Trading\IPM\MIT\Power\PI\Processbook\Monitor Overview.PDI"

Start-Process "$PBShell" -FilePath "$MonOv"
Start-Process - $PBShell -ArgumentList -FilePath $ShiOv

<# 

${p} = Get-Process ProcBook
${pist} = $p.Count
# ${k} = Get-Process KEPInfo
# ${kist} = $k.Count
Write-Host "$pist Process Books started."
# & $kist KEPInfo 
# Read-Host
Start-Process "C:\Program Files (x86)\PIPC\Procbook\Procbook.exe" -ArgumentList '-file "\\group.rwe.com\trading\RWE-Trading\IPM\MIT\Power\PI\Processbook\Monitor Overview.PDI"'
Test-Path $KEPInfoRoot
Get-ItemProperty $KEPInfoRoot
#>