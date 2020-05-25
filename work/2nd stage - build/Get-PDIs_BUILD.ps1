<# 
Check if local ProcessBook displays are up-to-date.
Download latest ProcessBook displays.
Open both displays in two sperate ProcessBook processes.

last build: 15.01.19
UI221223
#>

#check dirs
$LocalData = "C:\LocalData\PI-Displays\"
$TestLocalData = Test-Path -Path $LocalData

$MITShare = "\\group.rwe.com\trading\RWE-Trading\IPM\MIT\Power\PI\Processbook\"
$TestMITShare = Test-Path -Path $MITShare

$PBHome = "$env:PIHOME\Procbook\Procbook.exe"
$TestPBHome = Test-Path -Path $PBHome

#check hashs
$HashMon1 = Get-FileHash -Path $LocalData\Monitor_Overview.PDI -Algorithm MD5
$HashMon2 = Get-FileHash -Path $MITShare\Monitor_Overview.PDI -Algorithm MD5

$HashShiba1 = Get-FileHash -Path $LocalData\Shiba_Overview.PDI -Algorithm MD5
$HashShiba2 = Get-FileHash -Path $MITShare\Shiba_Overview.PDI -Algorithm MD5

#copy displays
if ($TestLocalData -eq $false) {
    mkdir $LocalData
    Write-Host "Created C:\LocalData\PI-Displays\"
} #end if LocalData

if ($TestMITShare -eq $true) {
    if ($HashMon1 -ieq $HashMon2) {
        Copy-Item -Path $MITShare\Monitor_Overview.PDI -Destination $LocalData\
        Write-Host "New Monitor_Overview.PDI loaded."
    } #end if Mon
    Write-Host "Monitor_Overview.PDI ready."
    
    if ($HashShiba1 -ieq $HashShiba2) {
        Copy-Item -Path $MITShare\Shiba_Overview.PDI -Destination $LocalData\
        Write-Host "New Shiba_Overview.PDI loaded."
    } #end if Shiba
    Write-Host "Shiba_Overview.PDI ready."
} #end if MITShare

elseif ($TestMITShare -eq $false) {
    Write-Host "MIT-Share not available - check connection to RCN."
} # end elseif MITShare

#start displays
if ($TestPIHome -eq $true) {
    Start-Process -FilePath "$env:PIHOME\Procbook\Procbook.exe" -ArgumentList "-file $LocalData\Monitor_Overview.PDI"
    Write-Host "Monitor_Overview.PDI started from LocalData."

    Start-Process -FilePath "$env:PIHOME\Procbook\Procbook.exe" -ArgumentList "-file $LocalData\Shiba_Overview.PDI"
    Write-Host "Shiba_Overview.PDI started from LocalData."
} #end if PBStart
elseif ($TestPBHome -eq $false) {
    Write-Host "ProcessBook not found - check PI installation."
}

<#
c/p
#>