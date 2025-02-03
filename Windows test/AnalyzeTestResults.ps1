param (
    [Parameter(Mandatory = $true)]
    [ValidateSet('UnelevatedPesterTests', 'ElevatedPesterTests')]
    [string]$Purpose
)

# Load the XML file
[xml]$xml = Get-Content -Path "CombinedTestResults.xml"

# Filter tests by the specified purpose
$filteredTests = $xml.testsuites.testcase | Where-Object { $_.purpose -eq $Purpose } | ForEach-Object {
    Add-Member -NotePropertyName time -NotePropertyValue ([float]$_.time) -InputObject $_ -Force -PassThru
}

# Group tests by priority
$groupedTestsByPriority = $filteredTests | Group-Object -Property priority

# Calculate total duration for the specified purpose
$totalDuration = ($filteredTests | Measure-Object -Property time -Sum).Sum
$priorityCount = $groupedTestsByPriority.Count
$goalDuration = $totalDuration / $priorityCount

foreach ($priority in $groupedTestsByPriority) {
    Write-Output "Priority: $($priority.Name)"
    $time = ($priority.Group | Measure-Object -Property time -Sum).Sum
    Write-Output "Total Duration: $time"
    Write-Output ""
    $priority | Add-Member -NotePropertyName time -NotePropertyValue $time
}


Write-Output "Total Duration for Purpose '$Purpose': $totalDuration"
Write-Output "Priority Count: $priorityCount"
Write-Output "Goal Duration per Priority: $goalDuration"

# Function to find the best distribution of tests
function Find-BestDistribution {
    param (
        [Parameter(Mandatory = $true)]
        [array]$tests,
        [Parameter(Mandatory = $true)]
        [float]$goalDuration
    )

    $currentDuration = 0
    $selectedTests = @()
    $rejectedTests = @()

    foreach ($test in ($tests | Sort-Object -Property time)) {
        if ($currentDuration + [float]$test.time -le $goalDuration) {
            $selectedTests += $test
            $currentDuration += [float]$test.time
        }
        else {
            $rejectedTests += $test
        }
    }

    return [pscustomobject]@{
        SelectedTests = $selectedTests
        RejectedTests = $rejectedTests
    }
}

# Distribute tests to meet the goal duration for each priority
$distributedTests = @()

$carryOverTests = @()
foreach ($priorityGroup in $groupedTestsByPriority | sort-object -Property time -Descending) {
    $distribution = Find-BestDistribution -tests ($priorityGroup.Group + $carryOverTests) -goalDuration $goalDuration
    $selectedTests = $distribution.SelectedTests
    $carryOverTests = $distribution.RejectedTests
    $distributedTests += [PSCustomObject]@{
        Priority = $priorityGroup.Name
        SelectedTests = $selectedTests
        SelectedDuration = ($selectedTests | Measure-Object -Property time -Sum).Sum
    }
}

if($carryOverTests.Count -gt 0) {
    $distributedTests += [PSCustomObject]@{
        Priority = "CarryOver"
        SelectedTests = $carryOverTests
        SelectedDuration = ($carryOverTests | Measure-Object -Property time -Sum).Sum
    }
}

# Print only the tests that changed
foreach ($distribution in $distributedTests) {
    $changedTests = $distribution.SelectedTests | Where-Object { $_.Priority -ne $distribution.Priority }

    if ($changedTests.Count -gt 0) {
        Write-Output "Priority: $($distribution.Priority)"
        Write-Output "Selected Duration: $($distribution.SelectedDuration)"
        Write-Output "Changed Tests to $($distribution.Priority):"
        $changedTests | sort-object -Property time -Descending | ForEach-Object {
            Write-Output "  - $($_.name) ($($_.time) seconds)"
        }
        Write-Output ""
    }
    else {
        Write-Output "Priority: $($distribution.Priority)"
        Write-Output "Selected Duration: $($distribution.SelectedDuration)"
        Write-Output "No changes"
        Write-Output ""
    }
}
