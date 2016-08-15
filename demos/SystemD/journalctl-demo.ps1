#Import-Module $PSScriptRoot/SystemD/SystemD.psm1
Import-Module ~/Workspace/krbash/PowerShell/demos/SystemD/SystemD/SystemD.ps1

#list recent journal events
Write-host -Foreground Blue "Get recent SystemD journal messages"
Get-SystemDJournal -args "-xe" |Out-Host

#Drill into SystemD unit messages
Write-host -Foreground Blue "Get recent SystemD jounal messages for services and return Unit, Message"
Get-SystemDJournal -args "-xe" |where {$_._SYSTEMD_UNIT -like "*.service"} |ft _SYSTEMD_UNIT, MESSAGE |select-object -first 10|Out-Host
