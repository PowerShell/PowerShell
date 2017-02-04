Describe "Test-ModuleManifest tests" -tags "CI" {

    It "module manifest containing paths with backslashes or forwardslashes are resolved correctly" {

        New-Item -ItemType Directory -Path testdrive:/module
        New-Item -ItemType Directory -Path testdrive:/module/foo
        New-Item -ItemType Directory -Path testdrive:/module/bar
        "" > testdrive:/module/foo/bar.psm1
        "" > testdrive:/module/bar/foo.psm1
        New-ModuleManifest -NestedModules foo\bar.psm1,bar/foo.psm1 -RootModule foo\bar.psm1 -RequiredAssemblies foo/bar.psm1,bar\foo.psm1 -Path testdrive:/module/test.psd1
        Test-ModuleManifest -Path testdrive:/module/test.psd1 -ErrorAction Stop | Should BeOfType System.Management.Automation.PSModuleInfo
    }
}
