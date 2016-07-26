
#list recent journal events
Get-SystemDJournal -args "-xe"

#Drill into SystemD unit messages
Get-SystemDJournal -args "-xe" |where {$_._SYSTEMD_UNIT -like "*.service"} |ft _SYSTEMD_UNIT, MESSAGE |select-object -first 10
