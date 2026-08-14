# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Remove-TypeData DRT Unit Tests" -Tags "CI" {
    BeforeAll {
        $XMLFile1 = Join-Path $TestDrive -ChildPath "testFile1.ps1xml"
        $XMLFile2 = Join-Path $TestDrive -ChildPath "testFile2.ps1xml"
        $content1 = @"
        <Types>
            <Type>
                <Name>System.Array</Name>
                    <Members>
                        <AliasProperty>
                            <Name>Yada</Name>
                            <ReferencedMemberName>Length</ReferencedMemberName>
                        </AliasProperty>
                    </Members>
            </Type>
        </Types>
"@
        $content2 = @"
                <Types>
                    <Type>
                        <Name>System.Array</Name>
                            <Members>
                                <AliasProperty>
                                    <Name>Yoda</Name>
                                    <ReferencedMemberName>Length</ReferencedMemberName>
                                </AliasProperty>
                            </Members>
                    </Type>
                </Types>
"@
        $content1 > $XMLFile1
        $content2 > $XMLFile2
    }

    BeforeEach {
        $ps = [powershell]::Create()
        $iss = [initialsessionstate]::CreateDefault2()
        $rs = [system.management.automation.runspaces.runspacefactory]::CreateRunspace($iss)
        $rs.Open()
        $ps.Runspace = $rs
    }

    AfterEach {
        $rs.Close()
        $ps.Dispose()
    }

    It "Remove With Pipe line Input Pass Type Shortcut String" {
        $null = $ps.AddScript("Update-TypeData -MemberType NoteProperty -MemberName TestNote -Value TestNote -TypeName int").Invoke()
        $ps.Commands.Clear()
        $ps.AddScript("(Get-TypeData System.Int32).TypeName").Invoke() | Should -Be System.Int32
        $ps.Commands.Clear()
        $null = $ps.AddScript("'int' | Remove-TypeData").Invoke()
        $ps.HadErrors | Should -BeFalse
    }

    It "Remove Type File In Initial Session State" {
        # setup
        $null = $ps.AddScript("Update-TypeData -AppendPath $XMLFile1").Invoke()
        $ps.Commands.Clear()
        $null = $ps.AddScript("Update-TypeData -AppendPath $XMLFile2").Invoke()
        $ps.Commands.Clear()
        $null = $ps.AddScript('$a = 1..3').Invoke()
        $ps.Commands.Clear()
        # test
        $ps.AddScript('$a.Yada').Invoke() | Should -Be 3
        $ps.Commands.Clear()
        $ps.AddScript('$a.Yoda').Invoke() | Should -Be 3
        $ps.Commands.Clear()
        $null = $ps.AddScript("Remove-TypeData -Path $XMLFile1").Invoke()
        $ps.Commands.Clear()
        $ps.AddScript('$a.Yada').Invoke() | Should -BeNullOrEmpty
        $ps.Commands.Clear()
        $ps.AddScript('$a.Yoda').Invoke() | Should -Be 3
        $ps.Commands.Clear()
    }

    It "Remove Type File In Initial Session State File Not In Cache" {
        $null = $ps.AddScript("Remove-TypeData -Path fakefile").Invoke()
        $ps.HadErrors | Should -BeTrue
        $ps.Streams.Error[0].FullyQualifiedErrorID | Should -BeExactly "TypePathException,Microsoft.PowerShell.Commands.RemoveTypeDataCommand"
    }
}
