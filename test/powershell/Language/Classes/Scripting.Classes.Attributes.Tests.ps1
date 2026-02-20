# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'Attributes Test' -Tags "CI" {

    BeforeAll {
        $dummyAttributesSource = @'
using System.Management.Automation;
namespace Dummy
{
    public class DoubleStringTransformationAttribute : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            string arg = inputData as string;
            if (arg != null)
            {
                return arg + arg;
            }
            return inputData;
        }
    }

    public class AppendStringTransformationAttribute : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            string arg = inputData as string;
            if (arg != null)
            {
                return arg + "___";
            }
            return inputData;
        }
    }

    public class DoubleInt : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            int? arg = inputData as int?;
            if (arg != null)
            {
                return arg + arg;
            }
            return inputData;
        }
    }
}
'@
        Add-Type -TypeDefinition $dummyAttributesSource
    }

    Context 'Property.Instance.ValidateSet.String' {
        class C1 { [ValidateSet("Present", "Absent")][string]$Ensure }
        # This call should not throw exception
        [C1]::new().Ensure = "Present"

        It 'Error when ValidateSet should be ExceptionWhenSetting' {
            { [C1]::new().Ensure = "foo" } | Should -Throw -ErrorId 'ExceptionWhenSetting'
        }
    }

    Context 'Property.Static.ValidateSet.String' {
        class C1 { static [ValidateSet("Present", "Absent")][string]$Ensure }
        # This call should not throw exception
        [C1]::Ensure = "Present"
        It 'Error when ValidateSet should be ExceptionWhenSetting'{
            { [C1]::Ensure = "foo" } | Should -Throw -ErrorId 'ExceptionWhenSetting'
        }
    }

    Context 'Property.Instance.ValidateRange.Int' {
        class C1 { [ValidateRange(1, 10)][int]$f }
        # This call should not throw exception
        [C1]::new().f = 10
        [C1]::new().f = 1
        It 'Error when ValidateSet should be ExceptionWhenSetting'{
            { [C1]::new().f = 20 } | Should -Throw -ErrorId 'ExceptionWhenSetting'
        }
    }

    Context 'Property.Static.ValidateRange.Int' {
        class C1 { static [ValidateRange(1, 10)][int]$f }
        # This call should not throw exception
        [C1]::f = 5
        It 'Error when ValidateSet should be ExceptionWhenSetting'{
            { [C1]::f = 20 } | Should -Throw -ErrorId 'ExceptionWhenSetting'
        }
    }

    Context 'Property.Static.ValidateSet.ImplicitObject' {
        class C1 { static [ValidateSet("abc", 5)]$o }
        # This call should not throw exception
        [C1]::o = "abc"
        [C1]::o = 5
        It 'Error when ValidateSet should be ExceptionWhenSetting'{
            { [C1]::o = 1 } | Should -Throw -ErrorId 'ExceptionWhenSetting'
        }
    }

    #
    # We use [scriptblock]::Create() here to allow SuiteSetup add Dummy.Transformation type to
    # the scope. Otherwise, we will need to have all classes for attributes in parse time.
    #
    # Invoke() returns an array, we need first element of it.
    #

    Context 'Property.Instance.Transformation.ImplicitObject' {
        $c = [scriptblock]::Create('class C1 { [Dummy.DoubleStringTransformation()]$arg }; [C1]::new()').Invoke()[0]

        It 'Implicitly Transform to 100' {
            $c.arg = 100
            $c.arg | Should -Be 100
        }
        It 'Implicitly Transform to foo' {
            $c.arg = "foo"
            $c.arg | Should -BeExactly "foofoo"
        }
    }

    Context 'Property.Instance.Transformation.String' {
        $c = [scriptblock]::Create('class C1 { [Dummy.DoubleStringTransformation()][string]$arg }; [C1]::new()').Invoke()[0]
        It 'set to foo' {
            $c.arg = "foo"
            $c.arg | Should -BeExactly "foofoo"
        }
    }

    Context Property.Instance.Transformation.Int {
        $c = [scriptblock]::Create('class C1 { [Dummy.DoubleInt()][int]$arg }; [C1]::new()').Invoke()[0]
        It 'arg should be 200' {
            $c.arg = 100
            $c.arg | Should -Be 200
        }
        It 'Set to string should fail with ExceptionWhenSetting' {
            { $c.arg = "abc" } | Should -Throw -ErrorId 'ExceptionWhenSetting'
        }
    }

    Context Property.Instance.Transformation.Nullable {
        $c = [scriptblock]::Create('class C1 { [Nullable[int]][Dummy.DoubleStringTransformation()]$arg }; [C1]::new()').Invoke()[0]
        It 'arg should be 100' {
            $c.arg = 100
            $c.arg | Should -Be 100
        }
    }

    Context Property.Instance.Transformation.Order {
        $c = [scriptblock]::Create('class C1 { [Dummy.DoubleStringTransformation()][Dummy.AppendStringTransformation()]$arg }; [C1]::new()').Invoke()[0]
        It 'arg should be 100' {
            $c.arg = 100
            $c.arg | Should -Be 100
        }

        It 'arg should be foo___foo___g' {
            $c.arg = "foo"
            $c.arg | Should -BeExactly "foo___foo___"
        }
    }
}

