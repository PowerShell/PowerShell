# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "SxS Module Path Basic Tests" -tags "CI" {

    BeforeAll {
        if ($IsWindows)
        {
            $powershell = "$PSHOME\pwsh.exe"
            $ProductName = "WindowsPowerShell"
            if ($IsCoreCLR -and ($PSHOME -notlike "*Windows\System32\WindowsPowerShell\v1.0"))
            {
                $ProductName =  "PowerShell"
            }
            $expectedUserPath = Join-Path -Path $HOME -ChildPath "Documents\$ProductName\Modules"
            $expectedSharedPath = Join-Path -Path $env:ProgramFiles -ChildPath "$ProductName\Modules"
            $userConfigPath = "~/Documents/powershell/powershell.config.json"
        }
        else
        {
            $powershell = "$PSHOME/pwsh"
            $expectedUserPath = [System.Management.Automation.Platform]::SelectProductNameForDirectory("USER_MODULES")
            $expectedSharedPath = [System.Management.Automation.Platform]::SelectProductNameForDirectory("SHARED_MODULES")
            $userConfigPath = "~/.config/powershell/powershell.config.json"
        }

        $userConfigExists = $false
        if (Test-Path $userConfigPath) {
            $userConfigExists = $true
            Copy-Item $userConfigPath "$userConfigPath.backup" -Force -ErrorAction Ignore
        }

        $expectedSystemPath = Join-Path -Path $PSHOME -ChildPath 'Modules'

        # Skip these tests in cases when there is no 'pwsh' executable (e.g. when framework dependent PS package is used)
        $skipNoPwsh = -not (Test-Path $powershell)

        if ($IsWindows)
        {
            $expectedWindowsPowerShellPSHomePath = Join-Path $env:windir "System32" "WindowsPowerShell" "v1.0" "Modules"
        }

        ## Setup a fake PSHome
        $fakePSHome = Join-Path -Path $TestDrive -ChildPath 'FakePSHome'
        $fakePSHomeModuleDir = Join-Path -Path $fakePSHome -ChildPath 'Modules'
        $fakePowerShell = Join-Path -Path $fakePSHome -ChildPath (Split-Path -Path $powershell -Leaf)
        $fakePSDepsFile = Join-Path -Path $fakePSHome -ChildPath "pwsh.deps.json"

        New-Item -Path $fakePSHome -ItemType Directory > $null
        New-Item -Path $fakePSHomeModuleDir -ItemType Directory > $null
    }

    AfterAll {
        if ($userConfigExists) {
            Move-Item "$userConfigPath.backup" $userConfigPath -Force -ErrorAction Ignore
        }
        else {
            Remove-Item "$userConfigPath" -Force -ErrorAction Ignore
        }
    }

    BeforeEach {
        $originalModulePath = $env:PSModulePath
    }

    AfterEach {
        $env:PSModulePath = $originalModulePath
    }

    It "validate sxs module path" -Skip:$skipNoPwsh {

        $env:PSModulePath = ""
        $defaultModulePath = & $powershell -nopro -c '$env:PSModulePath'
        $pathSeparator = [System.IO.Path]::PathSeparator

        $paths = $defaultModulePath.Replace("$pathSeparator$pathSeparator", "$pathSeparator") -split $pathSeparator

        if ($IsWindows)
        {
            $expectedPaths = 3 # user, shared, pshome
            $userPaths = [System.Environment]::GetEnvironmentVariable("PSModulePath", [System.EnvironmentVariableTarget]::User)
            $expectedPaths += $userPaths ? $userPaths.Split($pathSeparator).Count : 0
            $machinePaths = [System.Environment]::GetEnvironmentVariable("PSModulePath", [System.EnvironmentVariableTarget]::Machine)
            $expectedPaths += $machinePaths ? $machinePaths.Split($pathSeparator).Count : 0

            $paths.Count | Should -Be $expectedPaths
        }
        else
        {
            $paths.Count | Should -Be 3
        }

        $paths[0].TrimEnd([System.IO.Path]::DirectorySeparatorChar) | Should -Be $expectedUserPath
        $paths[1].TrimEnd([System.IO.Path]::DirectorySeparatorChar) | Should -Be $expectedSharedPath
        $paths[2].TrimEnd([System.IO.Path]::DirectorySeparatorChar) | Should -Be $expectedSystemPath
        $defaultModulePath | Should -BeLike "*$expectedWindowsPowerShellPSHomePath*"
    }

    It "Works with pshome module path derived from a different PowerShell instance" -Skip:(!$IsCoreCLR -or $skipNoPwsh) {

        ## Create 'powershell' and 'pwsh.deps.json' in the fake PSHome folder,
        ## so that the module path calculation logic would believe it's real.
        New-Item -Path $fakePowerShell -ItemType File -Force > $null
        New-Item -Path $fakePSDepsFile -ItemType File -Force > $null

        try {

            ## PSHome module path derived from another PowerShell instance should be ignored
            $env:PSModulePath = $fakePSHomeModuleDir
            $newModulePath = & $powershell -nopro -c '$env:PSModulePath'
            $paths = $newModulePath -split [System.IO.Path]::PathSeparator
            $paths.Count | Should -Be 4
            $paths[0] | Should -Be $expectedUserPath
            $paths[1] | Should -Be $expectedSharedPath
            $paths[2] | Should -Be $expectedSystemPath
            $paths[3] | Should -Be $fakePSHomeModuleDir
        } finally {

            ## Remove 'powershell' and 'pwsh.deps.json' from the fake PSHome folder
            Remove-Item -Path $fakePowerShell -Force -ErrorAction SilentlyContinue
            Remove-Item -Path $fakePSDepsFile -Force -ErrorAction SilentlyContinue
        }
    }

    It "keep non-pshome module path derived from PowerShell instance parent" -Skip:(!$IsCoreCLR -or $skipNoPwsh) {

        ## non-pshome module path derived from another PowerShell instance should be preserved
        $customeModules = Join-Path -Path $TestDrive -ChildPath 'CustomModules'
        $env:PSModulePath = $fakePSHomeModuleDir, $customeModules -join ([System.IO.Path]::PathSeparator)
        $newModulePath = & $powershell -nopro -c '$env:PSModulePath'
        $paths = $newModulePath -split [System.IO.Path]::PathSeparator
        $paths.Count | Should -Be 5
        $paths -contains $fakePSHomeModuleDir | Should -BeTrue
        $paths -contains $customeModules | Should -BeTrue
    }

    It 'Ensures $PSHOME\Modules is inserted correctly when launched from a different version of PowerShell' -Skip:(!($IsCoreCLR -and $IsWindows) -or $skipNoPwsh) {
        # When launched from a different version of PowerShell, PSModulePath contains the other version's PSHOME\Modules path
        # and the Windows PowerShell module path. The other version's module path should be removed and this version's
        # PSHOME\Modules path should be inserted before Windows PowerShell module path.
        $winpwshModulePath = [System.IO.Path]::Combine([System.Environment]::SystemDirectory, "WindowsPowerShell", "v1.0", "Modules");
        $pwshModulePath = Join-Path -Path $PSHOME -ChildPath 'Modules'

        # create a fake 'other version' $PSHOME and $PSHOME\Modules
        $fakeHome = Join-Path -Path $TestDrive -ChildPath 'fakepwsh'
        $fakeModulePath = Join-Path -Path $fakeHome -ChildPath 'Modules'

        $null = New-Item -Path $fakeHome -ItemType Directory
        $null = New-Item -Path $fakeModulePath -ItemType Directory

        # powershell looks for these to files to determine the directory is a pwsh directory.
        Set-Content -Path "$fakeHome\pwsh.exe" -Value "fake pwsh.exe"
        Set-Content -Path "$fakeHome\pwsh.deps.json" -Value 'fake pwsh.deps.json'

        # replace the actual pwsh module path with the fake one.
        $fakeModulePath = $env:PSModulePath.Replace($pwshModulePath, $fakeModulePath, [StringComparison]::OrdinalIgnoreCase)

        $newModulePath = & $powershell -nopro -c '$env:PSModulePath'
        $pwshIndex = $newModulePath.IndexOf($pwshModulePath, [StringComparison]::OrdinalIgnoreCase)
        $wpshIndex = $newModulePath.IndexOf($winpwshModulePath, [StringComparison]::OrdinalIgnoreCase)
        # ensure both module paths exist and the pwsh module path occurs before the Windows PowerShell module path
        $pwshIndex | Should -Not -Be -1
        $wpshIndex | Should -Not -Be -1
        $pwshIndex | Should -BeLessThan $wpshIndex
    }

    It 'Windows PowerShell does not inherit PowerShell paths' -Skip:(!$IsWindows) {
        $out = powershell.exe -noprofile -command '$env:PSModulePath'
        $out | Should -Not -BeLike "*$expectedUserPath*"
        $out | Should -Not -BeLike "*$expectedSharedPath*"
        $out | Should -Not -BeLike "*$expectedSystemPath*"
    }

    It 'Windows PowerShell inherits user added paths' -Skip:(!$IsWindows) {
        $env:PSModulePath += ";myPath"
        $out = powershell.exe -noprofile -command '$env:PSModulePath'
        $out | Should -BeLike '*;myPath'
    }

    It 'Windows PowerShell does not inherit path defined in powershell.config.json' -Skip:(!$IsWindows) {
        try {
            $userConfig = '{ "PSModulePath": "myUserPath" }'
            Set-Content -Path $userConfigPath -Value $userConfig -Force
            $out = & $powershell -noprofile -command 'powershell.exe -noprofile -command $env:PSModulePath'
            $out | Should -Not -BeLike 'myUserPath;*'
        }
        finally {
            Remove-Item -Path $userConfigPath -Force
        }
    }
}

    It 'User PSModulePath has trailing separator' {
        if ($IsWindows) {
            $validation = "*\$env:SystemDrive\*"
        }
        else {
            $validation = "*//*"
        }

        $newUserPath = Join-Path $expectedUserPath ([System.IO.Path]::DirectorySeparatorChar)
        $env:PSModulePath = $env:PSModulePath.Replace($expectedUserPath, $newUserPath).Replace($expectedSharedPath,"")
        $out = & $powershell -noprofile -command '$env:PSModulePath'
        $out.Split([System.IO.Path]::PathSeparator, [System.StringSplitOptions]::RemoveEmptyEntries) | Should -Not -BeLike $validation
    }

    Context "ModuleIntrinsics.GetPSModulePath API tests" {
        BeforeAll {
            # create a local repostory and install a module
            $localSourceName = [Guid]::NewGuid().ToString("n")
            $localSourceLocation = Join-Path $PSScriptRoot assets
            Register-PSRepository -Name $localSourceName -SourceLocation $localSourceLocation -InstallationPolicy Trusted -ErrorAction SilentlyContinue
            Install-Module -Force -Scope AllUsers -Name PowerShell.TestPackage -Repository $localSourceName -ErrorAction SilentlyContinue
            Install-Module -Force -Scope CurrentUser -Name PowerShell.TestPackage -Repository $localSourceName -ErrorAction SilentlyContinue

        $testCases = @(
            @{ Name = "User"   ; Expected = $IsWindows ?
                (Resolve-Path ([Environment]::GetFolderPath("Personal") + "\PowerShell\Modules")).Path :
                (Resolve-Path ([System.Management.Automation.Platform]::SelectProductNameForDirectory("USER_MODULES"))).Path
                }
            @{ Name = "Shared" ; Expected = $IsWindows ?
                [Environment]::GetFolderPath("ProgramFiles") + "\PowerShell\Modules" :
                (Resolve-Path ([System.Management.Automation.Platform]::SelectProductNameForDirectory("SHARED_MODULES"))).Path
            }
            @{ Name = "PSHome" ; Expected = (Resolve-Path (Join-Path $PSHOME Modules)).Path }
        )
        # resolve the paths to ensure they are in the correct format
        $currentModulePathElements = $env:PSModulePath -split [System.IO.Path]::PathSeparator | Foreach-Object { (Resolve-Path $_).Path }
    }

    AfterAll {
        Unregister-PSRepository -Name $localSourceName -ErrorAction SilentlyContinue
    }

    It "The value '<Name>' should return the proper value" -testcase $testCases {
        param ( $Name, $Expected )
        $result = [System.Management.Automation.ModuleIntrinsics]::GetPSModulePath($name)
        $result | Should -not -BeNullOrEmpty
        # spot check pshome, the user and shared paths may not be present
        if ( $name -eq "PSHOME") {
            $result | Should -Be $Expected
        }
    }

    It "The current module path should contain the expected paths for '<Name>'" -testcase $testCases {
        param ( $Name, $Expected )
        $mPath = (Resolve-Path ([System.Management.Automation.ModuleIntrinsics]::GetPSModulePath($name))).Path
        $currentModulePathElements | Should -Contain $mPath
    }
}
