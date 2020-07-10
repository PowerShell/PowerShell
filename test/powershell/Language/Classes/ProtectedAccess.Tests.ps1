# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Add-Type -WarningAction Ignore @'
public class Base
{
    private int data;

    protected Base()
    {
        data = 10;
    }

    protected Base(int i)
    {
        data = i;
    }

    protected int Field;
    protected int Property { get; set; }
    public int Property1 { get; protected set; }
    public int Property2 { protected get; set; }

    protected int Method()
    {
        return 32 + data;
    }
    protected int OverloadedMethod1(int i)
    {
        return 32 + i + data;
    }
    protected int OverloadedMethod1(string i)
    {
        return 1 + data;
    }
    public int OverloadedMethod2(int i)
    {
        return 32 + i + data;
    }
    protected int OverloadedMethod2(string i)
    {
        return 1 + data;
    }
    protected int OverloadedMethod3(int i)
    {
        return 32 + i + data;
    }
    public int OverloadedMethod3(string i)
    {
        return 1 + data;
    }
}
'@

$derived1,$derived2,$derived3 = Invoke-Expression @'
class Derived : Base
{
    Derived() : Base() {}
    Derived([int] $i) : Base($i) {}

    [int] TestPropertyAccess()
    {
        $this.Property = 1111
        return $this.Property
    }

    [int] TestPropertyAccess1()
    {
        $this.Property1 = 2111
        return $this.Property1
    }

    [int] TestPropertyAccess2()
    {
        $this.Property2 = 3111
        return $this.Property2
    }

    [int] TestDynamicPropertyAccess()
    {
        $p = 'Property'
        $this.$p = 1112
        return $this.$p
    }

    [int] TestFieldAccess()
    {
        $this.Field = 11
        return $this.Field
    }

    [int] TestDynamicFieldAccess()
    {
        $f = 'Field'
        $this.$f = 12
        return $this.$f
    }

    [int] TestMethodAccess()
    {
        return $this.Method()
    }

    [int] TestDynamicMethodAccess()
    {
        $m = 'Method'
        return $this.$m()
    }

    [int] TestOverloadedMethodAccess1a()
    {
        return $this.OverloadedMethod1(42)
    }
    [int] TestOverloadedMethodAccess1b()
    {
        return $this.OverloadedMethod1("abc")
    }
    [int] TestOverloadedMethodAccess2a()
    {
        return $this.OverloadedMethod2(42)
    }
    [int] TestOverloadedMethodAccess2b()
    {
        return $this.OverloadedMethod2("abc")
    }
    [int] TestOverloadedMethodAccess3a()
    {
        return $this.OverloadedMethod3(42)
    }
    [int] TestOverloadedMethodAccess3b()
    {
        return $this.OverloadedMethod3("abc")
    }
}

class Derived2 : Base {}

[Derived]::new()
[Derived]::new(20)
[Derived2]::new()
'@

Describe "Protected Member Access - w/ default ctor" -Tags "CI" {
    It "Method Access" { $derived1.TestMethodAccess() | Should -Be 42 }
    It "Dynamic Method Access" { $derived1.TestDynamicMethodAccess() | Should -Be 42 }
    It "Field Access" { $derived1.TestFieldAccess() | Should -Be 11 }
    It "Dynamic Field Access" { $derived1.TestDynamicFieldAccess() | Should -Be 12 }
    It "Property Access - protected get/protected set" { $derived1.TestPropertyAccess() | Should -Be 1111 }
    It "Property Access - public get/protected set " { $derived1.TestPropertyAccess1() | Should -Be 2111 }
    It "Property Access - protected get/public set" { $derived1.TestPropertyAccess2() | Should -Be 3111 }
    It "Dynamic Property Access" { $derived1.TestDynamicPropertyAccess() | Should -Be 1112 }

    It "Method Access - overloaded 1a" { $derived1.TestOverloadedMethodAccess1a() | Should -Be 84 }
    It "Method Access - overloaded 1b" { $derived1.TestOverloadedMethodAccess1b() | Should -Be 11 }
    It "Method Access - overloaded 2a" { $derived1.TestOverloadedMethodAccess2a() | Should -Be 84 }
    It "Method Access - overloaded 2b" { $derived1.TestOverloadedMethodAccess2b() | Should -Be 11 }
    It "Method Access - overloaded 3a" { $derived1.TestOverloadedMethodAccess3a() | Should -Be 84 }
    It "Method Access - overloaded 3b" { $derived1.TestOverloadedMethodAccess3b() | Should -Be 11 }
    It "Implicit ctor calls protected ctor" { $derived3.OverloadedMethod2(42) | Should -Be 84 }
}

Describe "Protected Member Access - w/ non-default ctor" -Tags "CI" {
    It "Method Access" { $derived2.TestMethodAccess() | Should -Be 52 }
    It "Dynamic Method Access" { $derived2.TestDynamicMethodAccess() | Should -Be 52 }
    It "Field Access" { $derived2.TestFieldAccess() | Should -Be 11 }
    It "Dynamic Field Access" { $derived2.TestDynamicFieldAccess() | Should -Be 12 }
    It "Property Access - protected get/protected set" { $derived1.TestPropertyAccess() | Should -Be 1111 }
    It "Property Access - public get/protected set " { $derived1.TestPropertyAccess1() | Should -Be 2111 }
    It "Property Access - protected get/public set" { $derived1.TestPropertyAccess2() | Should -Be 3111 }
    It "Dynamic Property Access" { $derived2.TestDynamicPropertyAccess() | Should -Be 1112 }

    It "Method Access - overloaded 1a" { $derived2.TestOverloadedMethodAccess1a() | Should -Be 94 }
    It "Method Access - overloaded 1b" { $derived2.TestOverloadedMethodAccess1b() | Should -Be 21 }
    It "Method Access - overloaded 2a" { $derived2.TestOverloadedMethodAccess2a() | Should -Be 94 }
    It "Method Access - overloaded 2b" { $derived2.TestOverloadedMethodAccess2b() | Should -Be 21 }
    It "Method Access - overloaded 3a" { $derived2.TestOverloadedMethodAccess3a() | Should -Be 94 }
    It "Method Access - overloaded 3b" { $derived2.TestOverloadedMethodAccess3b() | Should -Be 21 }
}

Describe "Protected Member Access - members not visible outside class" -Tags "CI" {
    Set-StrictMode -v 3
    It "Invalid protected field Get Access" { { $derived1.Field } | Should -Throw -ErrorId "PropertyNotFoundStrict" }
    It "Invalid protected property Get Access" { { $derived1.Property } | Should -Throw -ErrorId "PropertyNotFoundStrict" }
    It "Invalid protected field Set Access" { { $derived1.Field = 1 } | Should -Throw -ErrorId "PropertyAssignmentException"}
    It "Invalid protected property Set Access" { { $derived1.Property = 1 } | Should -Throw -ErrorId "PropertyAssignmentException" }

    It "Invalid protected constructor Access" { { [Base]::new() } | Should -Throw -ErrorId "MethodCountCouldNotFindBest" }
    It "Invalid protected method Access" { { $derived1.Method() } | Should -Throw -ErrorId "MethodNotFound" }
}

