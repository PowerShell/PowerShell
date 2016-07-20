
if (!($env:PSMODULEPATH -split ';' | Where-Object { $_.StartsWith($PSScriptRoot) }))
{
    $env:PSMODULEPATH += ";$PSScriptRoot/modules"
}

Import-Module CronTab

# Get the existing cron jobs
Get-CronJob

# New cron job to clean out tmp every day at 1am
New-CronJob -Command 'rm -rf /tmp/*' -Hour 1

# New cron job to start a build
New-CronJob -Command 'powershell -c "cd ~/src/PowerShell; ipmo ./build.psm1; Start-PSBuild"' -Hour 2 -DayOfWeek 1-5

# Sort them by data
Get-CronJob | Sort-Object Command

# Show in bash that the new cron job exists
crontab -l

# Remove a cron job
Get-CronJob | Where-Object { $_.Command -match '^powershell.*' } | Remove-CronJob

