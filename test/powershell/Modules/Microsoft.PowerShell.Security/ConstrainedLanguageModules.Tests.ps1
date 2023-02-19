# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

##
## ----------
## Test Note:
## ----------
## Since these tests change session and system state (constrained language and system lockdown)
## they will all use try/finally blocks instead of Pester AfterEach/AfterAll to ensure session
## and system state is restored.
## Pester AfterEach, AfterAll is not reliable when the session is constrained language or locked down.
##

Import-Module HelpersSecurity

$defaultParamValues = $PSDefaultParameterValues.Clone()
$PSDefaultParameterValues["it:Skip"] = !$IsWindows

try
{
    Describe "Export-ModuleMember should not work across language boundaries" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {

            $script = @'
            function IEXInjectableFunction
            {
                param ([string] $path)
                Invoke-Expression -Command "dir $path"
            }

            function PrivateAddTypeAndRun
            {
                param ([string] $source)
                $type = Add-Type -TypeDefinition $source -passthru
                $type::new()
            }

            Export-ModuleMember -Function IEXInjectableFunction
'@

            $modulePathName = "modulePath_$(Get-Random -Max 9999)"
            $modulePath = Join-Path $testdrive $modulePathName
            New-Item -ItemType Directory $modulePath
            $trustedModuleFile = Join-Path $modulePath "T1TestModule_System32.psm1"
            $script | Out-File -FilePath $trustedModuleFile
        }

        AfterAll {

            Remove-Module -Name T1TestModule_System32 -Force -ErrorAction Ignore

        }

        It "Verifies that IEX running in ConstrainedLanguage cannot export functions from trusted module" {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                Import-Module -Name $trustedModuleFile -Force

                # Use the vulnerable IEXInjectableFunction function to export all functions from module
                # Note that Invoke-Expression will run in constrained language mode because it is known to be vulnerable
                T1TestModule_System32\IEXInjectableFunction -path 'c:\windows\system32\CodeIntegrity; Export-ModuleMember -Function *'
                throw "No Error!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode -RevertLockdownMode
            }

            # A security error should be thrown
            $expectedError.FullyQualifiedErrorId | Should -BeExactly "Modules_CannotExportMembersAcrossLanguageBoundaries,Microsoft.PowerShell.Commands.ExportModuleMemberCommand"

            # PrivateAddTypeAndRun private function should not be exposed
            $result = Get-Command -Name T1TestModule_System32\PrivateAddTypeAndRun 2> $null
            $result | Should -BeNullOrEmpty
        }
    }

    Describe "Dot-source operator is not allowed in modules on locked down systems that export functions with wildcards" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {

            $TestModulePath = Join-Path $TestDrive "Modules_$(Get-Random -Maximum 99999)"
            New-Item -Path $TestModulePath -ItemType Directory -Force -ErrorAction SilentlyContinue

            # Module that dot sources ps1 file while and exports functions with wildcard.
            $scriptModuleNameA = "ModuleDotSourceWildcard_System32"
            $moduleFilePathA = Join-Path $TestModulePath ($scriptModuleNameA + ".psm1")
            $dotSourceNameA = "DotSourceFileNoWildCard_System32"
            $dotSourceFilePathA = Join-Path $TestModulePath ($dotSourceNameA + ".ps1")
            @'
            function PublicDSFnA { "PublicDSFnA"; PrivateDSFnA }
            function PrivateDSFnA { "PrivateDSFnA" }
'@ | Out-File -FilePath $dotSourceFilePathA
            @'
            . {0}
            function PublicFnA {{ "PublicFnA"; PublicDSFnA }}
            function PrivateFnA {{ "PrivateFnA"; PrivateDSFnA }}

            Export-ModuleMember -Function "*"
'@ -f $dotSourceFilePathA | Out-File -FilePath $moduleFilePathA

            # Module that dot sources ps1 file that exports module functions.  Parent module exports nothing.
            $scriptModuleNameB = "ModuleDotSourceNoExport_System32"
            $moduleFilePathB = Join-Path $TestModulePath ($scriptModuleNameB + ".psm1")
            $dotSourceNameB = "DotSourceFileWildCard_System32"
            $dotSourceFilePathB = Join-Path $TestModulePath ($dotSourceNameB + ".ps1")
            @'
            function PublicDSFnB { "PublicDSFnB"; PrivateDSFnB }
            function PrivateDSFnB { "PrivateDSFnB" }

            Export-ModuleMember -Function "*"
'@ | Out-File -FilePath $dotSourceFilePathB
            @'
            . {0}
            function PublicFnB {{ "PublicFnB"; PrivateFnB }}
            function PrivateFnB {{ "PrivateFnB" }}
'@ -f $dotSourceFilePathB | Out-File -FilePath $moduleFilePathB

            # Module that dot sources ps1 file and exports functions with wildcard, but has overriding manifest.
            $scriptModuleNameC = "ModuleDotSourceWildCardM_System32"
            $moduleFilePathC = Join-Path $TestModulePath ($scriptModuleNameC + ".psm1")
            $dotSourceNameC = "DotSourceFileNoWildCardM_System32"
            $dotSourceFilePathC = Join-Path $TestModulePath ($dotSourceNameC + ".ps1")
            $manifestFilePathC = Join-Path $TestModulePath ($scriptModuleNameC + ".psd1")
            @'
            function PublicDSFnC { "PublicDSFnC"; PrivateDSFnC }
            function PrivateDSFnC { "PrivateDSFnC" }
'@ | Out-File -FilePath $dotSourceFilePathC
            @'
            . {0}
            function PublicFnC {{ "PublicFnC"; PublicDSFnC }}
            function PrivateFnC {{ "PrivateFnC"; PrivateDSFnC }}

            Export-ModuleMember -Function "*"
'@ -f $dotSourceFilePathC | Out-File -FilePath $moduleFilePathC
            '@{{ ModuleVersion = "1.0"; RootModule = "{0}"; FunctionsToExport = @("PublicFnC","PublicDSFnC") }}' -f $moduleFilePathC | Out-File -FilePath $manifestFilePathC

            # Module that dot sources ps1 file while and exports functions with no wildcards.
            $scriptModuleNameD = "ModuleDotSourceNoWildcard_System32"
            $moduleFilePathD = Join-Path $TestModulePath ($scriptModuleNameD + ".psm1")
            $dotSourceNameD = "DotSourceFileNoWildCardD_System32"
            $dotSourceFilePathD = Join-Path $TestModulePath ($dotSourceNameD + ".ps1")
            @'
            function PublicDSFnD { "PublicDSFnD"; PrivateDSFnD }
            function PrivateDSFnD { "PrivateDSFnD" }
'@ | Out-File -FilePath $dotSourceFilePathD
            @'
            . {0}
            function PublicFnD {{ "PublicFnD"; PublicDSFnD }}
            function PrivateFnD {{ "PrivateFnD"; PrivateDSFnD }}

            Export-ModuleMember -Function "PublicFnD","PublicDSFnD"
'@ -f $dotSourceFilePathD | Out-File -FilePath $moduleFilePathD

            # Module that dot sources ps1 file but does not use Export-ModuleMember
            $scriptModuleNameE = "ModuleDotSourceNoExportE_System32"
            $moduleFilePathE = Join-Path $TestModulePath ($scriptModuleNameE + ".psm1")
            $dotSourceNameE = "DotSourceFileNoExportE_System32"
            $dotSourceFilePathE = Join-Path $TestModulePath ($dotSourceNameE + ".ps1")
            @'
            function PublicDSFnE { "PublicDSFnE"; PrivateDSFnE }
            function PrivateDSFnE { "PrivateDSFnE" }
'@ | Out-File -FilePath $dotSourceFilePathE
            @'
            . {0}
            function PublicFnE {{ "PublicFnE"; PublicDSFnE }}
            function PrivateFnE {{ "PrivateFnE"; PrivateDSFnE }}
'@ -f $dotSourceFilePathE | Out-File -FilePath $moduleFilePathE

            # Module with dot source ps1 file and nested modules that do use Export-ModuleMember
            $scriptModuleNameF = "ModuleDotSourceNestedExport_System32"
            $moduleFilePathF = Join-Path $TestModulePath ($scriptModuleNameF + ".psm1")
            $manifestFilePathF = Join-Path $TestModulePath ($scriptModuleNameF + ".psd1")
            $nestedSourceNameF = "NestedSourceWithExport_System32"
            $nestedSourceFilePathF = Join-Path $TestModulePath ($nestedSourceNameF + ".psm1")
            @'
            . {0}
            function NestedPubFnF {{ "NestedPubFnF"; PublicDSFnE }}

            Export-ModuleMember -Function *
'@ -f $dotSourceFilePathE | Out-File -FilePath $nestedSourceFilePathF
            @'
            function PublicFnF { "PublicFnF"; NestedPubFnF }
'@ | Out-File -FilePath $moduleFilePathF
            '@{{ ModuleVersion = "1.0"; RootModule = "{0}"; NestedModules = "{1}"; FunctionsToExport = "PublicFnF","NestedPubFnF" }}' -f $moduleFilePathF,$nestedSourceFilePathF | Out-File -FilePath $manifestFilePathF

            # Module with dot source ps1 file and import module and Export-ModuleMember with wildcard
            $scriptModuleNameG = "ModuleDotSourceImportExport_System32"
            $moduleFilePathG = Join-Path $TestModulePath ($scriptModuleNameG + ".psm1")
            $importModNameG = "ImportModWitExport_System32"
            $importModFilePathG = Join-Path $TestModulePath ($importModNameG + ".psm1")
            @'
            . {0}
            function ImportPubFnG {{ "ImportPubFnG"; PublicDSFnE }}

            Export-ModuleMember -Function *
'@ -f $dotSourceFilePathE | Out-File $importModFilePathG
            @'
            Import-Module {0}
            function PublicFnG {{ "PublicFnG"; ImportPubFnG }}

            Export-ModuleMember -Function PublicFnG
'@ -f $importModFilePathG | Out-File -FilePath $moduleFilePathG

            # Module with dot source and with multiple Export-ModuleMember use.
            $scriptModuleNameH = "ModuleDotSourceImportExportH_System32"
            $moduleFilePathH = Join-Path $TestModulePath ($scriptModuleNameH + ".psm1")
            @'
            . {0}
            function PublicFnH {{ "PublicFnH"; PrivateFnH }}
            function PrivateFnH {{ "PrivateFnH" }}

            Export-ModuleMember -Function *
            Export-ModuleMember -Function PublicFnH
'@ -f $dotSourceFilePathE | Out-File $moduleFilePathH

            # Module with dot source and only class definition, and no functions exported.
            $scriptModuleNameI = "ModuleDotSourceClassesOnly_System32"
            $moduleFilePathI = Join-Path $TestModulePath ($scriptModuleNameI + ".psm1")
            @'
            class Class1 {{ static [string] GetMessage() {{ . {0}; return "Message" }} }}
'@ -f $dotSourceFilePathE | Out-File $moduleFilePathI

            # Module manifest with dot source and only class definition, and no functions exported.
            $scriptManifestNameI = "ManifestDotSourceClassesOnly_System32"
            $moduleManifestPathI = Join-Path $TestModulePath ($scriptManifestNameI + ".psd1")
            "@{ ModuleVersion='1.0'; RootModule='$moduleFilePathI' }" | Out-File $moduleManifestPathI

            # Module with using directive
            $scriptModuleNameJ = "ModuleWithUsing_System32"
            $moduleFilePathJ = Join-Path $TestModulePath ($scriptModuleNameJ + ".psm1")
            @'
            using module {0}
            function PublicUsingFn {{ [Class1]::GetMessage() }}
            Export-ModuleMember -Function PublicUsingFn
'@ -f $moduleManifestPathI | Out-File $moduleFilePathJ

            Write-Verbose "Test module files created"
        }

        AfterAll {
            Remove-Module $scriptModuleNameA -Force -ErrorAction SilentlyContinue
            Remove-Module $scriptModuleNameB -Force -ErrorAction SilentlyContinue
            Remove-Module $scriptModuleNameC -Force -ErrorAction SilentlyContinue
            Remove-Module $scriptModuleNameD -Force -ErrorAction SilentlyContinue
            Remove-Module $scriptModuleNameE -Force -ErrorAction SilentlyContinue
            Remove-Module $scriptModuleNameF -Force -ErrorAction SilentlyContinue
            Remove-Module $scriptModuleNameG -Force -ErrorAction SilentlyContinue
            Remove-Module $scriptModuleNameH -Force -ErrorAction SilentlyContinue
            Remove-Module $scriptModuleNameI -Force -ErrorAction SilentlyContinue
            Remove-Module $scriptModuleNameJ -Force -ErrorAction SilentlyContinue
        }

        It "Verifies that importing trusted module in system lockdown which dot sources a ps1 file while exporting all functions with wildcard throws expected error" {

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                Import-Module -Name $moduleFilePathA -Force 2> $null
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "Modules_SystemLockDown_CannotUseDotSourceWithWildCardFunctionExport,Microsoft.PowerShell.Commands.ImportModuleCommand"
        }

        It "Verifies that importing trusted module in system lockdown which dot sources a ps1 file that exports functions with wildcard throws expected error" {

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                Import-Module -Name $moduleFilePathB -Force 2> $null
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "Modules_SystemLockDown_CannotUseDotSourceWithWildCardFunctionExport,Microsoft.PowerShell.Commands.ImportModuleCommand"
        }

        It "Verifies that importing trusted module in system lockdown which dot sources a ps1 file while exporting functions with wildcard but has overriding manifest export does not throw error" {

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                $module = Import-Module -Name $manifestFilePathC -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $module.ExportedCommands.Count | Should -Be 2
            $module.ExportedCommands["PublicFnC"] | Should -Not -BeNullOrEmpty
            $module.ExportedCommands["PublicDSFnC"] | Should -Not -BeNullOrEmpty
        }

        It "Verifies that importing trusted module in system lockdown which dot sources ps1 file but does not export functions with wildcard does not throw error" {

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                $module = Import-Module -Name $moduleFilePathD -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $module.ExportedCommands.Count | Should -Be 2
            $module.ExportedCommands["PublicFnD"] | Should -Not -BeNullOrEmpty
            $module.ExportedCommands["PublicDSFnD"] | Should -Not -BeNullOrEmpty
        }

        It "Verifies that importing trusted module with dotsource and wildcard function export works when not in lock down mode" {

            $module = Import-Module -Name $moduleFilePathA -Force -PassThru

            $module.ExportedCommands.Count | Should -Be 4
            $module.ExportedCommands["PublicFnA"] | Should -Not -BeNullOrEmpty
            $module.ExportedCommands["PrivateFnA"] | Should -Not -BeNullOrEmpty
            $module.ExportedCommands["PublicDSFnA"] | Should -Not -BeNullOrEmpty
            $module.ExportedCommands["PrivateDSFnA"] | Should -Not -BeNullOrEmpty
        }

        It "Verifies that importing trusted module with dotsource and no function export works without error in lockdown mode" {

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                $module = Import-Module -Name $moduleFilePathE -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $module.ExportedCommands.Count | Should -Be 0
        }

        It "Verifies that dot source manifest and module with nested module works as expected in system lock down" {

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                $module = Import-Module -Name $manifestFilePathF -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $module.ExportedCommands.Count | Should -Be 2
            $module.ExportedCommands["PublicFnF"] | Should -Not -BeNullOrEmpty
            $module.ExportedCommands["NestedPubFnF"] | Should -Not -BeNullOrEmpty
        }

        It "Verifies that an imported module that dot sources and exports via wildcard is detected and disallowed" {

            try
            {
                $expectedError = $null
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                $module = Import-Module -Name $moduleFilePathG -Force -PassThru -ErrorVariable expectedError 2> $null
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $expectedError[0].FullyQualifiedErrorId | Should -BeExactly "Modules_SystemLockDown_CannotUseDotSourceWithWildCardFunctionExport,Microsoft.PowerShell.Commands.ImportModuleCommand"
        }

        It "Verifies that a module with dot source file and multiple Export-ModuleMember calls still errors with wildcard" {

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                $module = Import-Module -Name $moduleFilePathH -Force -PassThru 2> $null
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "Modules_SystemLockDown_CannotUseDotSourceWithWildCardFunctionExport,Microsoft.PowerShell.Commands.ImportModuleCommand"
        }

        It "Verifies that a classes only module with dot-source and with using directive loads successfully" {
            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                $module = Import-Module -Name $moduleFilePathJ -Force -PassThru
                $result = PublicUsingFn
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $module | Should -Not -BeNullOrEmpty
            $result | Should -BeExactly "Message"
        }
    }

    Describe "Call operator invocation of trusted module private function" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {

            $scriptModuleName = "ImportTrustedManifestWithCallOperator_System32"
            $moduleFileName = Join-Path $TestDrive ($scriptModuleName + ".psm1")
            $manifestFileName = Join-Path $TestDrive ($scriptModuleName + ".psd1")
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
'@ > $moduleFileName

            "@{ ModuleVersion = '1.0'; RootModule = '$moduleFileName'; FunctionsToExport = 'PublicFn' }" > $manifestFileName
        }

        AfterAll {
            Remove-Module ImportTrustedManifestWithCallOperator_System32 -Force -ErrorAction SilentlyContinue
        }

        It "Verifies expected error when call operator attempts to access trusted module scope function" {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                $module = Import-Module -Name $manifestFileName -Force -PassThru

                & $module PrivateFn

                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "CantInvokeCallOperatorAcrossLanguageBoundaries"
        }
    }

    Describe "Tests module table restrictions" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {

            # Module directory
            $moduleName = "Modules_" + (Get-RandomFileName)
            $modulePath = Join-Path $TestDrive $moduleName
            New-Item -ItemType Directory -Path $modulePath

            # Parent module directory
            $scriptModuleName = "TrustedParentModule_System32"
            $scriptModulePath = Join-Path $modulePath $scriptModuleName
            $moduleFileName = Join-Path $scriptModulePath ($scriptModuleName + ".psm1")
            $manifestFileName = Join-Path $scriptModulePath ($scriptModuleName + ".psd1")
            New-Item -ItemType Directory -Path $scriptModulePath

            # Import module directory
            $scriptModuleImportName = "TrustedImportModule_System32"
            $scriptModuleImportPath = Join-Path $modulePath $scriptModuleImportName
            $moduleImportFileName = Join-Path $scriptModuleImportPath ($scriptModuleImportName + ".psm1")
            New-Item -ItemType Directory -Path $scriptModuleImportPath

            @'
            Import-Module -Name {0}

            function PublicFn
            {{
                Write-Host ""
                Write-Host "PublicFn"
                PrivateFn1
            }}
'@ -f $scriptModuleImportName > $moduleFileName

            @'
            function PrivateFn1
            {
                Write-Host ""
                Write-Host "PrivateFn1"
                Write-Host "Language mode: $($ExecutionContext.SessionState.LanguageMode)"
            }
'@ > $moduleImportFileName

            "@{ ModuleVersion = '1.0'; NestedModules = '$moduleFileName'; FunctionsToExport = 'PublicFn' }" > $manifestFileName

            $savedPSModulePath = $env:PSModulePath
            $env:PSModulePath += (";" + $modulePath)
        }

        AfterAll {

            if ($savedPSModulePath -ne $null) { $env:PSModulePath = $savedPSModulePath }
        }

        It "Verifies that Get-Command does not expose private module function under system lock down" {

            $GetCommandPublicFnCmdInfo = $null
            $GetCommandPrivateFnCmdInfo = $null

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                # Imports both TrustedParentModule_System32 and TrustedImportModule_System32 modules
                Import-Module -Name $scriptModuleName -Force

                # Public functions should be available in the session
                $GetCommandPublicFnCmdInfo = Get-Command -Name "PublicFn" 2> $null

                # Private functions should not be available in the session
                # Get-Command will import the TrustedImportModule_System32 module from the PSModulePath to find PrivateFn1
                # However, it should not get TrustedImportModule_System32 from the module cache because it was loaded in a
                # different language mode, and should instead re-load it (equivalent to Import-Module -Force)
                $GetCommandPrivateFnCmdInfo = Get-Command -Name "PrivateFn1" 2> $null
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $GetCommandPublicFnCmdInfo | Should -Not -BeNullOrEmpty
            $GetCommandPrivateFnCmdInfo | Should -BeNullOrEmpty
        }

        It "Verifies that Get-Command does not expose private function after explicitly importing nested module file under system lock down" {

            $ReImportPublicFnCmdInfo = $null
            $ReImportPrivateFnCmdInfo = $null

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                # Imports both TrustedParentModule_System32 and TrustedImportModule_System32 modules
                Import-Module -Name $scriptModuleName -Force

                # Directly import nested TrustedImportModule_System32 module.
                # This makes TrustedImportModule_System32 functions visible but should not use the existing loaded module
                # since all functions are visible, but instead should re-load the module with the correct language context,
                # ensuring only explicitly exported functions are visible.
                Import-Module -Name $scriptModuleImportName

                # Public functions should be available in the session
                $ReImportPublicFnCmdInfo = Get-Command -Name "PublicFn" 2> $null

                # Private functions should not be available in the session
                $ReImportPrivateFnCmdInfo = Get-Command -Name "PrivateFn1" 2> $null
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $ReImportPublicFnCmdInfo | Should -Not -BeNullOrEmpty
            $ReImportPrivateFnCmdInfo | Should -BeNullOrEmpty
        }
    }

    Describe "Import mix of trusted and untrusted manifest and module files" -Tags 'Feature','RequireAdminOnWindows' {

        It "Verifies that an untrusted manifest with a trusted module will not load under system lockdown" {

            $manifestFileName = Join-Path $TestDrive "ImportUnTrustedManifestWithFnExport.psd1"
            $moduleFileName = Join-Path $TestDrive "ImportUnTrustedManifestWithFnExport_System32.psm1"

            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
'@ > $moduleFileName

            "@{ ModuleVersion = '1.0'; RootModule = '$moduleFileName'; FunctionsToExport = 'PublicFn','PrivateFn' }" > $manifestFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                Import-Module -Name $manifestFileName -Force -ErrorAction Stop
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportUnTrustedManifestWithFnExport_System32 -Force -ErrorAction SilentlyContinue
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "Modules_MismatchedLanguageModes,Microsoft.PowerShell.Commands.ImportModuleCommand"
        }

        It "Verifies that an untrusted manifest with a trusted binary module does load under system lockdown" {

            $modulePath = "$PSScriptRoot\Modules"
            New-Item -Path $modulePath -ItemType Directory -Force

            $manifestFileName = Join-Path $modulePath "ImportUnTrustedManifestWithBinFnExport.psd1"
            $moduleFileName = Join-Path $modulePath "ImportUnTrustedManifestWithBinFnExport_System32.dll"
            "@{ ModuleVersion = '1.0'; NestedModules = '$moduleFileName'; CmdletsToExport = 'Invoke-Hello' }" > $manifestFileName

            $code = @'
            using System;
            using System.Management.Automation;

            [Cmdlet("Invoke", "Hello")]
            public sealed class InvokeHello : PSCmdlet
            {
                protected override void EndProcessing()
                {
                    System.Console.WriteLine("Hello!");
                }
            }
'@
            try { Add-Type -TypeDefinition $code -OutputAssembly $moduleFileName -ErrorAction Ignore } catch {}

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                $module = Import-Module -Name $manifestFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $module | Should -Not -BeNullOrEmpty
            $module.ExportedCommands["Invoke-Hello"] | Should -Not -BeNullOrEmpty

            if ($module -ne $null) { Remove-Module -Name $module.Name -Force -ErrorAction Ignore }
        }

        It "Verifies that an untrusted module with nested trusted modules cannot load in a locked down system" {

            $manifestFileName = Join-Path $TestDrive "ImportUnTrustedManifestWithTrustedModule.psd1"
            $moduleFileName = Join-Path $TestDrive "ImportUnTrustedManifestWithTrustedModule_System32.psm1"
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
'@ > $moduleFileName

            "@{ ModuleVersion = '1.0'; NestedModules = '$moduleFileName'; FunctionsToExport = 'PublicFn','PrivateFn' }" > $manifestFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                Import-Module -Name $manifestFileName -Force -ErrorAction Stop
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "Modules_MismatchedLanguageModes,Microsoft.PowerShell.Commands.ImportModuleCommand"
        }

        It "Verifies that an untrusted manifest containing all trusted modules does not load under system lock down" {

            $moduleFileName1 = Join-Path $TestDrive "ImportUnTrustedManifestWithTrustedModules1_System32.psm1"
            $moduleFileName2 = Join-Path $TestDrive "ImportUnTrustedManifestWithTrustedModules2_System32.psm1"
            $manifestFileName = Join-Path $TestDrive "ImportUnTrustedManifestWithTrustedModules.psd1"
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
'@ > $moduleFileName1
            @'
            function PublicFn2
            {
                Write-Output "PublicFn2"
            }
'@ > $moduleFileName2

            "@{ ModuleVersion = '1.0'; NestedModules = '$moduleFileName1'; RootModule = '$moduleFileName2'; FunctionsToExport = 'PublicFn','PrivateFn' }" > $manifestFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                Import-Module -Name $manifestFileName -Force -ErrorAction Stop
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "Modules_MismatchedLanguageModes,Microsoft.PowerShell.Commands.ImportModuleCommand"
        }

        # End Describe Block
    }

    Describe "Import trusted module files in system lockdown mode" -Tags 'Feature','RequireAdminOnWindows' {

        function CreateModuleNames
        {
            param (
                [string] $moduleName
            )

            $script:scriptModuleName = $moduleName
            $script:moduleFileName = Join-Path $TestDrive ($moduleName + ".psm1")
        }

        It "Verifies that trusted module file exports no functions in system lockdown" {

            CreateModuleNames "ImportTrustedModuleWithNoFnExport_System32"
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
'@ > $moduleFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $moduleFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportTrustedModuleWithNoFnExport_System32 -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 0
        }

        It "Verifies that trusted module file exports only exported function in system lockdown" {

            CreateModuleNames "ImportTrustedModuleWithFnExport_System32"
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
            Export-ModuleMember -Function PublicFn
'@ > $moduleFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $moduleFileName -Force -PassThr
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportTrustedModuleWithFnExport_System32 -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 1
            $module.ExportedCommands.Values[0].Name | Should -BeExactly "PublicFn"
        }

        It "Verifies that trusted module with wild card function export in system lockdown" {

            CreateModuleNames "ImportTrustedModuleWithWildcardFnExport_System32"
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
            Export-ModuleMember -Function *
'@ > $moduleFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $moduleFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportTrustedModuleWithWildcardFnExport_System32 -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 2
        }
    }

    Describe "Import trusted manifest files in system lockdown mode" -Tags 'Feature','RequireAdminOnWindows' {

        function CreateManifestNames
        {
            param (
                [string] $moduleName,
                [switch] $twoModules,
                [switch] $noExtension,
                [switch] $dotSourceModule
            )

            $script:scriptModuleName = $moduleName
            $script:moduleFileName = Join-Path $TestDrive ($moduleName + ".psm1")
            $script:manifestFileName = Join-Path $TestDrive ($moduleName + ".psd1")
            if ($twoModules)
            {
                $script:moduleFileName2 = Join-Path $TestDrive ($moduleName + "2.psm1")
            }
            if ($noExtension)
            {
                $script:moduleFileNameNoExt = Join-Path $TestDrive $scriptModuleName
            }
            if ($dotSourceModule)
            {
                $script:dotmoduleFileName = Join-Path $TestDrive ($moduleName + "Dot" + ".ps1")
            }
        }

        It "Verifies that trusted manifest exports no functions by default in lock down mode" {

            CreateManifestNames "ImportTrustedManifestWithNoFnExport_System32"
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
'@ > $moduleFileName
            "@{ ModuleVersion = '1.0'; RootModule = '$moduleFileName' }" > $manifestFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $manifestFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportTrustedManifestWithNoFnExport_System32 -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 0
        }

        It "Verifies that trusted manifest exports no functions through wildcard in lock down mode" {

            CreateManifestNames "ImportTrustedManifestWithWildcardFnExport1_System32"
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
'@ > $moduleFileName
            "@{ ModuleVersion = '1.0'; RootModule = '$moduleFileName'; FunctionsToExport = '*' }" > $manifestFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $manifestFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportTrustedManifestWithWildcardFnExport1_System32 -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 0
        }

        It "Verifies that trusted manifest exports no functions through name wildcard in lock down mode" {

            CreateManifestNames "ImportTrustedManifestWithWildcardNameFnExport_System32"
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
'@ > $moduleFileName
            "@{ ModuleVersion = '1.0'; RootModule = '$moduleFileName'; FunctionsToExport = '*Fn*' }" > $manifestFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $manifestFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportTrustedManifestWithWildcardNameFnExport_System32 -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 0
        }

        It "Verifies that trusted manifest exports a single module function and ignores wildcard in lock down mode" {

            CreateManifestNames "ImportTrustedManifestWithWildcardModFnExport_System32"
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }

            Export-ModuleMember -Function "PublicFn"
'@ > $moduleFileName
            "@{ ModuleVersion = '1.0'; RootModule = '$moduleFileName'; FunctionsToExport = '*' }" > $manifestFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $manifestFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportTrustedManifestWithWildcardModFnExport_System32 -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 1
            $module.ExportedCommands.Values[0].Name | Should -BeExactly "PublicFn"
        }

        It "Verifies that trusted manifest exports no functions through the cmdlets export keyword" {

            CreateManifestNames "ImportTrustedManifestWithCmdletExport_System32"
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
'@ > $moduleFileName
            "@{ ModuleVersion = '1.0'; RootModule = '$moduleFileName'; CmdletsToExport = '*' }" > $manifestFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $manifestFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportTrustedManifestWithCmdletExport_System32 -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 0
        }

        It "Verifies that trusted manifest with wildcard exports a single function from two modules" {

            CreateManifestNames "ImportTrustedManifestWithTwoMods_System32" -TwoModules
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
            Export-ModuleMember -Function PublicFn
'@ > $moduleFileName
            @'
            function PrivateFn3
            {
                Write-Output "PublicFn"
            }

            function PrivateFn4
            {
                Write-Output "PrivateFn"
            }
'@ > $moduleFileName2
            "@{ ModuleVersion = '1.0'; RootModule = '$moduleFileName'; NestedModules = '$moduleFileName2'; FunctionsToExport = '*' }" > $manifestFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $manifestFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportTrustedManifestWithTwoMods_System32 -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 1
            $module.ExportedCommands.Values[0].Name | Should -BeExactly "PublicFn"
        }

        It "Verifies that trusted manifest explicitly exports a single function" {

            CreateManifestNames "ImportTrustedManifestWithExportFn_System32"

            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
'@ > $moduleFileName
            "@{ ModuleVersion = '1.0'; RootModule = '$moduleFileName'; FunctionsToExport = 'PublicFn' }" > $manifestFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $manifestFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportTrustedManifestWithExportFn_System32 -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 1
            $module.ExportedCommands.Values[0].Name | Should -BeExactly "PublicFn"
        }

        It "Verifies that trusted manifest with nested modules exports explicit function" {

            CreateManifestNames "ImportTrustedManifestWithNestedModsAndFnExport_System32"
        @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
'@ > $moduleFileName
            "@{ ModuleVersion = '1.0'; NestedModules = '$moduleFileName'; FunctionsToExport = 'PublicFn' }" > $manifestFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $manifestFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportTrustedManifestWithNestedModsAndFnExport_System32 -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 1
            $module.ExportedCommands.Values[0].Name | Should -BeExactly "PublicFn"
        }

        It "Verifies that trusted manifest with nested modules exports no functions by default" {

            CreateManifestNames "ImportTrustedManifestWithNestedModsAndNoFnExport_System32"
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
'@ > $moduleFileName
            "@{ ModuleVersion = '1.0'; NestedModules = '$moduleFileName' }" > $manifestFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $manifestFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportTrustedManifestWithNestedModsAndNoFnExport_System32 -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 0
        }

        It "Verifies that trusted manifest with nested modules and no extension module exports explicit function" {

            CreateManifestNames "ImportTrustedManifestWithNestedModsAndNoExtNoFnExport_System32" -NoExtension
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
'@ > $moduleFileName
            "@{ ModuleVersion = '1.0'; NestedModules = '$moduleFileNameNoExt'; FunctionsToExport = 'PublicFn' }" > $manifestFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $manifestFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportTrustedManifestWithNestedModsAndNoExtNoFnExport_System32 -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 1
            $module.ExportedCommands.Values[0].Name | Should -BeExactly "PublicFn"
        }

        It "Verifies that trusted manifest with dot source module file respects lock down mode" {

            CreateManifestNames "ImportTrustedManifestWithDotSourceModAndFnExport_System32" -DotSourceModule
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }
            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
