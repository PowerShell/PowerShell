<# 
Remote Reboot Win-Server via WMI with reboot comment
+ Ping server - see if its coming back

last build: 19.11.2018
UI221223
#>

#Get user defined variables
$AddServer = Read-Host -Prompt "Please enter server"
$AddUser = Read-Host -Prompt "Please enter user"
$BuildLoginCred = Get-Credential -UserName $AddServer\$AddUser -Message "Please enter password for $AddServer\$AddUser"
$RebootComment = Read-Host -Prompt "Please provide reboot cause" | Out-String

#Reboot the Server via WMI
Read-Host -Prompt "Press return to reboot $AddServer"
$rebootServer = Get-WmiObject -Class Win32_OperatingSystem -ComputerName $AddServer -Credential $BuildLoginCred
$rebootServer.Win32Shutdowntracker(0, $RebootComment, 0x00000000, 6)

#Ping to see if server comes back
Start-Process -FilePath C:\Windows\System32\cmd.exe -ArgumentList "/K ping -t $ServerToLogon"

<#
c/p
#>