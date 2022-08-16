# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Set a 5 minute timeout
$timeOut = (Get-Date).AddSeconds(300)
# create a file path to get the result
$output = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "RegisterMuOutput.txt"
# start the process to register MU and write the result to 
$process = Start-Process -PassThru -FilePath pwsh.exe -NoNewWindow -ArgumentList '-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', "`$null = (New-Object -ComObject Microsoft.Update.ServiceManager).AddService2('7971f918-a847-4430-9279-4a52d1efe18d', 7, '').IsPendingRegistrationWithAu > $output"
$result = $false
$null = [bool]::TryParse((Get-Content $output),[ref]$result)

# Wait for the process to exit or the timeout to pass
while (!$process.HasExited -and $timeOut -gt (Get-Date)) {
    Write-Verbose -Verbose "$($timeout.Subtract((Get-Date)).TotalMinutes) minutes left"
    Start-Sleep -Seconds 5
}

# If the process hasn't exited, exit with the timeout exit code
if (! $process.HasExited) {
    Write-Verbose -Verbose "scripted timedout, exiting with code 258"
    exit 258
} elseif (!$result) {
    Write-Verbose -Verbose "scripted failed"
    exit 1
} else {
    Write-Verbose -Verbose "scripted passed"
    exit 0
}
    
