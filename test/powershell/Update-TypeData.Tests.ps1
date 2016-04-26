Describe "Update-TypeData" {

    Context "Validate Update-Type updates correctly" {

	It "Should not throw upon reloading previous formatting file" {
	    { Update-TypeData } | Should Not throw
	}

	It "Should validly load formatting data" {
	    { Get-TypeData -typename System.Diagnostics.Process | Export-TypeData -Path "outputfile.ps1xml" }
	    { Update-TypeData -prependPath "outputfile.ps1xml" | Should Not throw }
	    { Remove-Item "outputfile.ps1xml" -ErrorAction SilentlyContinue }
	}
    }
}

Describe "Update-TypeData basic functionality" -Tags DRT{
	$tmpDirectory = $TestDrive
    $testfilename = "testfile.ps1xml"
    $testfile = Join-Path -Path $tmpDirectory -ChildPath $testfilename
	$invalidFileExtensionFile = Join-Path -Path $tmpDirectory -ChildPath "notmshxml"
	$filelist = Join-Path -Path $tmpDirectory -ChildPath "fileList.ps1xml"
	
	#Pester bug:https://github.com/PowerShell/psl-pester/issues/6
	It "Update-TypeData with Invalid TypesXml should throw Exception" -Pending{
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
		try
		{
			Update-TypeData -PrependPath $testfile -EA Stop
			Throw "Execution OK"
		}
		catch 
		{
			$error[0] | Should Match "Error: The node MembersTypo2 is not allowed."
			$_.CategoryInfo | Should Match "RuntimeException"
			$_.FullyQualifiedErrorId | Should be "TypesXmlUpdateException,Microsoft.PowerShell.Commands.UpdateTypeDataCommand"
		}
		finally
		{
			Remove-TypeData -Path $testfile
			rm $testfile
		}
	}
	
	It "Update-TypeData with Invalid File Extension should throw Exception"{
		$xmlContent="not really an xml file, but we will not use it"
		$xmlContent>$invalidFileExtensionFile
		try
		{
			Update-TypeData -PrependPath $invalidFileExtensionFile -EA Stop
			Throw "Execution OK"
		}
		catch 
		{
			$error[0] | Should Match "because it does not have the file name extension"
			$_.CategoryInfo | Should Match "PSInvalidOperationException"
			$_.FullyQualifiedErrorId | Should be "WrongExtension,Microsoft.PowerShell.Commands.UpdateTypeDataCommand"
		}
		finally
		{
			rm $invalidFileExtensionFile
		}
	}
	
	It "Update-TypeData with Invalid File List Extension should throw Exception"{
		$xmlContent="not really an xml file, but we will not use it"
		$xmlContent>$invalidFileExtensionFile
		$filelistContent = "<Files><File>" + $invalidFileExtensionFile + "</File></Files>"
		$filelistContent>$filelist
		try
		{
			Update-TypeData -AppendPath $filelist -EA Stop
			Throw "Execution OK"
		}
		catch 
		{
			$error[0] | Should Match "ps1xml"
			$_.CategoryInfo | Should Match "RuntimeException"
			$_.FullyQualifiedErrorId | Should be "TypesXmlUpdateException,Microsoft.PowerShell.Commands.UpdateTypeDataCommand"
		}
		finally
		{
			Remove-TypeData -Path $filelist
			rm $invalidFileExtensionFile
			rm $filelist
		}
	}
	
	It "Update-TypeData with Valid Dynamic Type NoteProperty with Force should work"{
		Update-TypeData -MemberType NoteProperty -MemberName TestNote -Value 2 -TypeName System.String
		"string".TestNote | Should Be 2
		Update-TypeData -MemberType NoteProperty -MemberName TestNote -Value "test" -TypeName System.String -Force
		"string".TestNote | Should Be "test"
		Update-TypeData -MemberType NoteProperty -MemberName TestNote -Value $null -TypeName System.String -Force
		"string".TestNote | Should BeNullOrEmpty
	}
	
	It "Update-TypeData with Valid Dynamic Type AliasProperty with Force should work"{
		Update-TypeData -MemberType AliasProperty -MemberName TestAlias -Value "Length" -TypeName System.String
		"string".TestAlias.GetType().Name | Should Be "Int32"
		"string".TestAlias | Should Be 6
		Update-TypeData -MemberType AliasProperty -MemberName TestAlias -Value "Length" -SecondValue "string" -TypeName System.String -Force
		"string".TestAlias.GetType().Name | Should Be "String"
		"string".TestAlias | Should Be 6
	}
	
	It "Update-TypeData with Valid Dynamic Type ScriptMethod with Force should work"{
		$script1={"script method"}
		Update-TypeData -MemberType ScriptMethod -MemberName TestScriptMethod -Value $script1 -TypeName System.String
		"string".TestScriptMethod() | Should Be "script method"
		$script2={"new script method"}
		Update-TypeData -MemberType ScriptMethod -MemberName TestScriptMethod -Value $script2 -TypeName System.String -Force
		"string".TestScriptMethod() | Should Be "new script method"
	}
	
	It "Update-TypeData with Valid Dynamic Type Accept TypeData Object should work"{
		Add-Type -TypeDefinition "public class TypeDataTest{}"
		Update-TypeData -TypeName TypeDataTest -MemberType NoteProperty -MemberName TestNote1 -Value "Hello"
		Update-TypeData -MemberType NoteProperty -MemberName TestNote -Value 2 -TypeName TypeDataTest
		$result=new-object TypeDataTest
		$result.TestNote1 | Should Be "Hello"
		$result.TestNote | Should Be 2
	}
	
	It "Update-TypeData with Invalid DynamicType Null Value For AliasProperty should throw Exception"{
		try
		{
			Update-TypeData -MemberType AliasProperty -MemberName TestAlias -Value $null -TypeName System.String -EA Stop
			Throw "Execution OK"
		}
		catch 
		{
			$error[0] | Should Match 'The Value parameter should not be null or an empty string for a member of type "AliasProperty". Specify a non-null value for the Value parameter when updating this member type.'
			$_.CategoryInfo | Should Match "InvalidOperationException"
			$_.FullyQualifiedErrorId | Should be "ValueShouldBeSpecified,Microsoft.PowerShell.Commands.UpdateTypeDataCommand"
		}
	}
	
	It "Update-TypeData with Invalid DynamicType with No MemberName should throw Exception"{
		try
		{
			Update-TypeData -MemberType NoteProperty -Value "Error" -TypeName System.String -EA Stop
			Throw "Execution OK"
		}
		catch 
		{
			$error[0] | Should Match 'The MemberName parameter is required for the type "NoteProperty". Please specify the MemberName parameter.'
			$_.CategoryInfo | Should Match "InvalidOperationException"
			$_.FullyQualifiedErrorId | Should be "MemberNameShouldBeSpecified,Microsoft.PowerShell.Commands.UpdateTypeDataCommand"
		}
	}
	
	It "Update-TypeData with Invalid DynamicType with No Value should throw Exception"{
		try
		{
			Update-TypeData -MemberType NoteProperty -MemberName TestNote -TypeName System.String -EA Stop
			Throw "Execution OK"
		}
		catch 
		{
			$error[0] | Should Match 'The Value parameter is required for the type "NoteProperty". Please specify the Value parameter.'
			$_.CategoryInfo | Should Match "InvalidOperationException"
			$_.FullyQualifiedErrorId | Should be "ValueShouldBeSpecified,Microsoft.PowerShell.Commands.UpdateTypeDataCommand"
		}
	}
	
	It "Update-TypeData with Invalid DynamicType with Empty TypeData should throw Exception"{
		try
		{
			Update-TypeData -TypeName System.String -EA Stop
			Throw "Execution OK"
		}
		catch 
		{
			$error[0] | Should Match 'No member is specified for the update on type "System.String".'
			$_.CategoryInfo | Should Match "InvalidOperationException"
			$_.FullyQualifiedErrorId | Should be "TypeDataEmpty,Microsoft.PowerShell.Commands.UpdateTypeDataCommand"
		}
	}
	
	It "Update-TypeData with Valid Standard Members Serialization Method String should work"{
		try{
			Update-TypeData -SerializationMethod string -StringSerializationSource Length -TargetTypeForDeserialization string -TypeName System.String
			"string".PSStandardMembers.SerializationMethod | Should Be "String"
			"string".PSStandardMembers.StringSerializationSource | Should Be 6
			"string".PSStandardMembers.TargetTypeForDeserialization | Should Be "string"
		}
		finally
		{
			Remove-TypeData System.String
		}
	}
	
	It "Update-TypeData with Valid Standard Members Serialization Method SpecificProperties should work"{
		try{
			Update-TypeData -SerializationMethod SpecificProperties -StringSerializationSource Length -PropertySerializationSet Length -InheritPropertySerializationSet $true -SerializationDepth 2 -TypeName System.String
			"string".PSStandardMembers.SerializationMethod | Should Be "SpecificProperties"
			"string".PSStandardMembers.StringSerializationSource | Should Be 6
			"string".PSStandardMembers.SerializationDepth | Should Be 2
			"string".PSStandardMembers.InheritPropertySerializationSet | Should Be "True"
		}
		finally
		{
			Remove-TypeData System.String
		}
	}
	
	It "Update-TypeData with Valid Standard Members Serialization Method AllPublicProperties should work"{
		try{
			Update-TypeData -SerializationMethod AllPublicProperties -StringSerializationSource Length -SerializationDepth 2 -TypeName System.String
			"string".PSStandardMembers.SerializationMethod | Should Be "AllPublicProperties"
			"string".PSStandardMembers.StringSerializationSource | Should Be 6
			"string".PSStandardMembers.SerializationDepth | Should Be 2
		}
		finally
		{
			Remove-TypeData System.String
		}
	}
	
	It "Update-TypeData with Valid ISS UpdateType Command Test With DynamicType Set should work"{
		try{
			Update-TypeData -MemberType NoteProperty -MemberName "TestNote" -Value "test the note" -SerializationMethod SpecificProperties -SerializationDepth 5 -PropertySerializationSet Length -DefaultDisplayPropertySet Length -TypeName System.String
			"string".TestNote | Should Be "test the note"
			"string".PSStandardMembers.SerializationMethod | Should Be "SpecificProperties"
			"string".PSStandardMembers.SerializationDepth | Should Be 5
			"string".PSStandardMembers.PropertySerializationSet.ReferencedPropertyNames[0] | Should Be "Length"
			"string".PSStandardMembers.DefaultDisplayPropertySet.ReferencedPropertyNames[0] | Should Be "Length"
			
			Update-TypeData -MemberType NoteProperty -MemberName TestNote -Value "test the note again" -TargetTypeForDeserialization string -Force -TypeName System.String
			"string".TestNote | Should Be "test the note again"
			"string".PSStandardMembers.SerializationMethod | Should Be "SpecificProperties"
			"string".PSStandardMembers.SerializationDepth | Should Be 5
			"string".PSStandardMembers.PropertySerializationSet.ReferencedPropertyNames[0] | Should Be "Length"
			"string".PSStandardMembers.DefaultDisplayPropertySet.ReferencedPropertyNames[0] | Should Be "Length"
			"string".PSStandardMembers.TargetTypeForDeserialization | Should Be "string"
		}
		finally
		{
			Remove-TypeData System.String
		}
	}
	
	It "Update-TypeData with Valid ISS UpdateType Command Test With StrongType Set should work"{
		try{
			Update-TypeData -TypeName System.Array -MemberType NoteProperty -MemberName TestNote -Value "TestNote"
			Update-TypeData -TypeName System.Array -MemberType AliasProperty -MemberName TestAlias -Value "Length"
			$script1={"script method"}
			Update-TypeData -TypeName System.Array -MemberType ScriptMethod -MemberName TestScriptMethod -Value $script1
			$script2={$this.Length}
			Update-TypeData -TypeName System.Array -MemberType ScriptProperty -MemberName TestScriptProperty -Value $script2
			Update-TypeData -TypeName System.Array -SerializationMethod AllPublicProperties -SerializationDepth 2 -StringSerializationSource Length -TargetTypeForDeserialization string
			(1, 3).TestNote | Should Be "TestNote"
			(1, 3).TestAlias | Should Be 2
			(1, 3).TestScriptMethod() | Should Be "script method"
			(1, 3).TestScriptProperty | Should Be 2
			(1, 3).PSStandardMembers.SerializationMethod | Should Be "AllPublicProperties"
			(1, 3).PSStandardMembers.SerializationDepth | Should Be 2
			(1, 3).PSStandardMembers.StringSerializationSource | Should Be 2
			(1, 3).PSStandardMembers.TargetTypeForDeserialization | Should Be "string"
		}
		finally
		{
			Remove-TypeData System.Array
		}
	}
	
	It "Update-TypeData with ISS Type Table API Test Add And Remove TypeData should work"{
		try{
			Update-TypeData -TypeName System.Object[] -MemberType NoteProperty -MemberName TestNote -Value "TestNote"
			Update-TypeData -TypeName System.Object[] -MemberType AliasProperty -MemberName TestAlias -Value "Length"
			$script1={"script method"}
			Update-TypeData -TypeName System.Object[] -MemberType ScriptMethod -MemberName TestScriptMethod -Value $script1
			$script2={$this.Length}
			Update-TypeData -TypeName System.Object[] -MemberType ScriptProperty -MemberName TestScriptProperty -Value $script2
			Update-TypeData -TypeName System.Object[] -SerializationMethod AllPublicProperties -SerializationDepth 2 -StringSerializationSource Length -TargetTypeForDeserialization string
			(1, 3).TestNote | Should Be "TestNote"
			(1, 3).TestAlias | Should Be 2
			(1, 3).TestScriptMethod() | Should Be "script method"
			(1, 3).TestScriptProperty | Should Be 2
			(1, 3).PSStandardMembers.SerializationMethod | Should Be "AllPublicProperties"
			(1, 3).PSStandardMembers.SerializationDepth | Should Be 2
			(1, 3).PSStandardMembers.StringSerializationSource | Should Be 2
			(1, 3).PSStandardMembers.TargetTypeForDeserialization | Should Be "string"
		}
		finally
		{
			Remove-TypeData System.Object[]
		}
	}
	
	It "Update-TypeData with Duplicate XML Files should work"{
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
		try
		{
			Update-TypeData -AppendPath $testfile
			$a=1..3
			$a.Yada | Should Be 3
		}
		finally
		{
			Remove-TypeData -Path $testfile
			rm $testfile
		}
	}
}