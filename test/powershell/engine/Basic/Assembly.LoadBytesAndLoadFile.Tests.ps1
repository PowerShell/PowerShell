# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Assembly loaded in IndividualAssemblyLoadContext should be visible to PowerShell" -Tags "CI" {
    BeforeAll {
        $code1 = @'
        namespace LoadBytes {
            public class MyLoadBytesTest {
                public static string GetName() { return "MyLoadBytesTest"; }
            }
        }
'@
        $code2 = @'
        namespace LoadFile {
            public class MyLoadFileTest {
                public static string GetName() { return "MyLoadFileTest"; }
            }
        }
'@

        $tempFolderPath = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "IndividualALCTest")
        New-Item $tempFolderPath -ItemType Directory -Force > $null
        $loadBytesFile = [System.IO.Path]::Combine($tempFolderPath, "MyLoadBytesTest.dll")
        $loadFileFile = [System.IO.Path]::Combine($tempFolderPath, "MyLoadFileTest.dll")

        if (-not (Test-Path $loadBytesFile)) {
            Add-Type -TypeDefinition $code1 -OutputAssembly $loadBytesFile
        }

        if (-not (Test-Path $loadFileFile)) {
            Add-Type -TypeDefinition $code2 -OutputAssembly $loadFileFile
        }
    }

    It "Assembly loaded via 'Assembly.Load(byte[])' should be discoverable" {
        $bytes = [System.IO.File]::ReadAllBytes($loadBytesFile)
        [System.Reflection.Assembly]::Load($bytes) > $null

        [LoadBytes.MyLoadBytesTest]::GetName() | Should -BeExactly "MyLoadBytesTest"
    }

    It "Assembly loaded via 'Assembly.LoadFile' should be discoverable" {
        [System.Reflection.Assembly]::LoadFile($loadFileFile) > $null

        [LoadFile.MyLoadFileTest]::GetName() | Should -BeExactly "MyLoadFileTest"
    }
}
