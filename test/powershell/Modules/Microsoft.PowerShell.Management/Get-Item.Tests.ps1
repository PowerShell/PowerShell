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
        ,$items | Should BeOfType 'System.Object[]'
    }

    It "Should return the name of the current working directory when a dot is used" {
        $item = Get-Item $PSScriptRoot
        $item | Should BeOfType 'System.IO.DirectoryInfo'
        $item.Name | Should Be (Split-Path $PSScriptRoot -Leaf)
    }

    It "Should return the proper Name and BaseType for directory objects vs file system objects" {
        $rootitem = Get-Item $PSScriptRoot
        $rootitem | Should BeOfType 'System.IO.DirectoryInfo'
        $childitem = (Get-Item (Join-Path -Path $PSScriptRoot -ChildPath Get-Item.Tests.ps1))
        $childitem | Should BeOfType 'System.IO.FileInfo'
    }

    It "Using -literalpath should find no additional files" {
        $null = New-Item -type file "$TESTDRIVE/file[abc].txt"
        $null = New-Item -type file "$TESTDRIVE/filea.txt"
        # if literalpath is not correct we would see filea.txt
        $item = Get-Item -literalpath "$TESTDRIVE/file[abc].txt"
        @($item).Count | Should Be 1
        $item.Name | Should Be 'file[abc].txt'
    }

    It "Should have mode flags set" {
        Get-ChildItem $PSScriptRoot | foreach-object { $_.Mode | Should Not BeNullOrEmpty }
    }

    It "Should not return the item unless force is used if hidden" {
        ${hiddenFile} = "${TESTDRIVE}/.hidden.txt"
        ${item} = New-Item -type file "${hiddenFile}"
        if ( ${IsWindows} ) {
            attrib +h "$hiddenFile"
        }
        ${result} = Get-Item "${hiddenFile}" -ErrorAction SilentlyContinue
        ${result} | Should BeNullOrEmpty
        ${result} = Get-Item -force "${hiddenFile}" -ErrorAction SilentlyContinue
        ${result}.FullName | Should Be ${item}.FullName
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
            ($result).Count | Should Be 1
            $result.Name | should be "file1.txt"
        }
        It "Should respect -Include" {
            $result = Get-Item "${testBaseDir}/*" -Include "file2.txt"
            ($result).Count | Should Be 1
            $result.Name | should be "file2.txt"
        }
        It "Should respect -Filter" {
            $result = Get-Item "${testBaseDir}/*" -Filter "*2*"
            ($result).Count | Should Be 1
            $result.Name | should be "file2.txt"
        }
        It "Should respect combinations of filter, include, and exclude" {
            $result = get-item "${testBaseDir}/*" -filter *.txt -include "file[12].txt" -exclude file2.txt
            ($result).Count | Should Be 1
            $result.Name | should be "file1.txt"
        }
    }

    Context "Error Condition Checking" {
        It "Should return an error if the provider does not exist" {
            { Get-Item BadProvider::/BadFile -ErrorAction Stop } | ShouldBeErrorId "ProviderNotFound,Microsoft.PowerShell.Commands.GetItemCommand"
        }

        It "Should return an error if the drive does not exist" {
            { Get-Item BadDrive:/BadFile -ErrorAction Stop } | ShouldBeErrorId "DriveNotFound,Microsoft.PowerShell.Commands.GetItemCommand"
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
            $result.Length | Should Be ($stringData.Length + [Environment]::NewLine.Length)
            $result.Stream | Should Be $streamName
        }
    }

    Context "Registry Provider" {
        It "Can retrieve an item from registry" -skip:$skipNotWindows {
            ${result} = Get-Item HKLM:/Software
            ${result} | Should BeOfType "Microsoft.Win32.RegistryKey"
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
