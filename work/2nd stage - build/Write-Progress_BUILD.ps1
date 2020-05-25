<#
Progressbar as Snippet for future projects.
i needs to be a not fix int, due to the math & real world.
last build: 11.11.2018
UI221223
#>

Write-Progress -Activity $action
$collItems = Get-WmiObject -Query $stuff
For ($i = 1;
    $i -le $collItems.count; $i++) {
        Write-Progress -Activity "Searching..." -Status "Found $i information" `
        -percentComplete ($i / $collItems.count * 100)
}
$collItems | Select-Object -Unique "Whatever..."

<#
int32
int64

Test??
#>