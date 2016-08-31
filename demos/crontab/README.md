## CronTab demo

This demo shows examining, creating, and removing cron jobs via crontab.

Output of Get-CronJob is a strongly typed object with properties like DayOfWeek or Command.
Remove-CronJob prompts before removing the job unless you specify -Force.

Tab completion of -UserName is supported, e.g.

Get-CronJob -u <TAB>

NYI: no way to run crontab with sudo if necessary
NYI: ignoring shell variables or comments
NYI: New-CronJob -Description "..." (save in comments"
NYI: @reboot,@daily,@hourly,etc
