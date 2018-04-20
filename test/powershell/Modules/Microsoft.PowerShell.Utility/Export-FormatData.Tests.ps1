# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Export-FormatData" -Tags "CI" {
    BeforeAll {
        $fd = Get-FormatData
        $testOutput = Join-Path -Path $TestDrive -ChildPath "outputfile"
    }

    AfterEach {
        Remove-Item $testOutput -Force -ErrorAction SilentlyContinue
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

    It "Should well format output xml" {
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
"@
        $testfilename = [guid]::NewGuid().ToString('N')
        $testfile = Join-Path -Path $TestDrive -ChildPath "$testfilename.ps1xml"
        Set-Content -Path $testfile -Value $xmlContent
        Update-FormatData -Append $testfile
        Get-FormatData -TypeName "ExportFormatDataTypeName" | Export-FormatData -Path $testOutput
        $content = Get-Content $testOutput -Raw

        $content | Should -BeExactly $expected
    }
}