'@ > $dotmoduleFileName
            @'
            . {0}

            function PrivateFn1
            {{
                Write-Output "PrivateFn1"

            }}

            function PrivateFn2
            {{
                Write-Output "PrivateFn2"
            }}
'@ -f $dotmoduleFileName > $moduleFileName
            "@{ ModuleVersion = '1.0'; NestedModules = '$moduleFileName'; FunctionsToExport = 'PublicFn' }" > $manifestFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $manifestFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportTrustedManifestWithDotSourceModAndFnExport_System32 -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 1
            $module.ExportedCommands.Values[0].Name | Should -BeExactly "PublicFn"
        }
    }

    Describe "Untrusted manifest and module files import in lock down mode" -Tags 'Feature','RequireAdminOnWindows' {

        function CreateManifestNames
        {
            param (
                [string] $moduleName
            )

            $script:scriptModuleName = $moduleName
            $script:moduleFileName = Join-Path $TestDrive ($moduleName + ".psm1")
            $script:manifestFileName = Join-Path $TestDrive ($moduleName + ".psd1")
        }

        It "Verifies that importing untrusted manifest in lock down mode exports all functions by default" {

            CreateManifestNames "ImportUntrustedManifestWithNoFnExport"
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
'@ > $moduleFileName
            "@{ ModuleVersion = '1.0'; RootModule = '$moduleFileName' }" > $manifestFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $manifestFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportUntrustedManifestWithNoFnExport -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 2
        }

        It "Verifies that importing untrusted manifest in lock down mode exports explicit function" {

            CreateManifestNames "ImportUntrustedManifestWithFnExport"
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
'@ > $moduleFileName
            "@{ ModuleVersion = '1.0'; RootModule = '$moduleFileName'; FunctionsToExport = 'PrivateFn' }" > $manifestFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $manifestFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportUntrustedManifestWithFnExport -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 1
            $module.ExportedCommands.Values[0].Name | Should -BeExactly 'PrivateFn'
        }

        It "Verifies that importing untrusted module file in lock down mode exports all functions by default" {

            CreateManifestNames "ImportUnTrustedModuleWithNoFnExport"
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
'@ > $moduleFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $moduleFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportUnTrustedModuleWithNoFnExport -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 2
        }

        It "Verifies that importing untrusted module file in lock down mode exports explicit function" {

            CreateManifestNames "ImportUnTrustedModuleWithFnExport"
            @'
            function PublicFn
            {
                Write-Output "PublicFn"
            }

            function PrivateFn
            {
                Write-Output "PrivateFn"
            }
            Export-ModuleMember -Function PublicFn
'@ > $moduleFileName

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $module = Import-Module -Name $moduleFileName -Force -PassThru
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module ImportUnTrustedModuleWithFnExport -Force -ErrorAction SilentlyContinue
            }

            $module.ExportedCommands.Count | Should -Be 1
            $module.ExportedCommands.Values[0].Name | Should -BeExactly 'PublicFn'
        }
    }

    Describe "Export-ModuleMember should succeed in FullLanguage mode with scriptblock created without context" -Tag 'Feature' {

        BeforeAll {

            $typeDef = @'
            using System;
            using System.Management.Automation;
            using System.Management.Automation.Runspaces;

            public class TestScriptBlockCreate
            {
                private ScriptBlock _scriptBlock;

                public ScriptBlock CreateScriptBlock()
                {
                    var thread = new System.Threading.Thread(ThreadProc);
                    thread.Start(null);
                    thread.Join();

                    return _scriptBlock;
                }

                private void ThreadProc(object state)
                {
                    // Create script block on thread with no PowerShell context
                    _scriptBlock = ScriptBlock.Create(@"function Do-Nothing {}; Export-ModuleMember -Function Do-Nothing");
                }
            }
'@

            try
            {
                Add-Type -TypeDefinition $typeDef
            }
            catch { }
        }

        It "Verifies that Export-ModuleMember does not throw error with context-less scriptblock" {

            $scriptBlockCreator = [TestScriptBlockCreate]::new()
            $testScriptBlock = $scriptBlockCreator.CreateScriptBlock()

            $testScriptBlock | Should -Not -BeNullOrEmpty

            { New-Module -ScriptBlock $testScriptBlock -ErrorAction Stop } | Should -Not -Throw -Because "Scriptblock without execution context is allowed in Full Language"
        }
    }

    Describe "New-Module should not create module from trusted scriptblock when running in ConstrainedLanguage context" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {

            $script = @'
            function ScriptFn { Write-Output $ExecutionContext.SessionState.LanguageMode }
'@

            $scriptFileNameT = "NewModuleTrustedScriptBlock_System32"
            $scriptFilePathT = Join-Path $TestDrive ($scriptFileNameT + ".ps1")
            $script | Out-File -FilePath $scriptFilePathT

            $scriptFileNameU = "NewModuleUntrustedScriptBlock"
            $scriptFilePathU = Join-Path $TestDrive ($scriptFileNameU + ".ps1")
            $script | Out-File -FilePath $scriptFilePathU
        }

        It "New-Module throws error when creating module with trusted scriptblock in ConstrainedLanguage" {

            $expectedError = $null
            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                # Get scriptblock from trusted script file
                $sb = (Get-Command $scriptFilePathT).ScriptBlock

                # Create new module from trusted scriptblock while in ConstrainedLanguage
                try
                {
                    New-Module -Name TrustedScriptFoo -ScriptBlock $sb
                    throw "No Exception!"
                }
                catch
                {
                    $expectedError = $_
                }
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "Modules_CannotCreateModuleWithFullLanguageScriptBlock,Microsoft.PowerShell.Commands.NewModuleCommand"
        }

        It "New-Module succeeds in creating module with untrusted scriptblock in ConstrainedLanguage" {

            $result = $null

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                # Get scriptblock from untrusted script file
                $sb = (Get-Command $scriptFilePathU).ScriptBlock

                # Create and import module from scriptblock
                $m = New-Module -Name UntrustedScriptFoo -ScriptBlock $sb
                Import-Module -ModuleInfo $m -Force

                $result = ScriptFn
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                Remove-Module UntrustedScriptFoo -Force -ErrorAction SilentlyContinue
            }

            $result | Should -BeExactly "ConstrainedLanguage"
        }
    }
}
finally
{
    if ($defaultParamValues -ne $null)
    {
        $Global:PSDefaultParameterValues = $defaultParamValues
    }
}
