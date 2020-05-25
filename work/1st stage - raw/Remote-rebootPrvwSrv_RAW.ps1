# Data collection
# $AddServer = Read-Host -Prompt "Servername"
# $AddUser = Read-Host -Prompt "PreviewDesktop"
# $BuildLoginCred = Get-Credential $PreviewDesktop
# $RebootComment = Read-Host -Prompt "Reboot comment"

# Reboot section
$rebootServer1 = Get-WmiObject Win32_OperatingSystem -ComputerName S930A3703 -Credential $BuildLoginCred
$rebootServer1.Win32Shutdowntracker(0, "PV crashed.", 0x00000000, 6)

$rebootServer2 = Get-WmiObject Win32_OperatingSystem -ComputerName S930A3704 -Credential $BuildLoginCred
$rebootServer2.Win32Shutdowntracker(0, "PV crashed.", 0x00000000, 6)

$rebootServer3 = Get-WmiObject Win32_OperatingSystem -ComputerName S930A3705 -Credential $BuildLoginCred
$rebootServer3.Win32Shutdowntracker(0, "PV crashed.", 0x00000000, 6)

$rebootServer4 = Get-WmiObject Win32_OperatingSystem -ComputerName S930A3824 -Credential $BuildLoginCred
$rebootServer4.Win32Shutdowntracker(0, "PV crashed.", 0x00000000, 6)

# additional functions
# mstsc /v:$Srv
# Start-Process -FilePath C:\Windows\System32\cmd.exe -ArgumentList "/K ping -t $AddServer"

<#
c/p
#>