# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Tests Get-Command with relative paths and wildcards" -Tag "CI" {

    BeforeAll {
        # Create temporary EXE command files
        $file1 = Setup -f WildCardCommandA.exe -pass
        $file2 = Setup -f WildCardCommand[B].exe -pass
        #$null = New-Item -ItemType File -Path (Join-Path $TestDrive WildCardCommandA.exe) -ErrorAction Ignore
        #$null = New-Item -ItemType File -Path (Join-Path $TestDRive WildCardCommand[B].exe) -ErrorAction Ignore
        if ( $IsLinux -or $IsMacOS -or $IsFreeBSD) {
            /bin/chmod a+rw "$file1"
            /bin/chmod a+rw "$file2"
        }
        $commandInfo = Get-Command Get-Date -ShowCommandInfo
    }

    # this test doesn't test anything on non-windows platforms
    It "Test wildcard with drive relative directory path" -Skip:(!$IsWindows) {
        $pathName = Join-Path $TestDrive "WildCardCommandA*"
        $driveOffset = $pathName.IndexOf(":")
        $driveName = $pathName.Substring(0,$driveOffset + 1)
        Push-Location -Path $driveName
        try {
            $pathName = $pathName.Substring($driveOffset + 1)
            $result = Get-Command -Name $pathName
            $result | Should -Not -BeNullOrEmpty
            $result.Name | Should -Be WildCardCommandA.exe
        }
        catch {
            Pop-Location
        }
    }

    It "Test wildcard with relative directory path" {
        Push-Location $TestDrive
        $result = Get-Command -Name .\WildCardCommandA*
        Pop-Location
        $result | Should -Not -BeNullOrEmpty
        $result | Should -Be WildCardCommandA.exe
    }

    It "Test with PowerShell wildcard and relative path" {
        Push-Location $TestDrive

        # This should use the wildcard to find WildCardCommandA.exe
        $result = Get-Command -Name .\WildCardCommand[A].exe
        $result | Should -Not -BeNullOrEmpty
        $result | Should -Be WildCardCommandA.exe

        # This should find the file WildCardCommand[B].exe
        $result = Get-Command -Name .\WildCardCommand[B].exe
        $result | Should -Not -BeNullOrEmpty
        $result | Should -Be WildCardCommand[B].exe

        Pop-Location
    }

    It "Get-Command -ShowCommandInfo property field test" {
        $properties = ($commandInfo | Get-Member -MemberType NoteProperty)
        $propertiesAsString =  $properties.name | Out-String
        $propertiesAsString | Should -MatchExactly 'CommandType'
        $propertiesAsString | Should -MatchExactly 'Definition'
        $propertiesAsString | Should -MatchExactly 'Module'
        $propertiesAsString | Should -MatchExactly 'ModuleName'
        $propertiesAsString | Should -MatchExactly 'Name'
        $propertiesAsString | Should -MatchExactly 'ParameterSets'
    }

    $testcases = @(
                  @{observed = $commandInfo.Name; testname = "Name"; result = "Get-Date"}
                  @{observed = $commandInfo.ModuleName; testname = "Name"; result = "Microsoft.PowerShell.Utility"}
                  @{observed = $commandInfo.Module.Name; testname = "ModuleName"; result = "Microsoft.PowerShell.Utility"}
                  @{observed = $commandInfo.CommandType; testname = "CommandType"; result = "Cmdlet"}
                  @{observed = $commandInfo.Definition.Count; testname = "Definition"; result = 1}
               )

    It "Get-Command -ShowCommandInfo property test - <testname>" -TestCases $testcases{
            param (
            $observed,
            $result
        )
        $observed | Should -BeExactly $result
    }

    It "Get-Command -ShowCommandInfo ParameterSets property field test" {
        $properties = ($commandInfo.ParameterSets[0] | Get-Member -MemberType NoteProperty)
        $propertiesAsString =  $properties.name | Out-String
        $propertiesAsString | Should -MatchExactly 'IsDefault'
        $propertiesAsString | Should -MatchExactly 'Name'
        $propertiesAsString | Should -MatchExactly 'Parameters'
    }

    It "Get-Command -ShowCommandInfo Parameters property field test" {
        $properties = ($commandInfo.ParameterSets[0].Parameters | Get-Member -MemberType NoteProperty)
        $propertiesAsString =  $properties.name | Out-String
        $propertiesAsString | Should -MatchExactly 'HasParameterSet'
        $propertiesAsString | Should -MatchExactly 'IsMandatory'
        $propertiesAsString | Should -MatchExactly 'Name'
        $propertiesAsString | Should -MatchExactly 'ParameterType'
        $propertiesAsString | Should -MatchExactly 'Position'
        $propertiesAsString | Should -MatchExactly 'ValidParamSetValues'
        $propertiesAsString | Should -MatchExactly 'ValueFromPipeline'
    }

}
