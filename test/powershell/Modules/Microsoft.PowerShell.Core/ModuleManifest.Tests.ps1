# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Recursively creates a module structure given a hashtable to describe it:
#  - psd1 keys have values splatted to New-ModuleManifest
#  - psm1 keys have values written to files
#  - Other keys with hashtable values are treated as recursive module definitions
function New-ModuleFromLayout
{
    param(
        [Parameter(Mandatory=$true)]
        [hashtable]
        $Layout,

        [Parameter(Mandatory=$true)]
        [string]
        $BaseDir
    )

    if (-not (Test-Path $BaseDir))
    {
        $null = New-Item -Path $BaseDir -ItemType Directory
    }

    foreach ($item in $Layout.get_Keys())
    {
        $itemPath = Join-Path $BaseDir $item
        $ext = [System.IO.Path]::GetExtension($item)

        switch ($ext)
        {
            '.psd1'
            {
                $moduleParameters = $Layout[$item]
                New-ModuleManifest -Path $itemPath @moduleParameters
                break
            }

            { '.psm1','.ps1' -contains $_ }
            {
                $null = New-Item -Path $itemPath -Value $Layout[$item]
                break
            }

            default
            {
                if ($Layout[$item] -is [hashtable])
                {
                    New-ModuleFromLayout -BaseDir $itemPath -Layout $Layout[$item]
                }
                break
            }
        }
    }
}

Describe "Manifest required module autoloading from module path with simple names" -Tags "CI" {
    BeforeAll {
        $prevModulePath = $env:PSModulePath
        $env:PSModulePath = ($TestDrive -as [string]) + [System.IO.Path]::PathSeparator + $env:PSModulePath

        $mainModule = 'mainmod'
        $requiredModule = 'reqmod'

        New-ModuleFromLayout -BaseDir $TestDrive -Layout @{
            $mainModule = @{
                "$mainModule.psd1" = @{
                    RequiredModules = $requiredModule
                }
            }
            $requiredModule = @{
                "$requiredModule.psd1" = @{}
            }
        }
    }

    AfterAll {
        $env:PSModulePath = $prevModulePath
        Get-Module $mainModule,$requiredModule | Remove-Module
    }

    It "Importing main module loads required modules successfully" {
        $mainMod = Import-Module $mainModule -PassThru -ErrorAction Stop
        $reqMod = Get-Module -Name $requiredModule

        $mainMod.Name | Should -BeExactly $mainModule
        $mainMod.RequiredModules[0].Name | Should -Be $requiredModule
        $reqMod.Name | Should -BeExactly $requiredModule
    }
}

Describe "Manifest required module autoloading with relative path to dir" -Tags "CI" {
    BeforeAll {
        $mainModule = 'mainmod'
        $requiredModule = 'reqmod'

        $mainModPath = Join-Path $TestDrive $mainModule

        # Test to ensure that we treat backslashes as path separators on UNIX and vice-versa
        $altSep = [System.IO.Path]::AltDirectorySeparatorChar

        New-ModuleFromLayout -BaseDir $TestDrive -Layout @{
            $mainModule = @{
                "$mainModule.psd1" = @{
                    RequiredModules = "..${altSep}$requiredModule"
                }
            }
            $requiredModule = @{
                "$requiredModule.psd1" = @{}
            }
        }
    }

    AfterAll {
        Get-Module $mainModule,$requiredModule | Remove-Module
    }

    It "Importing main module loads required modules successfully" {
        $mainMod = Import-Module $mainModPath -PassThru -ErrorAction Stop
        $reqMod = Get-Module -Name $requiredModule

        $mainMod.Name | Should -BeExactly $mainModule
        $mainMod.RequiredModules[0].Name | Should -Be $requiredModule
        $reqMod.Name | Should -BeExactly $requiredModule
    }
}

