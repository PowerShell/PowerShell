Describe "Module Path Basic Tests" -tags "CI" {
    
    It "validate module path on windows" -Skip:(!$IsWindows) {
        
        $paths = $env:PSMODULEPATH -split [System.IO.Path]::PathSeparator
        $paths.Count | Should Be 3

        $expectedUserPath = Join-Path -Path $HOME -ChildPath 'Documents\PowerShell\Modules'
        $paths[0] | Should Be $expectedUserPath

        $expectedSharedPath = Join-Path -Path $env:ProgramFiles -ChildPath 'PowerShell\Modules'
        $paths[1] | Should Be $expectedSharedPath

        $expectedSystemPath = Join-Path -Path $PSHOME -ChildPath 'Modules'
        $paths[2] | Should Be $expectedSystemPath
    }

    It "validate module path on unix" -Skip:($IsWindows) {
    
        $paths = $env:PSMODULEPATH -split [System.IO.Path]::PathSeparator
        $paths.Count | Should Be 3

        $expectedUserPath = [System.Management.Automation.Platform]::SelectProductNameForDirectory("USER_MODULES")
        $paths[0] | Should Be $expectedUserPath

        $expectedSharedPath = [System.Management.Automation.Platform]::SelectProductNameForDirectory("SHARED_MODULES")
        $paths[1] | Should Be $expectedSharedPath

        $expectedSystemPath = Join-Path -Path $PSHOME -ChildPath 'Modules'
        $paths[2] | Should Be $expectedSystemPath
    }
}
