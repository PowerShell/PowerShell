# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
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

                $ps.Streams.Error.Count | Should -Be 1 # the host does not implement it.
                $ps.InvocationStateInfo.State | Should -BeExactly 'Completed'
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
                $prompt | Should -Not -BeNullOrEmpty
                $result = $prompt.split(":")
                $result[0] | Should -Match 'get-foo'
                $result[-1] | Should -BeExactly 'a'
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

        get-foo a | Should -BeExactly 'a'
        get-foo -a b | Should -BeExactly 'b'
    }

    It 'Positional parameters when only one position specified: position = 1' {
        function get-foo
        {
            param([Parameter(position=1)] $a )
            $a
        }

        get-foo b | Should -BeExactly 'b'
    }

    It 'Positional parameters when only position specified: position = 2' {
        function get-foo
        {
            param([Parameter(position=2)] $a )
            $a
        }

        get-foo b | Should -BeExactly 'b'
    }

    It 'Multiple positional parameters case 1' {
        function get-foo
        {
            param( [Parameter(position=1)] $a,
                   [Parameter(position=2)] $b )
            $a; $b
        }

        ( get-foo c d ) -join ',' | Should -BeExactly 'c,d'
        ( get-foo -a c d ) -join ',' | Should -BeExactly 'c,d'
        ( get-foo -a c -b d ) -join ',' | Should -BeExactly 'c,d'
        ( get-foo -b d c ) -join ',' | Should -BeExactly 'c,d'
        ( get-foo c -b d ) -join ',' | Should -BeExactly 'c,d'
    }

    It 'Multiple positional parameters case 2:  the parameters are put in different order?' {
        function get-foo
        {
            # the parameters are purposefully out of order.
            param( [Parameter(position=2)] $a,
                   [Parameter(position=1)] $b )
            $a; $b
        }

        (get-foo c d) -join ',' | Should -BeExactly 'd,c'
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

        (1..10 | get-foo) -join ',' | Should -BeExactly '2,4,6,8,10'
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

        $b = 1..10 | Select-Object @{name='foo'; expression={$_ * 10}} | get-foo
        $b -join ',' | Should -BeExactly '10,20,30,40,50,60,70,80,90,100'
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

        ( get-foo a b c d ) -join ',' | Should -BeExactly 'b,c,d'
        ( get-foo a b -a c d ) -join ',' | Should -BeExactly 'a,b,d'
        ( get-foo a b -a c -q d ) -join ',' | Should -BeExactly 'a,b,-q,d'
    }

    It 'Multiple parameter sets with Value from remaining arguments' {
        function get-foo
        {
            param( [Parameter(parametersetname='set1',position=1)] $a,
                   [Parameter(parametersetname='set2',position=1)] $b,
                   [parameter(valuefromremainingarguments=$true)] $foo )
            $foo
        }

        { get-foo -a a -b b c d } | Should -Throw -ErrorId 'AmbiguousParameterSet,get-foo'
        ( get-foo -a a b c d ) -join ',' | Should -BeExactly 'b,c,d'
        ( get-foo -b b a c d ) -join ',' | Should -BeExactly 'a,c,d'
    }

    It 'Too many parameter sets defined' {
        $scriptblock = {
            param($numSets=1)
            $parameters = (1..($numSets) | ForEach-Object { "[Parameter(parametersetname='set$_')]`$a$_" }) -join ', '
            $body = "param($parameters) 'working'"
            $sb = [scriptblock]::Create($body)
            & $sb -a1 123
        }

        & $scriptblock -numSets 32 | Should -Be 'working'
        { & $scriptblock -numSets 33 } | Should -Throw -ErrorId 'ParsingTooManyParameterSets'
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
        $x | Should -BeExactly 'a'
        $y | Should -BeNullOrEmpty
        $z -join ',' | Should -BeExactly 'b,c,d'
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

        $x | Should -BeNullOrEmpty
        $y | Should -BeExactly 'a'
        $z -join ',' | Should -BeExactly 'b,c,d'
    }

    It 'Alias are specified for parameters' {
        function get-foo
        {
            param( [Parameter(mandatory=$true)][alias("foo", "bar")] $a )
            $a
        }

        get-foo -foo b | Should -BeExactly 'b'
    }

    It 'Invoking with script block' {
        $foo = . { param([Parameter(position=2)] $a, [Parameter(position=1)]$b); $a; $b} a b
        $foo[0] | Should -BeExactly 'b'
    }

    It 'Normal functions' {
        function foo ($a, $b) {$b, $a}
        ( foo a b ) -join ',' | Should -BeExactly 'b,a'
    }

    It 'Null is not Allowed when AllowNull attribute is not set' {
        function get-foo
        {
            param( [Parameter(mandatory=$true)] $a )
            $a
        }

        { get-foo -a $null } | Should -Throw -ErrorId 'ParameterArgumentValidationErrorNullNotAllowed,get-foo'

    }

    It 'Null is allowed when Allownull attribute is set' {
        function get-foo
        {
            param( [Parameter(mandatory=$true)][allownull()] $a )
            $a
        }

        (get-foo -a $null) | Should -BeNullOrEmpty

    }

    It 'Empty string is not allowed AllowEmptyString Attribute is not set' {
        function get-foo
        {
            param( [Parameter(mandatory=$true)][string] $a )
            $a
        }

        { get-foo -a '' } | Should -Throw -ErrorId 'ParameterArgumentValidationErrorEmptyStringNotAllowed,get-foo'
    }

    It 'Empty string is allowed when AllowEmptyString Attribute is set' {
        function get-foo
        {
            param( [Parameter(mandatory=$true)][allowemptystring()][string] $a )
            $a
        }

        get-foo -a '' | Should -BeExactly ''
    }

    It 'Empty collection is not allowed when AllowEmptyCollection it not set' {
        function get-foo
        {
            param( [Parameter(mandatory=$true)][string[]] $a )
            $a
        }

        { get-foo -a @() } | Should -Throw -ErrorId 'ParameterArgumentValidationErrorEmptyArrayNotAllowed,get-foo'
    }

    It 'Empty collection is allowed when allowEmptyCollection is set' {
        function get-foo
        {
            param( [Parameter(mandatory=$true)][allowemptycollection()][string[]] $a )
            $a
        }

        get-foo -a @() | Should -BeNullOrEmpty
    }

    It 'Unspecified non-mandatory bool should not cause exception' {
        function get-foo
        {
            param([Parameter(Mandatory=$true, Position=0, ValueFromPipeline=$true)] $a,
                  [System.Boolean] $b)
            $a
        }

        42 | get-foo | Should -Be 42
    }

    It 'Parameter binding failure on Parameter1 should not cause parameter binding failure on Length' {
        function get-foo
        {
          param( [Parameter(ValueFromPipeline = $true)] [int] $Parameter1 = 10,
                 [Parameter(ValueFromPipelineByPropertyName = $true)] [int] $Length = 100 )
          process  { $Length }
        }

        'abc' | get-foo | Should -Be 3
    }

    It 'Binding array of string to array of bool should fail (cmdletbinding)' {
        function get-foo
        {
           [cmdletbinding()]
           param ([bool[]] $Parameter )
           $Parameter
        }

        { get-foo 'a','b' } | Should -Throw -ErrorId 'ParameterArgumentTransformationError,get-foo'
    }

    It "Binding array of string to array of bool should succeed" {
        function get-foo
        {
           param ([bool[]] $Parameter)
           $Parameter
        }

        $x = get-foo 'a','b'
        $x[0] | Should -BeTrue
        $x[1] | Should -BeTrue
    }

    Context 'Default value conversion tests' {
        It 'Parameter default value is converted correctly to the proper type when nothing is set on parameter' {
            function get-fooa
            {
                param( [System.Reflection.MemberTypes] $memberTypes = $([Enum]::GetNames("System.Reflection.MemberTypes") -join ",") )
                $memberTypes | Should -BeOfType System.Reflection.MemberTypes
            }

            get-fooa
        }

        It "Parameter default value is converted correctly to the proper type when CmdletBinding is set on param" {
            function get-foob
            {
                [CmdletBinding()]
                param( [System.Reflection.MemberTypes] $memberTypes = $([Enum]::GetNames("System.Reflection.MemberTypes") -join ",") )
                $memberTypes | Should -BeOfType System.Reflection.MemberTypes
            }

            get-foob
        }

        It "No default value specified should not cause error when parameter attribute is set on the parameter" {
            function get-fooc
            {
                param( [Parameter()] [System.Reflection.MemberTypes] $memberTypes )
                $memberTypes | Should -BeNullOrEmpty
            }

            get-fooc
        }

        It "No default value specified should not cause error when nothing is set on parameter" {
            function get-food
            {
                param( [System.Reflection.MemberTypes] $memberTypes )
                $memberTypes | Should -BeNullOrEmpty
            }

            get-food
        }

        It "Validation attributes should not run on default values when nothing is set on the parameter" {
            function get-fooe
            {
                param([ValidateRange(1,42)] $p = 55)
                $p
            }

            get-fooe | Should -Be 55
        }

        It "Validation attributes should not run on default values when CmdletBinding is set on the parameter" {
            function get-foof
            {
                [CmdletBinding()]
                param([ValidateRange(1,42)] $p = 55)
                $p
            }

            get-foof | Should -Be 55
        }

        It "Validation attributes should not run on default values" {
            function get-foog
            {
                param([ValidateRange(1,42)] $p)
                $p
            }

            { get-foog } | Should -Not -Throw
        }

        It "Validation attributes should not run on default values when CmdletBinding is set" {
            function get-fooh
            {
                [CmdletBinding()]
                param([ValidateRange(1,42)] $p)
                $p
            }

            { get-fooh } | Should -Not -Throw
        }

        It "ValidateScript can use custom ErrorMessage" {
            function get-fooi {
                [CmdletBinding()]
                param([ValidateScript({$_ -gt 2}, ErrorMessage = "Item '{0}' failed '{1}' validation")] $p)
                $p
            }

            $err = { get-fooi -p 2 } | Should -Throw -ErrorId 'ParameterArgumentValidationError,get-fooi' -PassThru
            $err.Exception.Message | Should -BeExactly "Cannot validate argument on parameter 'p'. Item '2' failed '`$_ -gt 2' validation"
        }

        It "ValidatePattern can use custom ErrorMessage" {
            function get-fooj
            {
                [CmdletBinding()]
                param([ValidatePattern("\s+", ErrorMessage = "Item '{0}' failed '{1}' regex")] $p)
                $p
            }

            $err = { get-fooj -p 2 } | Should -Throw -ErrorId 'ParameterArgumentValidationError,get-fooj' -PassThru
            $err.Exception.Message | Should -BeExactly "Cannot validate argument on parameter 'p'. Item '2' failed '\s+' regex"
        }

        It "ValidateSet can use custom ErrorMessage" {
            function get-fook
            {
                param([ValidateSet('A', 'B', 'C', IgnoreCase=$false, ErrorMessage="Item '{0}' is not in '{1}'")] $p)
            }

            $err = { get-fook -p 2 } | Should -Throw -ErrorId 'ParameterArgumentValidationError,get-fook' -PassThru
            $set = 'A','B','C' -join [Globalization.CultureInfo]::CurrentUICulture.TextInfo.ListSeparator
            $err.Exception.Message | Should -BeExactly "Cannot validate argument on parameter 'p'. Item '2' is not in '$set'"
        }

    }

    #known issue 2069
    It 'Some conversions should be attempted before trying to encode a collection' -Skip:$IsCoreCLR {
        try {
                 $null = [Test.Language.ParameterBinding.MyClass]
            }
            catch {
                Add-Type -PassThru -TypeDefinition @'
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
'@ | ForEach-Object {$_.assembly} | Import-Module
            }

        Get-TestCmdlet -MyParameter @{ a = 42 } | Should -BeExactly 'hashtable'
    }

    It 'Parameter passing is consuming enumerators' {
        $a = 1..4
        $b = $a.getenumerator()
        $null = $b.MoveNext()
        $null = $b.current
        & { } $b

        #The position of the enumerator shouldn't be modified
        $b.current | Should -Be 1
    }
}
