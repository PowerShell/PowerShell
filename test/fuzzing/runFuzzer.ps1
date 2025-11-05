# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param (
    [string]$libFuzzer = ".\libfuzzer-dotnet-windows.exe",
    [string]$project = ".\FuzzingApp\powershell-fuzz-tests.csproj",
    [Parameter(Mandatory=$true)]
    [string]$corpus,
    [string]$command = "sharpfuzz.exe"
)

Set-StrictMode -Version Latest

$outputDir = "out"

if (Test-Path $outputDir) {
    Remove-Item -Recurse -Force $outputDir
}

Write-Host "dotnet publish $project -c release -o $outputDir"
dotnet publish $project -c release -o $outputDir
Write-Host "build completed"

$projectName = (Get-Item $project).BaseName
$projectDll = "$projectName.dll"
$project = Join-Path $outputDir $projectDll

$exclusions = @(
    "dnlib.dll",
    "SharpFuzz.dll",
    "SharpFuzz.Common.dll"
)

$exclusions += $projectDll

Write-Host "instrumenting: $project"
& $command $project Target
Write-Host "done instrumenting $project"

$fuzzingTargets = Get-ChildItem $outputDir -Filter *.dll `
| Where-Object { $_.Name -notin $exclusions } `
| Where-Object { $_.Name -notlike "System.*.dll" }
| Where-Object { $_.Name -notlike "Newtonsoft.*.dll" }
| Where-Object { $_.Name -notlike "Microsoft.*.dll" }

foreach ($fuzzingTarget in $fuzzingTargets) {
    Write-Output "Instrumenting $fuzzingTarget"
    & $command $fuzzingTarget.FullName

    if ($LastExitCode -ne 0) {
        Write-Error "An error occurred while instrumenting $fuzzingTarget"
        exit 1
    }
}

$smaDllPath = Join-Path $outputDir "System.Management.Automation.dll"

Write-Host "instrumenting: $smaDllPath"
& $command $smaDllPath Remoting
Write-Host "done instrumenting: $smaDllPath"

$fuzzingTargets += $projectDll
$fuzzingTargets += $smaDllPath

if (($fuzzingTargets | Measure-Object).Count -eq 0) {
    Write-Error "No fuzzing targets found"
    exit 1
}

$outputPath = Join-Path $outputDir "output.txt"

Write-Host "launching fuzzer on $project"
Write-Host "$libFuzzer --target_path=dotnet --target_arg=$project $corpus"
& $libFuzzer --target_path=dotnet --target_arg=$project $corpus -max_len=1024 2>&1 `
| Tee-Object -FilePath $outputPath
