Describe "XDG Base Directory Specification" {

    BeforeAll {
        $powershell = Join-Path -Path $PsHome -ChildPath "powershell"
        $profileName = "Microsoft.PowerShell_profile.ps1"
    }

    BeforeEach {
        $original_XDG_CONFIG_HOME = $env:XDG_CONFIG_HOME
        $original_XDG_CACHE_HOME = $env:XDG_CACHE_HOME
        $original_XDG_DATA_HOME = $env:XDG_DATA_HOME

    }

    AfterEach {
        $env:XDG_CONFIG_HOME = $original_XDG_CONFIG_HOME
        $env:XDG_CACHE_HOME = $original_XDG_CACHE_HOME
        $env:XDG_DATA_HOME = $original_XDG_DATA_HOME
    }

    Context "Profile" {

        It "Should not change Windows behavior" -Skip:($IsLinux -or $IsOSX) {
            $expected = [IO.Path]::Combine($env:HOME, "Documents", "WindowsPowerShell", $profileName)
            & $powershell -noprofile `$PROFILE | Should Be $expected
        }

        It "Should start with the default profile on Linux" -Skip:($IsWindows) {
            $expected = [IO.Path]::Combine($env:HOME, ".config", "powershell", $profileName)
            & $powershell -noprofile `$PROFILE | Should Be $expected
        }

        It "Should respect XDG_CONFIG_HOME on Linux" -Skip:$IsWindows {
            $env:XDG_CONFIG_HOME = $TestDrive
            $expected = [IO.Path]::Combine($TestDrive, "powershell", $profileName)
            & $powershell -noprofile `$PROFILE | Should Be $expected
        }
    }

    Context "Modules" {

        It "Should not change Windows behavior" -Skip:($IsLinux -or $IsOSX) {
            $expected = [IO.Path]::Combine($env:HOME, "Documents", "WindowsPowerShell", "Modules")
            $actual = & $powershell -noprofile `$env:PSMODULEPATH
            $actual.split(';')[0] | Should Be $expected
        }

        It "Should start with the default module path on Linux" -Skip:$IsWindows {
            $env:PSMODULEPATH = "" # must not be sent to child process
            $expected = [IO.Path]::Combine($env:HOME, ".local", "share", "powershell", "Modules")
            $actual = & $powershell -noprofile `$env:PSMODULEPATH
            $actual.split(';')[0] | Should Be $expected
        }

        It "Should respect XDG_DATA_HOME on Linux" -Skip:$IsWindows {
            $env:PSMODULEPATH = "" # must not be sent to child process
            $env:XDG_DATA_HOME = $TestDrive
            $expected = [IO.Path]::Combine($TestDrive, "powershell", "Modules")
            $actual = & $powershell -noprofile `$env:PSMODULEPATH
            $actual.split(';')[0] | Should Be $expected
        }

    }

    Context "PSReadLine" {

        It "Should not change Windows behavior" -Skip:($IsLinux -or $IsOSX) {
            $expected = [IO.Path]::Combine($env:AppData, "Microsoft", "Windows", "PowerShell", "PSReadline", "ConsoleHost_history.txt")
            & $powershell -noprofile { (Get-PSReadlineOption).HistorySavePath } | Should Be $expected
        }

        It "Should start with the default history save path on Linux" -Skip:$IsWindows {
            $expected = [IO.Path]::Combine($env:HOME, ".local", "share", "powershell", "PSReadLine", "ConsoleHost_history.txt")
            & $powershell -noprofile { (Get-PSReadlineOption).HistorySavePath } | Should Be $expected
        }

        It "Should respect XDG_DATA_HOME on Linux" -Skip:$IsWindows {
            $env:XDG_DATA_HOME = $TestDrive
            $expected = [IO.Path]::Combine($TestDrive, "powershell", "PSReadLine", "ConsoleHost_history.txt")
            & $powershell -noprofile { (Get-PSReadlineOption).HistorySavePath } | Should Be $expected
        }

    }

    Context "Cache" {

        It "Should not change Windows behavior" -Skip:($IsLinux -or $IsOSX) {
            $expected = [IO.Path]::Combine($env:HOME, "Documents", "WindowsPowerShell", "StartupProfileData-NonInteractive")
            Remove-Item -ErrorAction SilentlyContinue $expected
            & $powershell -noprofile { exit }
            $expected | Should Exist
        }

        It "Should start with the default StartupProfileData on Linux" -Skip:$IsWindows {
            $expected = [IO.Path]::Combine($env:HOME, ".cache", "powershell", "StartupProfileData-NonInteractive")
            Remove-Item -ErrorAction SilentlyContinue $expected
            & $powershell -noprofile { exit }
            $expected | Should Exist
        }

        It "Should respect XDG_CACHE_HOME on Linux" -Skip:$IsWindows {
            $env:XDG_CACHE_HOME = $TestDrive
            $expected = [IO.Path]::Combine($TestDrive, "powershell", "StartupProfileData-NonInteractive")
            Remove-Item -ErrorAction SilentlyContinue $expected
            & $powershell -noprofile { exit }
            $expected | Should Exist
        }

        # The ModuleAnalysisCache cannot be forced to exist, thus we cannot test it

    }
}
