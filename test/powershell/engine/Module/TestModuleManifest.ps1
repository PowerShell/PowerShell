Import-Module $PSScriptRoot\..\..\Common\Test.Helpers.psm1

Describe "Test-ModuleManifest tests" -tags "CI" {

    AfterEach {
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue testdrive:/module
    }

    It "module manifest containing paths with backslashes or forwardslashes are resolved correctly" {

        New-Item -ItemType Directory -Path testdrive:/module
        New-Item -ItemType Directory -Path testdrive:/module/foo
        New-Item -ItemType Directory -Path testdrive:/module/bar
        Set-Content -Value "" -Path testdrive:/module/foo/bar.psm1
        Set-Content -Value "" -Path testdrive:/module/bar/foo.psm1
        $testModulePath = "testdrive:/module/test.psd1"
        $fileList = "foo\bar.psm1","bar/foo.psm1"

        New-ModuleManifest -NestedModules $fileList -RootModule foo\bar.psm1 -RequiredAssemblies $fileList -Path $testModulePath -TypesToProcess $fileList -FormatsToProcess $fileList -ScriptsToProcess $fileList -FileList $fileList -ModuleList $fileList

        Test-Path $testModulePath | Should Be $true

        # use -ErrorAction Stop to cause test to fail if Test-ModuleManifest writes to error stream
        Test-ModuleManifest -Path $testModulePath -ErrorAction Stop | Should BeOfType System.Management.Automation.PSModuleInfo
    }

    It "module manifest containing missing files returns error" {

        New-Item -ItemType Directory -Path testdrive:/module
        New-Item -ItemType Directory -Path testdrive:/module/foo
        Set-Content -Value "" -Path testdrive:/module/foo/bar.psm1
        $testModulePath = "testdrive:/module/test.psd1"

        $parametersAndErrors = @{"RequiredAssemblies"="Modules_InvalidRequiredAssembliesInModuleManifest";
            "NestedModules"="Modules_InvalidNestedModuleinModuleManifest";
            "RequiredModules"="Modules_InvalidRequiredModulesinModuleManifest";
            "FileList"="Modules_InvalidFilePathinModuleManifest";
            "ModuleList"="Modules_InvalidModuleListinModuleManifest";
            "TypesToProcess"="Modules_InvalidManifest";
            "FormatsToProcess"="Modules_InvalidManifest";
            #"RootModule"="Modules_InvalidManifest";
            "ScriptsToProcess"="Modules_InvalidManifest"
        }
        foreach ($parameter in $parametersAndErrors.Keys) {
            Write-Warning "Testing $parameter"
            $args = @{$parameter = "doesnotexist.psm1"}
            New-ModuleManifest -Path $testModulePath @args
            Test-Path $testModulePath | Should Be $true
            [string]$errorId = $parametersAndErrors[$parameter] + ",Microsoft.PowerShell.Commands.TestModuleManifestCommand"

            { Test-ModuleManifest -Path $testModulePath -ErrorAction Stop } | ShouldBeErrorId $errorId
            Remove-Item -Recurse -Force $testModulePath
        }
    }
}
