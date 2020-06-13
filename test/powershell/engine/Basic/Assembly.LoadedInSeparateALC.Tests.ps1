# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Assembly loaded in a separate AssemblyLoadContext should not be seen by PowerShell type resolution" -Tags "CI" {
    BeforeAll {
        $codeVersion1 = @'
        using System.Reflection;

        [assembly: AssemblyVersion("1.0.0.0")]

        namespace ALC.Test {
            public class Blah { public static string GetVersion() { return "1.0.0.0"; } }
        }
'@

        $codeVersion2 = @'
        using System.Reflection;

        [assembly: AssemblyVersion("2.0.0.0")]

        namespace ALC.Test {
            public class Blah { public static string GetVersion() { return "2.0.0.0"; } }
        }
'@

        $tempFolderPath = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "ALCTest")
        $v1_Folder = [System.IO.Path]::Combine($tempFolderPath, "V1")
        $v2_Folder = [System.IO.Path]::Combine($tempFolderPath, "V2")

        $null = New-Item $tempFolderPath -ItemType Directory -Force
        $null = New-Item $v1_Folder -ItemType Directory -Force
        $null = New-Item $v2_Folder -ItemType Directory -Force

        $v1_assembly_file = [System.IO.Path]::Combine($v1_Folder, "Test.dll")
        $v2_assembly_file = [System.IO.Path]::Combine($v2_Folder, "Test.dll")

        if (-not (Test-Path $v1_assembly_file)) {
            Add-Type -TypeDefinition $codeVersion1 -OutputAssembly $v1_assembly_file
        }

        if (-not (Test-Path $v2_assembly_file)) {
            Add-Type -TypeDefinition $codeVersion2 -OutputAssembly $v2_assembly_file
        }

        ## Load the V2 assembly in a separate ALC
        $alc = [System.Runtime.Loader.AssemblyLoadContext]::new("MyALC", $false)
        $v2_Assembly = $alc.LoadFromAssemblyPath($v2_assembly_file)
        ## Load the V1 assembly in the default ALC
        $v1_Assembly = [System.Reflection.Assembly]::LoadFrom($v1_assembly_file)

        $v1_Blah_AssemblyQualifiedName = "[ALC.Test.Blah, {0}]" -f $v1_Assembly.FullName
        $v2_Blah_AssemblyQualifiedName = "[ALC.Test.Blah, {0}]" -f $v2_Assembly.FullName
    }

    It "Type from the assembly loaded into a separate ALC should not be resolved by PowerShell" {
        ## Type resolution should only find the assembly loaded in the default ALC.
        [ALC.Test.Blah]::GetVersion() | Should -BeExactly "1.0.0.0"

        ## Type resolution should fail even with the AssemblyQualifiedName for the 2.0 assembly that was loaded in the separate ALC.
        $resolve_v1_Blah_script = [scriptblock]::Create("{0}::GetVersion()" -f $v1_Blah_AssemblyQualifiedName)
        $resolve_v2_Blah_script = [scriptblock]::Create($v2_Blah_AssemblyQualifiedName)

        $resolve_v2_Blah_script | Should -Throw -ErrorId 'TypeNotFound'
        & $resolve_v1_Blah_script | Should -BeExactly "1.0.0.0"
    }
}
