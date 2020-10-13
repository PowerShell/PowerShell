## Requirements:
* any OS supported by PowerShell (tested on Windows 10 + WSL2 Ubuntu 18.04 )
* PowerShell 7 installed

## Steps:

```
# Download Microsoft Management Infrastructure source code
git clone --recursive --quiet https://github.com/PowerShell/MMI.git demos/Blazor/Microsoft.Management.Infrastructure

# Build on Windows > Additional Step : Force MSBuild property IsWindows to false
$propsPath = 'PowerShell.Common.props'
(Get-Content -Path $propsPath) -replace '</HighEntropyVA>', '</HighEntropyVA><IsWindows>false</IsWindows>' | Set-Content -Path $propsPath

# Build PowerShell
Import-Module Build.psm1
Start-PSBootstrap
Start-PSBuild

# Run PowerShell Blazor WebAssembly Demo
cd demos/Blazor/powershell-blazor-wasm-demo
dotnet run
```

