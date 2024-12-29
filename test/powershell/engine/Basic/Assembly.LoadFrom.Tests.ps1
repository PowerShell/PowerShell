# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Assembly.LoadFrom Validation Test" -Tags "CI" {
    BeforeAll {
        $ConsumerCode = @'
            using System;
            using Assembly.Bar;

            namespace Assembly.Foo
            {
                public class Consumer
                {
                    public static string GetName()
                    {
                        return Provider.GetProviderName();
                    }
                }
            }
'@
        $ProviderCode = @'
            using System;

            namespace Assembly.Bar
            {
                public class Provider
                {
                    public static string GetProviderName()
                    {
                        return "Assembly.Bar.Provider";
                    }
                }
            }
'@

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

        $ConsumerAssembly = Join-Path -Path $TempPath -ChildPath "Consumer.dll"
        $ProviderAssembly = Join-Path -Path $TempPath -ChildPath "Provider.dll"

        Add-Type -TypeDefinition $ProviderCode -OutputType Library -OutputAssembly $ProviderAssembly
        Add-Type -TypeDefinition $ConsumerCode -OutputType Library -OutputAssembly $ConsumerAssembly -ReferencedAssemblies $ProviderAssembly

        ## Add-Type uses a random name as the AssemblyName, so we need to change the file name of the to-be-referenced assembly
        ## to the same AssemblyName, so that `Assembly.LoadFrom` can discover the to-be-referenced assembly from the same folder.
        $AssemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($ProviderAssembly)
        $ProviderAssemblyNewPath = Join-Path -Path $TempPath -ChildPath "$($AssemblyName.Name).dll"
        Move-Item -Path $ProviderAssembly -Destination $ProviderAssemblyNewPath
    }

    It "Assembly.LoadFrom should automatically load the implicit referenced assembly from the same folder" {
        ## Both types should not be available before loading the test assemblies
        { [Assembly.Foo.Consumer] } | Should -Throw -ErrorId "TypeNotFound"
        { [Assembly.Bar.Provider] } | Should -Throw -ErrorId "TypeNotFound"

        ## The type 'Assembly.Foo.Consumer' should be available after loading 'Consumer.dll'
        [System.Reflection.Assembly]::LoadFrom($ConsumerAssembly) > $null
        [Assembly.Foo.Consumer].FullName | Should -Be "Assembly.Foo.Consumer"
        ## The type 'Assembly.Bar.Provider' should still not be available
        { [Assembly.Bar.Provider] } | Should -Throw -ErrorId "TypeNotFound"

        ## Calling '[Assembly.Foo.Consumer]::GetName()' will trigger implicit loading of 'Provider.dll' and the call should work
        [Assembly.Foo.Consumer]::GetName() | Should -BeExactly "Assembly.Bar.Provider"
        ## Now the type 'Assembly.Bar.Provider' should be available
        [Assembly.Bar.Provider].FullName | Should -BeExactly "Assembly.Bar.Provider"
    }
}
