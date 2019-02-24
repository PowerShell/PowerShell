# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "String cmdlets" -Tags "CI" {
    Context "Select-String" {
        BeforeAll {
            $date = Get-date -Date "2030-4-5 1:2:3.09"
            $matchInfoObj = [Microsoft.PowerShell.Commands.MatchInfo]::new()
            $matchInfoObj.Line = 'list1duck'

            $sep = [io.path]::DirectorySeparatorChar
            $fileName = New-Item 'TestDrive:\selectStr[ingLi]teralPath.txt'
            "abc" | Out-File -LiteralPath $fileName.fullname
	        "bcd" | Out-File -LiteralPath $fileName.fullname -Append
	        "cde" | Out-File -LiteralPath $fileName.fullname -Append

            $fileNameWithDots = $fileName.FullName.Replace("\", "\.\")

            $subDirName = Join-Path $TestDrive 'selectSubDir'
            New-Item -Path $subDirName -ItemType Directory -Force > $null
            $pathWithoutWildcard = $TestDrive
            $pathWithWildcard = Join-Path $TestDrive '*'

            # Here Get-ChildItem adds 'PSDrive' property
            $tempFile = New-TemporaryFile | Get-Item
            "abc" | Out-File -LiteralPath $tempFile.fullname
	        "bcd" | Out-File -LiteralPath $tempFile.fullname -Append
	        "cde" | Out-File -LiteralPath $tempFile.fullname -Append
            $driveLetter = $tempFile.PSDrive.Name
            $fileNameAsNetworkPath = "\\localhost\$driveLetter`$" + $tempFile.FullName.SubString(2)

	        Push-Location "$fileName\.."
        }

        AfterAll {
            Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
            Pop-Location
        }

        It "Select-String does not throw on subdirectory (path without wildcard)" {
            { select-string -Path  $pathWithoutWildcard "noExists" -ErrorAction Stop } | Should -Not -Throw
        }

        It "Select-String does not throw on subdirectory (path with wildcard)" {
            { select-string -Path  $pathWithWildcard "noExists" -ErrorAction Stop } | Should -Not -Throw
        }

        It "LiteralPath with relative path" {
            (select-string -LiteralPath (Get-Item -LiteralPath $fileName).Name "b").count | Should -Be 2
        }

        It "LiteralPath with absolute path" {
            (select-string -LiteralPath $fileName "b").count | Should -Be 2
        }

        It "LiteralPath with dots in path" {
            (select-string -LiteralPath $fileNameWithDots "b").count | Should -Be 2
        }

        It "Network path" -skip:(!$IsWindows) {
            (select-string -LiteralPath $fileNameAsNetworkPath "b").count | Should -Be 2
        }

        It "throws error for non filesystem providers" {
            $aaa = "aaaaaaaaaa"
            select-string -literalPath variable:\aaa "a" -ErrorAction SilentlyContinue -ErrorVariable selectStringError
            $selectStringError.FullyQualifiedErrorId | Should -Be 'ProcessingFile,Microsoft.PowerShell.Commands.SelectStringCommand'
        }

        It "throws parameter binding exception for invalid context" {
            { select-string It $PSScriptRoot -Context -1,-1 } | Should -Throw Context
        }

        It "match object supports RelativePath method" {
            $file = "Modules${sep}Microsoft.PowerShell.Utility${sep}Microsoft.PowerShell.Utility.psd1"

            $match = Select-String CmdletsToExport $pshome/$file

            $match.RelativePath($pshome) | Should -Be $file
            $match.RelativePath($pshome.ToLower()) | Should -Be $file
            $match.RelativePath($pshome.ToUpper()) | Should -Be $file
        }

        It "OnlyMatching '<testInput>' with no extra parameters" -TestCases @(
            #Tests for strings
            @{testInput = ''; regex = 'l'; output = $null}
            @{testInput = 'list1'; regex = 'l'; output = 'l'}
            @{testInput = 'list1duck'; regex = '\d'; output = '1'}
            @{testInput = 'list1duck    asdfl;kjasdf   '; regex = '\d'; output = '1'}
            @{testInput = 'list1231duck'; regex = '\d+'; output = '1231'}
            @{testInput = '      list1'; regex = '\w'; output = 'l'}
            @{testInput = '    1duck3'; regex = '\d', '\w'; output = '1'}
            @{testInput = 'list1', 'list2'; regex = '\d'; output = '1', '2'}
            @{testInput = 'list1', 'list2', 'duck', '503'; regex = '\d'; output = '1', '2', '5'}

            #Tests for MatchInfo objects
            @{testInput = $matchInfoObj; regex = '\d'; output = '1'}
            @{testInput = $matchInfoObj; regex = 's'; output = 's'}

            #Tests for objects converted to string
            @{testInput = $date.ToString(); regex = '\d'; output = '4'}
            @{testInput = $date; regex = '\d+'; output = '04'}

        ) {
            param($testInput, $output, $regex)

            $testInput | Select-String -Pattern $regex -OnlyMatching | Should -BeExactly $output
        }
    }
}
