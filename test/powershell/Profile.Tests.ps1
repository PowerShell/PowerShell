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
            # Assuming this is run outside the OPS host on Windows
            & $powershell -noprofile `$PROFILE | Should Be $PROFILE
        }

        It "Should start with the default profile" -Skip:$IsWindows {
            $expected = [IO.Path]::Combine($env:HOME, ".config/powershell", $profileName)
            # Escape variable with backtick so new process interpolates it
             & $powershell -noprofile `$PROFILE | Should Be $expected
        }

        It "Should respect XDG_CONFIG_HOME" -Skip:$IsWindows {
            $env:XDG_CONFIG_HOME = [IO.Path]::Combine($pwd, "test", "path")
            # Escape variable with backtick so new process interpolates it 
            $profileDir =  [IO.Path]::Combine($env:XDG_CONFIG_HOME, "powershell", $profileName)
            & $powershell -noprofile `$PROFILE | Should Be $profileDir
        }
}

    Context "Modules" {

        It "Should start with the default module path" -Skip:$IsWindows {
            $expected = [IO.Path]::Combine($env:HOME, ".config", "powershell")
            $modulepath = & $powershell -noprofile `$env:PSMODULEPATH
            $modulepath = $modulepath.split(';')[0]
            $modulepath | Should Be $expected
        }

	It "Should respect XDG_CACHE_HOME" -Skip:$IsWindows {
	    $env:XDG_CACHE_HOME = [IO.Path]::Combine($pwd, "test", "path")
            $expected = [IO.Path]::Combine($HOME, "Powershell", "test", "path")
            & $powershell -noprofile `$env:XDG_CACHE_HOME | Should Be $expected
	}
	
	It "Should respect XDG_DATA_HOME" -Skip:$IsWindows {
            $env:XDG_DATA_HOME = [IO.Path]::Combine($pwd, "test", "path")
            $datahomeDir = [IO.Path]::Combine($env:XDG_DATA_HOME, "ConsoleHost_history.txt")
            $expected = [IO.Path]::Combine($HOME, "Powershell", "test", "path", "ConsoleHost_history.txt")
            $datahomeDir | Should Be $expected
	}
    }
}
