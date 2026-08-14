# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
1. libfuzzer-dotnet-windows.exe can be installed from https://github.com/Metalnem/libfuzzer-dotnet/releases

2. sharpfuzz can be installed with dotnet-tool:
   > dotnet tool install --global SharpFuzz.CommandLine --version 2.2.0

Usage: sharpfuzz [path-to-assembly] [prefix ...]

path-to-assembly:
  The path to an assembly .dll file to instrument.

prefix:
  The class or the namespace to instrument.
  If not present, all types in the assembly will be instrumented.
  At least one prefix is required when instrumenting System.Private.CoreLib.

Examples:
  sharpfuzz Newtonsoft.Json.dll
  sharpfuzz System.Private.CoreLib.dll System.Number
  sharpfuzz System.Private.CoreLib.dll System.DateTimeFormat System.DateTimeParse
#>

param (
    [string]$libFuzzer = "$PSScriptRoot\libfuzzer-dotnet-windows.exe",
    [string]$project = "$PSScriptRoot\FuzzingApp\powershell-fuzz-tests.csproj",
    [string]$corpus = "$PSScriptRoot\inputs",
    [string]$command = "sharpfuzz.exe"
)

Set-StrictMode -Version Latest

$outputDir = "$PSScriptRoot\out"

if (Test-Path $outputDir) {
    Remove-Item -Recurse -Force $outputDir
}

Write-Host "dotnet publish $project -c Debug -o $outputDir"
dotnet publish $project -c Debug -o $outputDir
Write-Host "build completed"

$projectName = (Get-Item $project).BaseName
$projectDll = "$projectName.dll"
$project = Join-Path $outputDir $projectDll
$smaDllPath = Join-Path $outputDir "System.Management.Automation.dll"

## Instrument the specific class within the test assembly.
Write-Host "instrumenting: $project"
## !NOTE! If you instrument the class that defines "Main", it will fail.
& $command $project "FuzzTests.Target"
Write-Host "done instrumenting $project"

## Instrument any other assemblies that need to be tested.
Write-Host "instrumenting: $smaDllPath"
& $command $smaDllPath "System.Management.Automation.Remoting.RemoteSessionHyperVSocketClient"
Write-Host "done instrumenting: $smaDllPath"

$outputPath = Join-Path $outputDir "output.txt"

Write-Host "launching fuzzer on $project"
Write-Host "$libFuzzer --target_path=dotnet --target_arg=$project $corpus"
& $libFuzzer --target_path=dotnet --target_arg=$project $corpus -max_len=1024 2>&1 | Tee-Object -FilePath $outputPath
