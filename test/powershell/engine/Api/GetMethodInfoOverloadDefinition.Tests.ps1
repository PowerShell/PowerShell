# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Method definition Tests" -tags "CI" {

    BeforeAll {
        Add-Type -NameSpace OverloadDefinitionTest -Name TestClass -MemberDefinition @"
public static void Bar() { }
public static void Bar(int param) { }
public static void Bar(int param1, string param2) { }
public static string Bar(int? param1, string param2) { return param2; }
"@

        Add-Type -NameSpace MethodDefinitionTest -Name TestClass -MemberDefinition @"
public static void TestMethod_Ref(ref int refParam) { }

public static void TestMethod_InOut(in int inParam, out string outParam) { outParam = null; }

public static void TestMethod_Params(params int[] intParams) { }

public static void TestMethod_Generic1<T>(T param) { }

public static void TestMethod_Generic2<T, U>(T param1, U param2) { }

public static void TestMethod_OptInt(int optParam = int.MinValue) { }

public static void TestMethod_OptString(string optParam = "test string") { }

public static void TestMethod_OptStruct(DateTime optParam = new DateTime()) { }

public static void TestMethod_OptEnum(UriKind optParam = UriKind.Relative) { }

public static void TestMethod_OptGeneric<T>(T param = default) { }
"@

        $testCases = @(
            @{
                Description = "parameter passed by reference";
                MethodName = "TestMethod_Ref";
                ExpectedDefinition = "static void TestMethod_Ref([ref] int refParam)"
            }
            @{
                Description = "in and out parameters";
                MethodName = "TestMethod_InOut";
                ExpectedDefinition = "static void TestMethod_InOut([ref] int inParam, [ref] string outParam)"
            }
            @{
                Description = "parameters array";
                MethodName = "TestMethod_Params";
                ExpectedDefinition = "static void TestMethod_Params(Params int[] intParams)"
            }
            @{
                Description = "one generic parameter";
                MethodName = "TestMethod_Generic1";
                ExpectedDefinition = "static void TestMethod_Generic1[T](T param)"
            }
            @{
                Description = "two generic parameters";
                MethodName = "TestMethod_Generic2";
                ExpectedDefinition = "static void TestMethod_Generic2[T, U](T param1, U param2)"
            }
            @{
                Description = "optional int parameter";
                MethodName = "TestMethod_OptInt";
                ExpectedDefinition = "static void TestMethod_OptInt(int optParam = $([int]::MinValue))"
            }
            @{
                Description = "optional string parameter";
                MethodName = "TestMethod_OptString";
                ExpectedDefinition = 'static void TestMethod_OptString(string optParam = "test string")'
            }
            @{
                Description = "optional struct parameter";
                MethodName = "TestMethod_OptStruct";
                ExpectedDefinition = 'static void TestMethod_OptStruct(datetime optParam = default)'
            }
            @{
                Description = "optional enum parameter";
                MethodName = "TestMethod_OptEnum";
                ExpectedDefinition = "static void TestMethod_OptEnum(System.UriKind optParam = System.UriKind.Relative)"
            }
            @{
                Description = "optional generic parameter";
                MethodName = "TestMethod_OptGeneric";
                ExpectedDefinition = "static void TestMethod_OptGeneric[T](T param = default)"
            }
        )
    }

    Context "Verify method definitions" {

        It "Get methods' overload definitions" {
            $definitions = ([OverloadDefinitionTest.TestClass]::Bar).OverloadDefinitions
            $definitions.Count | Should -Be 4
            $definitions[0] | Should -BeExactly "static void Bar()"
            $definitions[1] | Should -BeExactly "static void Bar(int param)"
            $definitions[2] | Should -BeExactly "static void Bar(int param1, string param2)"
            $definitions[3] | Should -BeExactly "static string Bar(System.Nullable[int] param1, string param2)"
        }

        It "Get definition of the method with <Description>" -TestCases $testCases {
            param($MethodName, $ExpectedDefinition)

            $definition = ([MethodDefinitionTest.TestClass] | Get-Member -Type Method -Static $MethodName).Definition
            $definition | Should -BeExactly $ExpectedDefinition

            $overloadDefinition = ([MethodDefinitionTest.TestClass]::$MethodName).OverloadDefinitions
            $overloadDefinition | Should -BeExactly $definition
        }
    }

    Context "Verify method definitions' tooltip" {

        It "Tooltip should contain methods' overload definitions" {
            $result = (TabExpansion2 -inputScript ($s = '[OverloadDefinitionTest.TestClass]::Bar') -cursorColumn $s.Length).CompletionMatches
            $tooltipParts = $result.ToolTip -split "\r?\n"

            $tooltipParts.Count | Should -Be 4
            $tooltipParts[0] | Should -BeExactly "static void Bar()"
            $tooltipParts[1] | Should -BeExactly "static void Bar(int param)"
            $tooltipParts[2] | Should -BeExactly "static void Bar(int param1, string param2)"
            $tooltipParts[3] | Should -BeExactly "static string Bar(System.Nullable[int] param1, string param2)"
        }

        It "Tooltip should contain definition of the method with an optional parameter" {
            $result = (TabExpansion2 -inputScript ($s = "[MethodDefinitionTest.TestClass]::TestMethod_OptInt") -cursorColumn $s.Length).CompletionMatches
            $result.ToolTip | Should -BeExactly "static void TestMethod_OptInt(int optParam = $([int]::MinValue))"
        }
    }

    Context "Verify Definition of Parameterized Property" {
        BeforeAll {
            Add-Type -TypeDefinition @"
namespace TestParameterizedPropertyDefinition {
    public class TestClass1
    {
        private string[] _indexerArray = new string[1];
        public string this[int i]
        {
            get => _indexerArray[i];
            set => _indexerArray[i] = value;
        }
    }

    public class TestClass2
    {
        public int this[int i] => 42;
    }

    public class TestClass3
    {
        [System.Runtime.CompilerServices.IndexerName("TheItem")]
        public int this[int i] => 42;
    }
}
"@
        }

        It "Get definition of parametrized property" {
            $obj = [TestParameterizedPropertyDefinition.TestClass1]::new()
            $result = $obj | Get-Member -Type ParameterizedProperty
            $result.Definition | Should -BeExactly "string Item(int i) {get;set;}"
        }

        It "Get definition of readonly parametrized property" {
            $obj = [TestParameterizedPropertyDefinition.TestClass2]::new()
            $result = $obj | Get-Member -Type ParameterizedProperty
            $result.Definition | Should -BeExactly "int Item(int i) {get;}"
        }

        It "Get definition of parametrized property with changed name" {
            $obj = [TestParameterizedPropertyDefinition.TestClass3]::new()
            $result = $obj | Get-Member -Type ParameterizedProperty
            $result.Name | Should -BeExactly "TheItem"
            $result.Definition | Should -BeExactly "int TheItem(int i) {get;}"
        }
    }

    Context "Verify Definition of CodeMethod" {
        BeforeAll {
            Add-Type -TypeDefinition @"
using System.Management.Automation;

namespace TestCodeMethodDefinition;

public static class TestClass1
{
    public static T Generic1<T>(PSObject obj, T val) => val;

    public static T1 Generic2<T1, T2>(PSObject obj, T1 val, T2 dummy) => val;
}
"@

            $mi1 = [TestCodeMethodDefinition.TestClass1].GetMethod("Generic1")
            $mi2 = [TestCodeMethodDefinition.TestClass1].GetMethod("Generic2")

            $obj = [PSCustomObject]@{}
            $obj | Add-Member -MemberType CodeMethod -Name Generic1 -Value $mi1
            $obj | Add-Member -MemberType CodeMethod -Name Generic2 -Value $mi2
            $obj | Add-Member -MemberType CodeMethod -Name FromGeneric1 -Value $mi1.MakeGenericMethod(@([int]))
            $obj | Add-Member -MemberType CodeMethod -Name FromGeneric2 -Value $mi2.MakeGenericMethod(@([string], [int]))
        }

        It "Get definition of CodeMethod with generic method of 1 type" {
            $result = $obj | Get-Member -Name Generic1
            $result.Definition | Should -BeExactly "static T Generic1[T](psobject obj, T val)"
        }

        It "Get definition of CodeMethod with generic method of 2 types" {
            $result = $obj | Get-Member -Name Generic2
            $result.Definition | Should -BeExactly "static T1 Generic2[T1, T2](psobject obj, T1 val, T2 dummy)"
        }

        It "Get definition of CodeMethod with constructed generic method of 1 type" {
            $result = $obj | Get-Member -Name FromGeneric1
            $result.Definition | Should -BeExactly "static int Generic1[int](psobject obj, int val)"
        }

        It "Get definition of CodeMethod with constructed generic method of 2 types" {
            $result = $obj | Get-Member -Name FromGeneric2
            $result.Definition | Should -BeExactly "static string Generic2[string, int](psobject obj, string val, int dummy)"
        }
    }
}
