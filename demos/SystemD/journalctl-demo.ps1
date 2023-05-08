# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module $PSScriptRoot/SystemD/SystemD.psm1

#list recent journal events
Write-Host -Foreground Blue "Get recent SystemD journal messages"
Get-SystemDJournal -args "-xe" |Out-Host

#Drill into SystemD unit messages
Write-Host -Foreground Blue "Get recent SystemD journal messages for services and return Unit, Message"
Get-SystemDJournal -args "-xe" | Where-Object {$_._SYSTEMD_UNIT -like "*.service"} | Format-Table _SYSTEMD_UNIT, MESSAGE | Select-Object -First 10 | Out-Host
