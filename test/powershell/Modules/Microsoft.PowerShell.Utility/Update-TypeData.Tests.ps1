# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Update-TypeData basic functionality" -Tags "CI" {
    BeforeAll {
        $testfilename = "testfile.ps1xml"
        $testfile = Join-Path -Path $TestDrive -ChildPath $testfilename
        $invalidFileExtensionFile = Join-Path -Path $TestDrive -ChildPath "notmshxml"

        $xmlContent=@"
				<Types>
                    <Type>
                        <Name>AnyName</Name>
                        <MembersTypo>
                            <PropertySet>
                                <Name>PropertySetName</Name>
                                <ReferencedProperties>
                                    <Name>FirstName</Name>
                                    <Name>LastName</Name>
                                </ReferencedProperties>
                            </PropertySet>
                        </MembersTypo>
                        <MembersTypo2>
                            <PropertySet>
                                <Name>PropertySetName</Name>
                                <ReferencedProperties>
                                    <Name>FirstName</Name>
                                    <Name>LastName</Name>
                                </ReferencedProperties>
                            </PropertySet>
                        </MembersTypo2>
                    </Type>
				</Types>
"@
        $xmlContent>$testfile
    }

    BeforeEach {
        $ps = [powershell]::Create()
        $iss = [system.management.automation.runspaces.initialsessionstate]::CreateDefault2()
        $rs = [system.management.automation.runspaces.runspacefactory]::CreateRunspace($iss)
        $rs.Open()
        $ps.Runspace = $rs
    }

    AfterEach {
        $rs.Close()
        $ps.Dispose()
    }

    It "Update-TypeData with attributes on root node should succeed"   {
        $xmlContent = @"
        <Types xmlns:foo="bar">
            <Type>
                <Name>Test</Name>
                    <Members>
                        <AliasProperty>
                            <Name>Yada</Name>
                            <ReferencedMemberName>Length</ReferencedMemberName>
                        </AliasProperty>
                    </Members>
            </Type>
        </Types>
"@
        $path = "$testdrive\test.types.ps1xml"
        Set-Content -Value $xmlContent -Path $path
        $null = $ps.AddScript("Update-TypeData -AppendPath $path")
        $ps.Invoke()
        $ps.HadErrors | Should -BeFalse
        $ps.Commands.Clear()
        $null = $ps.AddScript("Get-TypeData test")
        $typeData = $ps.Invoke()
        $typeData | Should -HaveCount 1
        $typeData.TypeName | Should -BeExactly "Test"
    }

    It "Update-TypeData with Invalid TypesXml should throw Exception" {
        $null = $ps.AddScript("Update-TypeData -PrependPath $testfile")
        $ps.Invoke()
        $ps.HadErrors | Should -BeTrue
        $ps.Streams.Error[0].FullyQualifiedErrorId | Should -BeExactly "TypesXmlUpdateException,Microsoft.PowerShell.Commands.UpdateTypeDataCommand"
	}

	It "Update-TypeData with Invalid File Extension should throw Exception"{
		$xmlContent="not really an xml file, but we will not use it"
		$xmlContent>$invalidFileExtensionFile
        	$null = $ps.AddScript("Update-TypeData -PrependPath $invalidFileExtensionFile")
        	$ps.Invoke()
        	$ps.HadErrors | Should -BeTrue
        	$ps.Streams.Error[0].FullyQualifiedErrorId | Should -BeExactly "WrongExtension,Microsoft.PowerShell.Commands.UpdateTypeDataCommand"
	}

	It "Update-TypeData with Valid Dynamic Type NoteProperty with Force should work"{
        $null = $ps.AddScript("Update-TypeData -MemberType NoteProperty -MemberName TestNote -Value 2 -TypeName System.String")
        $ps.Invoke()
        $ps.Commands.Clear()
        $null = $ps.AddScript("'string'.TestNote")
        $ps.Invoke() | Should -Be 2
        $ps.Commands.Clear()
        $null = $ps.AddScript("Update-TypeData -MemberType NoteProperty -MemberName TestNote -Value 'test' -TypeName System.String -Force")
        $ps.Invoke()
        $ps.commands.clear()
        $null = $ps.AddScript("'string'.TestNote")
        $ps.Invoke() | Should -BeExactly "test"
        $ps.commands.clear()
        $null = $ps.AddScript('Update-TypeData -MemberType NoteProperty -MemberName TestNote -Value $null -TypeName System.String -Force')
        $ps.Invoke()
        $ps.Commands.Clear()
        $null = $ps.AddScript("'string'.TestNote")
        $ps.Invoke() | Should -BeNullOrEmpty
	}

    It "Update-TypeData with Valid Dynamic Type AliasProperty with Force should work"{
        # setup
        $null = $ps.AddScript("Update-TypeData -MemberType AliasProperty -MemberName TestAlias -Value Length -TypeName System.String")
        $ps.Invoke()
        $ps.Commands.Clear()
        $null = $ps.AddScript("'string'.TestAlias.GetType().Name")
        $ps.Invoke() | Should -BeExactly "Int32"

        # test
        $ps.Commands.Clear()
        $null = $ps.AddScript("'string'.TestAlias")
        $ps.Invoke() | Should -Be 6

        # setup
        $ps.Commands.Clear()
        $null = $ps.AddScript("Update-TypeData -MemberType AliasProperty -MemberName TestAlias -Value 'Length' -SecondValue 'string' -TypeName System.String -Force")
        $ps.Invoke()

        # test
        $ps.Commands.Clear()
        $null = $ps.AddScript("'string'.TestAlias.GetType().Name")
        $ps.Invoke() | Should -BeExactly "String"

        # test
        $ps.Commands.Clear()
        $null = $ps.AddScript("'string'.TestAlias")
        $ps.Invoke() | Should -Be 6
    }

	It "Update-TypeData with Valid Dynamic Type ScriptMethod with Force should work"{
        $script1="script method"
        $null = $ps.AddScript("Update-TypeData -MemberType ScriptMethod -MemberName TestScriptMethod -Value {'$script1'} -TypeName System.String")
        $ps.Invoke()

        $ps.Commands.Clear()
        $null = $ps.AddScript('"string".TestScriptMethod()')
        $ps.Invoke() | Should -BeExactly "script method"
        $ps.Commands.Clear()
        $script2="new script method"
        $null = $ps.AddScript("Update-TypeData -MemberType ScriptMethod -MemberName TestScriptMethod -Value {'$script2'} -TypeName System.String -Force")
        $ps.Invoke()

        $ps.Commands.Clear()
        $null = $ps.AddScript('"string".TestScriptMethod()')
        $ps.Invoke() | Should -BeExactly "new script method"
	}

	It "Update-TypeData with Valid Dynamic Type Accept TypeData Object should work"{
		$ps.Commands.AddScript('Add-Type -TypeDefinition "public class TypeDataTest{}"')
        $ps.Commands.AddStatement()
		$ps.Commands.AddScript('Update-TypeData -TypeName TypeDataTest -MemberType NoteProperty -MemberName TestNote1 -Value "Hello"')
        $ps.Commands.AddStatement()
		$ps.Commands.AddScript('Update-TypeData -MemberType NoteProperty -MemberName TestNote -Value 2 -TypeName TypeDataTest')
        $ps.Commands.AddStatement()
		$ps.Commands.AddScript('$result=new-object TypeDataTest')
        $ps.Invoke()

        # tests
		$ps.Commands.Clear()
        $ps.AddScript('$result.TestNote1').Invoke() | Should -BeExactly "Hello"
		$ps.Commands.Clear()
		$ps.AddScript('$result.TestNote').Invoke() | Should -Be 2
	}

	It "Update-TypeData with Invalid DynamicType Null Value For AliasProperty should throw Exception"{
        $null = $ps.AddScript('Update-TypeData -MemberType AliasProperty -MemberName TestAlias -Value $null -TypeName System.String')
        $ps.Invoke()
        $ps.HadErrors | Should -BeTrue
        $ps.Streams.Error[0].FullyQualifiedErrorId  | Should -BeExactly "ValueShouldBeSpecified,Microsoft.PowerShell.Commands.UpdateTypeDataCommand"
	}

	It "Update-TypeData with Invalid DynamicType with No MemberName should throw Exception"{
	    $null = $ps.AddScript('Update-TypeData -MemberType NoteProperty -Value "Error" -TypeName System.String')
        $ps.Invoke()
        $ps.HadErrors | Should -BeTrue
        $ps.Streams.Error[0].FullyQualifiedErrorId  | Should -BeExactly "MemberNameShouldBeSpecified,Microsoft.PowerShell.Commands.UpdateTypeDataCommand"
	}

	It "Update-TypeData with Invalid DynamicType with No Value should throw Exception"{
		$null = $ps.AddScript('Update-TypeData -MemberType NoteProperty -MemberName TestNote -TypeName System.String')
        $ps.Invoke()
        $ps.HadErrors | Should -BeTrue
        $ps.Streams.Error[0].FullyQualifiedErrorId  | Should -BeExactly "ValueShouldBeSpecified,Microsoft.PowerShell.Commands.UpdateTypeDataCommand"
	}

	It "Update-TypeData with Invalid DynamicType with Empty TypeData should throw Exception"{
		$null = $ps.AddScript("Update-TypeData -TypeName System.String")
        $ps.Invoke()
        $ps.HadErrors | Should -BeTrue
        $ps.Streams.Error[0].FullyQualifiedErrorId  | Should -BeExactly "TypeDataEmpty,Microsoft.PowerShell.Commands.UpdateTypeDataCommand"
	}

	It "Update-TypeData with Valid Standard Members Serialization Method String should work"{
        $null = $ps.AddScript('Update-TypeData -SerializationMethod string -StringSerializationSource Length -TargetTypeForDeserialization string -TypeName System.String')
        $ps.Invoke()
        $ps.Commands.Clear(); $null = $ps.AddScript('"string".PSStandardMembers.SerializationMethod')
        $ps.Invoke() | Should -BeExactly "String"
        $ps.Commands.Clear(); $null = $ps.AddScript('"string".PSStandardMembers.StringSerializationSource')
        $ps.Invoke() | Should -Be 6
        $ps.Commands.Clear(); $null = $ps.AddScript('"string".PSStandardMembers.TargetTypeForDeserialization')
        $ps.Invoke() | Should -BeExactly "string"
	}

	It "Update-TypeData with Valid Standard Members Serialization Method SpecificProperties should work"{
        $null = $ps.AddScript('Update-TypeData -SerializationMethod SpecificProperties -StringSerializationSource Length -PropertySerializationSet Length -InheritPropertySerializationSet $true -SerializationDepth 2 -TypeName System.String')
        $ps.Invoke()
        $ps.Commands.Clear();$null=$ps.AddScript('"string".PSStandardMembers.SerializationMethod')
        $ps.Invoke() | Should -BeExactly "SpecificProperties"
        $ps.Commands.Clear();$null=$ps.AddScript('"string".PSStandardMembers.StringSerializationSource')
        $ps.Invoke() | Should -Be 6
        $ps.Commands.Clear();$null=$ps.AddScript('"string".PSStandardMembers.SerializationDepth')
        $ps.Invoke() | Should -Be 2
        $ps.Commands.Clear();$null=$ps.AddScript('"string".PSStandardMembers.InheritPropertySerializationSet')
        $ps.Invoke() | Should -BeExactly "True"
    }

	It "Update-TypeData with Valid Standard Members Serialization Method AllPublicProperties should work"{
        $null = $ps.AddScript('Update-TypeData -SerializationMethod AllPublicProperties -StringSerializationSource Length -SerializationDepth 2 -TypeName System.String')
        $ps.Invoke()
        $ps.Commands.Clear();$ps.AddScript('"string".PSStandardMembers.SerializationMethod')
        $ps.Invoke() | Should -BeExactly "AllPublicProperties"
        $ps.Commands.Clear();$ps.AddScript('"string".PSStandardMembers.StringSerializationSource')
        $ps.Invoke() | Should -Be 6
        $ps.Commands.Clear();$ps.AddScript('"string".PSStandardMembers.SerializationDepth')
        $ps.Invoke() | Should -Be 2
    }

    It "Update-TypeData with Valid ISS UpdateType Command Test With DynamicType Set should work"{
        $ps.AddScript('Update-TypeData -MemberType NoteProperty -MemberName "TestNote" -Value "test the note" -SerializationMethod SpecificProperties -SerializationDepth 5 -PropertySerializationSet Length -DefaultDisplayPropertySet Length -TypeName System.String')
        $ps.Invoke()
        $ps.Commands.Clear();$null = $ps.AddScript('"string".TestNote')
        $ps.Invoke() | Should -BeExactly "test the note"
        $ps.Commands.Clear();$null = $ps.AddScript('"string".PSStandardMembers.SerializationMethod')
        $ps.Invoke() | Should -BeExactly "SpecificProperties"
        $ps.Commands.Clear();$null = $ps.AddScript('"string".PSStandardMembers.SerializationDepth')
        $ps.Invoke() | Should -Be 5
        $ps.Commands.Clear();$null = $ps.AddScript('"string".PSStandardMembers.PropertySerializationSet.ReferencedPropertyNames[0]')
        $ps.Invoke() | Should -BeExactly "Length"
        $ps.Commands.Clear();$null = $ps.AddScript('"string".PSStandardMembers.DefaultDisplayPropertySet.ReferencedPropertyNames[0]')
        $ps.Invoke() | Should -BeExactly "Length"

        $ps.Commands.Clear();$null = $ps.AddScript('Update-TypeData -MemberType NoteProperty -MemberName TestNote -Value "test the note again" -TargetTypeForDeserialization string -Force -TypeName System.String')
        $ps.Invoke()
        $ps.Commands.Clear();$null = $ps.AddScript('"string".TestNote')
        $ps.Invoke() | Should -BeExactly "test the note again"
        $ps.Commands.Clear();$null = $ps.AddScript('"string".PSStandardMembers.SerializationMethod')
        $ps.Invoke() | Should -BeExactly "SpecificProperties"
        $ps.Commands.Clear();$null = $ps.AddScript('"string".PSStandardMembers.SerializationDepth')
        $ps.Invoke() | Should -Be 5
        $ps.Commands.Clear();$null = $ps.AddScript('"string".PSStandardMembers.PropertySerializationSet.ReferencedPropertyNames[0]')
        $ps.Invoke() | Should -BeExactly "Length"
        $ps.Commands.Clear();$null = $ps.AddScript('"string".PSStandardMembers.DefaultDisplayPropertySet.ReferencedPropertyNames[0]')
        $ps.Invoke() | Should -BeExactly "Length"
        $ps.Commands.Clear();$null = $ps.AddScript('"string".PSStandardMembers.TargetTypeForDeserialization')
        $ps.Invoke() | Should -BeExactly "string"
    }

    It "Update-TypeData with Valid ISS UpdateType Command Test With StrongType Set should work"{
        $ps.AddScript('Update-TypeData -TypeName System.Array -MemberType NoteProperty -MemberName TestNote -Value "TestNote"')
        $ps.Commands.AddStatement()
        $ps.AddScript('Update-TypeData -TypeName System.Array -MemberType AliasProperty -MemberName TestAlias -Value "Length"')
        $script1="script method"
        $ps.Commands.AddStatement()
        $ps.AddScript("Update-TypeData -TypeName System.Array -MemberType ScriptMethod -MemberName TestScriptMethod -Value {'$script1'}")
        $script2='$this.Length'
        $ps.Commands.AddStatement()
        $ps.AddScript("Update-TypeData -TypeName System.Array -MemberType ScriptProperty -MemberName TestScriptProperty -Value {$script2}")
        $ps.Commands.AddStatement()
        $ps.AddScript('Update-TypeData -TypeName System.Array -SerializationMethod AllPublicProperties -SerializationDepth 2 -StringSerializationSource Length -TargetTypeForDeserialization string')
        $ps.Invoke()
        $ps.Commands.Clear();$ps.AddScript('(1, 3).TestNote').Invoke() | Should -BeExactly "TestNote"
        $ps.Commands.Clear();$ps.AddScript('(1, 3).TestAlias').Invoke() | Should -Be 2
        $ps.Commands.Clear();$ps.AddScript('(1, 3).TestScriptMethod()').Invoke() | Should -BeExactly "script method"
        $ps.Commands.Clear();$ps.AddScript('(1, 3).TestScriptProperty').Invoke() | Should -Be 2
        $ps.Commands.Clear();$ps.AddScript('(1, 3).PSStandardMembers.SerializationMethod').Invoke() | Should -BeExactly "AllPublicProperties"
        $ps.Commands.Clear();$ps.AddScript('(1, 3).PSStandardMembers.SerializationDepth').Invoke() | Should -Be 2
        $ps.Commands.Clear();$ps.AddScript('(1, 3).PSStandardMembers.StringSerializationSource').Invoke() | Should -Be 2
        $ps.Commands.Clear();$ps.AddScript('(1, 3).PSStandardMembers.TargetTypeForDeserialization').Invoke() | Should -BeExactly "string"
    }

