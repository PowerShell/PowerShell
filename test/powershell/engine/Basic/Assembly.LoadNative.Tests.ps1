# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Can load a native assembly" -Tags "CI" {

    BeforeAll {
        ## The assembly files cannot be removed once they are loaded, unless the current PowerShell session exits.
        ## If we use $TestDrive here, then Pester will try to remove them afterward and result in errors.
        $TempPath = [System.IO.Path]::GetTempFileName()
        if (Test-Path $TempPath) { Remove-Item -Path $TempPath -Force -Recurse }
        New-Item -Path $TempPath -ItemType Directory -Force > $null

        $root = Join-Path $TempPath "testDllNativeFolder"
        New-Item -Path $root -ItemType Directory -Force > $null

        $processArch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString().ToLower()

        if ($IsWindows) {
            $arch = "win-" + $processArch
            $nativeDllName = "nativedll.dll"
            $sourceDllName = "hostpolicy.dll"
        } elseif ($IsLinux) {
            $arch = "linux-" + $processArch
            $nativeDllName = "nativedll.so"
            $sourceDllName = "libhostpolicy.so"
        } elseif ($IsMacOS) {
            $arch = "osx-" + $processArch
            $nativeDllName = "nativedll.dylib"
            $sourceDllName = "libhostpolicy.dylib"
        } else {
            throw "Unsupported OS"
        }

        $archFolder = Join-Path $root $arch
        New-Item -Path $archFolder -ItemType Directory -Force > $null
        #New-Item -Path $archFolder\$nativeDllName -ItemType File -Force > $null
        Copy-Item -Path $PSHOME\$sourceDllName -Destination $archFolder\$nativeDllName

        $managedDllPath = Join-Path $root managed.dll

        $source = @"
            using System;
            using System.Runtime.InteropServices;
            public class TestNativeClass2
            {
                public static int Add(int a, int b)
                {
                    return (a + b);
                }

                public static void LoadNative()
                {
                    TestEntry();
                }

                [DllImport ("nativedll", CallingConvention = CallingConvention.Cdecl)]
                internal static extern void TestEntry();
            }
"@

        Add-Type -OutputAssembly $managedDllPath -TypeDefinition $source
        Add-Type -Assembly $managedDllPath
    }

    It "Can load native dll" {
        # Managed dll is loaded
        [TestNativeClass2]::Add(1,2) | Should -Be 3

        # Native dll is loaded from the same managed dll
        { [TestNativeClass2]::LoadNative() } | Should -Throw -ErrorId "EntryPointNotFoundException"
    }
}
