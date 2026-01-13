# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Export-FormatData" -Tags "CI" {
    BeforeAll {
        $clientVersion = '5.0' # Preliminarily preserve the original test semantics in place before https://github.com/PowerShell/PowerShell/pull/11270
        $fd = Get-FormatData -PowerShellVersion $clientVersion
        $testOutput = Join-Path -Path $TestDrive -ChildPath "outputfile"
    }

    AfterEach {
        Remove-Item $testOutput -Force -ErrorAction SilentlyContinue
    }

    It "Can export all types" {
        try
        {
            $fd | Export-FormatData -Path $TESTDRIVE\allformat.ps1xml -IncludeScriptBlock

            $sessionState = [initialsessionstate]::CreateDefault()
            $sessionState.Formats.Clear()
            $sessionState.Types.Clear()

            $runspace = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace($sessionState)
            $runspace.Open()

            $runspace.CreatePipeline("Update-FormatData -AppendPath $TESTDRIVE\allformat.ps1xml").Invoke()
            $actualAllFormat = $runspace.CreatePipeline("Get-FormatData -PowerShellVersion $clientVersion").Invoke()

            $fd.Count | Should -Be $actualAllFormat.Count
            Compare-Object $fd $actualAllFormat | Should -Be $null
        }
        finally
        {
            $runspace.Close()
            Remove-Item -Path $TESTDRIVE\allformat.ps1xml -Force -ErrorAction SilentlyContinue
        }
    }

    It "Works with literal path" {
        $filename = 'TestDrive:\[formats.ps1xml'
        $fd | Export-FormatData -LiteralPath $filename
        (Test-Path -LiteralPath $filename) | Should -BeTrue
    }

    It "Should overwrite the destination file" {
        $filename = 'TestDrive:\ExportFormatDataWithForce.ps1xml'
        $unexpected = "SHOULD BE OVERWRITTEN"
        $unexpected | Out-File -FilePath $filename -Force
        $file = Get-Item  $filename
        $file.IsReadOnly = $true
        $fd | Export-FormatData -Path $filename -Force

        $actual = @(Get-Content $filename)[0]
        $actual | Should -Not -Be $unexpected
    }

    It "should not overwrite the destination file with NoClobber" {
        $filename = "TestDrive:\ExportFormatDataWithNoClobber.ps1xml"
        $fd | Export-FormatData -LiteralPath $filename

        { $fd | Export-FormatData -LiteralPath $filename -NoClobber } | Should -Throw -ErrorId 'NoClobber,Microsoft.PowerShell.Commands.ExportFormatDataCommand'
    }

    It "Test basic functionality" {
        Export-FormatData -InputObject $fd[0] -Path $testOutput
        $content = Get-Content $testOutput -Raw
        $formatViewDefinition = $fd[0].FormatViewDefinition
        $typeName = $fd[0].TypeName
        $content.Contains($typeName) | Should -BeTrue
        for ($i = 0; $i -lt $formatViewDefinition.Count;$i++)
        {
            $content.Contains($formatViewDefinition[$i].Name) | Should -BeTrue
        }
    }

    It "Should have a valid xml tag at the start of the file" {
        $fd | Export-FormatData -Path $testOutput
        $piped = Get-Content $testOutput -Raw
        $piped[0] | Should -BeExactly "<"
    }

    It "Should pretty print xml output" {
        $xmlContent=@"
            <Configuration>
            <ViewDefinitions>
            <View>
            <Name>ExportFormatDataName</Name>
            <ViewSelectedBy>
                <TypeName>ExportFormatDataTypeName</TypeName>
            </ViewSelectedBy>
            <TableControl>
                <TableHeaders />
                <TableRowEntries>
                <TableRowEntry>
                <TableColumnItems>
                <TableColumnItem>
                    <PropertyName>Guid</PropertyName>
                </TableColumnItem>
                </TableColumnItems>
                </TableRowEntry>
                </TableRowEntries>
            </TableControl>
            </View>
            </ViewDefinitions>
            </Configuration>
"@
        $expected = @"
<?xml version="1.0" encoding="utf-8"?>
<Configuration>
  <ViewDefinitions>
    <View>
      <Name>ExportFormatDataName</Name>
      <ViewSelectedBy>
        <TypeName>ExportFormatDataTypeName</TypeName>
      </ViewSelectedBy>
      <TableControl>
        <TableHeaders />
        <TableRowEntries>
          <TableRowEntry>
            <TableColumnItems>
              <TableColumnItem>
                <PropertyName>Guid</PropertyName>
              </TableColumnItem>
            </TableColumnItems>
          </TableRowEntry>
        </TableRowEntries>
      </TableControl>
    </View>
  </ViewDefinitions>
</Configuration>
"@ -replace "`r`n?|`n", ""
        try
        {
            $testfilename = [guid]::NewGuid().ToString('N')
            $testfile = Join-Path -Path $TestDrive -ChildPath "$testfilename.ps1xml"
            Set-Content -Path $testfile -Value $xmlContent

            $sessionState = [initialsessionstate]::CreateDefault()
            $sessionState.Formats.Clear()
            $sessionState.Types.Clear()

            $runspace = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace($sessionState)
            $runspace.Open()

            $runspace.CreatePipeline("Update-FormatData -prependPath $testfile").Invoke()
            $runspace.CreatePipeline("Get-FormatData -TypeName 'ExportFormatDataTypeName' | Export-FormatData -Path $testOutput").Invoke()

            $content = (Get-Content $testOutput -Raw) -replace "`r`n?|`n", ""

            $content | Should -BeExactly $expected
        }
        finally
        {
            $runspace.Close()
        }
    }

    It 'Should be able to export multiple views' {
        $listControl = [System.Management.Automation.ListControl]::Create().StartEntry().AddItemProperty('test').AddItemProperty('test2').EndEntry().EndList()
        $tableControl = [System.Management.Automation.TableControl]::Create().StartRowDefinition().AddPropertyColumn('test').AddPropertyColumn('test2').EndRowDefinition().EndTable()

        $listView = [System.Management.Automation.FormatViewDefinition]::new('Default', $listControl)
        $tableView = [System.Management.Automation.FormatViewDefinition]::new('Default', $tableControl)

        $list = New-Object System.Collections.Generic.List[System.Management.Automation.FormatViewDefinition]
        $list.Add($listView)
        $list.Add($tableView)

        $typeDef = [System.Management.Automation.ExtendedTypeDefinition]::new('TestTypeName', $list)
        $filePath = Join-Path $TestDrive "test.format.ps1xml"
        $typeDef | Export-FormatData -Path $filePath
        [xml]$xml = Get-Content -Path $filePath
        @($xml.Configuration.ViewDefinitions.View).Count | Should -Be 2
    }
}
