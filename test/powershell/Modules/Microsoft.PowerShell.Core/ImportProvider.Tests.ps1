# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Import PowerShell provider" -Tags "CI" {
    BeforeAll {
        $testModulePath = Join-Path $TestDrive "ReproModule"
        New-Item -Path $testModulePath -ItemType Directory > $null

        New-ModuleManifest -Path "$testModulePath/ReproModule.psd1" -RootModule 'testmodule.dll'

        $testBinaryModulePath = Join-Path $testModulePath "testmodule.dll"
        $binaryModule = @'
using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Provider;

namespace module {
    [CmdletProvider(
        "SamplePrv",
        ProviderCapabilities.ShouldProcess)]
    public class SampleProvider : ContainerCmdletProvider {
        protected override bool IsValidPath(string path) {
            return true;
        }

        protected override bool ItemExists(string path) {
            return path == "test.txt";
        }

        protected override void GetItem(string path) {
            Item resultItem;
            if (path == "test.txt") {
            resultItem = new Item { Name = "test.txt" };
            } else {
            throw new Exception("Item not found.");
            }

            WriteItemObject(resultItem, path, false);
        }

        protected override Collection<PSDriveInfo> InitializeDefaultDrives() {
            var drive = new PSDriveInfo(
            "defaultSampleDrive",
            ProviderInfo,
            "/",
            "Sample default drive",
            null);
            var result = new Collection<PSDriveInfo> {drive};
            return result;
        }

        private class Item {
            public string Name { get; set; }
        }
    }
}
'@
        Add-Type -OutputAssembly $testBinaryModulePath -TypeDefinition $binaryModule

        $pwsh = "$PSHOME\pwsh"
    }

    It "Import a PowerShell provider with correct name" {
        $result = & $pwsh -NoProfile -Command "Import-Module -Name $testModulePath; (Get-Item ReproModule\SamplePrv::test.txt).PSPath"
        $result | Should -BeExactly "ReproModule\SamplePrv::test.txt"
    }
}
