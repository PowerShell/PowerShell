# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Module auto-loading with DefaultCommandPrefix" -Tags "CI" {
    function Set-LegacyModuleAnalysisCache {
        param(
            [string] $Path,
            [string] $ModuleManifest,
            [string] $CommandName,
            [System.Management.Automation.CommandTypes] $CommandType
        )

        $stream = [IO.File]::Open($Path, [IO.FileMode]::Create, [IO.FileAccess]::Write)
        $writer = [IO.BinaryWriter]::new($stream)
        try
        {
            function Write-CacheString([string] $Value) {
                $bytes = [Text.Encoding]::UTF8.GetBytes($Value)
                $writer.Write([int] $Value.Length)
                $writer.Write($bytes)
            }

            # Version 1 is the cache format written before DefaultCommandPrefix
            # was included in cache analysis.
            $writer.Write([Text.Encoding]::ASCII.GetBytes('PSMODULECACHE'))
            $writer.Write([byte] 1)
            $writer.Write([int] 1)
            $writer.Write((Get-Item -LiteralPath $ModuleManifest).LastWriteTime.Ticks)
            Write-CacheString $ModuleManifest
            $writer.Write([int] 1)
            Write-CacheString $CommandName
            $writer.Write([int] $CommandType)
            $writer.Write([int] -1)
        }
        finally
        {
            $writer.Dispose()
            $stream.Dispose()
        }
    }

    It "auto-loads a module with <ExportDescription> function exports" -TestCases @(
        @{
            ExportDescription = "explicit"
            FunctionName = "Get-DefaultCommandPrefixExplicit"
            FunctionsToExport = @("Get-DefaultCommandPrefixExplicit")
        }
        @{
            ExportDescription = "wildcard"
            FunctionName = "Get-DefaultCommandPrefixWildcard"
            FunctionsToExport = @("*")
        }
    ) {
        param(
            [string] $ExportDescription,
            [string] $FunctionName,
            [string[]] $FunctionsToExport
        )

        $moduleName = "DefaultCommandPrefix$ExportDescription"
        $moduleRoot = Join-Path -Path $TestDrive -ChildPath "Modules"
        $moduleDirectory = Join-Path -Path $moduleRoot -ChildPath $moduleName
        $moduleManifest = Join-Path -Path $moduleDirectory -ChildPath "$moduleName.psd1"
        $moduleScript = Join-Path -Path $moduleDirectory -ChildPath "$moduleName.psm1"
        $commandName = $FunctionName -replace "-", "-Test"
        $analysisCachePath = Join-Path -Path $TestDrive -ChildPath "$moduleName.cache"
        $testScript = Join-Path -Path $TestDrive -ChildPath "$moduleName.ps1"
        $powershell = Join-Path -Path $PSHOME -ChildPath $(if ($IsWindows) { "pwsh.exe" } else { "pwsh" })

        $null = New-Item -Path $moduleDirectory -ItemType Directory -Force
        @"
function $FunctionName {
    'module auto-loaded'
}
"@ | Set-Content -Path $moduleScript

        New-ModuleManifest -Path $moduleManifest -RootModule "$moduleName.psm1" -ModuleVersion "1.0.0" -Guid ([guid]::NewGuid()) -DefaultCommandPrefix "Test" -FunctionsToExport $FunctionsToExport -CmdletsToExport @() -AliasesToExport @()

        @'
param(
    [string] $ModuleRoot,
    [string] $AnalysisCachePath,
    [string] $CommandName
)

$ErrorActionPreference = 'Stop'
$env:PSModulePath = "$ModuleRoot$([IO.Path]::PathSeparator)$env:PSModulePath"
$env:PSModuleAnalysisCachePath = $AnalysisCachePath

& $CommandName
'@ | Set-Content -Path $testScript

        $result = & $powershell -NoProfile -File $testScript -ModuleRoot $moduleRoot -AnalysisCachePath $analysisCachePath -CommandName $commandName

        $LASTEXITCODE | Should -Be 0
        $result | Should -BeExactly "module auto-loaded"
    }

    It "auto-loads a module with a convertible default command prefix" {
        $moduleName = "DefaultCommandPrefixConvertible"
        $moduleRoot = Join-Path -Path $TestDrive -ChildPath "Modules"
        $moduleDirectory = Join-Path -Path $moduleRoot -ChildPath $moduleName
        $moduleManifest = Join-Path -Path $moduleDirectory -ChildPath "$moduleName.psd1"
        $moduleScript = Join-Path -Path $moduleDirectory -ChildPath "$moduleName.psm1"
        $functionName = "Get-DefaultCommandPrefixConvertible"
        $commandName = $functionName -replace "-", "-42"
        $analysisCachePath = Join-Path -Path $TestDrive -ChildPath "$moduleName.cache"
        $testScript = Join-Path -Path $TestDrive -ChildPath "$moduleName.ps1"
        $powershell = Join-Path -Path $PSHOME -ChildPath $(if ($IsWindows) { "pwsh.exe" } else { "pwsh" })

        $null = New-Item -Path $moduleDirectory -ItemType Directory -Force
        @"
function $functionName {
    'convertible prefix auto-loaded'
}
"@ | Set-Content -Path $moduleScript

        @"
@{
    RootModule = '$moduleName.psm1'
    ModuleVersion = '1.0.0'
    GUID = '$([guid]::NewGuid())'
    DefaultCommandPrefix = 42
    FunctionsToExport = @('$functionName')
    CmdletsToExport = @()
    AliasesToExport = @()
}
"@ | Set-Content -Path $moduleManifest

        @'
param(
    [string] $ModuleRoot,
    [string] $AnalysisCachePath,
    [string] $CommandName
)

$ErrorActionPreference = 'Stop'
$env:PSModulePath = "$ModuleRoot$([IO.Path]::PathSeparator)$env:PSModulePath"
$env:PSModuleAnalysisCachePath = $AnalysisCachePath

& $CommandName
'@ | Set-Content -Path $testScript

        $result = & $powershell -NoProfile -File $testScript -ModuleRoot $moduleRoot -AnalysisCachePath $analysisCachePath -CommandName $commandName

        $LASTEXITCODE | Should -Be 0
        $result | Should -BeExactly "convertible prefix auto-loaded"
    }

    It "auto-loads a module with an explicit prefixed alias" {
        $moduleName = "DefaultCommandPrefixAlias"
        $moduleRoot = Join-Path -Path $TestDrive -ChildPath "Modules"
        $moduleDirectory = Join-Path -Path $moduleRoot -ChildPath $moduleName
        $moduleManifest = Join-Path -Path $moduleDirectory -ChildPath "$moduleName.psd1"
        $moduleScript = Join-Path -Path $moduleDirectory -ChildPath "$moduleName.psm1"
        $functionName = "Get-DefaultCommandPrefixAliasTarget"
        $aliasName = "Get-DefaultCommandPrefixAlias"
        $commandName = $aliasName -replace "-", "-Test"
        $analysisCachePath = Join-Path -Path $TestDrive -ChildPath "$moduleName.cache"
        $testScript = Join-Path -Path $TestDrive -ChildPath "$moduleName.ps1"
        $powershell = Join-Path -Path $PSHOME -ChildPath $(if ($IsWindows) { "pwsh.exe" } else { "pwsh" })

        $null = New-Item -Path $moduleDirectory -ItemType Directory -Force
        @"
function $functionName {
    'prefixed alias auto-loaded'
}

Set-Alias -Name $aliasName -Value $functionName
"@ | Set-Content -Path $moduleScript

        New-ModuleManifest -Path $moduleManifest -RootModule "$moduleName.psm1" -ModuleVersion "1.0.0" -Guid ([guid]::NewGuid()) -DefaultCommandPrefix "Test" -FunctionsToExport $functionName -CmdletsToExport @() -AliasesToExport $aliasName

        @'
param(
    [string] $ModuleRoot,
    [string] $AnalysisCachePath,
    [string] $CommandName
)

$ErrorActionPreference = 'Stop'
$env:PSModulePath = "$ModuleRoot$([IO.Path]::PathSeparator)$env:PSModulePath"
$env:PSModuleAnalysisCachePath = $AnalysisCachePath

& $CommandName
'@ | Set-Content -Path $testScript

        $result = & $powershell -NoProfile -File $testScript -ModuleRoot $moduleRoot -AnalysisCachePath $analysisCachePath -CommandName $commandName

        $LASTEXITCODE | Should -Be 0
        $result | Should -BeExactly "prefixed alias auto-loaded"
    }

    It "auto-loads a module with a parenthesized default command prefix key" {
        $moduleName = "DefaultCommandPrefixParenthesized"
        $moduleRoot = Join-Path -Path $TestDrive -ChildPath "Modules"
        $moduleDirectory = Join-Path -Path $moduleRoot -ChildPath $moduleName
        $moduleManifest = Join-Path -Path $moduleDirectory -ChildPath "$moduleName.psd1"
        $moduleScript = Join-Path -Path $moduleDirectory -ChildPath "$moduleName.psm1"
        $functionName = "Get-DefaultCommandPrefixParenthesized"
        $commandName = $functionName -replace "-", "-Parenthesized"
        $analysisCachePath = Join-Path -Path $TestDrive -ChildPath "$moduleName.cache"
        $testScript = Join-Path -Path $TestDrive -ChildPath "$moduleName.ps1"
        $powershell = Join-Path -Path $PSHOME -ChildPath $(if ($IsWindows) { "pwsh.exe" } else { "pwsh" })

        $null = New-Item -Path $moduleDirectory -ItemType Directory -Force
        @"
function $functionName {
    'parenthesized prefix auto-loaded'
}
"@ | Set-Content -Path $moduleScript

        @"
@{
    RootModule = '$moduleName.psm1'
    ModuleVersion = '1.0.0'
    GUID = '$([guid]::NewGuid())'
    ('DefaultCommandPrefix') = 'Parenthesized'
    FunctionsToExport = @('$functionName')
    CmdletsToExport = @()
    AliasesToExport = @()
}
"@ | Set-Content -Path $moduleManifest

        @'
param(
    [string] $ModuleRoot,
    [string] $AnalysisCachePath,
    [string] $CommandName
)

$ErrorActionPreference = 'Stop'
$env:PSModulePath = "$ModuleRoot$([IO.Path]::PathSeparator)$env:PSModulePath"
$env:PSModuleAnalysisCachePath = $AnalysisCachePath

& $CommandName
'@ | Set-Content -Path $testScript

        $result = & $powershell -NoProfile -File $testScript -ModuleRoot $moduleRoot -AnalysisCachePath $analysisCachePath -CommandName $commandName

        $LASTEXITCODE | Should -Be 0
        $result | Should -BeExactly "parenthesized prefix auto-loaded"
    }

    It "does not auto-load a module for its unprefixed command" {
        $moduleName = "DefaultCommandPrefixUnprefixed"
        $moduleRoot = Join-Path -Path $TestDrive -ChildPath "Modules"
        $moduleDirectory = Join-Path -Path $moduleRoot -ChildPath $moduleName
        $moduleManifest = Join-Path -Path $moduleDirectory -ChildPath "$moduleName.psd1"
        $moduleScript = Join-Path -Path $moduleDirectory -ChildPath "$moduleName.psm1"
        $functionName = "Get-DefaultCommandPrefixUnprefixed"
        $analysisCachePath = Join-Path -Path $TestDrive -ChildPath "$moduleName.cache"
        $testScript = Join-Path -Path $TestDrive -ChildPath "$moduleName.ps1"
        $powershell = Join-Path -Path $PSHOME -ChildPath $(if ($IsWindows) { "pwsh.exe" } else { "pwsh" })

        $null = New-Item -Path $moduleDirectory -ItemType Directory -Force
        @"
function $functionName {
    'module auto-loaded'
}
"@ | Set-Content -Path $moduleScript

        New-ModuleManifest -Path $moduleManifest -RootModule "$moduleName.psm1" -ModuleVersion "1.0.0" -Guid ([guid]::NewGuid()) -DefaultCommandPrefix "Test" -FunctionsToExport $functionName -CmdletsToExport @() -AliasesToExport @()

        @'
param(
    [string] $ModuleRoot,
    [string] $AnalysisCachePath,
    [string] $ModuleName,
    [string] $CommandName
)

$ErrorActionPreference = 'Stop'
$env:PSModulePath = "$ModuleRoot$([IO.Path]::PathSeparator)$env:PSModulePath"
$env:PSModuleAnalysisCachePath = $AnalysisCachePath

try {
    & $CommandName
    throw "Unexpectedly resolved unprefixed command: $CommandName"
}
catch [System.Management.Automation.CommandNotFoundException] {
}

if (Get-Module -Name $ModuleName) {
    throw "Unexpectedly auto-loaded module: $ModuleName"
}

'not auto-loaded'
'@ | Set-Content -Path $testScript

        $result = & $powershell -NoProfile -File $testScript -ModuleRoot $moduleRoot -AnalysisCachePath $analysisCachePath -ModuleName $moduleName -CommandName $functionName

        $LASTEXITCODE | Should -Be 0
        $result | Should -BeExactly "not auto-loaded"
    }

    It "discards version 1 cache entries that contain unprefixed exports" {
        $moduleName = "DefaultCommandPrefixStaleCache"
        $moduleRoot = Join-Path -Path $TestDrive -ChildPath "Modules"
        $moduleDirectory = Join-Path -Path $moduleRoot -ChildPath $moduleName
        $moduleManifest = Join-Path -Path $moduleDirectory -ChildPath "$moduleName.psd1"
        $moduleScript = Join-Path -Path $moduleDirectory -ChildPath "$moduleName.psm1"
        $functionName = "Get-DefaultCommandPrefixStaleCache"
        $commandName = $functionName -replace "-", "-Test"
        $analysisCachePath = Join-Path -Path $TestDrive -ChildPath "$moduleName.cache"
        $testScript = Join-Path -Path $TestDrive -ChildPath "$moduleName.ps1"
        $powershell = Join-Path -Path $PSHOME -ChildPath $(if ($IsWindows) { "pwsh.exe" } else { "pwsh" })

        $null = New-Item -Path $moduleDirectory -ItemType Directory -Force
        @"
function $functionName {
    'stale cache discarded'
}
"@ | Set-Content -Path $moduleScript

        New-ModuleManifest -Path $moduleManifest -RootModule "$moduleName.psm1" -ModuleVersion "1.0.0" -Guid ([guid]::NewGuid()) -DefaultCommandPrefix "Test" -FunctionsToExport $functionName -CmdletsToExport @() -AliasesToExport @()
        Set-LegacyModuleAnalysisCache -Path $analysisCachePath -ModuleManifest $moduleManifest -CommandName $functionName -CommandType Function

        @'
param(
    [string] $ModuleRoot,
    [string] $AnalysisCachePath,
    [string] $CommandName
)

$ErrorActionPreference = 'Stop'
$env:PSModulePath = "$ModuleRoot$([IO.Path]::PathSeparator)$env:PSModulePath"
$env:PSModuleAnalysisCachePath = $AnalysisCachePath

& $CommandName
'@ | Set-Content -Path $testScript

        $result = & $powershell -NoProfile -File $testScript -ModuleRoot $moduleRoot -AnalysisCachePath $analysisCachePath -CommandName $commandName

        $LASTEXITCODE | Should -Be 0
        $result | Should -BeExactly "stale cache discarded"
    }

}
