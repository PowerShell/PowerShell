<# 
Start PI displays in sperate PBs.

last build: 25.11.19
UI221223
#>

Start-Process -FilePath "$env:PIHOME\Procbook\Procbook.exe" -ArgumentList "-file C:\LocalData\PI-Displays\Monitor_Overview.PDI"
Start-Process -FilePath "$env:PIHOME\Procbook\Procbook.exe" -ArgumentList "-file C:\LocalData\PI-Displays\Shiba_Overview.PDI"

<#
c/p

#>