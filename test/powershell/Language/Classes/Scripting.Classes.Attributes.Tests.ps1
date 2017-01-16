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
        Add-Type -TypeDefinition $dummyAttributesSource -ReferencedAssemblies "System.Management.Automation","mscorlib"
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
