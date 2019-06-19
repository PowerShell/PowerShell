#requires -RunAsAdministrator

param(
    $ETLFileName = '.\PerfViewData.etl',

    [Parameter(Mandatory)]
    [scriptblock]
    $ScriptBlock,

    $LogFileName = '.\perfview.log',
    $PowerShellPath = $(Get-Command -Name pwsh.exe).Source,
    $PerfViewPath = $(Get-Command -Name PerfView.exe).Source
)

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

$process = Start-Process -FilePath $PerfViewPath -ArgumentList $perfViewArgs -PassThru
$process.WaitForExit()

Get-Content $LogFileName | Out-Host
