# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Using assembly" -Tags "CI" {

    try
    {
        Push-Location $PSScriptRoot
        $guid = [Guid]::NewGuid()

        Add-Type -OutputAssembly $PSScriptRoot\UsingAssemblyTest$guid.dll -TypeDefinition @"
public class ABC {}
"@

        It 'parse reports error on non-existing assembly by relative path' {
            $err = $null
            $ast = [System.Management.Automation.Language.Parser]::ParseInput("using assembly foo.dll", [ref]$null, [ref]$err)

            $err.Count | Should -Be 1
            $err[0].ErrorId | Should -Be ErrorLoadingAssembly
        }

        It 'parse reports error on assembly with non-existing fully qualified name' {
            $err = $null
            $ast = [System.Management.Automation.Language.Parser]::ParseInput("using assembly 'System.Management.Automation, Version=99.0.0.0'", [ref]$null, [ref]$err)

            $err.Count | Should -Be 1
            $err[0].ErrorId | Should -Be ErrorLoadingAssembly
        }

        It 'not allow UNC path' {
            $err = $null
            $ast = [System.Management.Automation.Language.Parser]::ParseInput("using assembly \\networkshare\foo.dll", [ref]$null, [ref]$err)

            $err.Count | Should -Be 1
            $err[0].ErrorId | Should -Be CannotLoadAssemblyFromUncPath
        }

        It 'not allow http path' {
            $err = $null
            $ast = [System.Management.Automation.Language.Parser]::ParseInput("using assembly http://microsoft.com/foo.dll", [ref]$null, [ref]$err)

            $err.Count | Should -Be 1
            $err[0].ErrorId | Should -Be CannotLoadAssemblyWithUriSchema
        }

        It "parse does not load the assembly" -pending {
            $assemblies = [Appdomain]::CurrentDomain.GetAssemblies().GetName().Name
            $assemblies -contains "UsingAssemblyTest$guid" | Should -BeFalse

            $err = $null
            $ast = [System.Management.Automation.Language.Parser]::ParseInput("using assembly .\UsingAssemblyTest$guid.dll", [ref]$null, [ref]$err)

            $assemblies = [Appdomain]::CurrentDomain.GetAssemblies().GetName().Name
            $assemblies -contains "UsingAssemblyTest$guid" | Should -BeFalse
            $err.Count | Should -Be 0

            $ast = [System.Management.Automation.Language.Parser]::ParseInput("using assembly '$PSScriptRoot\UsingAssemblyTest$guid.dll'", [ref]$null, [ref]$err)

            $assemblies = [Appdomain]::CurrentDomain.GetAssemblies().GetName().Name
            $assemblies -contains "UsingAssemblyTest$guid" | Should -BeFalse
            $err.Count | Should -Be 0

            $ast = [System.Management.Automation.Language.Parser]::ParseInput("using assembly `"$PSScriptRoot\UsingAssemblyTest$guid.dll`"", [ref]$null, [ref]$err)

            $assemblies = [Appdomain]::CurrentDomain.GetAssemblies().GetName().Name
            $assemblies -contains "UsingAssemblyTest$guid" | Should -BeFalse
            $err.Count | Should -Be 0
        }

        It "reports runtime error about non-existing assembly with relative path" {
            $e = { [scriptblock]::Create("using assembly .\NonExistingAssembly.dll") } | Should -Throw -ErrorId 'ParseException' -PassThru
            $e.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -Be 'ErrorLoadingAssembly'
        }
#>
        It "Assembly loaded at runtime" -pending {
            $assemblies = pwsh -noprofile -command @"
    using assembly .\UsingAssemblyTest$guid.dll
    [Appdomain]::CurrentDomain.GetAssemblies().GetName().Name
"@
            $assemblies -contains "UsingAssemblyTest$guid" | Should -BeTrue

            $assemblies = pwsh -noprofile -command @"
    using assembly $PSScriptRoot\UsingAssemblyTest$guid.dll
    [Appdomain]::CurrentDomain.GetAssemblies().GetName().Name
"@
            $assemblies -contains "UsingAssemblyTest$guid" | Should -BeTrue

            $assemblies = pwsh -noprofile -command @"
    using assembly System.Drawing
    [Appdomain]::CurrentDomain.GetAssemblies().GetName().Name
"@
            $assemblies -contains "System.Drawing" | Should -BeTrue

            $assemblies = pwsh -noprofile -command @"
    using assembly 'System.Drawing, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
    [Appdomain]::CurrentDomain.GetAssemblies().GetName().Name
"@
            $assemblies -contains "System.Drawing" | Should -BeTrue
        }
    }
    finally
    {
        Remove-Item .\UsingAssemblyTest$guid.dll
        Pop-Location
    }
}
