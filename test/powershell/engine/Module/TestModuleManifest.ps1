Describe "Test-ModuleManifest tests" -tags "CI" {

    AfterEach {
        if (Test-Path testdrive:/module) {
            Remove-Item -Recurse -Force testdrive:/module
        }
    }

    It "module manifest containing paths with backslashes or forwardslashes are resolved correctly" {

        New-Item -ItemType Directory -Path testdrive:/module
        New-Item -ItemType Directory -Path testdrive:/module/foo
        New-Item -ItemType Directory -Path testdrive:/module/bar
        "" > testdrive:/module/foo/bar.psm1
        "" > testdrive:/module/bar/foo.psm1
        $testModulePath = "testdrive:/module/test.psd1"
        $fileList = "foo\bar.psm1","bar/foo.psm1"

        New-ModuleManifest -NestedModules $fileList -RootModule foo\bar.psm1 -RequiredAssemblies $fileList -Path $testModulePath -TypesToProcess $fileList -FormatsToProcess $fileList -ScriptsToProcess $fileList -FileList $fileList -ModuleList $fileList

        Test-Path $testModulePath | Should Be $true

        Test-ModuleManifest -Path $testModulePath -ErrorAction Stop | Should BeOfType System.Management.Automation.PSModuleInfo
    }

    It "module manifest containing missing files returns error" {

        New-Item -ItemType Directory -Path testdrive:/module
        New-Item -ItemType Directory -Path testdrive:/module/foo
        "" > testdrive:/module/foo/bar.psm1
        $testModulePath = "testdrive:/module/test.psd1"

        $parametersAndErrors = @{"RequiredAssemblies"="Modules_InvalidRequiredAssembliesInModuleManifest";
            "NestedModules"="Modules_InvalidNestedModuleinModuleManifest";
            "RequiredModules"="Modules_InvalidRequiredModulesinModuleManifest";
            "FileList"="Modules_InvalidFilePathinModuleManifest";
            "ModuleList"="Modules_InvalidModuleListinModuleManifest";
            "TypesToProcess"="Modules_InvalidManifest";
            "FormatsToProcess"="Modules_InvalidManifest";
            "RootModule"="Modules_InvalidManifest";
            "ScriptsToProcess"="Modules_InvalidManifest"
        }
        foreach ($parameter in $parametersAndExecptions.Keys) {
            $args = @{$parameter = "doesnotexist.psm1"}
            New-ModuleManifest -Path $testModulePath @args
            Test-Path $testModulePath | Should Be $true
            $errorId = $parametersAndErrors[$parameter]

            try {
                Test-ModuleManifest -Path $testModulePath -ErrorAction Stop | Should BeOfType System.Management.Automation.PSModuleInfo
                throw "Test-ModuleManifest did not throw $errorId"         
            }
            catch {
                $_.FullQulaifiedErrorId | Should Match "$errorId"
            }
            Remove-Item -Recurse -Force $testModulePath
        }
    }
}
