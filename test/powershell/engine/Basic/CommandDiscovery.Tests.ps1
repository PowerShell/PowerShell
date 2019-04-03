# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Command Discovery tests" -Tags "CI" {

    BeforeAll {
        setup -f testscript.ps1 -content "'This script should not run. Running from testscript.ps1'"
        setup -f testscripp.ps1 -content "'This script should not run. Running from testscripp.ps1'"

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
            (Get-command 'TestFunctionA').count | Should -Be 1
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
            $aliasResult | Should -BeOfType [System.Management.Automation.AliasInfo]
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
        (& 'location').Path | Should -Be (get-location).Path
    }

    Context "Use literal path first when executing scripts" {
        BeforeAll {
            $firstFileName = '[test1].ps1'
            $secondFileName = '1.ps1'
            $thirdFileName = '2.ps1'
            $firstResult = "executing $firstFileName in root"
            $secondResult = "executing $secondFileName in root"
            $thirdResult = "executing $thirdFileName in root"
            setup -f $firstFileName -content "'$firstResult'"
            setup -f $secondFileName -content "'$secondResult'"
            setup -f $thirdFileName -content "'$thirdResult'"

            $subFolder = 'subFolder'
            $firstFileInSubFolder = Join-Path $subFolder -ChildPath $firstFileName
            $secondFileInSubFolder = Join-Path $subFolder -ChildPath $secondFileName
            $thirdFileInSubFolder = Join-Path $subFolder -ChildPath $thirdFileName
            setup -f $firstFileInSubFolder -content "'$firstResult'"
            setup -f $secondFileInSubFolder -content "'$secondResult'"
            setup -f $thirdFileInSubFolder -content "'$thirdResult'"

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
            param($command, $expectedResult, [String]$Pending)

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
            setup -f '[test1].ps1' -content "'$firstResult'"
            setup -f '1.ps1' -content "'$secondResult'"
            setup -f '2.ps1' -content "'$thirdResult'"

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
}
