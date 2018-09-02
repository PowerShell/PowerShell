#requires -RunAsAdministrator

param(
    $ETLFileName = '.\PerfViewData.etl',

    [Parameter(Mandatory)]
    [scriptblock]
    $ScriptBlock,

    $LogFileName = '.\perfview.log',

    $PowerShellPath = $(Get-Command pwsh.exe).Source)

$EncodedScriptBlock = [System.Convert]::ToBase64String([System.Text.Encoding]::UNICODE.GetBytes($ScriptBlock.ToString()))
$perfViewArgs = @(
    '/AcceptEula'
    '/ThreadTime'
    "/LogFile=$LogFileName"
    "/DataFile:$ETLFileName"
    '/noRundown'
    '/Merge'
    '/Zip:False'
# GCSampledObjectAllocationHigh is sometimes useful, but quite expensive so not included by default
#    '/ClrEvents=default+GCSampledObjectAllocationHigh'
    '/Providers:*Microsoft-PowerShell-Runspaces,*Microsoft-PowerShell-CommandDiscovery,*Microsoft-PowerShell-Parser,*Microsoft.Windows.PowerShell'
    'run'
    """$PowerShellPath"""
    '-NoProfile'
    '-EncodedCommand'
    $EncodedScriptBlock
)

$process = Start-Process -FilePath (Get-Command PerfView.exe).Source -ArgumentList $perfViewArgs -PassThru

$rs = [runspacefactory]::CreateRunspace($host)
$rs.Open()
$ps = [powershell]::Create()
$ps.Runspace = $rs

$null = $ps.AddCommand("Get-Content").
    AddArgument($LogFileName).
    AddParameter("Wait").
    AddParameter("Tail", 0)
$null = $ps.AddCommand("Out-Host")

# If log file doesn't exist yet, wait a little bit so Get-Content doesn't fail
while (!(Test-Path $LogFileName))
{
    Start-Sleep -Seconds 1
}

$null = $ps.BeginInvoke()
$process.WaitForExit()
$ps.Stop()

