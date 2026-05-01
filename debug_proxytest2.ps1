$title = "-------------------------- EXAMPLE 1: Retrieving processes --------------------------"
"Input: [$title]"

# Test the regex directly
$pattern = '^\s*-+\s*(?<inner>.*?)\s*-+\s*$'
$match = [regex]::Match($title, $pattern)
"Regex success: $($match.Success)"
if ($match.Success) {
    "Inner: [$($match.Groups['inner'].Value)]"
    $inner = $match.Groups['inner'].Value
    $colonIdx = $inner.IndexOf(':')
    "colon index: $colonIdx"
    if ($colonIdx -ge 0) {
        $extracted = $inner.Substring($colonIdx + 1).Trim()
        "extracted title: [$extracted]"
    }
}

# Now test via actual ProxyCommand
function HelpFuncForProxyTitles {
<#
  .SYNOPSIS
  A function with titled examples for proxy testing.

  .EXAMPLE Retrieving processes
  Get-Process

  Gets all processes

  .EXAMPLE
  Get-Service

  Gets all services

  .EXAMPLE Listing items in a directory
  Get-ChildItem -Path C:\

  Lists items in C:\
#>
    param()
}

$h = Get-Help HelpFuncForProxyTitles
"examples count: $($h.examples.example.Count)"
foreach ($i in 0..($h.examples.example.Count - 1)) {
    $ex = $h.examples.example[$i]
    "ex[$i] title: [$($ex.title)]"
}

$hc = [System.Management.Automation.ProxyCommand]::GetHelpComments($h)
"---helpComments---"
$hc
