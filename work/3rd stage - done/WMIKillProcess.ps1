<#
Just a script to shut down either many or hard to catch processes.
AS simple as cake.
Last build:
UI221223
#>
$CI = Read-Host -Prompt "CI?"
$Task = Read-Host -Prompt "Prozess? Fenstername/exe/PID"
$OpenData = Read-Host -Prompt "sensible Daten?"
Get-Process -

$PIDs = foreach ($process in $PIDs) {
  Get-WmiObject -Class Win32_Process -ComputerName $ComputerName -Filter "name='$ProcessName*'"

$returnval = $process.terminate()
$processid = $process.handle
$returnval.count -eq 0
write-host "The process '$ProcessName' PID: $processid terminated successfully"
write-host "The process '$ProcessName' PID: $processid termination has some problems"
<#

 #>