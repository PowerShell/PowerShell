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
#>
    param()
}

$h = Get-Help HelpFuncForProxyTitles
$ex0 = $h.examples.example[0]
"examples count: $($h.examples.example.Count)"
"ex0 title type:  $($ex0.title.GetType().FullName)"
"ex0 title bytes: $([System.Text.Encoding]::UTF8.GetBytes($ex0.title) | ForEach-Object { '{0:X2}' -f $_ })"
"ex0 title repr:  [$($ex0.title -replace '\t','<TAB>' -replace '\n','<LF>' -replace '\r','<CR>')]"

$hc = [System.Management.Automation.ProxyCommand]::GetHelpComments($h)
"---helpComments---"
$hc
