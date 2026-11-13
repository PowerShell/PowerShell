# Copyright (c) Microsoft Corporation.
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

        It "parse does not load the assembly '<Script>'" -TestCases @{ Script = "using assembly UsingAssemblyTest$guid.dll"; Expected = $false },
                                                      @{ Script = "using assembly '$(Join-Path -Path $PSScriptRoot -ChildPath UsingAssemblyTest$guid.dll)'"; Expected = $false },
                                                      @{ Script = "using assembly `"$(Join-Path -Path $PSScriptRoot -ChildPath UsingAssemblyTest$guid.dll)`""; Expected = $false } {
            param ($script)
            $assemblies = [Appdomain]::CurrentDomain.GetAssemblies().GetName().Name
            $assemblies -contains "UsingAssemblyTest$guid" | Should -BeFalse

            $err = $null
            $ast = [System.Management.Automation.Language.Parser]::ParseInput($script, [ref]$null, [ref]$err)

            $assemblies = [Appdomain]::CurrentDomain.GetAssemblies().GetName().Name
            $assemblies -contains "UsingAssemblyTest$guid" | Should -BeFalse
            $err.Count | Should -Be 0
        }

        It "reports runtime error about non-existing assembly with relative path" {
            $script = "using assembly {0}" -f (Join-Path "." "NonExistingAssembly.dll")
            $e = { [scriptblock]::Create($script) } | Should -Throw -ErrorId 'ParseException' -PassThru
            $e.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -Be 'ErrorLoadingAssembly'
        }
#>
        Context "Runtime loading of assemblies" {
            BeforeAll {
                copy-item "UsingAssemblyTest$guid.dll" $TestDrive
                $assemblyPath = Join-Path $TestDrive "UsingAssemblyTest$guid.dll"

                function InvokeScript ([string]$pathToScript) {
                    $result = & "$PSHOME/pwsh" -noprofile -file $pathToScript
                    return $result
                }
            }

            It "Assembly loaded at runtime" {
                $testFile = Join-Path $TestDrive "TestFile1.ps1"
                $script = "using assembly UsingAssemblyTest$guid.dll`n[ABC].Assembly.Location"
                Set-Content -Path $testFile -Value $script
                $assembly = InvokeScript $testFile
                $assembly | Should -Be $assemblyPath
            }

            It "Assembly loaded at runtime with fully qualified name" {
                $script = "using assembly $assemblyPath`n[ABC].Assembly.Location"
                $testFile = Join-Path $TestDrive "TestFile2.ps1"
                Set-Content -Path $testFile -Value $script
                $assembly = InvokeScript $testFile
                $assembly | Should -Be $assemblyPath
            }
        }
    }
    finally
    {
        Remove-Item -ErrorAction Ignore .\UsingAssemblyTest$guid.dll
        Pop-Location
    }
}
