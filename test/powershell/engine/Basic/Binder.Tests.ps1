# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe ".NET Method Binding Tests" -tags CI {
    BeforeAll {
        Add-Type -TypeDefinition @'
using System;
using System.Dynamic;
using System.Text;

namespace BinderTests;

public class TestClass
{
    public static string SingleOverload(string value) => value;

    public static string OverloadWithSameNameDifferentCase(string value, string Value) => $"{value}{Value}";

    public static string MultipleArguments(string value1, string value2 = "default1", string value3 = "default2") => $"{value1}{value2}{value3}";

    public static int OverloadByType(string stringValue) => 1;
    public static int OverloadByType(int intValue) => 2;

    public static int OverloadWithDefault(int intValue) => 1;
    public static int OverloadWithDefault(int intValue, int default1 = 1) => 2;
    public static int OverloadWithDefault(int intValue, int default1 = 1, int default2 = 2) => 3;

    public static string OverloadWithDefaults(string value1, int value2, string default1 = "foo", string default2 = null) => $"{value1}-{value2}-{default1}-{default2}";

    public static string Params(string value, params string[] remainder) => $"{value}-" + string.Join("-", remainder);

    public static string ParamsWithDefault(string value, string defaultArg = "foo", params string[] remainder) => $"{value}-{defaultArg}-" + string.Join("-", remainder);

    public static string MethodWithReservedIdentifier(string @if) => @if;

    public static string MethodWithSpecialArgName1(string Δ) => Δ;
    public static string MethodWithSpecialArgName2(string \u0394) => \u0394;
}

public class DynamicClass : DynamicObject
{
    public override bool TryInvokeMember(
        InvokeMemberBinder binder,
        object[] args,
        out object result)
    {
        int startNameIndex = binder.CallInfo.ArgumentCount - binder.CallInfo.ArgumentNames.Count;

        StringBuilder sb = new StringBuilder();
        sb.Append(binder.Name);
        sb.Append("(");
        for (int i = 0; i < args.Length; i++)
        {
            int argNameIndex = i - startNameIndex;
            if (argNameIndex >= 0 && binder.CallInfo.ArgumentNames.Count > argNameIndex)
            {
                sb.AppendFormat("{0}: ", binder.CallInfo.ArgumentNames[argNameIndex]);
            }
            sb.Append(args[i].ToString());

            if (i < args.Length - 1)
            {
                sb.Append(", ");
            }
        }
        sb.Append(")");

        result = sb.ToString();
        return true;
    }
}
'@
    }

    Context "Named Arguments - .NET Binder" {

        It "Fails with single overload with mismatch name" {
            $err = { [BinderTests.TestClass]::SingleOverload(other: 'foo') } | Should -Throw -PassThru
            [string]$err | Should -Be 'Cannot find an overload for "SingleOverload" and the argument count: "1".'
        }

        It "Fails with single overload with mismatch case sensitive name" {
            $err = { [BinderTests.TestClass]::SingleOverload(Value: 'foo') } | Should -Throw -PassThru
            [string]$err | Should -Be 'Cannot find an overload for "SingleOverload" and the argument count: "1".'
        }

        It "Uses single overload with correct name" {
            [BinderTests.TestClass]::SingleOverload(value: 'foo') | Should -Be foo
        }

        It "Fails to call method with too little arguments" {
            $err = { [BinderTests.TestClass]::SingleOverload() } | Should -Throw -PassThru
            [string]$err | Should -Be 'Cannot find an overload for "SingleOverload" and the argument count: "0".'
        }

        It "Fails to call method with too many arguments" {
            $err = { [BinderTests.TestClass]::SingleOverload('value', 'other') } | Should -Throw -PassThru
            [string]$err | Should -Be 'Cannot find an overload for "SingleOverload" and the argument count: "2".'
        }

        It "Uses overload with arg name that differs by case" {
            [BinderTests.TestClass]::OverloadWithSameNameDifferentCase(value: 'foo', Value: 'bar') | Should -Be foobar
        }

        It "Calls method with arguments in different positional order" {
            [BinderTests.TestClass]::MultipleArguments(value2: 'bar', value3: 'test', value1: 'foo') | Should -Be foobartest
        }

        It "Calls method with arguments with positional and named order" {
            [BinderTests.TestClass]::MultipleArguments('foo', value3: 'test', value2: 'bar') | Should -Be foobartest
        }

        It "Fails to find overload when positional argument was already used" {
            $err = { [BinderTests.TestClass]::MultipleArguments('foo', value1: 'bar') } | Should -Throw -PassThru
            [string]$err | Should -Be 'Cannot find an overload for "MultipleArguments" and the argument count: "2".'
        }

        It "Calls method with default overload by type" {
            [BinderTests.TestClass]::OverloadByType('1') | Should -Be 1
        }

        It "Calls method with overriden overload by name" {
            [BinderTests.TestClass]::OverloadByType(intValue: '1') | Should -Be 2
        }

        It "Calls method with extra matching overloads" {
            [BinderTests.TestClass]::OverloadWithDefault(1) | Should -Be 1
        }

        It "Calls method with extra matching overloads with positional default" {
            [BinderTests.TestClass]::OverloadWithDefault(1, 1) | Should -Be 2
        }

        It "Calls method with extra matching overloads with named default" {
            [BinderTests.TestClass]::OverloadWithDefault(1, default2: 1) | Should -Be 3
        }

        It "Calls method with named default normal" {
            [BinderTests.TestClass]::OverloadWithDefaults('v1', 2, default1: 'bar') | Should -Be 'v1-2-bar-'
        }

        It "Calls method with named default out of order" {
            [BinderTests.TestClass]::OverloadWithDefaults('v1', 2, default2: 'other') | Should -Be 'v1-2-foo-other'
        }

        It "Calls params with no value" {
            [BinderTests.TestClass]::Params("first") | Should -Be first-
        }

        It "Calls params with single value" {
            [BinderTests.TestClass]::Params("first", "second") | Should -Be first-second
        }

        It "Calls params with multiple values" {
            [BinderTests.TestClass]::Params("first", "second", "third") | Should -Be first-second-third
        }

        It "Calls params with array value" {
            [BinderTests.TestClass]::Params("first", [string[]]@("second", "third")) | Should -Be first-second-third
        }

        It "Calls params with array value and remainders" {
            [BinderTests.TestClass]::Params("first", [string[]]@("second", "third"), 'fourth') | Should -Be 'first-second third-fourth'
        }

        It "Calls params with empty array value" {
            [BinderTests.TestClass]::Params("first", [string[]]@()) | Should -Be first-
        }

        It "Calls params with single named value" {
            [BinderTests.TestClass]::Params("first", remainder: "second") | Should -Be first-second
        }

        It "Calls params with array named value" {
            [BinderTests.TestClass]::Params("first", remainder: [string[]]@("second", "third")) | Should -Be first-second-third
        }

        It "Calls params with empty array named value" {
            [BinderTests.TestClass]::Params("first", remainder: [string[]]@()) | Should -Be first-
        }

        It "Calls params with named arguments out of order" {
            [BinderTests.TestClass]::Params(remainder: "second", value: "first") | Should -Be first-second
        }

        It "Calls params with default value and no params" {
            [BinderTests.TestClass]::ParamsWithDefault("first") | Should -Be first-foo-
        }

        It "Calls params with default value and single params" {
            [BinderTests.TestClass]::ParamsWithDefault("first", remainder: "second") | Should -Be first-foo-second
        }

        It "Calls params with default value and array params" {
            [BinderTests.TestClass]::ParamsWithDefault("first", remainder: [string[]]@("second", "third")) | Should -Be first-foo-second-third
        }

        It "Calls params with default value set and no params" {
            [BinderTests.TestClass]::ParamsWithDefault("first", "bar") | Should -Be first-bar-
        }

        It "Calls params with default value set and single params through position" {
            [BinderTests.TestClass]::ParamsWithDefault("first", "bar", "second") | Should -Be first-bar-second
        }

        It "Calls params with default value set and single params through name" {
            [BinderTests.TestClass]::ParamsWithDefault("first", "bar", remainder: "second") | Should -Be first-bar-second
        }

        It "Calls params with default value set and array params through name" {
            [BinderTests.TestClass]::ParamsWithDefault("first", "bar", remainder: [string[]]@("second", "third")) | Should -Be first-bar-second-third
        }

        It "Calls params with default value set and array params through array position" {
            [BinderTests.TestClass]::ParamsWithDefault("first", "bar", [string[]]@("second", "third")) | Should -Be first-bar-second-third
        }

        It "Calls params with default value set and array params through multiple position" {
            [BinderTests.TestClass]::ParamsWithDefault("first", "bar", "second", "third") | Should -Be first-bar-second-third
        }

        It "Calls params with default value through named arg no params" {
            [BinderTests.TestClass]::ParamsWithDefault("first", defaultArg: "bar") | Should -Be first-bar-
        }

        It "Calls params with default value through named arg single params" {
            [BinderTests.TestClass]::ParamsWithDefault("first", defaultArg: "bar", remainder: "second") | Should -Be first-bar-second
        }

        It "Calls params with default value through named arg array params" {
            [BinderTests.TestClass]::ParamsWithDefault("first", defaultArg: "bar", remainder: [string[]]@("second", "third")) | Should -Be first-bar-second-third
        }

        It "Fails with params where default has already been set with single param value" {
            $err = { [BinderTests.TestClass]::ParamsWithDefault("first", "second", defaultArg: "bar") } | Should -Throw -PassThru
            [string]$err | Should -Be 'Cannot find an overload for "ParamsWithDefault" and the argument count: "3".'
        }

        It "Fails with params where default has already been set with array param value" {
            $err = { [BinderTests.TestClass]::ParamsWithDefault("first", [string[]]@("second", "third"), defaultArg: "bar") } | Should -Throw -PassThru
            [string]$err | Should -Be 'Cannot find an overload for "ParamsWithDefault" and the argument count: "3".'
        }

        It "Fails with params where defualt set positionally as well as through named argument" {
            $err = { [BinderTests.TestClass]::ParamsWithDefault("first", "second", "third", defaultArg: "bar") } | Should -Throw -PassThru
            [string]$err | Should -Be 'Cannot find an overload for "ParamsWithDefault" and the argument count: "4".'
        }

        It "Calls params with default value through named arg after named single params" {
            [BinderTests.TestClass]::ParamsWithDefault("first", remainder: "second", defaultArg: "bar") | Should -Be first-bar-second
        }

        It "Calls params with default value through named arg after named array params" {
            [BinderTests.TestClass]::ParamsWithDefault("first", remainder: [string[]]@("second", "third"), defaultArg: "bar") | Should -Be first-bar-second-third
        }

        It "Calls with reserved identifier" {
            [BinderTests.TestClass]::MethodWithReservedIdentifier(if: 'abc') | Should -Be abc
        }

        It "Calls with unicode argument identifier 1" {
            [BinderTests.TestClass]::MethodWithSpecialArgName1(Δ: 'abc') | Should -Be abc
        }

        It "Calls with unicode argument identifier 2" {
            [BinderTests.TestClass]::MethodWithSpecialArgName2(Δ: 'abc') | Should -Be abc
        }
    }

    Context "Named Arguments - MethodBuilder" {
        BeforeAll {
            $assemblyName = [System.Reflection.AssemblyName]::new('BinderTestsBuilder')
            $assembly = [System.Reflection.Emit.AssemblyBuilder]::DefineDynamicAssembly(
                $assemblyName,
                [System.Reflection.Emit.AssemblyBuilderAccess]::RunAndCollect)
            $builder = $assembly.DefineDynamicModule($assemblyName)
            $typeBuilder = $builder.DefineType('BinderTests.Reflection.TestClass', [System.Reflection.TypeAttributes]'Sealed, Public')
            $methBuilder = $typeBuilder.DefineMethod(
                'Method',
                [System.Reflection.MethodAttributes]'Public, Static',
                [string],
                [type[]]@([string], [string]))

            $param2 = $methBuilder.DefineParameter(2, [System.Reflection.ParameterAttributes]'HasDefault, Optional', 'arg0')
            $param2.SetConstant('default')

            $il = $methBuilder.GetILGenerator()
            $il.Emit([System.Reflection.Emit.OpCodes]::Ldarg_0)
            $il.Emit([System.Reflection.Emit.OpCodes]::Ldstr, "-")
            $il.Emit([System.Reflection.Emit.OpCodes]::Ldarg_1)
            $il.Emit([System.Reflection.Emit.OpCodes]::Call, [string].GetMethod('Concat', [type[]]@([string], [string], [string])))
            $il.Emit([System.Reflection.Emit.OpCodes]::Ret)

            $TestClass = $typeBuilder.CreateType()
        }

        It "Calls method with null argument name with default" {
            $TestClass::Method('foo') | Should -Be foo-default
        }

        It "Calls method with null argument name and positional default" {
            $TestClass::Method('foo', 'bar') | Should -Be foo-bar
        }

        It "Calls method with null argument name and named default" {
            $TestClass::Method('foo', arg0: 'bar') | Should -Be foo-bar
        }
    }

    Context "Named Arguments - DynamicObject Binder" {
        It "Calls DynamicObject with named argument" {
            $cls = [BinderTests.DynamicClass]::new()
            $cls.Testing('abc', bar: 'def') | Should -Be 'Testing(abc, bar: def)'
        }
    }

    Context "Named Arguments - PowerShell Class" {
        BeforeAll {
            class MyClass {
                [string] Method([string]$a, [string]$b = 'abc', [string]$c = 'def') {
                    return "${a}-${b}-${c}"
                }
            }

            class SuperClass : MyClass {
                [string] Method() {
                    return ([MyClass]$this).Method('a', c: 'foo', b: "test")
                }
            }
        }

        It "Calls method directory with named argument" {
            $c = [MyClass]::new()
            $c.Method('first', c: 'third', b: 'second') | Should -Be first-second-third
        }

        It "Calls overloaded method with named argument" {
            $c = [SuperClass]::new()
            $c.Method() | Should -Be a-test-foo
        }
    }
}
