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

Describe "Module format data appends" -Tags "CI" {
    BeforeAll {
        if ($IsWindows) {
            $script:originalPolicy = Get-ExecutionPolicy -Scope Process
            Set-ExecutionPolicy Unrestricted -Scope Process -Force
        }
        $script:formatTemplate = @'
<Configuration><ViewDefinitions><View>
  <Name>MARKER</Name>
  <ViewSelectedBy><TypeName>FormatAppend.Test</TypeName></ViewSelectedBy>
  <ListControl><ListEntries><ListEntry><ListItems><ListItem>
    <ScriptBlock>'MARKER'</ScriptBlock>
  </ListItem></ListItems></ListEntry></ListEntries></ListControl>
</View></ViewDefinitions></Configuration>
'@
        Add-Type @'
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Host;
public class FormatAppendAuthorizationManager : AuthorizationManager
{
    public readonly List<string> Paths = new List<string>();
    public string DeniedFile;
    public FormatAppendAuthorizationManager() : base("Microsoft.PowerShell") { }
    protected override bool ShouldRun(CommandInfo command, CommandOrigin origin, PSHost host, out Exception reason)
    {
        reason = null;
        if (command is ExternalScriptInfo script)
        {
            Paths.Add(script.Path);
            if (script.Path == DeniedFile)
            {
                reason = new UnauthorizedAccessException("Test authorization denial");
                return false;
            }
        }
        return true;
    }
}
'@
    }

    AfterAll {
        if ($IsWindows) {
            Set-ExecutionPolicy $originalPolicy -Scope Process -Force
        }
    }

    BeforeEach {
        $firstModule = Join-Path $TestDrive 'First.psd1'
        $secondModule = Join-Path $TestDrive 'Second.psd1'
        $firstFormat = Join-Path $TestDrive 'First.ps1xml'
        $secondFormat = Join-Path $TestDrive 'Second.ps1xml'
        Set-Content $firstModule "@{ ModuleVersion = '1.0'; FormatsToProcess = 'First.ps1xml' }"
        Set-Content $secondModule "@{ ModuleVersion = '1.0'; FormatsToProcess = 'Second.ps1xml' }"
        Set-Content $firstFormat $formatTemplate.Replace('MARKER', 'first')
        Set-Content $secondFormat $formatTemplate.Replace('MARKER', 'second')
        $script:changedFormat = $formatTemplate.Replace('MARKER', 'changed')
        $script:ps = [PowerShell]::Create()
        $script:runspace = $null
    }

    AfterEach {
        $ps.Dispose()
        if ($runspace) { $runspace.Dispose() }
    }

    It "Retains the loaded snapshot until <Operation>" -TestCases @(
        @{ Operation = 'Refresh' }
        @{ Operation = 'Force' }
        @{ Operation = 'Remove' }
    ) {
        param($Operation)
        $result = $ps.AddScript({
            param($FirstModule, $SecondModule, $FirstFormat, $ChangedFormat, $Operation)
            Import-Module $FirstModule -ErrorAction Stop
            Set-Content $FirstFormat $ChangedFormat
            Import-Module $SecondModule -ErrorAction Stop
            $value = [pscustomobject]@{ PSTypeName = 'FormatAppend.Test'; Value = 1 }
            $before = $value | Out-String
            $views = (Get-FormatData FormatAppend.Test).FormatViewDefinition.Name -join ','
            switch ($Operation) {
                Refresh { Update-FormatData -ErrorAction Stop }
                Force { Import-Module $SecondModule -Force -ErrorAction Stop }
                Remove { Remove-Module Second -ErrorAction Stop }
            }
            [pscustomobject]@{ Before = $before; After = ($value | Out-String); Views = $views }
        }).AddArgument($firstModule).AddArgument($secondModule).AddArgument($firstFormat).
            AddArgument($changedFormat).AddArgument($Operation).Invoke()
        $ps.HadErrors | Should -BeFalse
        $result.Before | Should -Match 'first'
        $result.Before | Should -Not -Match 'changed'
        $result.Views | Should -BeExactly 'first,second'
        $result.After | Should -Match 'changed'
    }

    It "Applies newly specified defaults without losing prior views" {
        $withDefaults = $formatTemplate.Replace('MARKER', 'second').Replace(
            '<Configuration>', '<Configuration><DefaultSettings><PropertyCountForTable>1</PropertyCountForTable></DefaultSettings>')
        Set-Content $secondFormat $withDefaults
        $result = $ps.AddScript({
            param($FirstModule, $SecondModule)
            Import-Module $FirstModule, $SecondModule -ErrorAction Stop
            [pscustomobject]@{
                Views = (Get-FormatData FormatAppend.Test).FormatViewDefinition.Name -join ','
                Rendered = ([pscustomobject]@{ Alpha = 1; Beta = 2 } | Out-String)
            }
        }).AddArgument($firstModule).AddArgument($secondModule).Invoke()
        $ps.HadErrors | Should -BeFalse
        $result.Views | Should -BeExactly 'first,second'
        $result.Rendered | Should -Match 'Alpha\s+:'
    }

    It "Resolves earlier selection sets and global controls after append" {
        [xml]$definitions = Get-Content (Join-Path $PSScriptRoot 'assets\UpdateFormatDataTests.format.ps1xml') -Raw
        $textNode = $definitions.SelectSingleNode('//Text')
        $textNode.RemoveAll()
        $textNode.InnerText = 'shared-control'
        $definitions.Save($firstFormat)
        $view = '<Configuration><ViewDefinitions><View><Name>shared-view</Name>' +
            '<ViewSelectedBy><SelectionSetName>TestTypes</SelectionSetName></ViewSelectedBy>' +
            '<GroupBy><PropertyName>Group</PropertyName><CustomControlName>TestTypes-GroupingFormat</CustomControlName></GroupBy>' +
            '<ListControl><ListEntries><ListEntry><ListItems><ListItem><PropertyName>Value</PropertyName>' +
            '</ListItem></ListItems></ListEntry></ListEntries></ListControl></View></ViewDefinitions></Configuration>'
        Set-Content $secondFormat $view
        $result = $ps.AddScript({
            param($FirstModule, $SecondModule)
            Import-Module $FirstModule, $SecondModule -ErrorAction Stop
            [pscustomobject]@{ PSTypeName = 'NonExistingTestType'; Group = 1; Value = 42 } | Out-String
        }).AddArgument($firstModule).AddArgument($secondModule).Invoke()
        $ps.HadErrors | Should -BeFalse
        $result | Should -Match 'shared-control'
        $result | Should -Match '42'
    }

    It "Does not publish a failed addition and retries it after repair" {
        $validSecond = Get-Content $secondFormat -Raw
        Set-Content $secondFormat '<Configuration><ViewDefinitions><View><Name>broken</Name></View></ViewDefinitions></Configuration>'
        $result = $ps.AddScript({
            param($FirstModule, $SecondModule, $SecondFormat, $ValidSecond)
            Import-Module $FirstModule -ErrorAction Stop
            $failure = $null
            try { Import-Module $SecondModule -ErrorAction Stop } catch { $failure = $_.FullyQualifiedErrorId }
            $before = (Get-FormatData FormatAppend.Test).FormatViewDefinition.Name -join ','
            Set-Content $SecondFormat $ValidSecond
            Import-Module $SecondModule -ErrorAction Stop
            [pscustomobject]@{
                Failure = $failure; Before = $before
                After = (Get-FormatData FormatAppend.Test).FormatViewDefinition.Name -join ','
            }
        }).AddArgument($firstModule).AddArgument($secondModule).AddArgument($secondFormat).AddArgument($validSecond).Invoke()
        $result.Failure | Should -Match 'FormatXmlUpdateException'
        $result.Before | Should -BeExactly 'first'
        $result.After | Should -BeExactly 'first,second'
    }

    It "Preserves custom authorization callbacks (deny=<Deny>)" -TestCases @(
        @{ Deny = $false }
        @{ Deny = $true }
    ) {
        param($Deny)
        $manager = [FormatAppendAuthorizationManager]::new()
        $iss = [initialsessionstate]::CreateDefault2()
        $iss.AuthorizationManager = $manager
        $script:runspace = [runspacefactory]::CreateRunspace($iss)
        $runspace.Open()
        $ps.Runspace = $runspace
        $result = $ps.AddScript({
            param($FirstModule, $SecondModule, $FirstFormat, $Manager, $Deny)
            Import-Module $FirstModule -ErrorAction Stop
            if ($Deny) { $Manager.DeniedFile = $FirstFormat }
            $failure = $null
            try { Import-Module $SecondModule -ErrorAction Stop } catch { $failure = $_.FullyQualifiedErrorId }
            [pscustomobject]@{
                Failure = $failure
                FirstFileChecks = @($Manager.Paths | Where-Object { $_ -eq $FirstFormat }).Count
            }
        }).AddArgument($firstModule).AddArgument($secondModule).AddArgument($firstFormat).
            AddArgument($manager).AddArgument($Deny).Invoke()
        $result.FirstFileChecks | Should -BeGreaterThan 1
        if ($Deny) {
            $result.Failure | Should -Match 'FormatXmlUpdateException'
        } else {
            $result.Failure | Should -BeNullOrEmpty
        }
    }

    It "Falls back to a full reload under RemoteSigned" -Skip:(-not $IsWindows) {
        try {
            Set-ExecutionPolicy RemoteSigned -Scope Process -Force
            $result = $ps.AddScript({
                param($FirstModule, $SecondModule, $FirstFormat, $ChangedFormat)
                Import-Module $FirstModule -ErrorAction Stop
                Set-Content $FirstFormat $ChangedFormat
                Import-Module $SecondModule -ErrorAction Stop
                (Get-FormatData FormatAppend.Test).FormatViewDefinition.Name -join ','
            }).AddArgument($firstModule).AddArgument($secondModule).AddArgument($firstFormat).AddArgument($changedFormat).Invoke()
            $ps.HadErrors | Should -BeFalse
            $result | Should -BeExactly 'changed,second'
        } finally {
            Set-ExecutionPolicy Unrestricted -Scope Process -Force
        }
    }
}
