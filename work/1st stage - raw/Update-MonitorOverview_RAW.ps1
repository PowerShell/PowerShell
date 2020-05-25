<# 
Simple copy job for Monitor Overview from UDO network share.
Delivers newest version to LocalData\PI-Displays and moves local version to LocalData\Archive

last build: 25.11.2019
UI221223
#>
$timestamp = Get-Date -Format ddMMyy

Copy-Item "\\group.rwe.com\trading\RWE-Trading\Applikationen\UDO\INSTALL\KEP\PI-Display-Admin\MonitorOverview\Produktion\Monitor Overview.PDI" -Destination "C:\LocalData\PI-Displays\"
Rename-Item "C:\LocalData\PI-Displays\Monitor_Overview.PDI" -NewName "C:\LocalData\PI-Displays\Monitor_Overview_$timestamp.PDI"
Move-Item "C:\LocalData\PI-Displays\Monitor_Overview_$timestamp.PDI" -Destination "C:\LocalData\Archive" -Force
Rename-Item "C:\LocalData\PI-Displays\Monitor Overview.PDI" -NewName "C:\LocalData\PI-Displays\Monitor_Overview.PDI" -Force

<#
c/p
#>