# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'using module' -Tags "CI" {
    BeforeAll {
        $originalPSModulePath = $env:PSModulePath

        function New-TestModule {
            param(
                [string]$Name,
                [string]$Content,
                [switch]$Manifest,
                [version]$Version = '1.0', # ignored, if $Manifest -eq $false
                [string]$ModulePathPrefix = 'modules' # module is created under TestDrive:\$ModulePathPrefix\$Name
            )

            if ($manifest) {
                New-Item -type directory -Force "${TestDrive}\$ModulePathPrefix\$Name\$Version" > $null
                Set-Content -Path "${TestDrive}\$ModulePathPrefix\$Name\$Version\$Name.psm1" -Value $Content
                New-ModuleManifest -RootModule "$Name.psm1" -Path "${TestDrive}\$ModulePathPrefix\$Name\$Version\$Name.psd1" -ModuleVersion $Version
            } else {
                New-Item -type directory -Force "${TestDrive}\$ModulePathPrefix\$Name" > $null
                Set-Content -Path "${TestDrive}\$ModulePathPrefix\$Name\$Name.psm1" -Value $Content
            }

            $resolvedTestDrivePath = Split-Path ((Get-ChildItem "${TestDrive}\$ModulePathPrefix")[0].FullName)
            if (-not ($env:PSModulePath -like "*$resolvedTestDrivePath*")) {
                $env:PSModulePath += "$([System.IO.Path]::PathSeparator)$resolvedTestDrivePath"
            }
        }

    }

    AfterAll {
        $env:PSModulePath = $originalPSModulePath
    }

    It 'Import-Module has ImplementedAssembly, when classes are present in the module' {
        # Create modules in TestDrive:\
        New-TestModule -Name Foo -Content 'class Foo { [string] GetModuleName() { return "Foo" } }'
        New-TestModule -Manifest -Name FooWithManifest -Content 'class Foo { [string] GetModuleName() { return "FooWithManifest" } }'

        $module = Import-Module Foo  -PassThru
        try {
            $module.ImplementingAssembly | Should -Not -BeNullOrEmpty
        } finally {
            $module | Remove-Module
        }
    }

    It "can use class from another module as a base class with using module" {
        $barType = [scriptblock]::Create(@"
using module Foo
class Bar : Foo {}
[Bar]
"@).Invoke()

        $barType.BaseType.Name | Should -BeExactly 'Foo'
    }

    It "can use class from another module in New-Object" {
        $foo = [scriptblock]::Create(@"
using module FooWithManifest
using module Foo
New-Object FooWithManifest.Foo
New-Object Foo.Foo
"@).Invoke()

        $foo.Count | Should -Be 2
        $foo[0].GetModuleName() | Should -BeExactly 'FooWithManifest'
        $foo[1].GetModuleName() | Should -BeExactly 'Foo'
    }

    It "can use class from another module by full name as base class and [type]" {
        $fooObject = [scriptblock]::Create(@"
using module Foo
class Bar : Foo.Foo {}
[Foo.Foo]::new()
"@).Invoke()
        $fooObject.GetModuleName() | Should -BeExactly 'Foo'
    }

    It "can use modules with classes collision" {
        # we use 3 classes with name Foo at the same time
        # two of them come from 'using module' and one is defined in the scriptblock itself.
        # we should be able to use first two of them by the module-qualified name and the third one it's name.
        $fooModuleName = [scriptblock]::Create(@"
using module Foo
using module FooWithManifest

class Foo { [string] GetModuleName() { return "This" } }

class Bar1 : Foo.Foo {}
class Bar2 : FooWithManifest.Foo {}
class Bar : Foo {}

[Bar1]::new().GetModuleName() # Foo
[Bar2]::new().GetModuleName() # FooWithManifest
[Bar]::new().GetModuleName() # This
(New-Object Foo).GetModuleName() # This
"@).Invoke()

        $fooModuleName.Count | Should -Be 4
        $fooModuleName[0] | Should -BeExactly 'Foo'
        $fooModuleName[1] | Should -BeExactly 'FooWithManifest'
        $fooModuleName[2] | Should -BeExactly 'This'
        $fooModuleName[3] | Should -BeExactly 'This'
    }

    It "doesn't mess up two consecutive scripts" {
        $sb1 = [scriptblock]::Create(@"
using module Foo
class Bar : Foo {}
[Bar]::new().GetModuleName()
"@)

        $sb2 = [scriptblock]::Create(@"
using module Foo

class Foo { [string] GetModuleName() { return "This" } }
class Bar : Foo {}
[Bar]::new().GetModuleName()

"@)
        $sb1.Invoke() | Should -BeExactly 'Foo'
        $sb2.Invoke() | Should -BeExactly 'This'
    }

    It "can use modules with classes collision simple" {
        $fooModuleName = [scriptblock]::Create(@"
using module Foo

class Foo { [string] GetModuleName() { return "This" } }

class Bar1 : Foo.Foo {}
class Bar : Foo {}

[Foo.Foo]::new().GetModuleName() # Foo
[Bar1]::new().GetModuleName() # Foo
[Bar]::new().GetModuleName() # This
[Foo]::new().GetModuleName() # This
(New-Object Foo).GetModuleName() # This
"@).Invoke()

        $fooModuleName.Count | Should -Be 5
        $fooModuleName[0] | Should -BeExactly 'Foo'
        $fooModuleName[1] | Should -BeExactly 'Foo'
        $fooModuleName[2] | Should -BeExactly 'This'
        $fooModuleName[3] | Should -BeExactly 'This'
        $fooModuleName[4] | Should -BeExactly 'This'
    }

    It "can use class from another module as a base class with using module with manifest" {
        $barType = [scriptblock]::Create(@"
using module FooWithManifest
class Bar : Foo {}
[Bar]
"@).Invoke()

        $barType.BaseType.Name | Should -BeExactly 'Foo'
    }

    It "can instantiate class from another module" {
        $foo = [scriptblock]::Create(@"
using module Foo
[Foo]::new()
"@).Invoke()

        $foo.GetModuleName() | Should -BeExactly 'Foo'
    }

    It "cannot instantiate class from another module without using statement" {
        $err = Get-RuntimeError @"
#using module Foo
[Foo]::new()
"@
        $err.FullyQualifiedErrorId | Should -BeExactly 'TypeNotFound'
    }

    It "can use class from another module in New-Object by short name" {
        $foo = [scriptblock]::Create(@"
using module FooWithManifest
New-Object Foo
"@).Invoke()
        $foo.GetModuleName() | Should -BeExactly 'FooWithManifest'
    }

    It "can use class from this module in New-Object by short name" {
        $foo = [scriptblock]::Create(@"
class Foo {}
New-Object Foo
"@).Invoke()
        $foo | Should -Not -BeNullOrEmpty
    }

    # Pending reason:
    # it's not yet implemented.
    It "accept module specification" {
        $foo = [scriptblock]::Create(@"
using module @{ ModuleName = 'FooWithManifest'; ModuleVersion = '1.0' }
New-Object Foo
"@).Invoke()
        $foo.GetModuleName() | Should -BeExactly 'FooWithManifest'
    }

    Context 'parse time errors' {

        It "report an error about not found module" {
            $err = Get-ParseResults "using module ThisModuleDoesntExist"
            $err.Count | Should -Be 1
            $err[0].ErrorId | Should -BeExactly 'ModuleNotFoundDuringParse'
        }

        It "report an error about misformatted module specification" {
            $err = Get-ParseResults "using module @{ Foo = 'Foo' }"
            $err.Count | Should -Be 1
            $err[0].ErrorId | Should -BeExactly 'RequiresModuleInvalid'
        }

        It "report an error about wildcard in the module name" {
            $err = Get-ParseResults "using module fo*"
            $err.Count | Should -Be 1
            $err[0].ErrorId | Should -BeExactly 'WildCardModuleNameError'
        }

        It "report an error about wildcard in the module path" {
            $err = Get-ParseResults "using module C:\fo*"
            $err.Count | Should -Be 1
            $err[0].ErrorId | Should -BeExactly 'WildCardModuleNameError'
        }

        It "report an error about wildcard in the module name inside ModuleSpecification hashtable" {
            $err = Get-ParseResults "using module @{ModuleName = 'Fo*'; RequiredVersion = '1.0'}"
            $err.Count | Should -Be 1
            $err[0].ErrorId | Should -BeExactly 'WildCardModuleNameError'
        }

        # MSFT:5246105
        It "report an error when tokenizer encounters comma" {
            $err = Get-ParseResults "using module ,FooWithManifest"
            $err.Count | Should -Be 1
            $err[0].ErrorId | Should -BeExactly 'MissingUsingItemName'
        }

        It "report an error when tokenizer encounters nothing" {
            $err = Get-ParseResults "using module "
            $err.Count | Should -Be 1
            $err[0].ErrorId | Should -BeExactly 'MissingUsingItemName'
        }

        It "report an error on badly formatted RequiredVersion" {
            $err = Get-ParseResults "using module @{ModuleName = 'FooWithManifest'; RequiredVersion = 1. }"
            $err.Count | Should -Be 1
            $err[0].ErrorId | Should -BeExactly 'RequiresModuleInvalid'
        }

        # MSFT:6897275
        It "report an error on incomplete using input" {
            $err = Get-ParseResults "using module @{ModuleName = 'FooWithManifest'; FooWithManifest = 1." # missing closing bracket
            $err.Count | Should -Be 2
            $err[0].ErrorId | Should -BeExactly 'MissingEndCurlyBrace'
            $err[1].ErrorId | Should -BeExactly 'RequiresModuleInvalid'
        }

        It "report an error when 'using module' terminating by NewLine" {
            $err = Get-ParseResults "using module"
            $err.Count | Should -Be 1
            $err[0].ErrorId | Should -BeExactly 'MissingUsingItemName'
        }

        It "report an error when 'using module' terminating by Semicolon" {
            $err = Get-ParseResults "using module; $testvar=1"
            $err.Count | Should -Be 1
            $err[0].ErrorId | Should -BeExactly 'MissingUsingItemName'
        }

        It "report an error when a value after 'using module' is a unallowed expression" {
            $err = Get-ParseResults "using module )"
            $err.Count | Should -Be 1
            $err[0].ErrorId | Should -BeExactly 'InvalidValueForUsingItemName'
        }

        It "report an error when a value after 'using module' is not a valid module name" {
            $err = Get-ParseResults "using module 123"
            $err.Count | Should -Be 1
            $err[0].ErrorId | Should -BeExactly 'InvalidValueForUsingItemName'
        }
    }

    Context 'short name in case of name collision' {
        It "cannot use as base class" {
            $err = Get-RuntimeError @"
using module Foo
using module FooWithManifest
class Bar : Foo {}
"@
            $err.FullyQualifiedErrorId | Should -Be AmbiguousTypeReference
        }

        It "cannot use as [...]" {
            $err = Get-RuntimeError @"
using module Foo
using module FooWithManifest
[Foo]
"@
            $err.FullyQualifiedErrorId | Should -BeExactly 'AmbiguousTypeReference'
        }

        It "cannot use in New-Object" {
            $err = Get-RuntimeError @"
using module Foo
using module FooWithManifest
New-Object Foo
"@
            $err.FullyQualifiedErrorId | Should -BeExactly 'AmbiguousTypeReference,Microsoft.PowerShell.Commands.NewObjectCommand'
        }

        It "cannot use [type] cast from string" {
            $err = Get-RuntimeError @"
using module Foo
using module FooWithManifest
[type]"Foo"
"@
            $err.FullyQualifiedErrorId | Should -BeExactly 'AmbiguousTypeReference'
        }
    }

    Context 'using use the latest version of module after Import-Module -Force' {
        BeforeAll {
            New-TestModule -Name Foo -Content 'class Foo { [string] GetModuleName() { return "Foo2" } }'
            Import-Module Foo -Force
        }
        It "can use class from another module as a base class with using module" {
            $moduleName = [scriptblock]::Create(@"
using module Foo
[Foo]::new().GetModuleName()
"@).Invoke()

            $moduleName | Should -BeExactly 'Foo2'
        }
    }

    Context 'Side by side' {
        BeforeAll {
            # Add side-by-side module
            $newVersion = '3.4.5'
            New-TestModule -Manifest -Name FooWithManifest -Content 'class Foo { [string] GetModuleName() { return "Foo230" } }' -Version '2.3.0'
            New-TestModule -Manifest -Name FooWithManifest -Content 'class Foo { [string] GetModuleName() { return "Foo345" } }' -Version '3.4.5' -ModulePathPrefix 'Modules2'
        }

        # 'using module' behavior must be aligned with Import-Module.
        # Import-Module does the following:
        # 1) find the first directory from $env:PSModulePath that contains the module
        # 2) Import highest available version of the module
        # In out case TestDrive:\Module is before TestDrive:\Modules2 and so 2.3.0 is the right version
        It "uses the last module, if multiple versions are present" {
            $foo = [scriptblock]::Create(@"
using module FooWithManifest
[Foo]::new()
"@).Invoke()
            $foo.GetModuleName() | Should -BeExactly 'Foo230'
        }

        It "uses right version, when RequiredModule=1.0 specified" {
            $foo = [scriptblock]::Create(@"
using module @{ModuleName = 'FooWithManifest'; RequiredVersion = '1.0'}
[Foo]::new()
"@).Invoke()
            $foo.GetModuleName() | Should -BeExactly 'FooWithManifest'
        }

        It "uses right version, when RequiredModule=2.3.0 specified" {
            $foo = [scriptblock]::Create(@"
using module @{ModuleName = 'FooWithManifest'; RequiredVersion = '2.3.0'}
[Foo]::new()
"@).Invoke()
            $foo.GetModuleName() | Should -BeExactly 'Foo230'
        }

        It "uses right version, when RequiredModule=3.4.5 specified" {
            $foo = [scriptblock]::Create(@"
using module @{ModuleName = 'FooWithManifest'; RequiredVersion = '3.4.5'}
[Foo]::new()
"@).Invoke()
            $foo.GetModuleName() | Should -BeExactly 'Foo345'
        }
    }

    Context 'Use module with runtime error' {
        BeforeAll {
            New-TestModule -Name ModuleWithRuntimeError -Content @'
class Foo { [string] GetModuleName() { return "ModuleWithRuntimeError" } }
throw 'error'
'@
        }

        It "handles runtime errors in imported module" {
            $err = Get-RuntimeError @"
using module ModuleWithRuntimeError
[Foo]::new().GetModuleName()
"@

                $err | Should -Be 'error'
        }
    }

    Context 'shared InitialSessionState' {

        It 'can pick the right module' {

            $scriptToProcessPath = "${TestDrive}\toProcess.ps1"
            Set-Content -Path $scriptToProcessPath -Value @'
using module Foo
function foo()
{
    [Foo]::new()
}
'@
            # resolve name to absolute path
            $scriptToProcessPath = (Get-ChildItem $scriptToProcessPath).FullName
            $iss = [initialsessionstate]::CreateDefault()
            $iss.StartupScripts.Add($scriptToProcessPath)

            $ps = [powershell]::Create($iss)
            $ps.AddCommand("foo").Invoke() | Should -BeExactly 'Foo'
            $ps.Streams.Error | Should -BeNullOrEmpty

            $ps1 = [powershell]::Create($iss)
            $ps1.AddCommand("foo").Invoke() | Should -BeExactly 'Foo'
            $ps1.Streams.Error | Should -BeNullOrEmpty

            $ps.Commands.Clear()
            $ps.Streams.Error.Clear()
            $ps.AddScript(". foo").Invoke() | Should -BeExactly 'Foo'
            $ps.Streams.Error | Should -BeNullOrEmpty
        }
    }

    # here we are back to normal $env:PSModulePath, but all modules are there
    Context "Module by path" {
        BeforeAll {
            # this is a setup for Context "Module by path"
            New-TestModule -Name FooForPaths -Content 'class Foo { [string] GetModuleName() { return "FooForPaths" } }'
            $env:PSModulePath = $originalPSModulePath

            New-Item -type directory -Force TestDrive:\FooRelativeConsumer
            Set-Content -Path "${TestDrive}\FooRelativeConsumer\FooRelativeConsumer.ps1" -Value @'
using module ..\modules\FooForPaths
class Bar : Foo {}
[Bar]::new()
'@

            Set-Content -Path "${TestDrive}\FooRelativeConsumerErr.ps1" -Value @'
using module FooForPaths
class Bar : Foo {}
[Bar]::new()
'@
        }

        It 'use non-modified PSModulePath' {
            $env:PSModulePath | Should -BeExactly $originalPSModulePath
        }

        It "can be accessed by relative path" {
            $barObject = & TestDrive:\FooRelativeConsumer\FooRelativeConsumer.ps1
            $barObject.GetModuleName() | Should -BeExactly 'FooForPaths'
        }

        It "cannot be accessed by relative path without .\ from a script" {
            $err = Get-RuntimeError '& TestDrive:\FooRelativeConsumerErr.ps1'
            $err.FullyQualifiedErrorId | Should -BeExactly 'ModuleNotFoundDuringParse'
        }

        It "can be accessed by absolute path" {
            $resolvedTestDrivePath = Split-Path ((Get-ChildItem TestDrive:\modules)[0].FullName)
            $s = @"
using module $resolvedTestDrivePath\FooForPaths
[Foo]::new()
"@
            $err = Get-ParseResults $s
            $err.Count | Should -Be 0
            $barObject = [scriptblock]::Create($s).Invoke()
            $barObject.GetModuleName() | Should -BeExactly 'FooForPaths'
        }

        It "can be accessed by absolute path with file extension" {
            $resolvedTestDrivePath = Split-Path ((Get-ChildItem TestDrive:\modules)[0].FullName)
            $barObject = [scriptblock]::Create(@"
using module $resolvedTestDrivePath\FooForPaths\FooForPaths.psm1
[Foo]::new()
"@).Invoke()
            $barObject.GetModuleName() | Should -BeExactly 'FooForPaths'
        }

        It "can be accessed by relative path without file" {
            # we should not be able to access .\FooForPaths without cd
            $err = Get-RuntimeError @"
using module .\FooForPaths
[Foo]::new()
"@
            $err.FullyQualifiedErrorId | Should -BeExactly 'ModuleNotFoundDuringParse'

            Push-Location TestDrive:\modules
            try {
                $barObject = [scriptblock]::Create(@"
using module .\FooForPaths
[Foo]::new()
"@).Invoke()
                $barObject.GetModuleName() | Should -BeExactly 'FooForPaths'
            } finally {
                Pop-Location
            }
        }

        It "cannot be accessed by relative path without .\" {
            Push-Location TestDrive:\modules
            try {
                $err = Get-RuntimeError @"
using module FooForPaths
[Foo]::new()
"@
                $err.FullyQualifiedErrorId | Should -BeExactly 'ModuleNotFoundDuringParse'
            } finally {
                Pop-Location
            }
        }

        It 'can be accessed by relative path with .<Separator>' -TestCases @(
            @{ Separator = '\' },
            @{ Separator = '/' }
        ) {
            param([string]$Separator)
            $name = 'relative-slash-paths'
            'function Get-TestString { "Worked" }' | Set-Content "TestDrive:\modules\$name.psm1"

            "using module .$Separator$name.psm1; Get-TestString" | Set-Content "TestDrive:\modules\$name.ps1"

            & "TestDrive:\modules\$name.ps1" | Should -BeExactly "Worked"
        }
    }

    Context "module has non-terminating error handled with 'SilentlyContinue'" {
        BeforeAll {
            $testFile = Join-Path -Path $TestDrive -ChildPath "testmodule.psm1"
            $content = @'
Get-Command -CommandType Application -Name NonExisting -ErrorAction SilentlyContinue
class TestClass { [string] GetName() { return "TestClass" } }
'@
            Set-Content -Path $testFile -Value $content -Force
        }
        AfterAll {
            Remove-Module -Name testmodule -Force -ErrorAction SilentlyContinue
        }

        It "'using module' should succeed" {
            $result = [scriptblock]::Create(@"
using module $testFile
[TestClass]::new()
"@).Invoke()
            $result.GetName() | Should -BeExactly "TestClass"
        }
    }
}
