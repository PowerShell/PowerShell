# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe ".NET Method Binding Tests" -tags CI {
    BeforeAll {
        Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

namespace CLRBindingTests;

public class TestClass
{
    public int Prop { get; }

    public TestClass(int value = 1)
    {
        Prop = value;
    }

    public static string StaticWithDefaultExpected() => StaticWithDefault();
    public static string StaticWithDefault(string value = "foo") => value;

    public static string StaticWithOptionalAndValueExpected() => StaticWithOptionalAndValue();
    public static string StaticWithOptionalAndValue([Optional, DefaultParameterValue("bar")] string value) => value;

    public static string StaticWithOptionalExpected() => StaticWithOptional();
    public static string StaticWithOptional([Optional] string value) => value;

    public static int PrimitiveTypeWithInDefault(in int value = default) => value;

    public static Guid ValueTypeWithInDefault(in Guid value = default) => value;

    public static string RefTypeWithInDefault(in string value = default) => value;

    public object InstanceWithDefaultExpected() => InstanceWithDefault();
    public object InstanceWithDefault(object value = null) => value;

    public object InstanceWithOptionalAndValueExpected() => InstanceWithOptionalAndValue();
    public object InstanceWithOptionalAndValue([Optional, DefaultParameterValue("foo")] object value) => value;

    public object InstanceWithOptionalExpected() => InstanceWithOptional();
    public object InstanceWithOptional([Optional] object value) => value;

    public string MultipleArgsWithDefaultExpected(string prefix) => MultipleArgsWithDefault(prefix);
    public string MultipleArgsWithDefault(string prefix, string extra = "abc") => $"{prefix}{extra}";

    public string MultipleArgsWithOptionalAndValueExpected(string prefix) => MultipleArgsWithOptionalAndValue(prefix);
    public string MultipleArgsWithOptionalAndValue(string prefix, [Optional, DefaultParameterValue("def")] string extra) => $"{prefix}{extra}";

    public string MultipleArgsWithOptionalExpected(string prefix) => MultipleArgsWithOptional(prefix);
    public string MultipleArgsWithOptional(string prefix, [Optional] string extra) => $"{prefix}{extra}";
}

public class TestClassCstorWithOptionalAndValue
{
    public int Prop { get; }

    public TestClassCstorWithOptionalAndValue([Optional, DefaultParameterValue(2)] int value)
    {
        Prop = value;
    }
}

public class TestClassCstorWithOptional
{
    public int Prop { get; }

    public TestClassCstorWithOptional([Optional] int value)
    {
        Prop = value;
    }
}
'@
    }

    It "Binds to constructor with default argument" {
        $c = [CLRBindingTests.TestClass]::new()
        $c.Prop | Should -Be 1
    }

    It "Binds to constructor with Optional with DefaultValue argument" {
        $c = [CLRBindingTests.TestClassCstorWithOptionalAndValue]::new()
        $c.Prop | Should -Be 2
    }

    It "Binds to constructor with Optional argument" {
        $c = [CLRBindingTests.TestClassCstorWithOptional]::new()
        $c.Prop | Should -Be 0
    }

    It "Binds to static method with default argument" {
        $expected = [CLRBindingTests.TestClass]::StaticWithDefaultExpected()
        $actual = [CLRBindingTests.TestClass]::StaticWithDefault()
        $actual | Should -Be $expected
    }

    It "Binds to static method with Optional with DefaultValue argument" {
        $expected = [CLRBindingTests.TestClass]::StaticWithOptionalAndValueExpected()
        $actual = [CLRBindingTests.TestClass]::StaticWithOptionalAndValue()
        $actual | Should -Be $expected
    }

    It "Binds to static method with Optional argument" {
        $expected = [CLRBindingTests.TestClass]::StaticWithOptionalExpected()
        $actual = [CLRBindingTests.TestClass]::StaticWithOptional()
        $actual | Should -Be $expected
    }

    It "Binds to static method with primitive type with in modifier and default argument" {
        $actual = [CLRBindingTests.TestClass]::PrimitiveTypeWithInDefault()
        $actual | Should -Be 0
    }

    It "Binds to static method with value type with in modifier and default argument" {
        $actual = [CLRBindingTests.TestClass]::ValueTypeWithInDefault()
        $actual | Should -Be ([Guid]::Empty)
    }

    It "Binds to static method with ref type with in modifier and default argument" {
        $actual = [CLRBindingTests.TestClass]::RefTypeWithInDefault()
        $null -eq $actual | Should -BeTrue
    }

    It "Binds to instance method with default argument" {
        $c = [CLRBindingTests.TestClass]::new()

        $expected = $c.InstanceWithDefaultExpected()
        $actual = $c.InstanceWithDefault()
        $actual | Should -Be $expected
    }

    It "Binds to instance method with Optional with DefaultValue argument" {
        $c = [CLRBindingTests.TestClass]::new()

        $expected = $c.InstanceWithOptionalAndValueExpected()
        $actual = $c.InstanceWithOptionalAndValue()
        $actual | Should -Be $expected
    }

    It "Binds to instance method with Optional argument" {
        $c = [CLRBindingTests.TestClass]::new()

        $expected = $c.InstanceWithOptionalExpected()
        $actual = $c.InstanceWithOptional()
        $actual | Should -Be $expected
    }

    It "Binds to instance method with normal arg and default argument" {
        $c = [CLRBindingTests.TestClass]::new()

        $expected = $c.MultipleArgsWithDefaultExpected("prefix")
        $actual = $c.MultipleArgsWithDefault("prefix")
        $actual | Should -Be $expected
    }

    It "Binds to instance method with Optional with normal arg and DefaultValue argument" {
        $c = [CLRBindingTests.TestClass]::new()

        $expected = $c.MultipleArgsWithOptionalAndValueExpected("prefix")
        $actual = $c.MultipleArgsWithOptionalAndValue("prefix")
        $actual | Should -Be $expected
    }

    It "Binds to instance method with normal arg and Optional argument" {
        $c = [CLRBindingTests.TestClass]::new()

        $expected = $c.MultipleArgsWithOptionalExpected("prefix")
        $actual = $c.MultipleArgsWithOptional("prefix")
        $actual | Should -Be $expected
    }
}
