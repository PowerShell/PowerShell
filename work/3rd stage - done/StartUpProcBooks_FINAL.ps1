<# 
Start x instances Processbooks for x Displays - MIT Build
+ KEPInfo - copy2Local + start

last Build: 19.11.2018
by: UI221223
#>

Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process
$Proc = & $env:PIHOME\Procbook\procbook.exe
$MonOv = \\group.rwe.com\trading\RWE-Trading\ipm\mit\power\PI\Processbook\Monitor Overview.PDI
$ShiOv = \\group.rwe.com\trading\RWE-Trading\ipm\mit\power\PI\Processbook\Shiba Processbook\SHIBA_Overview.pdi

Start-Process -FilePath $Proc -ArgumentList '-file '$MonOv''
Start-Process -FilePath $Proc -ArgumentList '-file '$ShiOv''