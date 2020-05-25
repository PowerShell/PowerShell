<#
check local KEPInfo version
load newest KEPInfo version
Start KEPInfo

last Build: 21.01.2019
by: UI221223
#>
$LocalData = "C:\LocalData\KEPInfo\"
$MITShare = "\\group.rwe.com\trading\RWE-Trading\IPM\MIT\Power\PI\KEPInfo - please copy into your LocalData\KEPInfo"

$KEPLocalHash = Get-FileHash "$LocalData\KEPInfo.exe" -Algorithm MD5
$KEPShareHash = Get-FileHash "$MITShare\KEPInfo.exe" -Algorithm MD5

if (!$KEPLocalHash -eq $KEPShareHash) {
    Copy-Item -Path $MITShare\ -Destination $LocalData\
} #end if KEPInfo

Start-Process -FilePath "$LocalData\KEPInfo.exe" -WorkingDirectory $LocalData
<#
c/p
#>