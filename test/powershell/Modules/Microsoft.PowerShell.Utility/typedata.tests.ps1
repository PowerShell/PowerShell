# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "TestData cmdlets" -Tags "CI" {
    Context "Get-TypeData" {
        It "System.DateTime" {
            (Get-TypeData System.DateTime).TypeName | Should -Be System.DateTime
            # Supports pipelining?
            ("System.DateTime" | Get-TypeData).TypeName | Should -Be System.DateTime
        }

        It "Type accelerators" {
            (Get-TypeData DateTime).TypeName | Should -Be System.DateTime
            (Get-TypeData psCredential).TypeName | Should -Be System.Management.Automation.PSCredential
        }

        It "Accept multiple types" {
            $types = Get-TypeData System.DateTime, System.Management.Automation* | Sort-Object -Property TypeName
            $types[0].TypeName | Should -Be System.DateTime
            for($i = 1; $i -lt $types.Count; $i++)
            {
                $types[$i].TypeName.StartsWith("System.Management.Automation") | Should -BeTrue
            }
        }

        It "System.Object" {
            Get-TypeData System.Object | Should -Be $null
        }
    }

    # The rest of these tests do their work in another runspace to avoid messing up the current runspace
    Context "Update-TypeData" {
        BeforeAll {
            $script:ps = [PowerShell]::Create()
        }

        BeforeEach {
            $ps.Commands.Clear()
            $ps.Streams.Error.Clear()
        }

        AfterAll {
            $ps.Dispose()
            $script:ps = $null
        }

        It "TypeAdapter parameter" {
            $type = $ps.AddScript(@"
Update-TypeData -TypeName Void -TypeAdapter Microsoft.PowerShell.Cim.CimInstanceAdapter
Get-TypeData System.Void
"@).Invoke()
            $type[0].TypeName | Should -Be System.Void
            $type[0].TypeAdapter.FullName | Should -Be Microsoft.PowerShell.Cim.CimInstanceAdapter
        }
    }

    Context "Remove-TypeData" {
        BeforeAll {
            $script:ps = [PowerShell]::Create()
            Setup -F dummy1.types.ps1xml -Content "<Types><Type><Name>yyyDummy</Name><Members><ScriptProperty><Name>yyyDummy</Name><GetScriptBlock>'yyyDummy'</GetScriptBlock></ScriptProperty></Members></Type></Types>"
            Setup -F dummy2.types.ps1xml -Content "<Types><Type><Name>zzzDummy</Name><Members><ScriptProperty><Name>zzzDummy</Name><GetScriptBlock>'zzzDummy'</GetScriptBlock></ScriptProperty></Members></Type></Types>"
        }

        BeforeEach {
            $ps.Commands.Clear()
            $ps.Streams.Error.Clear()
        }

        AfterAll {
            $ps.Dispose()
            $script:ps = $null
        }

        It "Remove type that doesn't exist" {
            $typeName = "TypeThatDoesNotExistsAnywhere" + (Get-Random)
            $ps.AddScript("Remove-TypeData -TypeName $typeName").Invoke()
            $ps.Streams.Error[0].FullyQualifiedErrorId | Should -Be "TypesDynamicRemoveException,Microsoft.PowerShell.Commands.RemoveTypeDataCommand"
        }

        ##{ All of the following It blocks are intended to run in sequence, so don't reorder them.

        It "Add type file," {
            $ps.AddScript("Update-TypeData -AppendPath $TestDrive\dummy1.types.ps1xml").Invoke()
            $ps.Streams.Error.Count | Should -Be 0
        }

        It "Get type data from file just added," {
            $type = $ps.AddScript("Get-TypeData -TypeName yyyDummy").Invoke()
            $ps.Streams.Error.Count | Should -Be 0
            $type[0].TypeName | Should -Be yyyDummy
        }

        It "Remove type file just added," {
            $ps.AddScript("Remove-TypeData -Path $TestDrive\dummy1.types.ps1xml").Invoke()
            $ps.Streams.Error.Count | Should -Be 0
        }

        It "Make sure type was removed" {
            $type = $ps.AddScript("Get-TypeData -TypeName yyyDummy").Invoke()
            $type.Count | Should -Be 0
        }

        It "Add another type file," {
            $ps.AddScript("Update-TypeData -AppendPath $TestDrive\dummy2.types.ps1xml").Invoke()
            $ps.Streams.Error.Count | Should -Be 0
        }

        It "Now add some type data to one of those newly added types" {
            $ps.AddScript("Update-TypeData -TypeName zzzDummy -MemberType NoteProperty -MemberName DynamicDummyProperty -Value 10").Invoke()
            $ps.Streams.Error.Count | Should -Be 0
        }

        It "Remove the newly type file," {
            $ps.AddScript("Remove-TypeData -Path $TestDrive\dummy2.types.ps1xml").Invoke()
            $ps.Streams.Error.Count | Should -Be 0
        }

        It "Remove the newly type file a second time, should error," {
            $ps.AddScript("Remove-TypeData -Path $TestDrive\dummy2.types.ps1xml").Invoke()
            $ps.Streams.Error.Count | Should -Be 1
            $ps.Streams.Error[0].FullyQualifiedErrorId | Should -Be "TypeFileNotExistsInCurrentSession,Microsoft.PowerShell.Commands.RemoveTypeDataCommand"
        }

        It "Dynamic property added should still be there" {
            $res = $ps.AddScript("Get-TypeData -TypeName zzzDummy").Invoke()
            $res.Count | Should -Be 1
            $res[0].Members.Keys[0] | Should -Be DynamicDummyProperty
        }

        ##} All of the preceding It blocks are intended to run in sequence, so don't reorder them.
    }
}
