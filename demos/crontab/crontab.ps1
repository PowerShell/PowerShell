# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module $PSScriptRoot/CronTab/CronTab.psd1

Write-Host -Foreground Yellow "Get the existing cron jobs"
Get-CronJob | Out-Host

Write-Host -Foreground Yellow "New cron job to clean out tmp every day at 1am"
New-CronJob -Command 'rm -rf /tmp/*; #demo' -Hour 1 | Out-Host

Write-Host -Foreground Yellow "Add some more jobs"
New-CronJob -Command 'python -c ~/scripts/backup_users; #demo' -Hour 2 -DayOfWeek 1-5 | Out-Host
New-CronJob -Command 'powershell -c "cd ~/src/PowerShell; ipmo ./build.psm1; Start-PSBuild"; #demo' -Hour 2 -DayOfWeek * | Out-Host

Write-Host -Foreground Yellow "Show in bash that the new cron job exists"
crontab -l

Write-Host -Foreground Yellow "Get jobs that run every day"
Get-CronJob | Where-Object { $_.DayOfWeek -eq '*' -or $_.DayOfWeek -eq '1-7' } | Out-Host

Write-Host -Foreground Yellow "Remove one cron job, with prompting to confirm"
Get-CronJob | Where-Object { $_.Command -match '^powershell.*' } | Remove-CronJob | Out-Host

Write-Host -Foreground Yellow "And the other job remains"
Get-CronJob | Out-Host

Write-Host -Foreground Yellow "Remove remaining demo jobs without prompting"
Get-CronJob | Where-Object { $_.Command -match '#demo'} | Remove-CronJob -Force

Write-Host -Foreground Yellow "Show in bash that cron should be clean"
crontab -l
