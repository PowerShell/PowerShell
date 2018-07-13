# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$script:oldModulePath = $env:PSModulePath

function Add-ModulePath
{
    param([string]$Path, [switch]$Prepend)

    $script:oldModulePath = $env:PSModulePath

    if ($Prepend)
    {
        $env:PSModulePAth = $Path + [System.IO.Path]::PathSeparator + $env:PSModulePath
    }
    else
    {
        $env:PSModulePath = $env:PSModulePath + [System.IO.Path]::PathSeparator + $Path
    }
}

function Restore-ModulePath
{
    $env:PSModulePath = $script:oldModulePath
}

# Creates a new dummy module compatible with the given PSEditions
function New-EditionCompatibleModule
{
    param(
        [Parameter(Mandatory = $true)][string]$ModuleName,
        [string]$DirPath,
        [string[]]$CompatiblePSEditions)

    $modulePath = Join-Path $DirPath $ModuleName

    $manifestPath = Join-Path $modulePath "$ModuleName.psd1"

    $psm1Name = "$ModuleName.psm1"
    $psm1Path = Join-Path $modulePath $psm1Name

    New-Item -Path $modulePath -ItemType Directory

    New-Item -Path $psm1Path -Value "function Test-$ModuleName { `$true }"

    if ($CompatiblePSEditions)
    {
        New-ModuleManifest -Path $manifestPath -CompatiblePSEditions $CompatiblePSEditions -RootModule $psm1Name
    }
    else
    {
        New-ModuleManifest -Path $manifestPath -RootModule $psm1Name
    }

    return $modulePath
}

function New-TestModules
{
    param([hashtable[]]$TestCases, [string]$BaseDir)

    for ($i = 0; $i -lt $TestCases.Count; $i++)
    {
        $path = New-EditionCompatibleModule -ModuleName $TestCases[$i].ModuleName -CompatiblePSEditions $TestCases[$i].Editions -Dir $BaseDir

        $TestCases[$i].Path = $path
        $TestCases[$i].Name = $TestCases[$i].Editions -join ","
    }
}

Describe "Get-Module with CompatiblePSEditions-checked paths" -Tag "CI" {

    BeforeAll {
        $successCases = @(
            @{ Editions = "Core","Desktop"; ModuleName = "BothModule" },
            @{ Editions = "Core"; ModuleName = "CoreModule" }
        )

        $failCases = @(
            @{ Editions = "Desktop"; ModuleName = "DesktopModule" },
            @{ Editions = $null; ModuleName = "NeitherModule" }
        )

        $basePath = Join-Path $TestDrive "EditionCompatibleModules"
        New-TestModules -TestCases $successCases -BaseDir $basePath
        New-TestModules -TestCases $failCases -BaseDir $basePath

        # Emulate the System32 module path for tests
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("TestWindowsPowerShellPSHomeLocation", $basePath)
    }

    AfterAll {
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("TestWindowsPowerShellPSHomeLocation", $null)
    }

    Context "Loading from checked paths on the module path with no flags" {
        BeforeAll {
            Add-ModulePath $basePath
            $modules = Get-Module -ListAvailable
        }

        AfterAll {
            Restore-ModulePath
        }

        It "Lists compatible modules from the module path with -ListAvailable for PSEdition <Editions>" -TestCases $successCases -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName)

            $modules.Name | Should -Contain $ModuleName
        }

        It "Does not list incompatible modules with -ListAvailable for PSEdition <Editions>" -TestCases $failCases -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName)

            $modules.Name | Should -Not -Contain $ModuleName
        }
    }

    Context "Loading from checked paths by absolute path with no flags" {
        It "Lists compatible modules with -ListAvailable for PSEdition <Editions>" -TestCases $successCases -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName)

            $modules = Get-Module -ListAvailable (Join-Path -Path $basePath -ChildPath $ModuleName)

            $modules.Name | Should -Contain $ModuleName
        }

        It "Does not list incompatible modules with -ListAvailable for PSEdition <Editions>" -TestCases $failCases -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName)

            $modules = Get-Module -ListAvailable (Join-Path -Path $basePath -ChildPath $ModuleName)

            $modules.Name | Should -Not -Contain $ModuleName
        }
    }

    Context "Loading from checked paths on the module path with -SkipEditionCheck" {
        BeforeAll {
            Add-ModulePath $basePath
            $modules = Get-Module -ListAvailable -SkipEditionCheck
        }

        AfterAll {
            Restore-ModulePath
        }

        It "Lists all modules from the module path with -ListAvailable for PSEdition <Editions>" -TestCases ($successCases + $failCases) -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName)

            $modules.Name | Should -Contain $ModuleName
        }
    }

    Context "Loading from checked paths by absolute path with -SkipEditionCheck" {
        It "Lists compatible modules with -ListAvailable for PSEdition <Editions>" -TestCases ($successCases + $failCases) -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName)

            $modules = Get-Module -ListAvailable -SkipEditionCheck (Join-Path -Path $basePath -ChildPath $ModuleName)

            $modules.Name | Should -Contain $ModuleName
        }
    }
}

