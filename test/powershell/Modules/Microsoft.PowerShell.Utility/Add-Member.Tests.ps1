# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Add-Member DRT Unit Tests" -Tags "CI" {

    It "Mandatory parameters should not be null nor empty" {
        # when Name is null
        { Add-Member -Name $null } | Should -Throw -ErrorId "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.AddMemberCommand"

        # when Name is empty
        { Add-Member -Name "" } | Should -Throw -ErrorId "ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.AddMemberCommand"

        # when MemberType is null
        { Add-Member -MemberType $null } | Should -Throw -ErrorId "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.AddMemberCommand"

        # when MemberType is empty
        { Add-Member -MemberType "" } | Should -Throw -ErrorId "CannotConvertArgumentNoMessage,Microsoft.PowerShell.Commands.AddMemberCommand"

        # when InputObject is null
        { Add-Member -InputObject $null } | Should -Throw -ErrorId "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.AddMemberCommand"
    }

    # It only support on AliasProperty, ScriptProperty, CodeProperty and CodeMethod
    It "Should Not Have Value2" {
        $memberTypesWhereV1CannotBeNull = "CodeMethod", "MemberSet", "PropertySet", "ScriptMethod", "NoteProperty"
        foreach ($memberType in $memberTypesWhereV1CannotBeNull)
        {
            { Add-Member -InputObject a -MemberType $memberType -Name Name -Value something -SecondValue somethingElse } |
                Should -Throw -ErrorId "Value2ShouldNotBeSpecified,Microsoft.PowerShell.Commands.AddMemberCommand"
        }
    }

    It "Cannot Add PS Property Or PS Method" {
        $membersYouCannotAdd = "Method", "Property", "ParameterizedProperty"
        foreach ($member in $membersYouCannotAdd)
        {
            { Add-Member -InputObject a -MemberType $member -Name Name } | Should -Throw -ErrorId "CannotAddMemberType,Microsoft.PowerShell.Commands.AddMemberCommand"
        }

        { Add-Member -InputObject a -MemberType AnythingElse -Name Name } | Should -Throw -ErrorId "CannotConvertArgumentNoMessage,Microsoft.PowerShell.Commands.AddMemberCommand"

    }

    It "Value1 And Value2 Should Not Both Null" {
        $memberTypes = "CodeProperty", "ScriptProperty"
        foreach ($memberType in $memberTypes)
        {
            { Add-Member -MemberType $memberType -Name PropertyName -Value $null -SecondValue $null -InputObject a } |
                Should -Throw -ErrorId "Value1AndValue2AreNotBothNull,Microsoft.PowerShell.Commands.AddMemberCommand"
        }

    }

    It "Fail to add unexisting type" {
        { Add-Member -InputObject a -MemberType AliasProperty -Name Name -Value something -SecondValue unexistingType } |
            Should -Throw -ErrorId "InvalidCastFromStringToType,Microsoft.PowerShell.Commands.AddMemberCommand"
    }

    It "Successful alias, no type" {
        $results = Add-Member -InputObject a -MemberType AliasProperty -Name Cnt -Value Length -PassThru
        $results.Cnt | Should -BeOfType Int32
        $results.Cnt | Should -Be 1
    }

    It "Successful alias, with type" {
        $results = Add-Member -InputObject a -MemberType AliasProperty -Name Cnt -Value Length -SecondValue String -PassThru
        $results.Cnt | Should -BeOfType String
        $results.Cnt | Should -Be '1'
    }

    It "CodeProperty Reference Wrong Type" {
        { Add-Member -InputObject a -MemberType CodeProperty -Name Name -Value something } |
            Should -Throw -ErrorId "ConvertToFinalInvalidCastException,Microsoft.PowerShell.Commands.AddMemberCommand"
    }

    It "Empty Member Set Null Value1" {
        $results = Add-Member -InputObject a -MemberType MemberSet -Name Name -Value $null -PassThru
        $results.Length | Should -Be 1
        $results.Name.a | Should -BeNullOrEmpty
    }

    It "Member Set With 1 Member" {
        $members = New-Object System.Collections.ObjectModel.Collection[System.Management.Automation.PSMemberInfo]
        $n=New-Object Management.Automation.PSNoteProperty a,1
        $members.Add($n)
        $r=Add-Member -InputObject a -MemberType MemberSet -Name Name -Value $members -PassThru
        $r.Name.a | Should -Be '1'
    }

    It "MemberSet With Wrong Type For Value1" {
        { Add-Member -InputObject a -MemberType MemberSet -Name Name -Value ImNotACollection } |
            Should -Throw -ErrorId "ConvertToFinalInvalidCastException,Microsoft.PowerShell.Commands.AddMemberCommand"
    }

    It "ScriptMethod Reference Wrong Type" {
        { Add-Member -InputObject a -MemberType ScriptMethod -Name Name -Value something } |
            Should -Throw -ErrorId "ConvertToFinalInvalidCastException,Microsoft.PowerShell.Commands.AddMemberCommand"
    }

    It "Add ScriptMethod Success" {
        $results = Add-Member -InputObject 'abc' -MemberType ScriptMethod -Name Name -Value {$this.length} -PassThru
        $results | Should -BeExactly 'abc'
        $results.Name() | Should -Be 3
    }

    It "ScriptProperty Reference Wrong Type" {
        { Add-Member -InputObject a -MemberType ScriptProperty -Name Name -Value something } |
            Should -Throw -ErrorId "ConvertToFinalInvalidCastException,Microsoft.PowerShell.Commands.AddMemberCommand"
    }

    It "Add ScriptProperty Success" {
        Set-Alias ScriptPropertyTestAlias dir
        $al=(Get-Alias ScriptPropertyTestAlias)
        $al.Description="MyDescription"
        $al | Add-Member -MemberType ScriptProperty -Name NewDescription -Value {$this.Description} -SecondValue {$this.Description=$args[0]}
        $al.NewDescription | Should -BeExactly 'MyDescription'
        $al.NewDescription = "some description"
        $al.NewDescription | Should -BeExactly 'some description'
    }

    It "Add TypeName MemberSet Success" {
        $a = 'string' | Add-Member -MemberType NoteProperty -Name TestNote -Value Any -TypeName MyType -PassThru
        $a.PSTypeNames[0] | Should -Be MyType
    }

    It "Add TypeName Existing Name Success" {
        $a = 'string' | Add-Member -TypeName System.Object -PassThru
        $a.PSTypeNames[0] | Should -Be System.Object
    }

    It "Add Single Note To Array" {
        $a=1,2,3
        $a = Add-Member -InputObject $a -MemberType NoteProperty -Name Name -Value Value -PassThru
        $a.Name | Should -Be Value
    }

    It "Add Multiple Note Members" {
        $obj=New-Object psobject
        $hash=@{Name='Name';TestInt=1;TestNull=$null}
        Add-Member -InputObject $obj $hash
        $obj.Name | Should -Be 'Name'
        $obj.TestInt | Should -Be 1
        $obj.TestNull | Should -BeNullOrEmpty
    }

    It "Add Multiple Note With TypeName" {
        $obj=New-Object psobject
        $hash=@{Name='Name';TestInt=1;TestNull=$null}
        $obj = Add-Member -InputObject $obj $hash -TypeName MyType -PassThru
        $obj.PSTypeNames[0] | Should -Be MyType
    }

    It "Add Multiple Members With Force" {
        $obj=New-Object psobject
        $hash=@{TestNote='hello'}
        $obj | Add-Member -MemberType NoteProperty -Name TestNote -Value 1
        $obj | Add-Member $hash -Force
        $obj.TestNote | Should -Be 'hello'
    }

    It "Simplified Add-Member should support using 'Property' as the NoteProperty member name" {
        $results = Add-Member -InputObject a property Any -PassThru
        $results.property | Should -BeExactly 'Any'

        $results = Add-Member -InputObject a Method Any -PassThru
        $results.Method | Should -BeExactly 'Any'

        $results = Add-Member -InputObject a 23 Any -PassThru
        $results.23 | Should -BeExactly 'Any'

        $results = Add-Member -InputObject a 8 np Any -PassThru
        $results.np | Should -BeExactly 'Any'

        $results = Add-Member -InputObject a 16 sp {1+1} -PassThru
        $results.sp | Should -Be 2
    }

    It "Verify Add-Member error message is not empty" {
        $object = @(1,2)
        Add-Member -InputObject $object "ABC" "Value1"
        Add-Member -InputObject $object "ABC" "Value2" -ErrorVariable errorVar -ErrorAction SilentlyContinue
        $errorVar.Exception | Should -BeOfType System.InvalidOperationException
        $errorVar.Exception.Message | Should -Not -BeNullOrEmpty
    }
}

Describe "Add-Member" -Tags "CI" {

    It "should be able to see a newly added member of an object" {
	$o = New-Object psobject
	Add-Member -InputObject $o -MemberType NoteProperty -Name proppy -Value "superVal"

	$o.proppy | Should -Not -BeNullOrEmpty
	$o.proppy | Should -BeExactly "superVal"
    }

    It "Should be able to add a member to an object that already has a member in it" {
	$o = New-Object psobject
	Add-Member -InputObject $o -MemberType NoteProperty -Name proppy -Value "superVal"
	Add-Member -InputObject $o -MemberType NoteProperty -Name AnotherMember -Value "AnotherValue"

	$o.AnotherMember | Should -Not -BeNullOrEmpty
	$o.AnotherMember | Should -BeExactly "AnotherValue"
    }
}
