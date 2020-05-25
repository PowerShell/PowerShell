<# 
Simple copy job for Shiba Overview from UDO network share.
Delivers newest version to LocalData\PI-Displays and moves local version to LocalData\Archive

last build: 25.11.2019
UI221223
#>

$timestamp = Get-Date -Format ddMMyy
Rename-Item "C:\LocalData\PI-Displays\Shiba_Overview.PDI" -NewName "C:\LocalData\PI-Displays\Shiba_Overview_$timestamp.PDI" -Force
Move-Item "C:\LocalData\PI-Displays\Shiba_Overview_$timestamp.PDI" -Destination "C:\LocalData\Archive" -Force
Copy-Item "\\group.rwe.com\trading\RWE-Trading\Applikationen\UDO\INSTALL\KEP\PI-Displays\Shiba\Produktion\Shiba_Overview.PDI" -Destination "C:\LocalData\PI-Displays\" -Force

<#
c/p
#>