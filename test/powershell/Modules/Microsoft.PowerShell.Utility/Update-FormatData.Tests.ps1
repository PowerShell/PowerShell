# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
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

        It "Update with attributes on Configuration node should be ignored" {
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
        $iss = [system.management.automation.runspaces.initialsessionstate]::CreateDefault2()
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
