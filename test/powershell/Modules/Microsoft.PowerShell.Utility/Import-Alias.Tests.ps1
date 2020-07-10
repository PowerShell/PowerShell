# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Import-Alias DRT Unit Tests" -Tags "CI" {
    $testAliasDirectory = Join-Path -Path $TestDrive -ChildPath ImportAliasTestDirectory
    $aliasFilename      = "aliasFilename"
    $fulltestpath       = Join-Path -Path $testAliasDirectory -ChildPath $aliasFilename

    BeforeEach {
        New-Item -Path $testAliasDirectory -ItemType Directory -Force
        Remove-Item alias:abcd* -Force -ErrorAction SilentlyContinue
        Remove-Item alias:ijkl* -Force -ErrorAction SilentlyContinue
        Set-Alias abcd01 efgh01
        Set-Alias abcd02 efgh02
        Set-Alias abcd03 efgh03
        Set-Alias abcd04 efgh04
        Set-Alias ijkl01 mnop01
        Set-Alias ijkl02 mnop02
        Set-Alias ijkl03 mnop03
        Set-Alias ijkl04 mnop04
    }

    AfterEach {
        Remove-Item -Path $testAliasDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }

    It "Import-Alias Resolve To Multiple will throw PSInvalidOperationException" {
        { Import-Alias * -ErrorAction Stop } | Should -Throw -ErrorId "NotSupported,Microsoft.PowerShell.Commands.ImportAliasCommand"
    }

    It "Import-Alias From Exported Alias File Aliases Already Exist should throw SessionStateException" {
        { Export-Alias  $fulltestpath abcd* } | Should -Not -Throw
        { Import-Alias $fulltestpath -ErrorAction Stop } | Should -Throw -ErrorId "AliasAlreadyExists,Microsoft.PowerShell.Commands.ImportAliasCommand"
    }

    It "Import-Alias Into Invalid Scope should throw PSArgumentException"{
        { Export-Alias  $fulltestpath abcd* } | Should -Not -Throw
        { Import-Alias $fulltestpath -Scope bogus } | Should -Throw -ErrorId "Argument,Microsoft.PowerShell.Commands.ImportAliasCommand"
    }

    It "Import-Alias From Exported Alias File Aliases Already Exist using force should not throw"{
        {Export-Alias  $fulltestpath abcd*} | Should -Not -Throw
        {Import-Alias $fulltestpath  -Force} | Should -Not -Throw
    }
}

Describe "Import-Alias" -Tags "CI" {

    BeforeAll {
        $newLine = [Environment]::NewLine

        $testAliasDirectory = Join-Path -Path $TestDrive -ChildPath ImportAliasTestDirectory
        $aliasFilename = "pesteralias.txt"
        $aliasFilenameMoreThanFourValues = "aliasFileMoreThanFourValues.txt"
        $aliasFilenameLessThanFourValues = "aliasFileLessThanFourValues.txt"

        $aliasfile = Join-Path -Path $testAliasDirectory -ChildPath $aliasFilename
        $aliasPathMoreThanFourValues = Join-Path -Path $testAliasDirectory -ChildPath $aliasFileNameMoreThanFourValues
        $aliasPathLessThanFourValues = Join-Path -Path $testAliasDirectory -ChildPath $aliasFileNameLessThanFourValues

        $commandToAlias = "echo"
        $alias1 = "pesterecho"
        $alias2    = '"abc""def"'
        $alias3    = '"aaa"'
        $alias4    = '"a,b"'

        # create alias file
        New-Item -Path $testAliasDirectory -ItemType Directory -Force > $null

        # set header
        $aliasFileContent = '# Alias File' + $newLine
        $aliasFileContent += '# Exported by : alex' + $newLine
        $aliasFileContent += '# Date/Time : Thursday, 12 November 2015 21:55:08' + $newLine
        $aliasFileContent += '# Computer : archvm'

        # add various aliases
        $aliasFileContent += $newLine + $alias1 + ',"' + $commandToAlias + '","","None"'
        $aliasFileContent += $newLine + $alias2 + ',"' + $commandToAlias + '","","None"'
        $aliasFileContent += $newLine + $alias3 + ',"' + $commandToAlias + '","","None"'
        $aliasFileContent += $newLine + $alias4 + ',"' + $commandToAlias + '","","None"'
        $aliasFileContent > $aliasfile

        # create invalid file with more than four values
        New-Item -Path $testAliasDirectory -ItemType Directory -Force > $null
        $aliasFileContent = $newLine + '"v_1","v_2","v_3","v_4","v_5"'
        $aliasFileContent > $aliasPathMoreThanFourValues

        # create invalid file with less than four values
        New-Item -Path $testAliasDirectory -ItemType Directory -Force > $null
        $aliasFileContent = $newLine + '"v_1","v_2","v_3"'
        $aliasFileContent > $aliasPathLessThanFourValues
    }

    It "Should be able to import an alias file successfully" {
        { Import-Alias -Path $aliasfile } | Should -Not -Throw
    }

    It "Should classify an alias as non existent when it is not imported yet" {
        {Get-Alias -Name invalid_alias -ErrorAction Stop} | Should -Throw -ErrorId 'ItemNotFoundException,Microsoft.PowerShell.Commands.GetAliasCommand'
    }

    It "Should be able to parse <aliasToTest>" -TestCases @(
        @{ aliasToTest = 'abc"def' }
        @{ aliasToTest = 'aaa' }
        @{ aliasToTest = 'a,b' }
        ) {
        param($aliasToTest)
        Import-Alias -Path $aliasfile
        ( Get-Alias -Name $aliasToTest ).Definition | Should -BeExactly $commandToAlias
    }

    It "Should throw an error when reading more than four values" {
        { Import-Alias -Path $aliasPathMoreThanFourValues } | Should -Throw -ErrorId "ImportAliasFileFormatError,Microsoft.PowerShell.Commands.ImportAliasCommand"
    }

    It "Should throw an error when reading less than four values" {
        { Import-Alias -Path $aliasPathLessThanFourValues } | Should -Throw -ErrorId "ImportAliasFileFormatError,Microsoft.PowerShell.Commands.ImportAliasCommand"
    }
}