# this looks identical to the test directly above
	It "Update-TypeData with ISS Type Table API Test Add And Remove TypeData should work" -Pending {
		try{
			Update-TypeData -TypeName System.Object[] -MemberType NoteProperty -MemberName TestNote -Value "TestNote"
			Update-TypeData -TypeName System.Object[] -MemberType AliasProperty -MemberName TestAlias -Value "Length"
			$script1={"script method"}
			Update-TypeData -TypeName System.Object[] -MemberType ScriptMethod -MemberName TestScriptMethod -Value $script1
			$script2={$this.Length}
			Update-TypeData -TypeName System.Object[] -MemberType ScriptProperty -MemberName TestScriptProperty -Value $script2
			Update-TypeData -TypeName System.Object[] -SerializationMethod AllPublicProperties -SerializationDepth 2 -StringSerializationSource Length -TargetTypeForDeserialization string
			(1, 3).TestNote | Should -BeExactly "TestNote"
			(1, 3).TestAlias | Should -Be 2
			(1, 3).TestScriptMethod() | Should -BeExactly "script method"
			(1, 3).TestScriptProperty | Should -Be 2
			(1, 3).PSStandardMembers.SerializationMethod | Should -BeExactly "AllPublicProperties"
			(1, 3).PSStandardMembers.SerializationDepth | Should -Be 2
			(1, 3).PSStandardMembers.StringSerializationSource | Should -Be 2
			(1, 3).PSStandardMembers.TargetTypeForDeserialization | Should -BeExactly "string"
		}
		finally
		{
			Remove-TypeData System.Object[]
		}
	}

    Context "Duplicate XML files" {
        BeforeAll {
            $xmlContent=@"
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
            $xmlContent>$testfile
        }
        It "Update-TypeData with Duplicate XML Files should work"{
            $null = $ps.AddScript("Update-TypeData -AppendPath $testfile")
            $ps.Invoke()
            $ps.Commands.Clear()
            $ps.AddScript('$a=1..3').Invoke()
            $ps.Commands.Clear()
            $ps.AddScript('$a.Yada').Invoke() | Should -Be 3
            $ps.AddScript("Remove-TypeData -Path $testfile").Invoke()
            $ps.HadErrors | Should -BeFalse
        }
    }
}
