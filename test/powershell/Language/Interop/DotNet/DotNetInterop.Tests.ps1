# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Handle ByRef-like types gracefully" -Tags "CI" {

    BeforeAll {
        $code = @'
using System;
using System.Management.Automation;
namespace DotNetInterop
{
    public class Test
    {
        public Span<int> this[int i]
        {
            get { return default(Span<int>); }
            set { DoNothing(value); }
        }

        public static Span<int> Space
        {
            get { return default(Span<int>); }
            set { DoNothing(value); }
        }

        public Span<int> Room
        {
            get { return default(Span<int>); }
            set { DoNothing(value); }
        }

        private static void DoNothing(Span<int> param)
        {
        }

        public string PrintMySpan(string str, ReadOnlySpan<char> mySpan = default)
        {
            if (mySpan.Length == 0)
            {
                return str;
            }
            else
            {
                return str + mySpan.Length;
            }
        }

        public Span<int> GetSpan(int[] array)
        {
            return array.AsSpan();
        }
    }

    public class Test2
    {
        public Test2(ReadOnlySpan<char> span)
        {
            name = $"Number of chars: {span.Length}";
        }

        public Test2() {}

        private string name = "Hello World";
        public string this[ReadOnlySpan<char> span]
        {
            get { return name; }
            set { name = value; }
        }

        public string Name => name;
    }

    public class CodeMethods
    {
        public static ReadOnlySpan<char> GetProperty(PSObject instance)
        {
            return default(ReadOnlySpan<char>);
        }

        public static void SetProperty(PSObject instance, ReadOnlySpan<char> span)
        {
        }

        public static string RunMethod(PSObject instance, string str, ReadOnlySpan<char> span)
        {
            return str + span.Length;
        }
    }

    public ref struct MyByRefLikeType
    {
        public MyByRefLikeType(int i) { }
        public static int Index;
    }

    public class ExampleProblemClass
    {
        public void ProblemMethod(ref MyByRefLikeType value)
        {
        }
    }
}
'@
        if (-not ("DotNetInterop.Test" -as [type]))
        {
            Add-Type -TypeDefinition $code -IgnoreWarnings
        }

        $testObj = [DotNetInterop.Test]::new()
        $test2Obj = [DotNetInterop.Test2]::new()
    }

    It "New-Object should fail gracefully when used for a ByRef-like type" {
        { New-Object -TypeName 'System.Span[string]' } | Should -Throw -ErrorId "CannotInstantiateBoxedByRefLikeType,Microsoft.PowerShell.Commands.NewObjectCommand"
        { New-Object -TypeName 'DotNetInterop.MyByRefLikeType' } | Should -Throw -ErrorId "CannotInstantiateBoxedByRefLikeType,Microsoft.PowerShell.Commands.NewObjectCommand"
    }

    It "The 'new' method call should fail gracefully when used on a ByRef-like type" {
        { [System.Span[string]]::new() } | Should -Throw -ErrorId "CannotInstantiateBoxedByRefLikeType"
        { [DotNetInterop.MyByRefLikeType]::new() } | Should -Throw -ErrorId "CannotInstantiateBoxedByRefLikeType"
    }

    It "Calling constructor of a ByRef-like type via dotnet adapter should fail gracefully - <Number>" -TestCases @(
        @{ Number = 1; Script = { [System.Span[string]]::new.Invoke([ref]$null) } }
        @{ Number = 2; Script = { [DotNetInterop.MyByRefLikeType]::new.Invoke(2) } }
    ) {
        param($Script)
        $expectedError = $null
        try {
            & $Script
        } catch {
            $expectedError = $_
        }

        $expectedError | Should -Not -BeNullOrEmpty
        $expectedError.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly "CannotInstantiateBoxedByRefLikeType"
    }

    It "Cast to a ByRef-like type should fail gracefully" {
        { [System.Span[int]] ([int[]]1,2,3) } | Should -Throw -ErrorId "InvalidCastToByRefLikeType"
        { [DotNetInterop.MyByRefLikeType] "text" } | Should -Throw -ErrorId "InvalidCastToByRefLikeType"
    }

    It "LanguagePrimitives.ConvertTo should fail gracefully for a ByRef-like type '<Name>'" -TestCases @(
        @{ Name = "Span";            Type = [System.Span[int]] }
        @{ Name = "MyByRefLikeType"; Type = [DotNetInterop.MyByRefLikeType] }
    ) {
        param($Type)
        $expectedError = $null
        try {
            [System.Management.Automation.LanguagePrimitives]::ConvertTo(([int[]]1,2,3), $Type)
        } catch {
            $expectedError = $_
        }

        $expectedError | Should -Not -BeNullOrEmpty
        $expectedError.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly "InvalidCastToByRefLikeType"
    }

    It "Getting value of a ByRef-like type instance property should not throw and should return null, even in strict mode - <Mechanism>" -TestCases @(
        @{ Mechanism = "Compiler/Binder"; Script = { [System.Text.Encoding]::ASCII.Preamble } }
        @{ Mechanism = "Dotnet-Adapter";  Script = { [System.Text.Encoding]::ASCII.PSObject.Properties["Preamble"].Value } }
    ) {
        param($Script)

        try {
            Set-StrictMode -Version 3.0
            & $Script | Should -Be $null
        } finally {
            Set-StrictMode -Off
        }
    }

    It "Setting value of a ByRef-like type instance property should fail gracefully - <Mechanism>" -TestCases @(
        @{ Mechanism = "Compiler/Binder"; Script = { $testObj.Room = [int[]](1,2,3) } }
        @{ Mechanism = "Dotnet-Adapter";  Script = { $testObj.PSObject.Properties["Room"].Value = [int[]](1,2,3) } }
    ) {
        param($Script)
        $Script | Should -Throw -ErrorId "CannotAccessByRefLikePropertyOrField"
    }

    It "<Action> value of a ByRef-like type static property should fail gracefully" -TestCases @(
        @{ Action = "Getting"; Script = { [DotNetInterop.Test]::Space } }
        @{ Action = "Setting"; Script = { [DotNetInterop.Test]::Space = "blah" } }
    ) {
        param($Script)
        $Script | Should -Throw -ErrorId "CannotAccessByRefLikePropertyOrField"
    }

    It "Invoke a method with optional ByRef-like parameter could work" {
        $testObj.PrintMySpan("Hello") | Should -BeExactly "Hello"
    }

    It "Invoke a method with ByRef-like parameter should fail gracefully - <Mechanism>" -TestCases @(
        @{ Mechanism = "Compiler/Binder"; Script = { $testObj.PrintMySpan("Hello", 1) } }
        @{ Mechanism = "Dotnet-Adapter";  Script = { $testObj.psobject.Methods["PrintMySpan"].Invoke("Hello", 1) } }
    ) {
        param($Script)
        $Script | Should -Throw -ErrorId "MethodArgumentConversionInvalidCastArgument"
    }

    It "Invoke a method with ByRef-like return type should fail gracefully - Compiler/Binder" {
        { $testObj.GetSpan([int[]]@(1,2,3)) } | Should -Throw -ErrorId "CannotCallMethodWithByRefLikeReturnType"
    }

    It "Invoke a method with ByRef-like return type should fail gracefully - Dotnet-Adapter" {
        $expectedError = $null
        try {
            $testObj.psobject.Methods["GetSpan"].Invoke([int[]]@(1,2,3))
        } catch {
            $expectedError = $_
        }
        $expectedError | Should -Not -BeNullOrEmpty
        $expectedError.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly "CannotCallMethodWithByRefLikeReturnType"
    }

    It "Access static property of a ByRef-like type" {
        [DotNetInterop.MyByRefLikeType]::Index = 10
        [DotNetInterop.MyByRefLikeType]::Index | Should -Be 10
    }

    It "Get access of an indexer that returns ByRef-like type should return null in no-strict mode" {
        $testObj[1] | Should -Be $null
    }

    It "Get access of an indexer that returns ByRef-like type should fail gracefully in strict mode" {
        try {
            Set-StrictMode -Version 3.0
            { $testObj[1] } | Should -Throw -ErrorId "CannotIndexWithByRefLikeReturnType"
        } finally {
            Set-StrictMode -Off
        }
    }

    It "Set access of an indexer that accepts ByRef-like type value should fail gracefully" {
        { $testObj[1] = 1 } | Should -Throw -ErrorId "CannotIndexWithByRefLikeReturnType"
    }

    It "Create instance of type with method that use a ByRef-like type as a ByRef parameter" {
        $obj = [DotNetInterop.ExampleProblemClass]::new()
        $obj | Should -BeOfType DotNetInterop.ExampleProblemClass
    }

    Context "Passing value that is implicitly/explicitly castable to ByRef-like parameter in method invocation" {

        BeforeAll {
            $ps = [powershell]::Create()

            # Define the CodeMethod 'RunTest'
            $ps.AddCommand("Update-TypeData").
                AddParameter("TypeName", "DotNetInterop.Test2").
                AddParameter("MemberType", "CodeMethod").
                AddParameter("MemberName", "RunTest").
                AddParameter("Value", [DotNetInterop.CodeMethods].GetMethod('RunMethod')).Invoke()
            $ps.Commands.Clear()

            # Define the CodeProperty 'TestName'
            $ps.AddCommand("Update-TypeData").
                AddParameter("TypeName", "DotNetInterop.Test2").
                AddParameter("MemberType", "CodeProperty").
                AddParameter("MemberName", "TestName").
                AddParameter("Value", [DotNetInterop.CodeMethods].GetMethod('GetProperty')).
                AddParameter("SecondValue", [DotNetInterop.CodeMethods].GetMethod('SetProperty')).Invoke()
            $ps.Commands.Clear()

            $ps.AddScript('$test = [DotNetInterop.Test2]::new()').Invoke()
            $ps.Commands.Clear()
        }

        AfterAll {
            $ps.Dispose()
        }

        It "Support method calls with ByRef-like parameter as long as the argument can be casted to the ByRef-like type" {
            $testObj.PrintMySpan("abc", "def") | Should -BeExactly "abc3"
            $testObj.PrintMySpan("abc", "Hello".ToCharArray()) | Should -BeExactly "abc5"
            { $testObj.PrintMySpan("abc", 12) } | Should -Throw -ErrorId "MethodArgumentConversionInvalidCastArgument"

            $path = [System.IO.Path]::GetTempPath()
            [System.IO.Path]::IsPathRooted($path.ToCharArray()) | Should -BeTrue
        }

        It "Support constructor calls with ByRef-like parameter as long as the argument can be casted to the ByRef-like type" {
            $result = [DotNetInterop.Test2]::new("abc")
            $result.Name | Should -BeExactly "Number of chars: 3"

            { [DotNetInterop.Test2]::new(12) } | Should -Throw -ErrorId "MethodCountCouldNotFindBest"
        }

        It "Support indexing operation with ByRef-like index as long as the argument can be casted to the ByRef-like type" {
            $test2Obj["abc"] | Should -BeExactly "Hello World"
            $test2Obj["abc"] = "pwsh"
            $test2Obj["abc"] | Should -BeExactly "pwsh"
        }

        It "Support CodeMethod with ByRef-like parameter as long as the argument can be casted to the ByRef-like type" {
            $result = $ps.AddScript('$test.RunTest("Hello", "World".ToCharArray())').Invoke()
            $ps.Commands.Clear()
            $result.Count | Should -Be 1
            $result[0] | Should -Be 'Hello5'
        }

        It "Return null for getter access of a CodeProperty that returns a ByRef-like type, even in strict mode" {
            $result = $ps.AddScript(
                'try { Set-StrictMode -Version 3.0; $test.TestName } finally { Set-StrictMode -Off }').Invoke()
            $ps.Commands.Clear()
            $result.Count | Should -Be 1
            $result[0] | Should -Be $null
        }

        It "Fail gracefully for setter access of a CodeProperty that returns a ByRef-like type" {
            $result = $ps.AddScript('$test.TestName = "Hello"').Invoke()
            $ps.Commands.Clear()
            $result.Count | Should -Be 0
            $ps.Streams.Error[0].FullyQualifiedErrorId | Should -Be "CannotAccessByRefLikePropertyOrField"
        }
    }
}
