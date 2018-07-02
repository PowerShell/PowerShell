# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Get-Item" -Tags "CI" {
    BeforeAll {
        if ( $IsWindows ) {
            $skipNotWindows = $false
        }
        else {
            $skipNotWindows = $true
        }
    }
    It "Should list all the items in the current working directory when asterisk is used" {
        $items = Get-Item (Join-Path -Path $PSScriptRoot -ChildPath "*")
        ,$items | Should -BeOfType 'System.Object[]'
    }

    It "Should return the name of the current working directory when a dot is used" {
        $item = Get-Item $PSScriptRoot
        $item | Should -BeOfType 'System.IO.DirectoryInfo'
        $item.Name | Should -BeExactly (Split-Path $PSScriptRoot -Leaf)
    }

    It "Should return the proper Name and BaseType for directory objects vs file system objects" {
        $rootitem = Get-Item $PSScriptRoot
        $rootitem | Should -BeOfType 'System.IO.DirectoryInfo'
        $childitem = (Get-Item (Join-Path -Path $PSScriptRoot -ChildPath Get-Item.Tests.ps1))
        $childitem | Should -BeOfType 'System.IO.FileInfo'
    }

    It "Using -literalpath should find no additional files" {
        $null = New-Item -type file "$TESTDRIVE/file[abc].txt"
        $null = New-Item -type file "$TESTDRIVE/filea.txt"
        # if literalpath is not correct we would see filea.txt
        $item = Get-Item -literalpath "$TESTDRIVE/file[abc].txt"
        @($item).Count | Should -Be 1
        $item.Name | Should -BeExactly 'file[abc].txt'
    }

    It "Should have mode flags set" {
        Get-ChildItem $PSScriptRoot | foreach-object { $_.Mode | Should -Not -BeNullOrEmpty }
    }

    It "Should not return the item unless force is used if hidden" {
        ${hiddenFile} = "${TESTDRIVE}/.hidden.txt"
        ${item} = New-Item -type file "${hiddenFile}"
        if ( ${IsWindows} ) {
            attrib +h "$hiddenFile"
        }
        ${result} = Get-Item "${hiddenFile}" -ErrorAction SilentlyContinue
        ${result} | Should -BeNullOrEmpty
        ${result} = Get-Item -force "${hiddenFile}" -ErrorAction SilentlyContinue
        ${result}.FullName | Should -BeExactly ${item}.FullName
    }

    Context "Test for Include, Exclude, and Filter" {
        BeforeAll {
            ${testBaseDir} = "${TESTDRIVE}/IncludeExclude"
            $null = New-Item -Type Directory "${testBaseDir}"
            $null = New-Item -Type File "${testBaseDir}/file1.txt"
            $null = New-Item -Type File "${testBaseDir}/file2.txt"
        }
        It "Should respect -Exclude" {
            $result = Get-Item "${testBaseDir}/*" -Exclude "file2.txt"
            ($result).Count | Should -Be 1
            $result.Name | Should -BeExactly "file1.txt"
        }
        It "Should respect -Include" {
            $result = Get-Item "${testBaseDir}/*" -Include "file2.txt"
            ($result).Count | Should -Be 1
            $result.Name | Should -BeExactly "file2.txt"
        }
        It "Should respect -Filter" {
            $result = Get-Item "${testBaseDir}/*" -Filter "*2*"
            ($result).Count | Should -Be 1
            $result.Name | Should -BeExactly "file2.txt"
        }
        It "Should respect combinations of filter, include, and exclude" {
            $result = get-item "${testBaseDir}/*" -filter *.txt -include "file[12].txt" -exclude file2.txt
            ($result).Count | Should -Be 1
            $result.Name | Should -BeExactly "file1.txt"
        }
    }

    Context "Error Condition Checking" {
        It "Should return an error if the provider does not exist" {
            { Get-Item BadProvider::/BadFile -ErrorAction Stop } | Should -Throw -ErrorId "ProviderNotFound,Microsoft.PowerShell.Commands.GetItemCommand"
        }

        It "Should return an error if the drive does not exist" {
            { Get-Item BadDrive:/BadFile -ErrorAction Stop } | Should -Throw -ErrorId "DriveNotFound,Microsoft.PowerShell.Commands.GetItemCommand"
        }
    }

    Context "Alternate Stream Tests" {
        BeforeAll {
            if ( $skipNotWindows )
            {
                return
            }
            $altStreamPath = "$TESTDRIVE/altStream.txt"
            $stringData = "test data"
            $streamName = "test"
            $item = new-item -type file $altStreamPath
            Set-Content -path $altStreamPath -Stream $streamName -Value $stringData
        }
        It "Should find an alternate stream if present" -skip:$skipNotWindows {
            $result = Get-Item $altStreamPath -Stream $streamName
            $result.Length | Should -Be ($stringData.Length + [Environment]::NewLine.Length)
            $result.Stream | Should -Be $streamName
        }
    }

    Context "Registry Provider" {
        It "Can retrieve an item from registry" -skip:$skipNotWindows {
            ${result} = Get-Item HKLM:/Software
            ${result} | Should -BeOfType "Microsoft.Win32.RegistryKey"
        }
    }

    Context "Environment provider" -tag "CI" {
        BeforeAll {
            $env:testvar="b"
            $env:testVar="a"
        }

        AfterAll {
            Clear-Item -Path env:testvar -ErrorAction SilentlyContinue
            Clear-Item -Path env:testVar -ErrorAction SilentlyContinue
        }

        It "get-item testVar" {
            (get-item env:\testVar).Value | Should -BeExactly "a"
        }

        It "get-item is case-sensitive/insensitive as appropriate" {
            $expectedValue = "b"
            if($IsWindows)
            {
                $expectedValue = "a"
            }

            (get-item env:\testvar).Value | Should -BeExactly $expectedValue
        }
    }
}

Describe "Get-Item environment provider on Windows with accidental case-variant duplicates" -Tags "Scenario" {
    BeforeAll {
        $env:testVar = 'a' # Note: Even though PSScriptAnalyzer can't detect it, this variable *is* used below, namely via Node.js.
    }
    AfterAll {
        $env:testVar = $null
    }
    It "Reports the effective value among accidental case-variant duplicates on Windows" -skip:$skipNotWindows {
        if (-not (Get-Command -ErrorAction Ignore node.exe)) {
            Write-Warning "Test skipped, because prerequisite Node.js is not installed."
        } else {
            $valDirect, $valGetItem, $unused = node.exe -pe @"
                env = {}
                env.testVar = process.env.testVar // include the original case variant with its original value.
                env.TESTVAR = 'b' // redefine with a case variant name and different value
                // Note: Which value will win is not deterministic(!); what matters, however, is that both
                //       $env:testvar and Get-Item env:testvar report the same value.
                //       The nondeterministic behavior makes it hard to prove that the values are *always* the
                //       same, however.
                require('child_process').execSync(\"\\\"$($PSHOME -replace '\\', '/')/pwsh.exe\\\" -noprofile -command `$env:testvar, (Get-Item env:testvar).Value\", { env: env }).toString()
"@
            $valGetItem | Should -BeExactly $valDirect
        }
    }
}
