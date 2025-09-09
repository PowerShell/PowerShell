# PipeName Module

A PowerShell module for managing the PipeName App.

## Running PipeName

```powershell
Import-Module .\build.psm1
Publish-PSTestTools
$pipeName = Get-PipeName
$Listener = Start-PipeServer -PipeName $pipeName
```

## Stopping PipeName

```powershell
Stop-PipeServer -PipeName $pipeName