Describe "Import-Module from CompatiblePSEditions-checked paths" -Tag "CI" {
    BeforeAll {
        $successCases = @(
            @{ Editions = "Core","Desktop"; ModuleName = "BothModule"; Result = $true },
            @{ Editions = "Core"; ModuleName = "CoreModule"; Result = $true }
        )

        $failCases = @(
            @{ Editions = "Desktop"; ModuleName = "DesktopModule"; Result = $true },
            @{ Editions = $null; ModuleName = "NeitherModule"; Result = $true }
        )

        $basePath = Join-Path $TestDrive "EditionCompatibleModules"
        New-TestModules -TestCases $successCases -BaseDir $basePath
        New-TestModules -TestCases $failCases -BaseDir $basePath

        $allModules = ($successCases + $failCases).ModuleName

        # Emulate the System32 module path for tests
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("TestWindowsPowerShellPSHomeLocation", $basePath)
    }

    AfterAll {
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("TestWindowsPowerShellPSHomeLocation", $null)
    }

    AfterEach {
        Get-Module $allModules | Remove-Module -Force
    }

    Context "Imports from module path" {
        BeforeAll {
            Add-ModulePath $basePath
        }

        AfterAll {
            Restore-ModulePath
        }

        It "Successfully imports compatible modules from the module path with PSEdition <Editions>" -TestCases $successCases -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName, $Result)

            Import-Module $ModuleName -Force
            & "Test-$ModuleName" | Should -Be $Result
        }

        It "Fails to import incompatible modules from the module path with PSEdition <Editions>" -TestCases $failCases -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName, $Result)

            { Import-Module $ModuleName -Force -ErrorAction 'Stop'; & "Test-$ModuleName" } | Should -Throw -ErrorId "Modules_PSEditionNotSupported,Microsoft.PowerShell.Commands.ImportModuleCommand"
        }

        It "Imports an incompatible module from the module path with -SkipEditionCheck with PSEdition <Editions>" -TestCases ($successCases + $failCases) -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName, $Result)

            Import-Module $ModuleName -SkipEditionCheck -Force
            & "Test-$ModuleName" | Should -Be $Result
        }
    }

    Context "Imports from absolute path" {
        It "Successfully imports compatible modules from an absolute path with PSEdition <Editions>" -TestCases $successCases -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName, $Result)

            $path = Join-Path -Path $basePath -ChildPath $ModuleName

            Import-Module $path -Force
            & "Test-$ModuleName" | Should -Be $Result
        }

        It "Fails to import incompatible modules from an absolute path with PSEdition <Editions>" -TestCases $failCases -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName, $Result)

            $path = Join-Path -Path $basePath -ChildPath $ModuleName

            { Import-Module $path -Force -ErrorAction 'Stop'; & "Test-$ModuleName" } | Should -Throw -ErrorId "Modules_PSEditionNotSupported,Microsoft.PowerShell.Commands.ImportModuleCommand"
        }

        It "Imports an incompatible module from an absolute path with -SkipEditionCheck with PSEdition <Editions>" -TestCases ($successCases + $failCases) -Skip:(-not $IsWindows) {
            param($Editions, $ModuleName, $Result)

            $path = Join-Path -Path $basePath -ChildPath $ModuleName

            Import-Module $path -SkipEditionCheck -Force
            & "Test-$ModuleName" | Should -Be $Result
        }
    }
}

Describe "PSModulePath changes interacting with other PowerShell processes" -Tag "Feature" {
    BeforeAll {
        Add-ModulePath (Join-Path $env:windir "System32\WindowsPowerShell\v1.0\Modules") -Prepend
    }

    AfterAll {
        Restore-ModulePath
    }

    It "Allows Windows PowerShell subprocesses to call `$PSHome modules still" -Skip:(-not $IsWindows) {
        $errors = powershell.exe -Command "Get-ChildItem" 2>&1 | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] }
        $errors | Should -Be $null
    }

    It "Allows PowerShell Core 6 subprocesses to call core modules" -Skip:(-not $IsWindows) {
        $errors = pwsh.exe -Command "Get-ChildItem" 2>&1 | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] }
        $errors | Should -Be $null
    }
}

