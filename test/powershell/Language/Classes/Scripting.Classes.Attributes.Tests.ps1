Import-Module $PSScriptRoot\..\..\Common\Test.Helpers.psm1

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
            try
            {
                [C1]::new().Ensure = "foo"
                throw "Exception expected"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should be 'ExceptionWhenSetting'
            }
        }
    }

    Context 'Property.Static.ValidateSet.String' {
        class C1 { static [ValidateSet("Present", "Absent")][string]$Ensure }
        # This call should not throw exception
        [C1]::Ensure = "Present"
        It 'Error when ValidateSet should be ExceptionWhenSetting'{
            try  {
                [C1]::Ensure = "foo"
                throw "Exception expected"
            }
            catch {
                $_.FullyQualifiedErrorId | Should be 'ExceptionWhenSetting'
            }
        }
    }

    Context 'Property.Instance.ValidateRange.Int' {
        class C1 { [ValidateRange(1, 10)][int]$f }
        # This call should not throw exception
        [C1]::new().f = 10
        [C1]::new().f = 1
        It 'Error when ValidateSet should be ExceptionWhenSetting'{
            try {
                [C1]::new().f = 20
                throw "Exception expected"
            }
            catch {
                $_.FullyQualifiedErrorId | Should be 'ExceptionWhenSetting'
            }
        }
    }

    Context 'Property.Static.ValidateRange.Int' {
        class C1 { static [ValidateRange(1, 10)][int]$f }
        # This call should not throw exception
        [C1]::f = 5
        It 'Error when ValidateSet should be ExceptionWhenSetting'{
            try {
                [C1]::f = 20
                throw "Exception expected"
            }
            catch {
                $_.FullyQualifiedErrorId | Should be 'ExceptionWhenSetting'
            }
        }
    }

    Context 'Property.Static.ValidateSet.ImplicitObject' {
        class C1 { static [ValidateSet("abc", 5)]$o }
        # This call should not throw exception
        [C1]::o = "abc"
        [C1]::o = 5
        It 'Error when ValidateSet should be ExceptionWhenSetting'{
            try {
                [C1]::o = 1
                throw "Exception expected"
            }
            catch {
                $_.FullyQualifiedErrorId | Should be 'ExceptionWhenSetting'
            }
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
            $c.arg | should be 100
        }
        It 'Implicitly Transform to foo' {
            $c.arg = "foo"
            $c.arg | should be "foofoo"
        }
    }

    Context 'Property.Instance.Transformation.String' {
        $c = [scriptblock]::Create('class C1 { [Dummy.DoubleStringTransformation()][string]$arg }; [C1]::new()').Invoke()[0]
        It 'set to foo' {
            $c.arg = "foo"
            $c.arg | should be "foofoo"
        }
    }

    Context Property.Instance.Transformation.Int {
        $c = [scriptblock]::Create('class C1 { [Dummy.DoubleInt()][int]$arg }; [C1]::new()').Invoke()[0]
        It 'arg should be 200' {
            $c.arg = 100
            $c.arg | should be 200
        }
        It 'Set to string should fail with ExceptionWhenSetting' {
            try {
                $c.arg = "abc"
                throw "Exception expected"
            }
            catch {
                $_.FullyQualifiedErrorId | Should be 'ExceptionWhenSetting'
            }
        }
    }

    Context Property.Instance.Transformation.Nullable {
        $c = [scriptblock]::Create('class C1 { [Nullable[int]][Dummy.DoubleStringTransformation()]$arg }; [C1]::new()').Invoke()[0]
        It 'arg should be 100' {
            $c.arg = 100
            $c.arg | should be 100
        }
    }

    Context Property.Instance.Transformation.Order {
        $c = [scriptblock]::Create('class C1 { [Dummy.DoubleStringTransformation()][Dummy.AppendStringTransformation()]$arg }; [C1]::new()').Invoke()[0]
        It 'arg should be 100' {
            $c.arg = 100
            $c.arg | should be 100
        }

        It 'arg should be foo___foo___g' {
            $c.arg = "foo"
            $c.arg | should be "foo___foo___"
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

            [MyEventSource]::new() | Should Not Be $null

        }
    }
}