Describe "Manifest required module autoloading with relative path to manifest" -Tags "CI" {
    BeforeAll {
        $mainModule = 'mainmod'
        $requiredModule = 'reqmod'

        $mainModPath = Join-Path $TestDrive $mainModule "$mainModule.psd1"

        # Test to ensure that we treat backslashes as path separators on UNIX and vice-versa
        $altSep = [System.IO.Path]::AltDirectorySeparatorChar
        $sep = [System.IO.Path]::DirectorySeparatorChar

        New-ModuleFromLayout -BaseDir $TestDrive -Layout @{
            $mainModule = @{
                "$mainModule.psd1" = @{
                    RequiredModules = "..${altSep}$requiredModule${sep}$requiredModule.psd1"
                }
            }
            $requiredModule = @{
                "$requiredModule.psd1" = @{}
            }
        }
    }

    AfterAll {
        Get-Module $mainModule,$requiredModule | Remove-Module
    }

    It "Importing main module loads required modules successfully" {
        $mainMod = Import-Module $mainModPath -PassThru -ErrorAction Stop
        $reqMod = Get-Module -Name $requiredModule

        $mainMod.Name | Should -BeExactly $mainModule
        $mainMod.RequiredModules[0].Name | Should -Be $requiredModule
        $reqMod.Name | Should -BeExactly $requiredModule
    }
}

Describe "Manifest required module autoloading with absolute path to dir" -Tags "CI" {
    BeforeAll {
        $mainModule = 'mainmod'
        $requiredModule = 'reqmod'

        $mainModPath = Join-Path $TestDrive $mainModule "$mainModule.psd1"

        # Test to ensure that we treat backslashes as path separators on UNIX and vice-versa
        $altSep = [System.IO.Path]::AltDirectorySeparatorChar
        $sep = [System.IO.Path]::DirectorySeparatorChar

        New-ModuleFromLayout -BaseDir $TestDrive -Layout @{
            $mainModule = @{
                "$mainModule.psd1" = @{
                    RequiredModules = "$TestDrive${altSep}$requiredModule${sep}"
                }
            }
            $requiredModule = @{
                "$requiredModule.psd1" = @{}
            }
        }
    }

    AfterAll {
        Get-Module $mainModule,$requiredModule | Remove-Module
    }

    It "Importing main module loads required modules successfully" {
        $mainMod = Import-Module $mainModPath -PassThru -ErrorAction Stop
        $reqMod = Get-Module -Name $requiredModule

        $mainMod.Name | Should -BeExactly $mainModule
        $mainMod.RequiredModules[0].Name | Should -Be $requiredModule
        $reqMod.Name | Should -BeExactly $requiredModule
    }
}

Describe "Manifest required module autoloading with absolute path to manifest" -Tags "CI" {
    BeforeAll {
        $mainModule = 'mainmod'
        $requiredModule = 'reqmod'

        $mainModPath = Join-Path $TestDrive $mainModule "$mainModule.psd1"

        # Test to ensure that we treat backslashes as path separators on UNIX and vice-versa
        $altSep = [System.IO.Path]::AltDirectorySeparatorChar
        $sep = [System.IO.Path]::DirectorySeparatorChar

        New-ModuleFromLayout -BaseDir $TestDrive -Layout @{
            $mainModule = @{
                "$mainModule.psd1" = @{
                    RequiredModules = "$TestDrive${altSep}$requiredModule${sep}$requiredModule.psd1"
                }
            }
            $requiredModule = @{
                "$requiredModule.psd1" = @{}
            }
        }
    }

    AfterAll {
        Get-Module $mainModule,$requiredModule | Remove-Module
    }

    It "Importing main module loads required modules successfully" {
        $mainMod = Import-Module $mainModPath -PassThru -ErrorAction Stop
        $reqMod = Get-Module -Name $requiredModule

        $mainMod.Name | Should -BeExactly $mainModule
        $mainMod.RequiredModules[0].Name | Should -Be $requiredModule
        $reqMod.Name | Should -BeExactly $requiredModule
    }
}
