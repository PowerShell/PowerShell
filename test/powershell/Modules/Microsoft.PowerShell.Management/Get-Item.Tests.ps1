# Copyright (c) Microsoft Corporation.
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
        ,$items | Should -BeOfType System.Object[]
    }

    It "Should return the name of the current working directory when a dot is used" {
        $item = Get-Item $PSScriptRoot
        $item | Should -BeOfType System.IO.DirectoryInfo
        $item.Name | Should -BeExactly (Split-Path $PSScriptRoot -Leaf)
    }

    It "Should return the proper Name and BaseType for directory objects vs file system objects" {
        $rootitem = Get-Item $PSScriptRoot
        $rootitem | Should -BeOfType System.IO.DirectoryInfo
        $childitem = (Get-Item (Join-Path -Path $PSScriptRoot -ChildPath Get-Item.Tests.ps1))
        $childitem | Should -BeOfType System.IO.FileInfo
    }

    It "Using -literalpath should find no additional files" {
        $null = New-Item -type file "$TESTDRIVE/file[abc].txt"
        $null = New-Item -type file "$TESTDRIVE/filea.txt"
        # if literalpath is not correct we would see filea.txt
        $item = Get-Item -LiteralPath "$TESTDRIVE/file[abc].txt"
        @($item).Count | Should -Be 1
        $item.Name | Should -BeExactly 'file[abc].txt'
    }

    It "Should have mode flags set" {
        Get-ChildItem $PSScriptRoot | ForEach-Object { $_.Mode | Should -Not -BeNullOrEmpty }
    }

    It "Should not return the item unless force is used if hidden" {
        ${hiddenFile} = "${TESTDRIVE}/.hidden.txt"
        ${item} = New-Item -type file "${hiddenFile}"
        if ( ${IsWindows} ) {
            attrib +h "$hiddenFile"
        }
        ${result} = Get-Item "${hiddenFile}" -ErrorAction SilentlyContinue
        ${result} | Should -BeNullOrEmpty
        ${result} = Get-Item -Force "${hiddenFile}" -ErrorAction SilentlyContinue
        ${result}.FullName | Should -BeExactly ${item}.FullName
    }

    It "Should get properties for special reparse points" -Skip:$skipNotWindows {
        $result = Get-Item -Path $HOME/Cookies -Force
        $result.LinkType | Should -BeExactly "Junction"
        $result.Target | Should -Not -BeNullOrEmpty
        $result.Name | Should -BeExactly "Cookies"
        $result.Mode | Should -BeExactly "l--hs"
        $result.Exists | Should -BeTrue
    }

    It "Should return correct result for ToString() on root of drive" {
        $root = $IsWindows ? "${env:SystemDrive}\" : "/"
        (Get-Item -Path $root).ToString() | Should -BeExactly $root
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
            $result = Get-Item "${testBaseDir}/*" -Filter *.txt -Include "file[12].txt" -Exclude file2.txt
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
            $altStreamDirectory = "$TESTDRIVE/altstreamdir"
            $noAltStreamDirectory = "$TESTDRIVE/noaltstreamdir"
            $stringData = "test data"
            $streamName = "test"
            $absentStreamName = "noExist"
            $null = New-Item -type file $altStreamPath
            Set-Content -Path $altStreamPath -Stream $streamName -Value $stringData
            $null = New-Item -type directory $altStreamDirectory
            Set-Content -Path $altStreamDirectory -Stream $streamName -Value $stringData
            $null = New-Item -type directory $noAltStreamDirectory
        }
        It "Should find an alternate stream on a file if present" -Skip:$skipNotWindows {
            $result = Get-Item $altStreamPath -Stream $streamName
            $result.Length | Should -Be ($stringData.Length + [Environment]::NewLine.Length)
            $result.Stream | Should -Be $streamName
        }
        It "Should error if it cannot find alternate stream on an existing file" -Skip:$skipNotWindows {
            { Get-Item $altStreamPath -Stream $absentStreamName -ErrorAction Stop } | Should -Throw -ErrorId "AlternateDataStreamNotFound,Microsoft.PowerShell.Commands.GetItemCommand"
        }
        It "Should find an alternate stream on a directory if present, and it should not be a container" -Skip:$skipNotWindows {
            $result = Get-Item $altStreamDirectory -Stream $streamName
            $result.Length | Should -Be ($stringData.Length + [Environment]::NewLine.Length )
            $result.Stream | Should -Be $streamName
            $result.PSIsContainer | Should -BeExactly $false
        }
        It "Should not find an alternate stream on a directory if not present" -Skip:$skipNotWindows {
            { Get-Item $noAltStreamDirectory -Stream $absentStreamName -ErrorAction Stop } | Should -Throw -ErrorId "AlternateDataStreamNotFound,Microsoft.PowerShell.Commands.GetItemCommand"
        }
        It "Should find zero alt streams and not fail on a directory with a wildcard stream name if no alt streams are present" -Skip:$skipNotWindows {
            $result = Get-Item $noAltStreamDirectory -Stream * -ErrorAction Stop
            $result | Should -BeExactly $null
        }
    }

    Context "Registry Provider" {
        It "Can retrieve an item from registry" -Skip:$skipNotWindows {
            ${result} = Get-Item HKLM:/Software
            ${result} | Should -BeOfType Microsoft.Win32.RegistryKey
        }
    }

    Context "Environment provider" -Tag "CI" {
        BeforeAll {
            $env:testvar="b"
            $env:testVar="a"
        }

        AfterAll {
            Clear-Item -Path env:testvar -ErrorAction SilentlyContinue
            Clear-Item -Path env:testVar -ErrorAction SilentlyContinue
        }

        It "get-item testVar" {
            (Get-Item env:\testVar).Value | Should -BeExactly "a"
        }

        It "get-item is case-sensitive/insensitive as appropriate" {
            $expectedValue = "b"
            if($IsWindows)
            {
                $expectedValue = "a"
            }

            (Get-Item env:\testvar).Value | Should -BeExactly $expectedValue
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
    It "Reports the effective value among accidental case-variant duplicates on Windows" -Skip:$skipNotWindows {
        if (-not (Get-Command -ErrorAction Ignore node.exe)) {
            Write-Warning "Test skipped, because prerequisite Node.js is not installed."
        } else {
            $fso = New-Object -ComObject Scripting.FileSystemObject
            $shortPath = $fso.GetFile("$PSHOME\pwsh.exe").ShortPath.Replace('\', '/')
            $script = @"
                env = {}
                env.testVar = process.env.testVar // include the original case variant with its original value.
                env.TESTVAR = 'b' // redefine with a case variant name and different value
                // Note: Which value will win is not deterministic(!); what matters, however, is that both
                //       `$env:testvar and Get-Item env:testvar report the same value.
                //       The nondeterministic behavior makes it hard to prove that the values are *always* the
                //       same, however.
                require('child_process').execSync('$shortPath -noprofile -command `$env:testvar, (Get-Item env:testvar).Value', { env: env }).toString()
"@
            $valDirect, $valGetItem, $unused = node.exe -pe $script
            $LASTEXITCODE | Should -Be 0
            $valDirect | Should -Not -BeNullOrEmpty
            $valGetItem | Should -BeExactly $valDirect
        }
    }
}

Describe 'Formatting for FileInfo objects' -Tags 'CI' {
    BeforeAll {
        $extensionTests = [System.Collections.Generic.List[HashTable]]::new()
        foreach ($extension in @('.zip', '.tgz', '.tar', '.gz', '.nupkg', '.cab', '.7z', '.ps1', '.psd1', '.psm1', '.ps1xml')) {
            $extensionTests.Add(@{extension = $extension})
        }
    }

    It 'File type <extension> should have correct color' -TestCases $extensionTests {
        param($extension)

        $testFile = Join-Path -Path $TestDrive -ChildPath "test$extension"
        $file = New-Item -ItemType File -Path $testFile
        $file.NameString | Should -BeExactly "$($PSStyle.FileInfo.Extension[$extension] + $file.Name + $PSStyle.Reset)"
    }

    It 'Directory should have correct color' {
        $dirPath = Join-Path -Path $TestDrive -ChildPath 'myDir'
        $dir = New-Item -ItemType Directory -Path $dirPath
        $dir.NameString | Should -BeExactly "$($PSStyle.FileInfo.Directory + $dir.Name + $PSStyle.Reset)"
    }

    It 'Executable should have correct color' {
        if ($IsWindows) {
            $exePath = Join-Path -Path $TestDrive -ChildPath 'myExe.exe'
            $exe = New-Item -ItemType File -Path $exePath
        }
        else {
            $exePath = Join-Path -Path $TestDrive -ChildPath 'myExe'
            $null = New-Item -ItemType File -Path $exePath
            chmod +x $exePath
            $exe = Get-Item -Path $exePath
        }

        $exe.NameString | Should -BeExactly "$($PSStyle.FileInfo.Executable + $exe.Name + $PSStyle.Reset)"
    }
}

Describe 'Formatting for FileInfo requiring admin' -Tags 'CI','RequireAdminOnWindows' {
    It 'Symlink should have correct color' {
        $linkPath = Join-Path -Path $TestDrive -ChildPath 'link'
        $link = New-Item -ItemType SymbolicLink -Name 'link' -Value $TestDrive -Path $TestDrive
        $link.NameString | Should -BeExactly "$($PSStyle.FileInfo.SymbolicLink + $link.Name + $PSStyle.Reset) -> $TestDrive"
    }
}
