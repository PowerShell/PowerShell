<#
check local KEPInfo version
load newest KEPInfo version
Start KEPInfo


last Build: 19.11.2018
by: UI221223
#>

#check dirs
$LocalData = "C:\LocalData\KEPInfo\"
$TestLocalData = Test-Path -Path $LocalData

$MITShare = "\\group.rwe.com\trading\RWE-Trading\IPM\MIT\Power\PI\KEPInfo - please copy into your LocalData\KEPInfo\"
$TestMITShare = Test-Path -Path $MITShare

if ($TestLocalData -eq $false) {
    mkdir $LocalData
    Write-Host "Created C:\LocalData\PI-Displays\"
} #end if check LocalData

#check hash
$KEPLocalHash = Get-FileHash "$LocalData\KEPInfo.exe" -Algorithm MD5
$KEPShareHash = Get-FileHash "$MITShare\KEPInfo.exe" -Algorithm MD5

#copy data
if ($TestMITShare -eq $true) {
    if (!$KEPLocalHash -eq $KEPShareHash) {
        Copy-Item -Path $MITShare\ -Destination $LocalData\
    }
}

Start-Process -FilePath "C:\LocalData\KEPInfo\KEPInfo.exe" -WorkingDirectory "C:\LocalData\KEPInfo\"
<# 
c/p
#>