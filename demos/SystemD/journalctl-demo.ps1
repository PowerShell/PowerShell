Import-Module $PSScriptRoot/SystemD/SystemD.psm1

#list recent journal events
Write-host -Foreground Blue "Get recent SystemD journal messages"
Get-SystemDJournal -args "-xe" |Out-Host

#Drill into SystemD unit messages
Write-host -Foreground Blue "Get recent SystemD journal messages for services and return Unit, Message"
Get-SystemDJournal -args "-xe" | Where {$_._SYSTEMD_UNIT -like "*.service"} | Format-Table _SYSTEMD_UNIT, MESSAGE | Select-Object -first 10 | Out-Host
