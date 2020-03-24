# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

#-----------------

function Get-Issue
{
    param([string]$UserName,
          [string]$Repo,
          [ValidateRange(1,100)][int]$PerPage = 100)

    $body = @{
        per_page = $PerPage
    }

    $uri = "https://api.github.com/repos/$UserName/$Repo/issues"
    while ($uri)
    {
        $response = Invoke-WebRequest -Uri $uri -Body $body
        $response.Content | ConvertFrom-Json | Write-Output

        $uri = $null
        foreach ($link in $response.Headers.Link -split ',')
        {
            if ($link -match '\s*<(.*)>;\s+rel="next"')
            {
                $uri = $Matches[1]
            }
        }
    }
}

$issues = Get-Issue -UserName lzybkr -Repo PSReadline

$issues.Count

$issues | Sort-Object -Descending comments | Select-Object -First 15 | ft number,comments,title

foreach ($issue in $issues)
{
    if ($issue.labels.name -contains 'bug' -and $issue.labels.name -contains 'vi mode')
    {
        "{0} is a vi mode bug" -f $issue.url
    }
}
