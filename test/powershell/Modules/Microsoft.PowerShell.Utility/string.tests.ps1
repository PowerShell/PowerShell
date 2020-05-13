# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "String cmdlets" -Tags "CI" {
    Context "Select-String" {
        BeforeAll {
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
            { Select-String -Path  $pathWithoutWildcard "noExists" -ErrorAction Stop } | Should -Not -Throw
        }

        It "Select-String does not throw on subdirectory (path with wildcard)" {
            { Select-String -Path  $pathWithWildcard "noExists" -ErrorAction Stop } | Should -Not -Throw
        }

        It "LiteralPath with relative path" {
            (Select-String -LiteralPath (Get-Item -LiteralPath $fileName).Name "b").count | Should -Be 2
        }

        It "LiteralPath with absolute path" {
            (Select-String -LiteralPath $fileName "b").count | Should -Be 2
        }

        It "LiteralPath with dots in path" {
            (Select-String -LiteralPath $fileNameWithDots "b").count | Should -Be 2
        }

        It "Network path" -Skip:(!$IsWindows) {
            (Select-String -LiteralPath $fileNameAsNetworkPath "b").count | Should -Be 2
        }

        It "throws error for non filesystem providers" {
            $aaa = "aaaaaaaaaa"
            Select-String -LiteralPath variable:\aaa "a" -ErrorAction SilentlyContinue -ErrorVariable selectStringError
            $selectStringError.FullyQualifiedErrorId | Should -Be 'ProcessingFile,Microsoft.PowerShell.Commands.SelectStringCommand'
        }

        It "throws parameter binding exception for invalid context" {
            { Select-String It $PSScriptRoot -Context -1,-1 } | Should -Throw Context
        }

        It "match object supports RelativePath method" {
            $file = "Modules${sep}Microsoft.PowerShell.Utility${sep}Microsoft.PowerShell.Utility.psd1"

            $match = Select-String CmdletsToExport $PSHOME/$file

            $match.RelativePath($PSHOME) | Should -Be $file
            $match.RelativePath($PSHOME.ToLower()) | Should -Be $file
            $match.RelativePath($PSHOME.ToUpper()) | Should -Be $file
        }
    }
}
