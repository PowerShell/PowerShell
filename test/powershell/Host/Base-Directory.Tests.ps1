Describe "Configuration file locations" -tags "CI","Slow" {

    BeforeAll {
        $powershell = Join-Path -Path $PsHome -ChildPath "powershell"
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
                $expectedCache    = [IO.Path]::Combine($env:LOCALAPPDATA, "Microsoft", "Windows", "PowerShell", "StartupProfileData-NonInteractive")
                $expectedModule   = [IO.Path]::Combine($env:USERPROFILE, "Documents", $ProductName, "Modules")
                $expectedProfile  = [io.path]::Combine($env:USERPROFILE, "Documents", $ProductName, $profileName)
                $expectedReadline = [IO.Path]::Combine($env:AppData, "Microsoft", "Windows", "PowerShell", "PSReadline", "ConsoleHost_history.txt")
            } else {
                $expectedCache    = [IO.Path]::Combine($env:HOME, ".cache", "powershell", "StartupProfileData-NonInteractive")
                $expectedModule   = [IO.Path]::Combine($env:HOME, ".local", "share", "powershell", "Modules")
                $expectedProfile  = [io.path]::Combine($env:HOME,".config","powershell",$profileName)
                $expectedReadline = [IO.Path]::Combine($env:HOME, ".local", "share", "powershell", "PSReadLine", "ConsoleHost_history.txt")
            }

            $ItArgs = @{}
        }

        BeforeEach {
            $original_PSMODULEPATH = $env:PSMODULEPATH
        }

        AfterEach {
            $env:PSMODULEPATH = $original_PSMODULEPATH
        }

        It @ItArgs "Profile location should be correct" {
            & $powershell -noprofile `$PROFILE | Should Be $expectedProfile
        }

        It @ItArgs "PSMODULEPATH should contain the correct path" {
            $env:PSMODULEPATH = ""
            $actual = & $powershell -noprofile `$env:PSMODULEPATH
            $actual | Should Match ([regex]::Escape($expectedModule))
        }

        It @ItArgs "PSReadLine history save location should be correct" {
            & $powershell -noprofile { (Get-PSReadlineOption).HistorySavePath } | Should Be $expectedReadline
        }

        # This feature (and thus test) has been disabled because of the AssemblyLoadContext scenario
        It "JIT cache should be created correctly" -Skip {
            Remove-Item -ErrorAction SilentlyContinue $expectedCache
            & $powershell -noprofile { exit }
            $expectedCache | Should Exist
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
            $original_PSMODULEPATH = $env:PSMODULEPATH
            $original_XDG_CONFIG_HOME = $env:XDG_CONFIG_HOME
            $original_XDG_CACHE_HOME = $env:XDG_CACHE_HOME
            $original_XDG_DATA_HOME = $env:XDG_DATA_HOME
        }

        AfterEach {
            $env:PSMODULEPATH = $original_PSMODULEPATH
            $env:XDG_CONFIG_HOME = $original_XDG_CONFIG_HOME
            $env:XDG_CACHE_HOME = $original_XDG_CACHE_HOME
            $env:XDG_DATA_HOME = $original_XDG_DATA_HOME
        }

        It @ItArgs "Profile should respect XDG_CONFIG_HOME" {
            $env:XDG_CONFIG_HOME = $TestDrive
            $expected = [IO.Path]::Combine($TestDrive, "powershell", $profileName)
            & $powershell -noprofile `$PROFILE | Should Be $expected
        }

        It @ItArgs "PSMODULEPATH should respect XDG_DATA_HOME" {
            $env:PSMODULEPATH = ""
            $env:XDG_DATA_HOME = $TestDrive
            $expected = [IO.Path]::Combine($TestDrive, "powershell", "Modules")
            $actual = & $powershell -noprofile `$env:PSMODULEPATH
            $actual | Should Match $expected
        }

        It @ItArgs "PSReadLine history should respect XDG_DATA_HOME" {
            $env:XDG_DATA_HOME = $TestDrive
            $expected = [IO.Path]::Combine($TestDrive, "powershell", "PSReadLine", "ConsoleHost_history.txt")
            & $powershell -noprofile { (Get-PSReadlineOption).HistorySavePath } | Should Be $expected
        }

        # This feature (and thus test) has been disabled because of the AssemblyLoadContext scenario
        It -Skip "JIT cache should respect XDG_CACHE_HOME" {
            $env:XDG_CACHE_HOME = $TestDrive
            $expected = [IO.Path]::Combine($TestDrive, "powershell", "StartupProfileData-NonInteractive")
            Remove-Item -ErrorAction SilentlyContinue $expected
            & $powershell -noprofile { exit }
            $expected | Should Exist
        }
    }
}
