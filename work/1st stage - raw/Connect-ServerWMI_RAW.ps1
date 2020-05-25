<#
Login Snippet for future projects.

Last build: 19.11.2018
by: UI221223
#>

$LogonTo = Read-Host -Prompt "Bitte Server eingeben"
$LogonAs = Read-Host -Prompt "Bitte Nutzernamen eingeben"
Get-Credential -Credential $Logon -UserName $LogonTo\$LogonAs

$Logon = Get-Credential -Message "Please provide password for $LogonTo\$LogonAs"
$Logon

Get-WmiObject -Class WIN32_Service -Property * -Filter * -ComputerName $LogonTo -Credential $Logon

<#
c/p
#>