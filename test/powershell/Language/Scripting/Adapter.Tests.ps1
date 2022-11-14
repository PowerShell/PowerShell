# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Interface static members" -Tags "CI" {
    BeforeAll {
        $testType = "InterfaceStaticMemberTest.ITest" -as [Type]
        if (-not $testType) {
            Add-Type -TypeDefinition @'
    using System;

    namespace InterfaceStaticMemberTest {
        public interface ITest {
            public const int MyConst = 17;

            public static string Type = "ITest-Type";
            public static string Name => "Joe";
            public static string GetPath() => "Path";
            public int GetAge();
            public int GetId();
        }

        public class Foo : ITest {
            int ITest.GetId() { return 100; }
            int ITest.GetAge() { return 2; }
        }

        public class Zoo : Foo {
            public static string Type = "Zoo-Type";
        }
    }
'@
        }
    }

    It "Access static/const members on the interface type" {
        [InterfaceStaticMemberTest.ITest]::MyConst | Should -Be 17
        [InterfaceStaticMemberTest.ITest]::Type | Should -Be "ITest-Type"
        [InterfaceStaticMemberTest.ITest]::Name | Should -Be "Joe"
        [InterfaceStaticMemberTest.ITest]::GetPath() | Should -Be "Path"

        $type = [InterfaceStaticMemberTest.ITest]
        $type::MyConst | Should -Be 17
        $type::Type | Should -Be "ITest-Type"
        $type::Name | Should -Be "Joe"
        $type::GetPath() | Should -Be "Path"

        { [InterfaceStaticMemberTest.ITest]::Name = 'Jane' } | Should -Throw -ErrorId 'PropertyAssignmentException'
        { $type::Name = 'Jane' } | Should -Throw -ErrorId 'PropertyAssignmentException'
    }

    It "Access interface static/const members on the implementation type" {
        [InterfaceStaticMemberTest.Foo]::MyConst | Should -Be 17
        [InterfaceStaticMemberTest.Foo]::Type | Should -Be "ITest-Type"
        [InterfaceStaticMemberTest.Foo]::Name | Should -Be "Joe"
        [InterfaceStaticMemberTest.Foo]::GetPath() | Should -Be "Path"
        [InterfaceStaticMemberTest.Foo]::get_Name() | Should -Be "Joe"

        $type = [InterfaceStaticMemberTest.Foo]
        $type::MyConst | Should -Be 17
        $type::Type | Should -Be "ITest-Type"
        $type::Name | Should -Be "Joe"
        $type::GetPath() | Should -Be "Path"
        $type::get_Name() | Should -Be "Joe"

        { [InterfaceStaticMemberTest.Foo]::Name = 'Jane' } | Should -Throw -ErrorId 'PropertyAssignmentException'
        { $type::Name = 'Jane' } | Should -Throw -ErrorId 'PropertyAssignmentException'
    }

    It "Static field with the same name on the implementation type overrides the one from the interface" {
        [InterfaceStaticMemberTest.Zoo]::Type | Should -Be "Zoo-Type"

        $type = [InterfaceStaticMemberTest.Zoo]
        $type::Type | Should -Be "Zoo-Type"

        $nameMember = $type | Get-Member -Static -Name Name
        $nameMember | Should -Not -BeNullOrEmpty
        $nameMember.Name | Should -Be "Name"
        $nameMember.MemberType | Should -Be "Property"

        $getNameMember = $type | Get-Member -Static -Name 'get_Name'
        $getNameMember | Should -BeNullOrEmpty

        $getNameMember = $type | Get-Member -Static -Name 'get_Name' -Force
        $getNameMember | Should -Not -BeNullOrEmpty
        $getNameMember.Name | Should -Be "get_Name"
        $getNameMember.MemberType | Should -Be "Method"

        $type::Name | Should -Be "Joe"
        $type::get_Name() | Should -Be "Joe"
    }

    It "Explicitly implemented interface members are visible/accessible by default" {
        $obj = [InterfaceStaticMemberTest.Zoo]::new()

        $ageMember = $obj | Get-Member -Name GetAge
        $ageMember | Should -Not -BeNullOrEmpty
        $ageMember.Name | Should -Be "GetAge"
        $ageMember.MemberType | Should -Be "Method"
        $ageMember.Definition | Should -Be "int ITest.GetAge()"

        $idMember = $obj | Get-Member -Name GetId
        $idMember | Should -Not -BeNullOrEmpty
        $idMember.Name | Should -Be "GetId"
        $idMember.MemberType | Should -Be "Method"
        $idMember.Definition | Should -Be "int ITest.GetId()"

        $obj.GetAge() | Should -Be 2
        $obj.GetId() | Should -Be 100
    }
}

Describe "ByRef property" -Tags "CI" {

    It "Get value from ByRef property" {
        $list = [System.Collections.Generic.LinkedList[int]]::new()
        $node = $list.AddLast(1)

        ## Get value through language binder.
        $node.ValueRef | Should -Be 1

        ## Get value through .NET adapter.
        $property = $node.PSObject.Properties["ValueRef"]
        $property.IsSettable | Should -BeFalse
        $property.IsGettable | Should -BeTrue
        $property.TypeNameOfValue | Should -BeExactly 'System.Int32&'
        $property.Value | Should -Be 1
    }
}
