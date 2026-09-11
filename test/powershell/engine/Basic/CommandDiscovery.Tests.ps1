# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Command Discovery tests" -Tags "CI" {

    BeforeAll {
        Setup -f testscript.ps1 -Content "'This script should not run. Running from testscript.ps1'"
        Setup -f testscripp.ps1 -Content "'This script should not run. Running from testscripp.ps1'"

        $TestCasesCommandNotFound = @(
                        @{command = 'CommandThatDoesnotExist' ; testName = 'Non-existent command'}
                        @{command = 'testscrip?.ps1' ; testName = 'Multiple matches for filename'}
                        @{command = "demo" + [System.IO.Path]::DirectorySeparatorChar; testName = 'Non existent command with directory separator'}
                        @{command = [System.IO.Path]::DirectorySeparatorChar; testName = 'Directory separator'}
                        @{command = 'environment::\path'; testName = 'Provider qualified path'}
                       )
    }

    It "<testName>" -TestCases $TestCasesCommandNotFound {
        param($command)
        { & $command } | Should -Throw -ErrorId 'CommandNotFoundException'
    }

    It "Command lookup with duplicate paths" {
        $previousPath = $env:PSModulePath

        try
        {
            New-Item -Path "$TestDrive\TestFunctionA" -ItemType Directory
            New-Item -Path "$TestDrive\\TestFunctionA\TestFunctionA.psm1" -Value "function TestFunctionA {}" | Out-Null

            $env:PSModulePath = "$TestDrive" + [System.IO.Path]::PathSeparator + "$TestDrive"
            (Get-Command 'TestFunctionA').count | Should -Be 1
        }
        finally
        {
            $env:PSModulePath = $previousPath
        }
    }

    It "Alias can be set for a cmdlet" {

            Set-Alias 'AliasCommandDiscoveryTest' Get-ChildItem
            $commands = (Get-Command 'AliasCommandDiscoveryTest')

            $commands.Count | Should -Be 1
            $aliasResult = $commands -as [System.Management.Automation.AliasInfo]
            $aliasResult | Should -BeOfType System.Management.Automation.AliasInfo
            $aliasResult.Name | Should -Be 'AliasCommandDiscoveryTest'
    }

    It "Cyclic aliases - direct" {
        {
            Set-Alias CyclicAliasA CyclicAliasB -Force
            Set-Alias CyclicAliasB CyclicAliasA -Force
            & CyclicAliasA
        } | Should -Throw -ErrorId 'CommandNotFoundException'
    }

    It "Cyclic aliases - indirect" {
        {
            Set-Alias CyclicAliasA CyclicAliasB -Force
            Set-Alias CyclicAliasB CyclicAliasC -Force
            Set-Alias CyclicAliasC CyclicAliasA -Force
            & CyclicAliasA
        } | Should -Throw -ErrorId 'CommandNotFoundException'
    }

    It "Get-Command should return only CmdletInfo, FunctionInfo, AliasInfo or FilterInfo" {

         $commands = Get-Command
         $commands.Count | Should -BeGreaterThan 0

        foreach($command in $commands)
        {
            $command.GetType().Name | Should -BeIn @("AliasInfo","FunctionInfo","CmdletInfo","FilterInfo")
        }
    }

    It "Non-existent commands with wildcard should not write errors" {
        Get-Command "CommandDoesNotExist*" -ErrorVariable ev -ErrorAction SilentlyContinue
        $ev | Should -BeNullOrEmpty
    }

    It "Get- is prepended to commands" {
        (& 'location').Path | Should -Be (Get-Location).Path
    }

    Context "Use literal path first when executing scripts" {
        BeforeAll {
            $firstFileName = '[test1].ps1'
            $secondFileName = '1.ps1'
            $thirdFileName = '2.ps1'
            $firstResult = "executing $firstFileName in root"
            $secondResult = "executing $secondFileName in root"
            $thirdResult = "executing $thirdFileName in root"
            Setup -f $firstFileName -Content "'$firstResult'"
            Setup -f $secondFileName -Content "'$secondResult'"
            Setup -f $thirdFileName -Content "'$thirdResult'"

            $subFolder = 'subFolder'
            $firstFileInSubFolder = Join-Path $subFolder -ChildPath $firstFileName
            $secondFileInSubFolder = Join-Path $subFolder -ChildPath $secondFileName
            $thirdFileInSubFolder = Join-Path $subFolder -ChildPath $thirdFileName
            Setup -f $firstFileInSubFolder -Content "'$firstResult'"
            Setup -f $secondFileInSubFolder -Content "'$secondResult'"
            Setup -f $thirdFileInSubFolder -Content "'$thirdResult'"

            $secondFileSearchInSubfolder = (Join-Path -Path $subFolder -ChildPath '[t1].ps1')

            $executionWithWildcardCases = @(
                #Region relative paths with './'
                    @{command = '.\[test1].ps1' ; expectedResult = $firstResult; name = '.\[test1].ps1'}
                    @{command = '.\[t1].ps1' ; expectedResult = $secondResult; name = '.\[t1].ps1'}
                #endregion

                #Region relative Subfolder paths without './'
                    @{command = $secondFileInSubFolder ; expectedResult = $secondResult; name = $secondFileInSubFolder}

                    # Wildcard search is not being performed in this scenario before this change.
                    # I noted the issue in the pending message
                    @{command = $firstFileInSubFolder ; expectedResult = $firstResult; name = $firstFileInSubFolder; Pending="See note about wildcard in https://github.com/PowerShell/PowerShell/issues/9256"}
                    @{command = $secondFileSearchInSubfolder ; expectedResult = $secondResult; name = $secondFileSearchInSubfolder; Pending="See note about wildcard in https://github.com/PowerShell/PowerShell/issues/9256"}
                #endregion
                #Region relative Subfolder paths with '.\'
                    @{command = '.\' + $secondFileInSubFolder ; expectedResult = $secondResult; name = $secondFileInSubFolder}
                    @{command = '.\subFolder\[test1].ps1' ; expectedResult = $firstResult; name = '.\subFolder\[test1].ps1'}
                    @{command = '.\subFolder\[t1].ps1' ; expectedResult = $secondResult; name = '.\' + $secondFileSearchInSubfolder}
                    @{command = '.\' + $firstFileInSubFolder ; expectedResult = $firstResult; name = '.\' + $firstFileInSubFolder}
                    @{command = '.\' + $secondFileSearchInSubfolder ; expectedResult = $secondResult; name = '.\' + $secondFileSearchInSubfolder}
                #endregion

                #region rooted paths
                    @{command = (Join-Path ${TestDrive}  -ChildPath '[test1].ps1') ; expectedResult = $firstResult; name = '.\[test1].ps1 by fully qualified path'}
                    @{command = (Join-Path ${TestDrive}  -ChildPath '[t1].ps1') ; expectedResult = $secondResult; name = '.\1.ps1 by fully qualified path with wildcard'}
                #endregion
            )

            $shouldNotExecuteCases = @(
                @{command = 'subFolder\[test1].ps1' ; testName = 'Relative path that where module qualified syntax overlaps'; ExpectedErrorId = 'CouldNotAutoLoadModule'}
                @{command = '.\[12].ps1' ; testName = 'relative path with bracket wildcard matctching multiple files'}
                @{command = (Join-Path ${TestDrive}  -ChildPath '[12].ps1') ; testName = 'fully qualified path with bracket wildcard matching multiple files'}
            )

            Push-Location ${TestDrive}\
        }

        AfterAll {
            Pop-Location
        }

        It "Invoking <name> should return '<expectedResult>'" -TestCases $executionWithWildcardCases {
            param($command, $expectedResult, [string]$Pending)

            if($Pending)
            {
                Set-TestInconclusive -Message $Pending
            }

            & $command | Should -BeExactly $expectedResult
        }

        It "'<testName>' should not execute" -TestCases $shouldNotExecuteCases {
            param(
                [string]
                $command,
                [string]
                $ExpectedErrorId = 'CommandNotFoundException'
                )
            { & $command } | Should -Throw -ErrorId $ExpectedErrorId
        }
    }

    Context "Get-Command should use globbing first for scripts" {
        BeforeAll {
            $firstResult = '[first script]'
            $secondResult = 'alt script'
            $thirdResult = 'bad script'
            Setup -f '[test1].ps1' -Content "'$firstResult'"
            Setup -f '1.ps1' -Content "'$secondResult'"
            Setup -f '2.ps1' -Content "'$thirdResult'"

            $gcmWithWildcardCases = @(
                @{command = '.\?[tb]est1?.ps1'; expectedCommand = '[test1].ps1'; expectedCommandCount =1; name = '''.\?[tb]est1?.ps1'''}
                @{command = (Join-Path ${TestDrive}  -ChildPath '?[tb]est1?.ps1'); expectedCommand = '[test1].ps1'; expectedCommandCount =1 ; name = '''.\?[tb]est1?.ps1'' by fully qualified path'}
                @{command = '.\[test1].ps1'; expectedCommand = '1.ps1'; expectedCommandCount =1; name = '''.\[test1].ps1'''}
                @{command = (Join-Path ${TestDrive}  -ChildPath '[test1].ps1'); expectedCommand = '1.ps1'; expectedCommandCount =1 ; name = '''.\[test1].ps1'' by fully qualified path'}
                @{command = '.\[12].ps1'; expectedCommand = '1.ps1'; expectedCommandCount =0; name = 'relative path with bracket wildcard matctching multiple files'}
                @{command = (Join-Path ${TestDrive}  -ChildPath '[12].ps1'); expectedCommand = '1.ps1'; expectedCommandCount =0 ; name = 'fully qualified path with bracket wildcard matctching multiple files'}
            )

            Push-Location ${TestDrive}\
        }

        AfterAll {
            Pop-Location
        }

        It "Get-Command <name> should return <expectedCommandCount> command named '<expectedCommand>'" -TestCases $gcmWithWildcardCases {
            param($command, $expectedCommand, $expectedCommandCount)
            $commands = @(Get-Command -Name $command)
            $commands.Count | Should -Be $expectedCommandCount
            if($expectedCommandCount -gt 0)
            {
                $commands.Name | Should -BeExactly $expectedCommand
            }
        }
    }

    Context "error cases" {
        It 'Get-Command "less `"-PsPage %db?B of %DoesNotExist:`"" should return nothing' {
            Get-Command -Name "less `"-PsPage %db?B of %DoesNotExist:`"" | Should -BeNullOrEmpty
        }

        It "Should return command not found for commands in the global scope" {
            {Get-Command -Name 'global:help' -ErrorAction Stop} | Should -Throw -ErrorId 'CommandNotFoundException'
        }
    }

    Context "Native command discovery" {
        It 'Can discover a native command without extension' {
            $expectedName = if ($IsWindows) { "ping.exe" } else { "ping" }
            (Get-Command -Name "ping" -CommandType Application).Name | Should -Match $expectedName
        }

        It 'Can discover a native command with extension on Windows' -Skip:(-not $IsWindows) {
            (Get-Command -Name "ping.exe" -CommandType Application).Name | Should -Match "ping.exe"
        }
    }
}
