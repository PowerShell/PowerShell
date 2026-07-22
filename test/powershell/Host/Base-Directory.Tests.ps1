# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Configuration file locations" -tags "CI","Slow" {

    BeforeAll {
        $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"
        $profileName = "Microsoft.PowerShell_profile.ps1"
    }

    Context "Default configuration file locations" {

        BeforeAll {

            if ($IsWindows) {
                $ProductName = "WindowsPowerShell"
                if ($IsCoreCLR -and ($PSHOME -notlike "*Windows\System32\WindowsPowerShell\v1.0"))
                {
                    $ProductName =  "PowerShell"
                }
                $expectedCache              = [IO.Path]::Combine($env:LOCALAPPDATA, "Microsoft", "Windows", "PowerShell", "StartupProfileData-NonInteractive")
                $expectedModule             = [IO.Path]::Combine($env:USERPROFILE, "Documents", $ProductName, "Modules")
                $expectedAllUsersProfile    = [IO.Path]::Combine($PSHOME, $profileName)
                $expectedCurrentUserProfile = [IO.Path]::Combine($env:USERPROFILE, "Documents", $ProductName, $profileName)
                $expectedReadline           = [IO.Path]::Combine($env:AppData, "Microsoft", "Windows", "PowerShell", "PSReadline", "ConsoleHost_history.txt")
            } else {
                if ($IsMacOS) {
                    $PowershellConfigRoot = "/Library/Application Support/PowerShell"
                } else {
                    $PowershellConfigRoot = "/etc/opt/powershell"
                }

                $expectedCache              = [IO.Path]::Combine($env:HOME, ".cache", "powershell", "StartupProfileData-NonInteractive")
                $expectedModule             = [IO.Path]::Combine($env:HOME, ".local", "share", "powershell", "Modules")
                $expectedAllUsersProfile    = [IO.Path]::Combine($PowershellConfigRoot, $profileName)
                $expectedCurrentUserProfile = [IO.Path]::Combine($env:HOME,".config","powershell", $profileName)
                $expectedReadline           = [IO.Path]::Combine($env:HOME, ".local", "share", "powershell", "PSReadLine", "ConsoleHost_history.txt")
            }

            $ItArgs = @{}
        }

        BeforeEach {
            $original_PSModulePath = $env:PSModulePath
        }

        AfterEach {
            $env:PSModulePath = $original_PSModulePath
        }

        It @ItArgs "Current User Profile location should be correct" {
            & $powershell -noprofile -c `$PROFILE | Should -Be $expectedCurrentUserProfile
        }

        It @ItArgs "All Users Profile location should be correct" {
            & $powershell -noprofile -c `$PROFILE.AllUsersAllHosts | Should -Be $expectedAllUsersProfile
        }

        It @ItArgs "All Users Profile location should be correct" {
            $env:POWERSHELL_COMMON_APPLICATION_DATA = Join-Path -Path $PSHOME -ChildPath $profileName
            & $powershell -noprofile -c `$PROFILE.AllUsersAllHosts | Should -Be $env:POWERSHELL_COMMON_APPLICATION_DATA
        }

        It @ItArgs "PSModulePath should contain the correct path" {
            $env:PSModulePath = $null
            $actual = & $powershell -noprofile -c `$env:PSModulePath
            $actual | Should -Match ([regex]::Escape($expectedModule))
        }

        It @ItArgs "PSReadLine history save location should be correct" {
            & $powershell -noprofile { (Get-PSReadLineOption).HistorySavePath } | Should -Be $expectedReadline
        }

        # This feature (and thus test) has been disabled because of the AssemblyLoadContext scenario
        It "JIT cache should be created correctly" -Skip {
            Remove-Item -ErrorAction SilentlyContinue $expectedCache
            & $powershell -noprofile { exit }
            $expectedCache | Should -Exist
        }

        # The ModuleAnalysisCache cannot be forced to exist, thus we cannot test it
    }

    Context "XDG Base Directory Specification is supported on Linux" {
        BeforeAll {
            # Using It @ItArgs, we automatically skip on Windows for all these tests
            if ($IsWindows) {
                $ItArgs = @{ skip = $true }
            } else {
                $ItArgs = @{}
            }
        }

        BeforeEach {
            $original_PSModulePath = $env:PSModulePath
            $original_XDG_CONFIG_HOME = $env:XDG_CONFIG_HOME
            $original_XDG_CACHE_HOME = $env:XDG_CACHE_HOME
            $original_XDG_DATA_HOME = $env:XDG_DATA_HOME
        }

        AfterEach {
            $env:PSModulePath = $original_PSModulePath
            $env:XDG_CONFIG_HOME = $original_XDG_CONFIG_HOME
            $env:XDG_CACHE_HOME = $original_XDG_CACHE_HOME
            $env:XDG_DATA_HOME = $original_XDG_DATA_HOME
        }

        It @ItArgs "Profile should respect XDG_CONFIG_HOME" {
            $env:XDG_CONFIG_HOME = $TestDrive
            $expected = [IO.Path]::Combine($TestDrive, "powershell", $profileName)
            & $powershell -noprofile -c `$PROFILE | Should -Be $expected
        }

        It @ItArgs "PSModulePath should respect XDG_DATA_HOME" {
            $env:PSModulePath = $null
            $env:XDG_DATA_HOME = $TestDrive
            $expected = [IO.Path]::Combine($TestDrive, "powershell", "Modules")
            $actual = & $powershell -noprofile -c `$env:PSModulePath
            $actual | Should -Match $expected
        }

        It @ItArgs "PSReadLine history should respect XDG_DATA_HOME" {
            $env:XDG_DATA_HOME = $TestDrive
            $expected = [IO.Path]::Combine($TestDrive, "powershell", "PSReadLine", "ConsoleHost_history.txt")
            & $powershell -noprofile { (Get-PSReadLineOption).HistorySavePath } | Should -Be $expected
        }

        # This feature (and thus test) has been disabled because of the AssemblyLoadContext scenario
        It -Skip "JIT cache should respect XDG_CACHE_HOME" {
            $env:XDG_CACHE_HOME = $TestDrive
            $expected = [IO.Path]::Combine($TestDrive, "powershell", "StartupProfileData-NonInteractive")
            Remove-Item -ErrorAction SilentlyContinue $expected
            & $powershell -noprofile { exit }
            $expected | Should -Exist
        }
    }
}

Describe "Working directory on startup" -Tag "CI" {
    BeforeAll {
        $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"
        $testPath = New-Item -ItemType Directory -Path "$TestDrive\test[dir]"
        $currentDirectory = Get-Location
    }

    AfterAll {
        Set-Location $currentDirectory
    }

    # https://github.com/PowerShell/PowerShell/issues/5752
    It "Can start in directory where name contains wildcard characters" -Pending {
        Set-Location -LiteralPath $testPath.FullName
        if ($IsMacOS) {
            # on macOS, /tmp is a symlink to /private so the real path is under /private/tmp
            $expectedPath = "/private" + $testPath.FullName
        } else {
            $expectedPath = $testPath.FullName
        }
        & $powershell -noprofile -c { $PWD.Path } | Should -BeExactly $expectedPath
    }
}
