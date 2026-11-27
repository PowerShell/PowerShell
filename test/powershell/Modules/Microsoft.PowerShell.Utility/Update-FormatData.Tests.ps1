# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Local ps1xml authorization" -Tags "CI" {
    BeforeAll {
        . "$PSScriptRoot\..\..\..\..\tools\buildCommon\startNativeExecution.ps1"
    }

    It "Honors <Policy> for <Kind> data in zone <Zone> (<Extension>)" -Skip:(-not $IsWindows) -TestCases @(
        @{ Policy = 'Unrestricted'; Kind = 'Format'; Zone = 0; Extension = '.ps1xml'; ExitCode = 0 }
        @{ Policy = 'Unrestricted'; Kind = 'Format'; Zone = 2; Extension = '.PS1XML'; ExitCode = 0 }
        @{ Policy = 'Unrestricted'; Kind = 'Format'; Zone = 3; Extension = '.ps1xml'; ExitCode = 1 }
        @{ Policy = 'Unrestricted'; Kind = 'Format'; Zone = 4; Extension = '.ps1xml'; ExitCode = 1 }
        @{ Policy = 'Unrestricted'; Kind = 'Type'; Zone = 0; Extension = '.ps1xml'; ExitCode = 0 }
        @{ Policy = 'Unrestricted'; Kind = 'Type'; Zone = 3; Extension = '.ps1xml'; ExitCode = 1 }
        @{ Policy = 'AllSigned'; Kind = 'Format'; Zone = 0; Extension = '.ps1xml'; ExitCode = 1 }
        @{ Policy = 'RemoteSigned'; Kind = 'Format'; Zone = 3; Extension = '.ps1xml'; ExitCode = 1 }
    ) {
        param($Policy, $Kind, $Zone, $Extension, $ExitCode)

        $path = Join-Path $TestDrive "authorization$Extension"
        $xml = if ($Kind -eq 'Format') {
            @'
<Configuration><ViewDefinitions><View>
  <Name>AuthorizationTest</Name>
  <ViewSelectedBy><TypeName>AuthorizationTest</TypeName></ViewSelectedBy>
  <ListControl><ListEntries><ListEntry><ListItems><ListItem>
    <PropertyName>Name</PropertyName>
  </ListItem></ListItems></ListEntry></ListEntries></ListControl>
</View></ViewDefinitions></Configuration>
'@
        } else {
            @'
<Types><Type><Name>AuthorizationTest</Name><Members><NoteProperty>
  <Name>Marker</Name><Value>Loaded</Value>
</NoteProperty></Members></Type></Types>
'@
        }
        Set-Content -LiteralPath $path -Value $xml
        Set-Content -LiteralPath $path -Stream Zone.Identifier -Value "[ZoneTransfer]`r`nZoneId=$Zone"
        $command = @"
try {
    Update-${Kind}Data -PrependPath '$($path.Replace("'", "''"))' -ErrorAction Stop
    'Authorized'
} catch {
    `$_.FullyQualifiedErrorId
    exit 1
}
"@
        $encodedCommand = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($command))
        $commandArguments = '-NoProfile', '-NonInteractive', '-ExecutionPolicy', $Policy, '-EncodedCommand', $encodedCommand
        $result = Start-NativeExecution -IgnoreExitcode {
            & "$PSHOME\pwsh.exe" @commandArguments
        }
        $LASTEXITCODE | Should -Be $ExitCode
        if ($ExitCode -eq 0) {
            $result | Should -BeExactly 'Authorized'
        } else {
            $result | Should -Match '(FormatXmlUpdateException|TypesXmlUpdateException)'
        }
    }
}

Describe "Update-FormatData" -Tags "CI" {

    BeforeEach {
        $ps = [PowerShell]::Create()
    }

    Context "Validate Update-FormatData update correctly" {

        It "Should not throw upon reloading previous formatting file" {
            { Update-FormatData } | Should -Not -Throw
        }

        It "Should validly load formatting data" {
            $path = Join-Path -Path $TestDrive -ChildPath "outputfile.ps1xml"
            Get-FormatData -TypeName System.Diagnostics.Process | Export-FormatData -Path $path
            $null = $ps.AddScript("Update-FormatData -prependPath $path")
            $ps.Invoke()
            $ps.HadErrors | Should -BeFalse
        }

        It "Update with atributes on Configuration node should be ignored" {
            $xmlContent = @"
    <Configuration xmlns:foo="bar">
        <ViewDefinitions>
            <View>
                <Name>Test</Name>
                <ViewSelectedBy>
                    <TypeName>Test</TypeName>
                </ViewSelectedBy>
                <ListControl>
                    <ListEntries>
                        <ListEntry>
                            <ListItems>
                                <ListItem>
                                    <PropertyName>Test</PropertyName>
                                </ListItem>
                            </ListItems>
                        </ListEntry>
                    </ListEntries>
                </ListControl>
            </View>
        </ViewDefinitions>
    </Configuration>
"@
            $path = "$testdrive\rootattribute.format.ps1xml"
            Set-Content -Path $path -Value $xmlContent
            $null = $ps.AddScript("Update-FormatData -prependPath $path")
            $ps.Invoke()
            $ps.HadErrors | Should -BeFalse
            $ps.Commands.Clear()
            $null = $ps.AddScript("Get-FormatData test")
            $formatData = $ps.Invoke()
            $formatData | Should -HaveCount 1
            $formatData.TypeNames | Should -BeExactly "Test"
            $formatData.FormatViewDefinition.Name | Should -BeExactly "Test"
        }
    }
}

Describe "Update-FormatData basic functionality" -Tags "CI" {
    BeforeAll {
        $testfilename = "testfile.ps1xml"
        $testfile = Join-Path -Path $TestDrive -ChildPath $testfilename

        $xmlContent=@"
                <Types>
                    <Type>
                        <Name>AnyName</Name>
                        <Members>
                            <PropertySet>
                                <Name>PropertySetName</Name>
                                <ReferencedProperties>
                                    <Name>FirstName</Name>
                                    <Name>LastName</Name>
                                </ReferencedProperties>
                            </PropertySet>
                        </Members>
                    </Type>
                </Types>
"@
        $xmlContent > $testfile
    }

    It "Update-FormatData with WhatIf should work"{

        { Update-FormatData -Append $testfile -WhatIf } | Should -Not -Throw
        { Update-FormatData -Prepend $testfile -WhatIf } | Should -Not -Throw
    }

    It "Update with invalid format xml should fail" {
        $xmlContent = @"
<Configuration>
    <ViewDefinitions>
        <View>
            <Name>T2</Name>
        </View>
    </ViewDefinitions>
</Configuration>
"@
        $xmlContent | Out-File -FilePath "$testdrive\invalid.format.ps1xml" -Encoding ascii
        { Update-FormatData -Path "$testdrive\invalid.format.ps1xml" -ErrorAction Stop } | Should -Throw -ErrorId "FormatXmlUpdateException,Microsoft.PowerShell.Commands.UpdateFormatDataCommand"
    }
}

Describe "Update-FormatData with resources in CustomControls" -Tags "CI" {

    BeforeAll {
        $templatePath = Join-Path $PSScriptRoot (Join-Path 'assets' 'UpdateFormatDataTests.format.ps1xml')
        $formatFilePath = Join-Path $TestDrive 'UpdateFormatDataTests.format.ps1xml'
        $ps = [powershell]::Create()
        $iss = [initialsessionstate]::CreateDefault2()
        $rs = [system.management.automation.runspaces.runspacefactory]::CreateRunspace($iss)
        $rs.Open()
        $ps.Runspace = $rs
    }
    AfterAll {
        $rs.Close()
        $ps.Dispose()
    }
    Context "Validate Update-FormatData" {
        It "Resources in WindowsPS syntax should be loaded successfully" {
            $format = Get-Content -Path $templatePath -Raw
            $format.Replace("%BaseName%","FileSystemProviderStrings") | Set-Content -Path $formatFilePath -Force
            $null = $ps.AddScript("Update-FormatData -PrependPath $formatFilePath")
            $ps.Streams.Error.Clear()
            $ps.Invoke()
            $ps.Streams.Error | Should -BeNullOrEmpty

        }
        It "Resources in CorePS syntax should be loaded successfully" {
            $format = Get-Content -Path $templatePath -Raw
            $format.Replace("%BaseName%","System.Management.Automation.resources.FileSystemProviderStrings") | Set-Content -Path $formatFilePath -Force
            $null = $ps.AddScript("Update-FormatData -PrependPath $formatFilePath")
            $ps.Streams.Error.Clear()
            $ps.Invoke()
            $ps.Streams.Error | Should -BeNullOrEmpty
        }
        It "Verify assembly path in error message when resource is Not found" {
            $format = Get-Content -Path $templatePath -Raw
            $format.Replace("%BaseName%","NonExistingResource") | Set-Content -Path $formatFilePath -Force
            $null = $ps.AddScript("Update-FormatData -PrependPath $formatFilePath")
            $ps.Streams.Error.Clear()
            $ps.Invoke()
            $sma = [appdomain]::CurrentDomain.GetAssemblies() | Where-Object { if ($_.Location) {$_.Location.EndsWith("System.Management.Automation.dll")}}
            $smaLocation = $sma.Location
            $ps.Streams.Error | ForEach-Object { $_.Exception.Message.Contains($smaLocation) | Should -BeTrue }
            $ps.Streams.Error | ForEach-Object { $_.FullyQualifiedErrorId | Should -Match 'FormatXmlUpdateException' }
        }
    }
}
