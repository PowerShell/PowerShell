Describe "Add-Member DRT Unit Tests" -Tags "CI" {

    It "Mandatory parameters should not be null nor empty" {
        # when Name is null
        try
        {
            Add-Member -Name $null
            Throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.AddMemberCommand"
        }

        # when Name is empty
        try
        {
            Add-Member -Name ""
            Throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be "ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.AddMemberCommand"
        }

        # when MemberType is null
        try
        {
            Add-Member -MemberType $null
            Throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.AddMemberCommand"
        }

        # when MemberType is empty
        try
        {
            Add-Member -MemberType ""
            Throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be "CannotConvertArgumentNoMessage,Microsoft.PowerShell.Commands.AddMemberCommand"
        }

        # when InputObject is null
        try
        {
            Add-Member -InputObject $null
            Throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.AddMemberCommand"
        }
    }

    # It only support on AliasProperty, ScriptProperty, CodeProperty and CodeMethod
    It "Should Not Have Value2" {
        $memberTypesWhereV1CannotBeNull = "CodeMethod", "MemberSet", "PropertySet", "ScriptMethod", "NoteProperty"
        foreach ($memberType in $memberTypesWhereV1CannotBeNull)
        {
            try
            {
                Add-Member -InputObject a -memberType $memberType -Name Name -Value something -SecondValue somethingElse
                Throw "Execution OK"
            }
            catch{
                $_.FullyQualifiedErrorId | Should Be "Value2ShouldNotBeSpecified,Microsoft.PowerShell.Commands.AddMemberCommand"
            }
        }
    }

    It "Cannot Add PS Property Or PS Method" {
        $membersYouCannotAdd = "Method", "Property", "ParameterizedProperty"
        foreach ($member in $membersYouCannotAdd)
        {
            try
            {
                Add-Member -InputObject a -memberType $member -Name Name
                Throw "Execution OK"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be "CannotAddMemberType,Microsoft.PowerShell.Commands.AddMemberCommand"

            }
        }

        try
        {
            Add-Member -InputObject a -memberType AnythingElse -Name Name
            Throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be "CannotConvertArgumentNoMessage,Microsoft.PowerShell.Commands.AddMemberCommand"
        }

    }

    It "Value1 And Value2 Should Not Both Null" {
        $memberTypes = "CodeProperty", "ScriptProperty"
        foreach ($memberType in $memberTypes)
        {
            try
            {
                Add-Member -memberType $memberType -Name PropertyName -Value $null -SecondValue $null -InputObject a
                Throw "Execution OK"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be "Value1AndValue2AreNotBothNull,Microsoft.PowerShell.Commands.AddMemberCommand"
            }
        }

    }

    It "Fail to add unexisting type" {
        try
        {
            Add-Member -InputObject a -MemberType AliasProperty -Name Name -Value something -SecondValue unexistingType
            Throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be "InvalidCastFromStringToType,Microsoft.PowerShell.Commands.AddMemberCommand"
        }
    }

    It "Successful alias, no type" {
        $results = Add-Member -InputObject a -MemberType AliasProperty -Name Cnt -Value Length -passthru
        $results.Cnt | Should BeOfType Int32
        $results.Cnt | Should Be 1
    }

    It "Successful alias, with type" {
        $results = add-member -InputObject a -MemberType AliasProperty -Name Cnt -Value Length -SecondValue String -passthru
        $results.Cnt | Should BeOfType String
        $results.Cnt | Should Be '1'
    }

    It "CodeProperty Reference Wrong Type" {
        try
        {
            add-member -InputObject a -MemberType CodeProperty -Name Name -Value something
            Throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be "ConvertToFinalInvalidCastException,Microsoft.PowerShell.Commands.AddMemberCommand"
        }
    }

    It "Empty Member Set Null Value1" {
        $results = add-member -InputObject a -MemberType MemberSet -Name Name -Value $null -passthru
        $results.Length | Should Be 1
        $results.Name.a | Should BeNullOrEmpty
    }

    It "Member Set With 1 Member" {
        $members = new-object System.Collections.ObjectModel.Collection[System.Management.Automation.PSMemberInfo]
        $n=new-object Management.Automation.PSNoteProperty a,1
        $members.Add($n)
        $r=add-member -InputObject a -MemberType MemberSet -Name Name -Value $members -passthru
        $r.Name.a | Should Be '1'
    }

    It "MemberSet With Wrong Type For Value1" {
        try
        {
            add-member -InputObject a -MemberType MemberSet -Name Name -Value ImNotACollection
            Throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be "ConvertToFinalInvalidCastException,Microsoft.PowerShell.Commands.AddMemberCommand"
        }
    }

    It "ScriptMethod Reference Wrong Type" {
        try
        {
            add-member -InputObject a -MemberType ScriptMethod -Name Name -Value something
            Throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be "ConvertToFinalInvalidCastException,Microsoft.PowerShell.Commands.AddMemberCommand"
        }
    }

    It "Add ScriptMethod Success" {
        $results = add-member -InputObject 'abc' -MemberType ScriptMethod -Name Name -Value {$this.length} -passthru
        $results | Should Be abc
        $results.Name() | Should Be 3
    }

    It "ScriptProperty Reference Wrong Type" {
        try
        {
            add-member -InputObject a -MemberType ScriptProperty -Name Name -Value something
            Throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be "ConvertToFinalInvalidCastException,Microsoft.PowerShell.Commands.AddMemberCommand"
        }
    }

    It "Add ScriptProperty Success" {
        set-alias ScriptPropertyTestAlias dir
        $al=(get-alias ScriptPropertyTestAlias)
        $al.Description="MyDescription"
        $al | add-member -MemberType ScriptProperty -Name NewDescription -Value {$this.Description} -SecondValue {$this.Description=$args[0]}
        $al.NewDescription | Should Be 'MyDescription'
        $al.NewDescription = "some description"
        $al.NewDescription | Should Be 'some description'
    }

    It "Add TypeName MemberSet Success" {
        $a = 'string' | add-member -MemberType NoteProperty -Name TestNote -Value Any -TypeName MyType -passthru
        $a.PSTypeNames[0] | Should Be MyType
    }

    It "Add TypeName Existing Name Success" {
        $a = 'string' | add-member -TypeName System.Object -passthru
        $a.PSTypeNames[0] | Should Be System.Object
    }

    It "Add Single Note To Array" {
        $a=1,2,3
        $a = Add-Member -InputObject $a -MemberType NoteProperty -Name Name -Value Value -PassThru
        $a.Name | Should Be Value
    }

    It "Add Multiple Note Members" {
        $obj=new-object psobject
        $hash=@{Name='Name';TestInt=1;TestNull=$null}
        add-member -InputObject $obj $hash
        $obj.Name | Should Be 'Name'
        $obj.TestInt | Should Be 1
        $obj.TestNull | Should BeNullOrEmpty
    }

    It "Add Multiple Note With TypeName" {
        $obj=new-object psobject
        $hash=@{Name='Name';TestInt=1;TestNull=$null}
        $obj = add-member -InputObject $obj $hash -TypeName MyType -Passthru
        $obj.PSTypeNames[0] | Should Be MyType
    }

    It "Add Multiple Members With Force" {
        $obj=new-object psobject
        $hash=@{TestNote='hello'}
        $obj | Add-Member -MemberType NoteProperty -Name TestNote -Value 1
        $obj | add-member $hash -force
        $obj.TestNote | Should Be 'hello'
    }

    It "Simplified Add-Member should support using 'Property' as the NoteProperty member name" {
        $results = add-member -InputObject a property Any -passthru
        $results.property | Should Be 'Any'

        $results = add-member -InputObject a Method Any -passthru
        $results.Method | Should Be 'Any'

        $results = add-member -InputObject a 23 Any -passthru
        $results.23 | Should Be 'Any'

        $results = add-member -InputObject a 8 np Any -passthru
        $results.np | Should Be 'Any'

        $results = add-member -InputObject a 16 sp {1+1} -passthru
        $results.sp | Should Be 2
    }
}

Describe "Add-Member" -Tags "CI" {

    It "should be able to see a newly added member of an object" {
	$o = New-Object psobject
	Add-Member -InputObject $o -MemberType NoteProperty -Name proppy -Value "superVal"

	$o.proppy | Should Not BeNullOrEmpty
	$o.proppy | Should Be "superVal"
    }

    It "Should be able to add a member to an object that already has a member in it" {
	$o = New-Object psobject
	Add-Member -InputObject $o -MemberType NoteProperty -Name proppy -Value "superVal"
	Add-Member -InputObject $o -MemberType NoteProperty -Name AnotherMember -Value "AnotherValue"

	$o.AnotherMember | Should Not BeNullOrEmpty
	$o.AnotherMember | Should Be "AnotherValue"
    }
}
