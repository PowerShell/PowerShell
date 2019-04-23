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

    Context "Get-Command should use globbing for scripts" {
        BeforeAll {
            $firstResult = '[first script]'
            $secondResult = 'alt script'
            setup -f '[test1].ps1' -content "'$firstResult'"
            setup -f '1.ps1' -content "'$secondResult'"

            $gcmWithWildcardCases = @(
                @{command = '.\?[tb]est1?.ps1'; expectedCommand = '[test1].ps1'; expectedCommandCount =1; name = '''.\?[tb]est1?.ps1'''}
                @{command = (Join-Path ${TestDrive}  -ChildPath '?[tb]est1?.ps1'); expectedCommand = '[test1].ps1'; expectedCommandCount =1 ; name = '''.\?[tb]est1?.ps1'' by fully qualified path'}
                @{command = '.\[test1].ps1'; expectedCommand = '1.ps1'; expectedCommandCount =1; name = '''.\[test1].ps1'''}
                @{command = (Join-Path ${TestDrive}  -ChildPath '[test1].ps1'); expectedCommand = '1.ps1'; expectedCommandCount =1 ; name = '''.\[test1].ps1'' by fully qualified path'}
            )

            Push-Location ${TestDrive}\
        }

        AfterAll {
            Pop-Location
        }

        It "Get-Command <name> should return <expectedCommandCount> command named '<expectedCommand>'" -TestCases $gcmWithWildcardCases {
            param($command, $expectedCommand, $expectedCommandCount)
            $commands = Get-Command -Name $command
            $commands.Count | Should -Be $expectedCommandCount
            $commands.Name | Should -BeExactly $expectedCommand
        }
    }
}
