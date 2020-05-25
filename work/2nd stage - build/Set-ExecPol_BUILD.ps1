<# 
Set-ExecPol, lazy af...
last build 11.11.2018
UI221223
#>
Get-Date
Write-Host "Current Execution Policy:"`n
$GPO1 = Get-ExecutionPolicy -List
$GPO1 | Format-Table -Property Scope, ExecutionPolicy
Write-Host "Process, LocalUser, LocalMachine`nswitch to`n--> UNRESTRICTED <---"

$YoN = ((Read-Host -Prompt "Please press [Y] to proceed and [N] to abort safely.") | Out-String)
if ($YoN -match "y*") {
    Set-ExecutionPolicy Unrestricted -Force -ErrorAction SilentlyContinue
    Set-ExecutionPolicy Unrestricted -Scope Process -ErrorAction SilentlyContinue
    Write-Host "Execution Policy now:"
} #end if
elseif ($YoN -like "n*") {
    Write-Host "Aborting...`n`nExecution Policy will remain like this:"
    Get-Date
    $GPO2 = Get-ExecutionPolicy -List
    $GPO2 | Format-Table -Property Scope, ExecutionPolicy
    Read-Host "Hit return to close this window."
} #end elseif