<# 
Remote Reboot Win-Server via WMI with reboot comment
>ToDo:
Add a "Do you want to RDP connect"-switch

last build: 19.11.2018
by: UI221223
#>

#Collect data to connect RDP/WMI
$ServerToLogon = Read-Host -Prompt "Please enter server"
$UserLogin = Get-Credential -UserName $\PreviewDesktop -Message "For Login to $ServerToLogon"
$RebootComment = Read-Host -Prompt "Please provide reboot cause" | Out-String

#Connect to server via WMI + Reboot with proper commentar
Read-Host -Prompt "Press return to reboot $AddServer."
$rebootServer = Get-WmiObject -Class Win32_OperatingSystem -ComputerName $ServerToLogon -Credential $UserLogin
$rebootServer.Win32Shutdowntracker(0, $RebootComment, 0x00000000, 6)

#Start ping to see if the machine comes back
Start-Process -FilePath C:\Windows\System32\cmd.exe -ArgumentList "/K ping -t $ServerToLogon"

<#
c/p

#Connect to Server via RDP
mstsc /v:$AddServer
#>