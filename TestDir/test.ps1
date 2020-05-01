import-module /mnt/d/repos/PowerShell/build.psm1
$Env:POWERSHELL_TELEMETRY_OPTOUT = 1
Start-PSPester /mnt/d/repos/PowerShell/test/powershell/Modules/Microsoft.PowerShell.Management/FileSystem.Tests.ps1 -Tag CI -ExcludeTag RequireSudoOnUnix
