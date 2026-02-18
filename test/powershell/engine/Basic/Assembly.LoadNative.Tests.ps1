# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Can load a native assembly" -Tags "CI" {

    BeforeAll {
        ## The assembly files cannot be removed once they are loaded, unless the current PowerShell session exits.
        ## If we use $TestDrive here, then Pester will try to remove them afterward and result in errors.
        if ($IsWindows) {
            $TempPath = [System.IO.Path]::GetTempFileName()
        }
        else {
            $TempPath = (Join-Path $env:HOME $([System.IO.Path]::GetRandomFileName()))
        }

        if (Test-Path $TempPath) { Remove-Item -Path $TempPath -Force -Recurse }
        New-Item -Path $TempPath -ItemType Directory -Force > $null

        $root = Join-Path $TempPath "testDllNativeFolder"
        New-Item -Path $root -ItemType Directory -Force > $null

        $processArch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString().ToLower()

        if ($IsWindows) {
            $arch = "win-" + $processArch
            $nativeDllName = "nativedll.dll"
            $sourceDllName = Join-Path $PSHome "hostpolicy.dll"
        } elseif ($IsLinux) {
            $arch = "linux-" + $processArch
            $nativeDllName = "nativedll.so"
            $sourceDllName = Join-Path $PSHome "libhostpolicy.so"
            # This won't be in PSHome in a FX Dependent install
            # Fallback to finding it using the process object
            if (!(Test-Path $sourceDllName)) {
                $thisProcess = Get-Process -pid $pid
                $sourceDllName = ($thisProcess.Modules | Where-Object {$_.moduleName -eq 'libhostpolicy.so'}).FileName
            }
        } elseif ($IsMacOS) {
            $arch = "osx-" + $processArch
            $nativeDllName = "nativedll.dylib"
            $sourceDllName = Join-Path $PSHome "libhostpolicy.dylib"
        } else {
            throw "Unsupported OS"
        }

        $archFolder = Join-Path $root $arch
        New-Item -Path $archFolder -ItemType Directory -Force > $null
        #New-Item -Path $archFolder\$nativeDllName -ItemType File -Force > $null
        Copy-Item -Path $sourceDllName -Destination $archFolder\$nativeDllName

        $managedDllPath = Join-Path $root managed.dll
        $managedDllPath_wrongextension = Join-Path $root managed2.dll
        $managedDllPath_rightextension = Join-Path $root managed3.dll

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

        # mix up the extension to make sure the right one is loaded
        if ($IsWindows) {
            $extension = "so"
        } elseif ($IsLinux) {
            $extension = "dylib"
        } elseif ($IsMacOS) {
            $extension = "dll"
        } else {
            throw "Unsupported OS"
        }

        $source_wrongextension = @"
            using System;
            using System.Runtime.InteropServices;
            public class TestNativeClass3
            {
                public static int Add(int a, int b)
                {
                    return (a + b);
                }

                public static void LoadNative()
                {
                    TestEntry();
                }

                [DllImport ("nativedll.$extension", CallingConvention = CallingConvention.Cdecl)]
                internal static extern void TestEntry();
            }
"@

        Add-Type -OutputAssembly $managedDllPath_wrongextension -TypeDefinition $source_wrongextension
        Add-Type -Assembly $managedDllPath_wrongextension

        # specify the right extension and ensure it's loaded
        if ($IsWindows) {
            $extension = "dll"
        } elseif ($IsLinux) {
            $extension = "so"
        } elseif ($IsMacOS) {
            $extension = "dylib"
        } else {
            throw "Unsupported OS"
        }

        $source_rightextension = @"
            using System;
            using System.Runtime.InteropServices;
            public class TestNativeClass4
            {
                public static int Add(int a, int b)
                {
                    return (a + b);
                }

                public static void LoadNative()
                {
                    TestEntry();
                }

                [DllImport ("nativedll.$extension", CallingConvention = CallingConvention.Cdecl)]
                internal static extern void TestEntry();
            }
"@

        Add-Type -OutputAssembly $managedDllPath_rightextension -TypeDefinition $source_rightextension
        Add-Type -Assembly $managedDllPath_rightextension
    }

    It "Can load native libary without extension" {
        # Managed dll is loaded
        [TestNativeClass2]::Add(1,2) | Should -Be 3

        # Native dll is loaded from the same managed dll
        { [TestNativeClass2]::LoadNative() } | Should -Throw -ErrorId "EntryPointNotFoundException"
    }

    It "Can load native libary with wrong extension" {
        # Managed dll is loaded
        [TestNativeClass3]::Add(1,2) | Should -Be 3

        # Native dll is loaded from the same managed dll
        { [TestNativeClass3]::LoadNative() } | Should -Throw -ErrorId "EntryPointNotFoundException"
    }

    It "Can load native libary with extension" {
        # Managed dll is loaded
        [TestNativeClass4]::Add(1,2) | Should -Be 3

        # Native dll is loaded from the same managed dll
        { [TestNativeClass4]::LoadNative() } | Should -Throw -ErrorId "EntryPointNotFoundException"
    }
}
