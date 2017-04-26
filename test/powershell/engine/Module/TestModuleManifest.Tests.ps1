Import-Module $PSScriptRoot\..\..\Common\Test.Helpers.psm1

Describe "Test-ModuleManifest tests" -tags "CI" {

    AfterEach {
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue testdrive:/module
    }

    It "module manifest containing paths with backslashes or forwardslashes are resolved correctly" {

        New-Item -ItemType Directory -Path testdrive:/module
        New-Item -ItemType Directory -Path testdrive:/module/foo
        New-Item -ItemType Directory -Path testdrive:/module/bar
        New-Item -ItemType File -Path testdrive:/module/foo/bar.psm1
        New-Item -ItemType File -Path testdrive:/module/bar/foo.psm1
        $testModulePath = "testdrive:/module/test.psd1"
        $fileList = "foo\bar.psm1","bar/foo.psm1"

        New-ModuleManifest -NestedModules $fileList -RootModule foo\bar.psm1 -RequiredAssemblies $fileList -Path $testModulePath -TypesToProcess $fileList -FormatsToProcess $fileList -ScriptsToProcess $fileList -FileList $fileList -ModuleList $fileList

        Test-Path $testModulePath | Should Be $true

        # use -ErrorAction Stop to cause test to fail if Test-ModuleManifest writes to error stream
        Test-ModuleManifest -Path $testModulePath -ErrorAction Stop | Should BeOfType System.Management.Automation.PSModuleInfo
    }

    It "module manifest containing missing files returns error" -TestCases (
        @{parameter = "RequiredAssemblies"; error = "Modules_InvalidRequiredAssembliesInModuleManifest"},
        @{parameter = "NestedModules"; error = "Modules_InvalidNestedModuleinModuleManifest"},
        @{parameter = "RequiredModules"; error = "Modules_InvalidRequiredModulesinModuleManifest"},
        @{parameter = "FileList"; error = "Modules_InvalidFilePathinModuleManifest"},
        @{parameter = "ModuleList"; error = "Modules_InvalidModuleListinModuleManifest"},
        @{parameter = "TypesToProcess"; error = "Modules_InvalidManifest"},
        @{parameter = "FormatsToProcess"; error = "Modules_InvalidManifest"},
        @{parameter = "RootModule"; error = "Modules_InvalidRootModuleInModuleManifest"},
        @{parameter = "ScriptsToProcess"; error = "Modules_InvalidManifest"}
     ) {

        param ($parameter, $error)

        New-Item -ItemType Directory -Path testdrive:/module
        New-Item -ItemType Directory -Path testdrive:/module/foo
        New-Item -ItemType File -Path testdrive:/module/foo/bar.psm1
        $testModulePath = "testdrive:/module/test.psd1"

        $args = @{$parameter = "doesnotexist.psm1"}
        New-ModuleManifest -Path $testModulePath @args
        [string]$errorId = "$error,Microsoft.PowerShell.Commands.TestModuleManifestCommand"

        { Test-ModuleManifest -Path $testModulePath -ErrorAction Stop } | ShouldBeErrorId $errorId
    }

    It "module manifest containing valid unprocessed rootmodule file type succeeds" -TestCases (
        @{rootModuleValue = "foo.psm1"},
        @{rootModuleValue = "foo.dll"}
    ) {

        param($rootModuleValue)

        New-Item -ItemType Directory -Path testdrive:/module
        $testModulePath = "testdrive:/module/test.psd1"

        New-Item -ItemType File -Path testdrive:/module/$rootModuleValue
        New-ModuleManifest -Path $testModulePath -RootModule $rootModuleValue
        $moduleManifest = Test-ModuleManifest -Path $testModulePath -ErrorAction Stop
        $moduleManifest | Should BeOfType System.Management.Automation.PSModuleInfo
        $moduleManifest.RootModule | Should Be $rootModuleValue
    }

    It "module manifest containing valid processed empty rootmodule file type fails" -TestCases (
        @{rootModuleValue = "foo.cdxml"; error = "System.Xml.XmlException"},  # fails when cmdlet tries to read it as XML
        @{rootModuleValue = "foo.xaml"; error = "NotSupported"}   # not supported on PowerShell Core
    ) {

        param($rootModuleValue, $error)

        New-Item -ItemType Directory -Path testdrive:/module
        $testModulePath = "testdrive:/module/test.psd1"

        New-Item -ItemType File -Path testdrive:/module/$rootModuleValue
        New-ModuleManifest -Path $testModulePath -RootModule $rootModuleValue
        { Test-ModuleManifest -Path $testModulePath -ErrorAction Stop } | ShouldBeErrorId "$error,Microsoft.PowerShell.Commands.TestModuleManifestCommand"
    }

    It "module manifest containing empty rootmodule succeeds" -TestCases (
        @{rootModuleValue = $null},
        @{rootModuleValue = ""}
    ) {

        param($rootModuleValue)

        New-Item -ItemType Directory -Path testdrive:/module
        $testModulePath = "testdrive:/module/test.psd1"

        New-ModuleManifest -Path $testModulePath -RootModule $rootModuleValue
        $moduleManifest = Test-ModuleManifest -Path $testModulePath -ErrorAction Stop
        $moduleManifest | Should BeOfType System.Management.Automation.PSModuleInfo
        $moduleManifest.RootModule | Should BeNullOrEmpty
    }

    It "module manifest containing invalid rootmodule returns error" -TestCases (
        @{rootModuleValue = "foo.psd1"; error = "Modules_InvalidManifest"}
    ) {

        param($rootModuleValue, $error)

        $testModulePath = "testdrive:/module/test.psd1"
        New-Item -ItemType Directory -Path testdrive:/module
        New-Item -ItemType File -Path testdrive:/module/$rootModuleValue

        New-ModuleManifest -Path $testModulePath -RootModule $rootModuleValue
        { Test-ModuleManifest -Path $testModulePath -ErrorAction Stop } | ShouldBeErrorId "$error,Microsoft.PowerShell.Commands.TestModuleManifestCommand"
    }

    It "module manifest containing non-existing rootmodule returns error" -TestCases (
        @{rootModuleValue = "doesnotexist.psm1"; error = "Modules_InvalidRootModuleInModuleManifest"}
    ) {

        param($rootModuleValue, $error)

        $testModulePath = "testdrive:/module/test.psd1"
        New-Item -ItemType Directory -Path testdrive:/module

        New-ModuleManifest -Path $testModulePath -RootModule $rootModuleValue
        { Test-ModuleManifest -Path $testModulePath -ErrorAction Stop } | ShouldBeErrorId "$error,Microsoft.PowerShell.Commands.TestModuleManifestCommand"
    }
}
