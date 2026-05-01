# Test from inside the newly built pwsh
$sma = [System.Reflection.Assembly]::GetAssembly([System.Management.Automation.ProxyCommand])
"SMA location: $($sma.Location)"
"SMA timestamp: $((Get-Item $sma.Location).LastWriteTime)"

$pc = $sma.GetType('System.Management.Automation.ProxyCommand')
"ProxyCommand type found: $($null -ne $pc)"

$methods = $pc.GetMethods([System.Reflection.BindingFlags]'NonPublic,Static')
"All private static methods:"
$methods | ForEach-Object { "  $($_.Name)" }