Describe 'ValidateSet support a dynamically generated set' -Tag "CI" {

    BeforeAll {
        $a=@'
        using System;
        using System.Management.Automation;
        using System.Collections.Generic;

        namespace Test.Language {

            [Cmdlet(VerbsCommon.Get, "TestValidateSet1")]
            public class TestValidateSetCommand1 : PSCmdlet
            {
                [Parameter]
                [ValidateSet(typeof(PSCmdlet))]
                public string Param1;

                protected override void EndProcessing()
                {
                    WriteObject(Param1);
                }
            }

            [Cmdlet(VerbsCommon.Get, "TestValidateSet2")]
            public class TestValidateSetCommand2 : PSCmdlet
            {
                [Parameter]
                [ValidateSet(typeof(GenValuesForParam1))]
                public string Param1;

                protected override void EndProcessing()
                {
                    WriteObject(Param1);
                }
            }

            [Cmdlet(VerbsCommon.Get, "TestValidateSet3")]
            public class TestValidateSetCommand3 : PSCmdlet
            {
                [Parameter]
                [ValidateSet(typeof(GenValuesForParam1Cache), CacheExpiration = 2)]
                public string Param1;

                protected override void EndProcessing()
                {
                    WriteObject(Param1);
                }
            }



            /// Implement of test IValidateSetValuesGenerator
            public class GenValuesForParam1 : IValidateSetValuesGenerator
            {
                public IEnumerable<string> GetValidValues()
                {
                    var testValues = new string[] {"Test1","TestString1","Test2"};
                    foreach (var value in testValues)
                    {
                        yield return value;
                    }
                }
            }

            public class GenValuesForParam1Cache : IValidateSetValuesGenerator
            {
                public IEnumerable<string> GetValidValues()
                {
                    var testValues1 = new string[] {"Test1","TestString1","Test2"};
                    var testValues2 = new string[] {"Test1","TestString2","Test2"};
                    string[] testValues;

                    var currentTime = DateTime.Now;
                    if (DateTime.Compare(_cacheTime, currentTime) <= 0)
                    {
                        testValues = testValues1;
                        //_cacheTime = currentTime.AddSeconds(2);
                    }
                    else
                    {
                        testValues = testValues2;
                    }
                    foreach (var value in testValues)
                    {
                        yield return value;
                    }
                }
                private static DateTime _cacheTime = DateTime.Now.AddSeconds(2);
            }
        }
'@

        Add-Type -TypeDefinition $a -PassThru | % {$_.assembly} | Import-module -Force

        class GenValuesForParam : System.Management.Automation.IValidateSetValuesGenerator {
            [System.Collections.Generic.IEnumerable[String]] GetValidValues() {

                return [string[]]("Test1","TestString1","Test2")
            }

            [DateTime] $cacheTime;
        }

        # Return '$testValues2' and after 2 seconds after first use return another array '$testValues1'.
        class GenValuesForParamCache : System.Management.Automation.IValidateSetValuesGenerator {
            [System.Collections.Generic.IEnumerable[String]] GetValidValues() {

                $testValues1 = "Test1","TestString1","Test2"
                $testValues2 = "Test1","TestString2","Test2"

                $currentTime = [DateTime]::Now
                if ([DateTime]::Compare([GenValuesForParamCache]::cacheTime, $currentTime) -le 0)
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

        function Get-TestValidateSet4
        {
            [CmdletBinding()]
            Param
            (
                [ValidateSet([GenValuesForParam])]
                $Param1
            )

            $Param1
        }

        function Get-TestValidateSet5
        {
            [CmdletBinding()]
            Param
            (
                [ValidateSet([GenValuesForParamCache], CacheExpiration = 2)]
                $Param1
            )

            $Param1
        }

    }

    It 'Throw if IValidateSetValuesGenerator is not implemented' {
        { Get-TestValidateSet1 -Param1 "TestString" -ErrorAction Stop } | ShouldBeErrorId "Argument"
    }

    It 'Dynamically generated set work in C#' {
        Get-TestValidateSet2 -Param1 "TestString1" -ErrorAction SilentlyContinue | Should BeExactly "TestString1"
    }

    It 'Dynamically generated set work in C# with cache expiration' {
        Get-TestValidateSet3 -Param1 "TestString2" -ErrorAction SilentlyContinue | Should BeExactly "TestString2"
        Get-TestValidateSet3 -Param1 "TestString2" -ErrorAction SilentlyContinue | Should BeExactly "TestString2"
        Start-Sleep -Seconds 2
        Get-TestValidateSet3 -Param1 "TestString1" -ErrorAction SilentlyContinue | Should BeExactly "TestString1"
    }

    It 'Dynamically generated set work in PowerShell script' {
        Get-TestValidateSet4 -Param1 "TestString1" -ErrorAction SilentlyContinue | Should BeExactly "TestString1"
    }

    It 'Dynamically generated set work in PowerShell script with cache expiration' {
        Get-TestValidateSet5 -Param1 "TestString2" -ErrorAction SilentlyContinue | Should BeExactly "TestString2"
        Get-TestValidateSet5 -Param1 "TestString2" -ErrorAction SilentlyContinue | Should BeExactly "TestString2"
        Start-Sleep -Seconds 2
        Get-TestValidateSet5 -Param1 "TestString1" -ErrorAction SilentlyContinue | Should BeExactly "TestString1"
    }
}
