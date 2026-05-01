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

"--- Property info ---"
$prop = $ex.PSObject.Properties['title']
"Property name: $($prop.Name)"
"Property TypeNameOfValue: $($prop.TypeNameOfValue)"
"Property Value type: $($prop.Value.GetType().FullName)"
"Property Value: [$($prop.Value)]"
"Value is string: $($prop.Value -is [string])"
"Value is PSObject: $($prop.Value -is [System.Management.Automation.PSObject])"

# Test: would 'Value as string' (C# behavior) work?
# In PS, casting null returns null (won't throw)
$asString = $prop.Value -as [string]
"Value -as string: [$asString]"

# If it's a PSObject, get its base object
if ($prop.Value -is [System.Management.Automation.PSObject]) {
    $base = $prop.Value.BaseObject
    "BaseObject type: $($base.GetType().FullName)"
    "BaseObject: [$base]"
}
