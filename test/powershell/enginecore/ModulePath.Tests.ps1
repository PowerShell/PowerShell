Describe "SxS Module Path Basic Tests" -tags "CI" {
    
    BeforeAll {

        if ($IsWindows) 
        {
            $powershell = "$PSHOME\powershell.exe"
            $ProductName = "WindowsPowerShell"
            if ($IsCoreCLR -and ($PSHOME -notlike "*Windows\System32\WindowsPowerShell\v1.0"))
            {
                $ProductName =  "PowerShell"
            }
            $expectedUserPath = Join-Path -Path $HOME -ChildPath "Documents\$ProductName\Modules"
            $expectedSharedPath = Join-Path -Path $env:ProgramFiles -ChildPath "$ProductName\Modules"
        }
        else
        {
            $powershell = "$PSHOME/powershell"
            $expectedUserPath = [System.Management.Automation.Platform]::SelectProductNameForDirectory("USER_MODULES")
            $expectedSharedPath = [System.Management.Automation.Platform]::SelectProductNameForDirectory("SHARED_MODULES")
        }
        $expectedSystemPath = Join-Path -Path $PSHOME -ChildPath 'Modules'
    }

    BeforeEach {
        $originalModulePath = $env:PSMODULEPATH
    }

    AfterEach {
        $env:PSMODULEPATH = $originalModulePath
    }

    It "validate sxs module path" {

        $env:PSMODULEPATH = ""
        $defaultModulePath = & $powershell -nopro -c '$env:PSMODULEPATH'

        $paths = $defaultModulePath -split [System.IO.Path]::PathSeparator

        $paths.Count | Should Be 3
        $paths[0] | Should Be $expectedUserPath
        $paths[1] | Should Be $expectedSharedPath
        $paths[2] | Should Be $expectedSystemPath
    }
}
