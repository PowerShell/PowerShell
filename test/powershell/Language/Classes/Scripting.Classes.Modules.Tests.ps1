# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'PSModuleInfo.GetExportedTypeDefinitions()' -Tags "CI" {
    It "doesn't throw for any module" {
        $discard = Get-Module -ListAvailable | ForEach-Object { $_.GetExportedTypeDefinitions() }
        $true | Should -BeTrue # we only verify that we didn't throw. This line contains a dummy Should to make pester happy.
    }
}

Describe 'use of a module from two runspaces' -Tags "CI" {
    function New-TestModule {
        param(
            [string]$Name,
            [string]$Content
        )

        $TestModulePath = Join-Path -Path $TestDrive -ChildPath "TestModule"
        $ModuleFolder = Join-Path -Path $TestModulePath -ChildPath $Name
        New-Item -Path $ModuleFolder -ItemType Directory -Force > $null

        Set-Content -Path "$ModuleFolder\$Name.psm1" -Value $Content

        $manifestParams = @{
            Path = "$ModuleFolder\$Name.psd1"
            RootModule = "$Name.psm1"
        }
        New-ModuleManifest @manifestParams

        if ($env:PSModulePath -NotLike "*$TestModulePath*") {
            $env:PSModulePath += "$([System.IO.Path]::PathSeparator)$TestModulePath"
        }
    }

    $originalPSModulePath = $env:PSModulePath
    try {

        New-TestModule -Name 'Random' -Content @'
$script:random = Get-Random
class RandomWrapper
{
    [int] getRandom()
    {
        return $script:random
    }
}
'@

        It 'use different sessionStates for different modules' {
            $ps = 1..2 | ForEach-Object { $p = [powershell]::Create().AddScript(@'
Import-Module Random
'@)
                $p.Invoke() > $null
                $p
            }
            $res = 1..2 | ForEach-Object {
                0..1 | ForEach-Object {
                    $ps[$_].Commands.Clear()
                    # The idea: instance created inside the context, in one runspace.
                    # Method is called on instance in the different runspace, but it should know about the origin.
                    $w = $ps[$_].AddScript('& (Get-Module Random) { [RandomWrapper]::new() }').Invoke()[0]
                    $w.getRandom()
                }
            }

            $res.Count | Should -Be 4
            $res[0] | Should -Not -Be $res[1]
            $res[0] | Should -Be $res[2]
            $res[1] | Should -Be $res[3]
        }

    } finally {
        $env:PSModulePath = $originalPSModulePath
    }

}

Describe 'Module reloading with Class definition' -Tags "CI" {

    BeforeAll {
        Set-Content -Path TestDrive:\TestModule.psm1 -Value @'
$passedArgs = $args
class Root { $passedIn = $passedArgs }
function Get-PassedArgsRoot { [Root]::new().passedIn }
function Get-PassedArgsNoRoot { $passedArgs }
'@
        $Arg_Hello = 'Hello'
        $Arg_World = 'World'
    }

    AfterEach {
        Remove-Module TestModule -Force -ErrorAction SilentlyContinue
    }

    It "Class execution reflects changes in module reloading with '-Force'" {
        Import-Module TestDrive:\TestModule.psm1 -ArgumentList $Arg_Hello
        Get-PassedArgsRoot | Should -BeExactly $Arg_Hello
        Get-PassedArgsNoRoot | Should -BeExactly $Arg_Hello

        Import-Module TestDrive:\TestModule.psm1 -ArgumentList $Arg_World -Force
        Get-PassedArgsRoot | Should -BeExactly $Arg_World
        Get-PassedArgsNoRoot | Should -BeExactly $Arg_World
    }

    It "Class execution reflects changes in module reloading with 'Remove-Module' and 'Import-Module'" {
        Import-Module TestDrive:\TestModule.psm1 -ArgumentList $Arg_Hello
        Get-PassedArgsRoot | Should -BeExactly $Arg_Hello
        Get-PassedArgsNoRoot | Should -BeExactly $Arg_Hello

        Remove-Module TestModule

        Import-Module TestDrive:\TestModule.psm1 -ArgumentList $Arg_World
        Get-PassedArgsRoot | Should -BeExactly $Arg_World
        Get-PassedArgsNoRoot | Should -BeExactly $Arg_World
    }
}
