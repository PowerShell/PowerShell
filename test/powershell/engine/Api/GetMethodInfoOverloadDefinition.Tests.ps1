# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Method definition Tests" -tags "CI" {

    Context "Verify Definition of Parameterized Property" {
        BeforeAll {
            $classDefinition1 = @"
namespace ParameterizedPropertyDefinitionTest
{
    public class TestClass
    {
        private string[] _indexerArray = new string[1];
        public string this[int i]
        {
            get => _indexerArray[i];
            set => _indexerArray[i] = value;
        }
    }
}
"@
            $classDefinition2 = @"
namespace ReadonlyParameterizedPropertyDefinitionTest
{
    public class TestClass
    {
        public int this[int i] => 42;
    }
}
"@
            $classDefinition3 = @"
namespace ReadonlyParameterizedPropertyWithChangedNameDefinitionTest
{
    public class TestClass
    {
        [System.Runtime.CompilerServices.IndexerName("TheItem")]
        public int this[int i] => 42;
    }
}
"@
            $TestType1 = Add-Type -PassThru -TypeDefinition $classDefinition1
            $TestType2 = Add-Type -PassThru -TypeDefinition $classDefinition2
            $TestType3 = Add-Type -PassThru -TypeDefinition $classDefinition3
        }

        It "Get definition of parametrized property" {
            $obj = $TestType1::new()
            $result = $obj | Get-Member -Type ParameterizedProperty
            $result.Definition | Should -BeExactly "string Item(int i) {get;set;}"
        }

        It "Get definition of readonly parametrized property" {
            $obj = $TestType2::new()
            $result = $obj | Get-Member -Type ParameterizedProperty
            $result.Definition | Should -BeExactly "int Item(int i) {get;}"
        }

        It "Get definition of parametrized property with changed name" {
            $obj = $TestType3::new()
            $result = $obj | Get-Member -Type ParameterizedProperty
            $result.Name | Should -BeExactly "TheItem"
            $result.Definition | Should -BeExactly "int TheItem(int i) {get;}"
        }
    }

    Context "Verify overloaded method definitions" {
        BeforeAll {
            Add-Type -NameSpace OverloadDefinitionTest -Name TestClass -MemberDefinition @"
public static void Bar() { }
public static void Bar(int param) { }
public static void Bar(int param1, string param2) { }
public static string Bar(int? param1, string param2) { return param2; }
"@
        }

        It "Get methods' overload definitions" {
            $definitions = ([OverloadDefinitionTest.TestClass]::Bar).OverloadDefinitions
            $definitions.Count | Should -BeExactly 4
            $definitions[0] | Should -BeExactly "static void Bar()"
            $definitions[1] | Should -BeExactly "static void Bar(int param)"
            $definitions[2] | Should -BeExactly "static void Bar(int param1, string param2)"
            $definitions[3] | Should -BeExactly "static string Bar(System.Nullable[int] param1, string param2)"
        }
    }

    Context "Verify method definitions with different parameter set" {
        BeforeAll {
            Add-Type -NameSpace MethodDefinitionTest -Name TestClass -MemberDefinition @"
public static void TestMethod_General(int param1, string param2) { }

public static void TestMethod_Ref(ref int refParam, out string outParam) { outParam = null; }

public static void TestMethod_Params(params int[] intParams) { }

public static void TestMethod_Generic<T>(T param) { }

public static void TestMethod_OptInt(int optParam = int.MinValue) { }

public static void TestMethod_OptString(string optParam = "default string") { }

public static void TestMethod_OptStruct(DateTime optParam = new DateTime()) { }

public static void TestMethod_OptEnum(UriKind optParam = UriKind.Relative) { }

public static void TestMethod_OptGeneric<T>(T param = default) { }
"@
        }

        $testCases = @(
            @{
                Description = "general parameters";
                MethodName = "TestMethod_General";
                ExpectedDefinition = "static void TestMethod_General(int param1, string param2)"
            }
            @{
                Description = "reference parameters";
                MethodName = "TestMethod_Ref";
                ExpectedDefinition = "static void TestMethod_Ref([ref] int refParam, [ref] string outParam)"
            }
            @{
                Description = "parameters array";
                MethodName = "TestMethod_Params";
                ExpectedDefinition = "static void TestMethod_Params(Params int[] intParams)"
            }
            @{
                Description = "generic parameter";
                MethodName = "TestMethod_Generic";
                ExpectedDefinition = "static void TestMethod_Generic[T](T param)"
            }
            @{
                Description = "optional int parameter";
                MethodName = "TestMethod_OptInt";
                ExpectedDefinition = "static void TestMethod_OptInt(int optParam = $([int]::MinValue))"
            }
            @{
                Description = "optional string parameter";
                MethodName = "TestMethod_OptString";
                ExpectedDefinition = 'static void TestMethod_OptString(string optParam = "default string")'
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

        It 'Get definition of the method with <Description>' -TestCases $testCases {
            param($MethodName, $ExpectedDefinition)

            $definition = ([MethodDefinitionTest.TestClass] | Get-Member -Type Method -Static $MethodName).Definition
            $definition | Should -BeExactly $ExpectedDefinition

            $overloadDefinition = ([MethodDefinitionTest.TestClass]::$MethodName).OverloadDefinitions
            $overloadDefinition | Should -BeExactly $definition
        }
    }
}
