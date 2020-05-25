<# 
Simple copy job of KEPInfo from UDO network share
Move latest version to LocalData\KEPInfo and last version to LocalData\Archive
Keeping your personal view setup.

last build: 25.11.2019
UI221223
#>

$timestamp = Get-Date -Format ddMMyy
Stop-Process -Name KEPInfo
Copy-Item "C:\LocalData\KEPInfo\KEPInfo.exe.config" -Destination "C:\LocalData\Archive\" -Force
Rename-Item "C:\LocalData\Archive\KEPInfo.exe.config" -NewName "C:\LocalData\Archive\KEPInfo.exe.config_$timestamp" -Force
Move-Item "C:\LocalData\KEPInfo" -Destination "C:\LocalData\Archive\" -Force
Rename-Item "C:\LocalData\Archive\KEPInfo" -NewName "C:\LocalData\Archive\KEPInfo_$timestamp" -Force
Copy-Item "\\group.rwe.com\trading\RWE-Trading\Applikationen\UDO\INSTALL\KEP\KEPInfo\Produktion\" -Recurse -Destination "C:\LocalData\KEPInfo\" -Force
Move-Item "C:\LocalData\Archive\KEPInfo.exe.config_$timestamp" -Destination "C:\LocalData\KEPInfo\" -Force
Remove-Item "C:\LocalData\KEPInfo\KEPInfo.exe.config" -Force
Rename-Item "C:\LocalData\KEPInfo\KEPInfo.exe.config_$timestamp" -NewName "C:\LocalData\KEPInfo\KEPInfo.exe.config" -Force

<#
c/p

#>