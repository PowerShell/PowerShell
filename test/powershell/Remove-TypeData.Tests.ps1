Describe "Remove-TypeData DRT Unit Tests" -Tags DRT{
    It "Remove With Pipe line Input Pass Type Shortcut String" {
        { Update-TypeData -MemberType NoteProperty -MemberName TestNote -Value TestNote -TypeName int } | Should Not Throw
        $a = Get-TypeData System.Int32
        $a.TypeName | Should Be System.Int32

        { 'int' | Remove-TypeData } | Should Not Throw
    }

    It "Remove Type File In Initial Session State" {
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
        Update-TypeData -AppendPath $XMLFile1
        Update-TypeData -AppendPath $XMLFile2
        $a = 1..3
        $a.Yada | Should be 3
        $a.Yoda | Should Be 3
        Remove-TypeData -Path $XMLFile1
        $a = 1..3
        $a.Yada | Should BeNullOrEmpty 
        $a.Yoda | Should Be 3
        
        #clean up
        Remove-TypeData -Path $XMLFile2
        Remove-Item $XMLFile1 -ErrorAction SilentlyContinue
        Remove-Item $XMLFile2 -ErrorAction SilentlyContinue 
    }

    It "Remove Type File In Initial Session State File Not In Cache" {
        try
        {
            Remove-TypeData -Path "fakefile" -ErrorAction Stop
            Throw "TypePathException"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be "TypePathException,Microsoft.PowerShell.Commands.RemoveTypeDataCommand"
        }
    }
}