Describe "Nested module behaviour" -Tag "Feature" {
    BeforeAll {
        $testConditions = @{
            SkipEditionCheck = @($true, $false)
            UseRootModule = @($true, $false)
            UseAbsolutePath = @($true, $false)
            MarkedEdition = @($null, "Desktop", "Core", @("Desktop","Core"))
        }

        # Combine all the test conditions into a list of test cases
        $testCases = @(@{})
        foreach ($condition in $testConditions.Keys)
        {
            $list = [System.Collections.Generic.List[hashtable]]::new()
            foreach ($obj in $testCases)
            {
                foreach ($value in $testConditions[$condition])
                {
                    $list.Add($obj + @{ $condition = $value })
                }
            }
            $testCases = $list
        }

        # Define nested script module
        $scriptModuleName = "NestedScriptModule"
        $scriptModuleFile = "$scriptModuleName.psm1"
        $scriptModuleContent = 'function Test-ScriptModule { return $true }'

        # Define nested binary module
        $binaryModuleName = "NestedBinaryModule"
        $binaryModuleFile = "$binaryModuleName.dll"
        $binaryModuleContent = 'public static class TestBinaryModuleClass { public static bool Test() { return true; } }'
        $binaryModuleSourcePath = Join-Path $TestDrive $binaryModuleFile
        Add-Type -OutputAssembly $binaryModuleSourcePath -TypeDefinition $binaryModuleContent

        # Define root module definition
        $rootModuleName = "RootModule"
        $rootModuleFile = "$rootModuleName.psm1"
        $rootModuleContent = 'function Test-RootModule { Test-ScriptModule; [TestBinaryModuleClass]::Test() }'

        # Module directory structure: $TestDrive/$compatibility/$guid/$moduleName/{module parts}
        $compatibleDir = "Compatible"
        $incompatibleDir = "Incompatible"
        $compatiblePath = Join-Path $TestDrive $compatibleDir
        $incompatiblePath = Join-Path $TestDrive $incompatibleDir

        foreach ($basePath in $compatiblePath,$incompatiblePath)
        {
            New-Item -Path $basePath -ItemType Directory
        }

        # Set up the test state
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("TestWindowsPowerShellPSHomeLocation", $incompatiblePath)
    }

    AfterAll {
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("TestWindowsPowerShellPSHomeLocation", $null)
    }

    Context "Get-Module -ListAvailable -All results OFF the System32 path" {
        BeforeEach {
            # Create the module directory
            $guid = New-Guid
            $compatibilityDir = $compatibleDir
            $containingDir = Join-Path $TestDrive $compatibilityDir $guid
            $moduleBase = Join-Path $containingDir "CpseTestModule"
            New-Item -Path $moduleBase -ItemType Directory
            Add-ModulePath $containingDir
        }

        AfterEach {
            Restore-ModulePath
        }

        It "Gets all compatible modules when SkipEditionCheck: <SkipEditionCheck>, using root module: <UseRootModule>, using absolute path: <UseAbsolutePath>, CompatiblePSEditions: <MarkedEdition>" -TestCases $testCases -Skip:(-not $IsWindows){
            param([bool]$SkipEditionCheck, [bool]$UseRootModule, [bool]$UseAbsolutePath, [string[]]$MarkedEdition)

            # Add nested module bits
            $nestedModules = [System.Collections.ArrayList]::new()
            # Script module
            New-Item -Path (Join-Path $moduleBase $scriptModuleFile) -Value $scriptModuleContent
            # Binary module
            Copy-Item -Path $binaryModuleSourcePath -Destination (Join-Path $moduleBase $binaryModuleFile)
            # Root module
            if ($UseRootModule)
            {
                New-Item -Path (Join-Path $moduleBase $rootModuleFile) -Value $rootModuleContent
            }

            $nestedModules = $scriptModuleFile,$binaryModuleFile
            $nestedModuleNames = $nestedModules -join ','

            # Create the manifest
            $moduleName = Split-Path -Leaf $moduleBase
            $manifestPath = Join-Path $moduleBase "$moduleName.psd1"
            $compatibleEditions = if ($CompatiblePSEditions) { $CompatiblePSEditions -join "," }

            $newManifestCmd = "New-ModuleManifest -Path $manifestPath -NestedModules $nestedModuleNames "
            if ($compatibleEditions) { $newManifestCmd += "-CompatiblePSEditions $compatibleEditions " }
            if ($UseRootModule) { $newManifestCmd += "-RootModule $rootModuleFile " }
            [scriptblock]::Create($newManifestCmd).Invoke()

            # Modules specified with an absolute path should only return themselves
            if ($UseAbsolutePath)
            {
                $modules = Get-Module -ListAvailable -All $moduleBase

                $modules.Count | Should -Be 1
                $modules[0].Name  | Should -BeExactly $moduleName
                return
            }

            $modules = if ($SkipEditionCheck)
            {
                Get-Module -ListAvailable -All -SkipEditionCheck | Where-Object { $_.Path.Contains($guid) }
            }
            else
            {
                Get-Module -ListAvailable -All | Where-Object { $_.Path.Contains($guid) }
            }

            if ($UseRootModule)
            {
                $modules.Count | Should -Be 4
            }
            else
            {
                $modules.Count | Should -Be 3
            }

            $names = $modules.Name
            $names | Should -Contain $moduleName
            $names | Should -Contain $scriptModuleName
            $names | Should -Contain $binaryModuleName
        }
    }

    Context "Get-Module -ListAvailable -All results ON the System32 path" {
        BeforeEach {
            # Create the module directory
            $guid = New-Guid
            $compatibilityDir = $incompatibleDir
            $containingDir = Join-Path $TestDrive $compatibilityDir $guid
            $moduleBase = Join-Path $containingDir "CpseTestModule"
            New-Item -Path $moduleBase -ItemType Directory
            Add-ModulePath $containingDir
        }

        AfterEach {
            Restore-ModulePath
        }

        It "Gets all compatible modules when SkipEditionCheck: <SkipEditionCheck>, using root module: <UseRootModule>, using absolute path: <UseAbsolutePath>, CompatiblePSEditions: <MarkedEdition>" -TestCases $testCases -Skip:(-not $IsWindows){
            param([bool]$SkipEditionCheck, [bool]$UseRootModule, [bool]$UseAbsolutePath, [string[]]$MarkedEdition)

            # Add nested module bits
            $nestedModules = [System.Collections.ArrayList]::new()
            # Script module
            New-Item -Path (Join-Path $moduleBase $scriptModuleFile) -Value $scriptModuleContent
            # Binary module
            Copy-Item -Path $binaryModuleSourcePath -Destination (Join-Path $moduleBase $binaryModuleFile)
            # Root module
            if ($UseRootModule)
            {
                New-Item -Path (Join-Path $moduleBase $rootModuleFile) -Value $rootModuleContent
            }

            $nestedModules = $scriptModuleFile,$binaryModuleFile
            $nestedModuleNames = $nestedModules -join ','

            # Create the manifest
            $moduleName = Split-Path -Leaf $moduleBase
            $manifestPath = Join-Path $moduleBase "$moduleName.psd1"
            $compatibleEditions = if ($MarkedEdition) { $MarkedEdition -join "," }

            $newManifestCmd = "New-ModuleManifest -Path $manifestPath -NestedModules $nestedModuleNames "
            if ($compatibleEditions) { $newManifestCmd += "-CompatiblePSEditions $compatibleEditions " }
            if ($UseRootModule) { $newManifestCmd += "-RootModule $rootModuleFile " }
            [scriptblock]::Create($newManifestCmd).Invoke()

            # Modules specified with an absolute path should only return themselves
            if ($UseAbsolutePath) {
                if ((-not $SkipEditionCheck) -and (-not ($MarkedEdition -contains "Core")))
                {
                    { Get-Module -ListAvailable -All $moduleBase -ErrorAction Stop } | Should -Throw -ErrorId "Modules_ModuleNotFoundForGetModule,Microsoft.PowerShell.Commands.GetModuleCommand"
                    return
                }

                $modules = if ($SkipEditionCheck)
                {
                    Get-Module -ListAvailable -All -SkipEditionCheck $moduleBase
                }
                else
                {
                    Get-Module -ListAvailable -All $moduleBase
                }

                $modules.Count | Should -Be 1
                $modules[0].Name  | Should -BeExactly $moduleName
                return
            }

            $modules = if ($SkipEditionCheck)
            {
                Get-Module -ListAvailable -All -SkipEditionCheck | Where-Object { $_.Path.Contains($guid) }
            }
            else
            {
                Get-Module -ListAvailable -All | Where-Object { $_.Path.Contains($guid) }
            }

            if ((-not $SkipEditionCheck) -and (-not ($MarkedEdition -contains "Core")))
            {
                $modules.Count | Should -Be 0
                return
            }

            if ($UseRootModule)
            {
                $modules.Count | Should -Be 4
            }
            else
            {
                $modules.Count | Should -Be 3
            }

            $names = $modules.Name
            $names | Should -Contain $moduleName
            $names | Should -Contain $scriptModuleName
            $names | Should -Contain $binaryModuleName
        }
    }
}