Describe 'Type resolution with attributes' -Tag "CI" {
    # There is kind of a collision between names
    # System.Diagnostics.Tracing.EventSource
    # System.Diagnostics.Tracing.EventSourceAttribute
    # We need to make sure that we resolve type name to the right class at each usage
    Context 'Name collision' {

        It 'Resolve System.Diagnostics.Tracing.EventSource to Attribute and to Type in the different contexts' {
            [System.Diagnostics.Tracing.EventSource(Name = "MyPSEventSource")]
            class MyEventSource : System.Diagnostics.Tracing.EventSource
            {
                [void] OnEvent([string]$Message) {}
            }

            [MyEventSource]::new() | Should -Not -BeNullOrEmpty

        }
    }
}

Describe 'ValidateSet support a dynamically generated set' -Tag "CI" {

    Context 'C# tests' {

        BeforeAll {
            $a=@'
        using System;
        using System.Management.Automation;
        using System.Collections.Generic;

        namespace Test.Language {

            [Cmdlet(VerbsCommon.Get, "TestValidateSet0")]
            public class TestValidateSetCommand0 : PSCmdlet
            {
                [Parameter]
                [ValidateSet(typeof(PSCmdlet))]
                public string Param1;

                protected override void EndProcessing()
                {
                    WriteObject(Param1);
                }
            }

            [Cmdlet(VerbsCommon.Get, "TestValidateSet4")]
            public class TestValidateSetCommand4 : PSCmdlet
            {
                [Parameter]
                [ValidateSet(typeof(GenValuesForParam))]
                public string Param1;

                protected override void EndProcessing()
                {
                    WriteObject(Param1);
                }
            }

            [Cmdlet(VerbsCommon.Get, "TestValidateSet5")]
            public class TestValidateSetCommand5 : PSCmdlet
            {
                [Parameter]
                [ValidateSet(typeof(GenValuesForParamNull))]
                public string Param1;

                protected override void EndProcessing()
                {
                    WriteObject(Param1);
                }
            }

            /// Implement of test IValidateSetValuesGenerator
            public class GenValuesForParamNull : IValidateSetValuesGenerator
            {
                public string[] GetValidValues()
                {
                    var testValues = new string[] {"Test1","TestString1","Test2"};
                    return null;
                }
            }

            public class GenValuesForParam : IValidateSetValuesGenerator
            {
                public string[] GetValidValues()
                {
                    var testValues = new string[] {"Test1","TestString1","Test2"};
                    return testValues;
                }
            }
        }
'@

            $cls = Add-Type -TypeDefinition $a -PassThru | Select-Object -First 1
            $testModule = Import-Module $cls.Assembly -PassThru
        }

        AfterAll {
            Remove-Module -ModuleInfo $testModule
        }

        It 'Throw if IValidateSetValuesGenerator is not implemented' {
            { Get-TestValidateSet0 -Param1 "TestString" -ErrorAction Stop } | Should -Throw -ErrorId "Argument"
        }

        It 'Dynamically generated set works in C# with default (immediate) cache expire' {
            Get-TestValidateSet4 -Param1 "TestString1" -ErrorAction SilentlyContinue | Should -BeExactly "TestString1"
        }

        It 'Empty dynamically generated set throws in C#' {
            $exc = {
                Get-TestValidateSet5 -Param1 "TestString1" -ErrorAction Stop
            } | Should -Throw -ErrorId "ParameterArgumentValidationError,Test.Language.TestValidateSetCommand5" -PassThru
            $exc.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly "ValidateSetGeneratedValidValuesListIsNull"
        }
    }

    Context 'Powershell tests' {

        BeforeAll {
            class GenValuesForParam : System.Management.Automation.IValidateSetValuesGenerator {
                [string[]] GetValidValues() {

                    return [string[]]("Test1","TestString1","Test2")
                }
            }

            class GenValuesForParamNull : System.Management.Automation.IValidateSetValuesGenerator {
                [string[]] GetValidValues() {

                    return [string[]]$null
                }
            }

            # Return '$testValues2' and after 2 seconds after first use return another array '$testValues1'.
            class GenValuesForParamCache1 : System.Management.Automation.IValidateSetValuesGenerator {
                [string[]] GetValidValues() {

                    $testValues1 = "Test11","TestString11","Test22"
                    $testValues2 = "Test11","TestString22","Test22"

                    $currentTime = [DateTime]::Now
                    if ([DateTime]::Compare([GenValuesForParamCache1]::cacheTime, $currentTime) -le 0)
                    {
                        $testValues = $testValues1;
                    }
                    else
                    {
                        $testValues = $testValues2;
                    }
                    return [string[]]$testValues
                }

                static [DateTime] $cacheTime = [DateTime]::Now.AddSeconds(2);
            }

            function Get-TestValidateSetPS4
            {
                [CmdletBinding()]
                Param
                (
                    [ValidateSet([GenValuesForParam])]
                    $Param1
                )

                $Param1
            }

            function Get-TestValidateSetPS5
            {
                [CmdletBinding()]
                Param
                (
                    [ValidateSet([GenValuesForParamNull])]
                    $Param1
                )

                $Param1
            }

            function Get-TestValidateSetPS6
            {
                [CmdletBinding()]
                Param
                (
                    [ValidateSet([UnImplementedGeneratorOfValues])]
                    $Param1
                )

                $Param1
            }
        }

        It 'Dynamically generated set works in PowerShell script with default (immediate) cache expire' {
            Get-TestValidateSetPS4 -Param1 "TestString1" -ErrorAction SilentlyContinue | Should -BeExactly "TestString1"
        }

        It 'Get the appropriate error message' {
            {Get-TestValidateSetPS4 -Param1 "TestStringWrong" -ErrorAction Stop} | Should -Throw -ErrorId "ParameterArgumentValidationError,Get-TestValidateSetPS4"
        }

        It 'Empty dynamically generated set throws in PowerShell script' {
            $exc = {
                Get-TestValidateSetPS5 -Param1 "TestString1" -ErrorAction Stop
            } | Should -Throw -ErrorId "ParameterArgumentValidationError,Get-TestValidateSetPS5" -PassThru
            $exc.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly "ValidateSetGeneratedValidValuesListIsNull"
        }

        It 'Unimplemented valid values generator type throws in PowerShell script' {
            {
                Get-TestValidateSetPS6 -Param1 "AnyTestString" -ErrorAction Stop
            } | Should -Throw -ErrorId "TypeNotFound"
        }

        It 'IValidateSetValuesGenerator works in PowerShell module' {
            $moduleFile = Join-Path $TestDrive -ChildPath "Test-Module-$((New-Guid).Guid).psm1"
            $module = @'
                class ValidateSetTest : System.Management.Automation.IValidateSetValuesGenerator
                {
                    [string[]] GetValidValues()
                    {
                        return 'Hello', 'World'
                    }
                }

                function Test-ValidateSet
                {
                    [CmdletBinding()]
                    param (
                        [Parameter(Mandatory = $true)]
                        [ValidateSet([ValidateSetTest])]
                        [string[]]
                        $Item
                    )
                    $Item
                }
'@
            Set-Content -Path $moduleFile -Value $module -Force

            try {
                Import-Module -Name $moduleFile -Force
                Test-ValidateSet 'Hello' | Should -BeExactly 'Hello'
            } finally {
                Remove-Module -Name $moduleFile -Force -ErrorAction SilentlyContinue
            }
        }
    }

    Context 'CachedValidValuesGeneratorBase class tests' {

        BeforeAll {
            class GenValuesForParam : System.Management.Automation.CachedValidValuesGeneratorBase {
                GenValuesForParam() : base(300) {
                }

                [string[]] GenerateValidValues() {

                    return [string[]]("Test1","TestString1","Test2")
                }
            }

            class GenValuesWithExpiration : System.Management.Automation.CachedValidValuesGeneratorBase {
                GenValuesWithExpiration() : base(2) {
                }

                Static [bool] $temp = $true;

                [string[]] GenerateValidValues() {

                    if ([GenValuesWithExpiration]::temp) {
                        [GenValuesWithExpiration]::temp = $false
                        return [string[]]("Test1","TestString1","Test2")
                    } else {
                        [GenValuesWithExpiration]::temp = $true
                        return [string[]]("Test1","TestString2","Test2")
                    }

                }
            }

            function Get-TestValidateSetPS4
            {
                [CmdletBinding()]
                Param
                (
                    [ValidateSet([GenValuesForParam])]
                    $Param1
                )

                $Param1
            }

            function Get-TestValidateSetPS5
            {
                [CmdletBinding()]
                Param
                (
                    [ValidateSet([GenValuesWithExpiration])]
                    $Param1
                )

                $Param1
            }
        }

        It 'Can implement CachedValidValuesGeneratorBase in PowerShell' {
            Get-TestValidateSetPS4 -Param1 "TestString1" -ErrorAction SilentlyContinue | Should -BeExactly "TestString1"
        }

        It 'Can implement CachedValidValuesGeneratorBase with cache expiration in PowerShell' {
            Get-TestValidateSetPS5 -Param1 "TestString1" -ErrorAction SilentlyContinue | Should -BeExactly "TestString1"
            Get-TestValidateSetPS5 -Param1 "TestString1" -ErrorAction SilentlyContinue | Should -BeExactly "TestString1"
            Start-Sleep -Seconds 3
            Get-TestValidateSetPS5 -Param1 "TestString2" -ErrorAction SilentlyContinue | Should -BeExactly "TestString2"
        }
    }
}
