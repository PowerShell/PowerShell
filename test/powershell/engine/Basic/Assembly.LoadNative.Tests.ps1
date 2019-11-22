# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Assembly::LoadWithPartialName Validation Test" -Tags "CI" {

    BeforeAll {

        ## The assembly files cannot be removed once they are loaded, unless the current PowerShell session exits.
        ## If we use $TestDrive here, then Pester will try to remove them afterward and result in errors.
        $TempPath = [System.IO.Path]::GetTempFileName()
        if (Test-Path $TempPath) { Remove-Item -Path $TempPath -Force -Recurse }
        New-Item -Path $TempPath -ItemType Directory -Force > $null

        $root = Join-Path $TempPath "testDllNativeFolder"
        New-Item -Path $root -ItemType Directory -Force > $null

        $dllSourceFolder = Join-Path $PSScriptRoot "assets"
        Copy-Item -Recurse -Path $dllSourceFolder\* -Destination $root

        $source = @"
            using System;
            using System.Runtime.InteropServices;
            public class TestNativeClass
            {
                public static int Add(int a, int b)
                {
                    return (a + b);
                }

                public static IntPtr LoadNative()
                {
                    return sk_codec_min_buffered_bytes_needed();
                }

                // Copied from https://github.com/mono/SkiaSharp/blob/e0f57880ca5eadfaddde520e8d8365bc00b91d5d/binding/Binding/SkiaApi.generated.cs
                [DllImport ("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
                internal static extern /* size_t */ IntPtr sk_codec_min_buffered_bytes_needed ();
            }
"@

        Add-Type -OutputAssembly $root\managed.dll -TypeDefinition $source

        Add-Type -Assembly $root\managed.dll
    }

    It "Can load native dll" {
        # Managed dll is loaded
        [TestNativeClass]::Add(1,2) | Should -Be 3

        # Native dll is loaded from the same managed dll
        [TestNativeClass]::LoadNative() | Should -Not -BeNullOrEmpty
    }
}
