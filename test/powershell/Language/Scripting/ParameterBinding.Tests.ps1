Describe "Tests for parameter binding" -Tags "CI" {
    Context 'Test of Mandatory parameters' {
        BeforeAll {
            $f = "function get-foo { param([Parameter(mandatory=`$true)] `$a) `$a };"
        }

        It 'Mandatory parameters used in non-interactive host' {
            $rs = [system.management.automation.runspaces.runspacefactory]::CreateRunspace()
            $rs.open()
            $ps = [System.Management.Automation.PowerShell]::Create()
            $ps.Runspace = $rs

            try
            {
                [void] $ps.AddScript($f + "get-foo")
                $asyncResult = $ps.BeginInvoke()
                $ps.EndInvoke($asyncResult)

                $ps.Streams.Error.Count | Should Be 1 # the host does not implement it.
                $ps.InvocationStateInfo.State | Should Be 'Completed'
            } finally {
                $ps.Dispose()
                $rs.Dispose()
            }
        }

        It 'Mandatory parameters used in interactive host' {
            $th = New-TestHost
            $rs = [system.management.automation.runspaces.runspacefactory]::Createrunspace($th)
            $rs.open()
            $ps = [System.Management.Automation.PowerShell]::Create()
            $ps.Runspace = $rs

            try
            {
                $ps.AddScript($f + "get-foo").invoke()
                $prompt = $th.ui.streams.prompt[0]
                $prompt | should Not BeNullOrEmpty
                $result = $prompt.split(":")
                $result[0] | Should Match 'get-foo'
                $result[-1] | should be 'a'
            } finally {
                $rs.Close()
                $rs.Dispose()
                $ps.Dispose()
            }
        }
    }

    It 'Test of positional parameters' {
        function get-foo
        {
            [CmdletBinding()]
            param($a)
            $a
        }

        get-foo a | Should Be a
        get-foo -a b | Should Be b
    }

    It 'Positional parameters when only one position specified: position = 1' {
        function get-foo
        {
            param([Parameter(position=1)] $a )
            $a
        }

        get-foo b | Should Be b
    }

    It 'Positional parameters when only position specified: position = 2' {
        function get-foo
        {
            param([Parameter(position=2)] $a )
            $a
        }

        get-foo b | Should Be b
    }

    It 'Multiple positional parameters case 1' {
        function get-foo
        {
            param( [Parameter(position=1)] $a,
                   [Parameter(position=2)] $b )
            $a; $b
        }

        ( get-foo c d ) -join ',' | Should Be 'c,d'
        ( get-foo -a c d ) -join ',' | Should Be 'c,d'
        ( get-foo -a c -b d ) -join ',' | Should Be 'c,d'
        ( get-foo -b d c ) -join ',' | Should Be 'c,d'
        ( get-foo c -b d ) -join ',' | Should Be 'c,d'
    }

    It 'Multiple positional parameters case 2:  the parameters are put in different order?' {
        function get-foo
        {
            # the parameters are purposefully out of order.
            param( [Parameter(position=2)] $a,
                   [Parameter(position=1)] $b )
            $a; $b
        }

        (get-foo c d) -join ',' | Should Be 'd,c'
    }

    It 'Value from pipeline' {
        function get-foo
        {
            param( [Parameter(valuefrompipeline=$true)] $a )
            process
            {
                if($a % 2 -eq 0)
                {
                    $a
                }
            }
        }

        (1..10 | get-foo) -join ',' | Should Be '2,4,6,8,10'
    }

    It 'Value from pipeline by property name' {
        function get-foo
        {
            param( [Parameter(valuefrompipelinebypropertyname=$true)] $foo )
            process
            {
                if($foo % 2 -eq 0)
                {
                    $foo
                }
            }
        }

        $b = 1..10 | select-object @{name='foo'; expression={$_ * 10}} | get-foo
        $b -join ',' | Should Be '10,20,30,40,50,60,70,80,90,100'
    }

    It 'Value from remaining arguments' {
        function get-foo
        {
            param(
                [Parameter(position=1)] $a,
                [Parameter(valuefromremainingarguments=$true)] $foo
            )
            $foo
        }

        ( get-foo a b c d ) -join ',' | Should Be 'b,c,d'
        ( get-foo a b -a c d ) -join ',' | Should Be 'a,b,d'
        ( get-foo a b -a c -q d ) -join ',' | Should Be 'a,b,-q,d'
    }

    It 'Multiple parameter sets with Value from remaining arguments' {
        function get-foo
        {
            param( [Parameter(parametersetname='set1',position=1)] $a,
                   [Parameter(parametersetname='set2',position=1)] $b,
                   [parameter(valuefromremainingarguments=$true)] $foo )
            $foo
        }

        { get-foo -a a -b b c d } | ShouldBeErrorId 'AmbiguousParameterSet,get-foo'
        ( get-foo -a a b c d ) -join ',' | Should Be 'b,c,d'
        ( get-foo -b b a c d ) -join ',' | Should Be 'a,c,d'
    }

    It 'Default parameter set with value from remaining arguments case 1' {
        function get-foo
        {
            [CmdletBinding(DefaultParameterSetName="set1")]
            param( [Parameter(parametersetname="set1", position=1)] $a,
                   [Parameter(parametersetname="set2", position=1)] $b,
                   [parameter(valuefromremainingarguments=$true)] $foo )
            $a,$b,$foo
        }

        $x,$y,$z=get-foo a b c d
        $x | Should Be a
        $y | Should Be $null
        $z -join ',' | Should Be 'b,c,d'
    }

    It 'Default parameter set with value from remaining argument case 2' {
        function get-foo
        {
            [CmdletBinding(DefaultParameterSetName="set2")]
            param( [Parameter(parametersetname="set1", position = 1)] $a,
                   [Parameter(parametersetname="set2", position = 1)] $b,
                   [parameter(valuefromremainingarguments=$true)] $foo )
            $a,$b,$foo
        }

        $x,$y,$z=get-foo a b c d

        $x | Should Be $null
        $y | Should Be 'a'
        $z -join ',' | Should Be 'b,c,d'
    }

    It 'Alias are specified for parameters' {
        function get-foo
        {
            param( [Parameter(mandatory=$true)][alias("foo", "bar")] $a )
            $a
        }

        get-foo -foo b | Should Be 'b'
    }

    It 'Invoking with script block' {
        $foo = . { param([Parameter(position=2)] $a, [Parameter(position=1)]$b); $a; $b} a b
        $foo[0] | Should Be b
    }

    It 'Normal functions' {
        function foo ($a, $b) {$b, $a}
        ( foo a b ) -join ',' | Should Be 'b,a'
    }

    It 'Null is not Allowed when AllowNull attribute is not set' {
        function get-foo
        {
            param( [Parameter(mandatory=$true)] $a )
            $a
        }

        { get-foo -a $null } | ShouldBeErrorId 'ParameterArgumentValidationErrorNullNotAllowed,get-foo'

    }

    It 'Null is allowed when Allownull attribute is set' {
        function get-foo
        {
            param( [Parameter(mandatory=$true)][allownull()] $a )
            $a
        }

        (get-foo -a $null) | Should Be $null

    }

    It 'Empty string is not allowed AllowEmptyString Attribute is not set' {
        function get-foo
        {
            param( [Parameter(mandatory=$true)][string] $a )
            $a
        }

        { get-foo -a '' } | ShouldBeErrorID 'ParameterArgumentValidationErrorEmptyStringNotAllowed,get-foo'
    }

    It 'Empty string is allowed when AllowEmptyString Attribute is set' {
        function get-foo
        {
            param( [Parameter(mandatory=$true)][allowemptystring()][string] $a )
            $a
        }

        get-foo -a '' | Should Be ''
    }

    It 'Empty collection is not allowed when AllowEmptyCollection it not set' {
        function get-foo
        {
            param( [Parameter(mandatory=$true)][string[]] $a )
            $a
        }

        { get-foo -a @() } | ShouldBeErrorId 'ParameterArgumentValidationErrorEmptyArrayNotAllowed,get-foo'
    }

    It 'Empty collection is allowed when allowEmptyCollection is set' {
        function get-foo
        {
            param( [Parameter(mandatory=$true)][allowemptycollection()][string[]] $a )
            $a
        }

        get-foo -a @() | Should Be $null
    }

    It 'Unspecified non-mandatory bool should not cause exception' {
        function get-foo
        {
            param([Parameter(Mandatory=$true, Position=0, ValueFromPipeline=$true)] $a,
                  [System.Boolean] $b)
            $a
        }

        42 | get-foo | Should be 42
    }

    It 'Parameter binding failure on Parameter1 should not cause parameter binding failure on Length' {
        function get-foo
        {
          param( [Parameter(ValueFromPipeline = $true)] [int] $Parameter1 = 10,
                 [Parameter(ValueFromPipelineByPropertyName = $true)] [int] $Length = 100 )
          process  { $Length }
        }

        'abc' | get-foo | Should Be 3
    }

    It 'Binding array of string to array of bool should fail (cmdletbinding)' {
        function get-foo
        {
           [cmdletbinding()]
           param ([bool[]] $Parameter )
           $Parameter
        }

        { get-foo 'a','b' } | ShouldBeErrorId 'ParameterArgumentTransformationError,get-foo'
    }

    It "Binding array of string to array of bool should succeed" {
        function get-foo
        {
           param ([bool[]] $Parameter)
           $Parameter
        }

        $x = get-foo 'a','b'
        $x[0] | Should be $true
        $x[1] | Should be $true
    }

    Context 'Default value conversion tests' {
        It 'Parameter default value is converted correctly to the proper type when nothing is set on parameter' {
            function get-fooa
            {
                param( [System.Reflection.MemberTypes] $memberTypes = $([Enum]::GetNames("System.Reflection.MemberTypes") -join ",") )
                $memberTypes | Should BeOfType System.Reflection.MemberTypes
            }

            get-fooa
        }

        It "Parameter default value is converted correctly to the proper type when CmdletBinding is set on param" {
            function get-foob
            {
                [CmdletBinding()]
                param( [System.Reflection.MemberTypes] $memberTypes = $([Enum]::GetNames("System.Reflection.MemberTypes") -join ",") )
                $memberTypes | Should BeOfType System.Reflection.MemberTypes
            }

            get-foob
        }

        It "No default value specified should not cause error when parameter attribute is set on the parameter" {
            function get-fooc
            {
                param( [Parameter()] [System.Reflection.MemberTypes] $memberTypes )
                $memberTypes | Should Be $null
            }

            get-fooc
        }


        It "No default value specified should not cause error when nothing is set on parameter" {
            function get-food
            {
                param( [System.Reflection.MemberTypes] $memberTypes )
                $memberTypes | Should Be $null
            }

            get-food
        }

        It "Validation attributes should not run on default values when nothing is set on the parameter" {
            function get-fooe
            {
                param([ValidateRange(1,42)] $p = 55)
                $p
            }

            get-fooe| Should Be 55
        }

        It "Validation attributes should not run on default values when CmdletBinding is set on the parameter" {
            function get-foof
            {
                [CmdletBinding()]
                param([ValidateRange(1,42)] $p = 55)
                $p
            }

            get-foof| Should Be 55
        }

        It "Validation attributes should not run on default values" {
            function get-foog
            {
                param([ValidateRange(1,42)] $p)
                $p
            }

            { get-foog } | Should not throw
        }

        It "Validation attributes should not run on default values when CmdletBinding is set" {
            function get-fooh
            {
                [CmdletBinding()]
                param([ValidateRange(1,42)] $p)
                $p
            }

            { get-fooh } | Should not throw
        }
    }

    #known issue 2069
    It 'Some conversions should be attempted before trying to encode a collection' -skip:$IsCoreCLR {
        try {
                 $null = [Test.Language.ParameterBinding.MyClass]
            }
            catch {
                add-type -PassThru -TypeDefinition @'
                using System.Management.Automation;
                using System;
                using System.Collections;
                using System.Collections.ObjectModel;
                using System.IO;

                namespace Test.Language.ParameterBinding {
                    public class MyClass : Collection<string>
                    {
                        public MyClass() {}
                        public MyClass(Hashtable h) {}
                    }

                    [Cmdlet("Get", "TestCmdlet")]
                    public class MyCmdlet : PSCmdlet {
                        [Parameter]
                        public MyClass MyParameter
                        {
                            get { return myParameter; }
                            set { myParameter = value; }
                        }
                        private MyClass myParameter;

                        protected override void ProcessRecord()
                        {
                            WriteObject((myParameter == null) ? "<null>" : "hashtable");
                        }
                    }
                }
'@ | % {$_.assembly} | Import-module
            }

        Get-TestCmdlet -MyParameter @{ a = 42 } | Should Be 'hashtable'
    }

    It 'Parameter pasing is consuming enumerators' {
        $a = 1..4
        $b = $a.getenumerator()
        $null = $b.MoveNext()
        $null = $b.current
        & { } $b

        #The position of the enumerator shouldn't be modified
        $b.current |Should Be 1
    }
}
