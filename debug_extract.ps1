function HelpFuncForProxyTitles {
<#
  .SYNOPSIS
  A function with titled examples for proxy testing.

  .EXAMPLE Retrieving processes
  Get-Process

  Gets all processes
#>
    param()
}

$h = Get-Help HelpFuncForProxyTitles
$ex = $h.examples.example[0]
$titleStr = $ex.title
"title: [$titleStr]"

# Call ExtractExampleTitle via reflection
$sma = [System.Reflection.Assembly]::GetAssembly([System.Management.Automation.ProxyCommand])
$pc = $sma.GetType('System.Management.Automation.ProxyCommand')
$m = $pc.GetMethod('ExtractExampleTitle', [System.Reflection.BindingFlags]'NonPublic,Static')
"Method found: $($null -ne $m)"

$result = $m.Invoke($null, @($titleStr))
"ExtractExampleTitle result: [$result]"
"Is null: $($null -eq $result)"

# Also test with empty string
$result2 = $m.Invoke($null, @(""))
"Empty string result: [$result2]"

# Test the untitled format
$untitled = "-------------------------- EXAMPLE 2 --------------------------"
$result3 = $m.Invoke($null, @($untitled))
"Untitled result: [$result3]"
