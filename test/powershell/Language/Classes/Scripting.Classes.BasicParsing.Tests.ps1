# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Positive Parse Properties Tests' -Tags "CI" {
    It 'PositiveParsePropertiesTest' {
        # Just a bunch of random basic things here
        # This test doesn't need to check anything, if there are
        # any parse errors, the entire suite will fail because the
        # script will fail to parse.

        # No members
        class C1 {}

        # Simple field
        class C2 { $x; }

        # Simple typed field
        class C3 { [int] $x; }

        # Multiple fields, one line, last w/o semicolon
        class C4 { $x; $y }

        # Multiple fields, multiple lines
        class C5
        {
            $x
            $y
        }

        # Static field
        class C6 { static $x; }

        # Static field w/ type - order doesn't matter
        class C7a { static [hashtable] $x; }
        class C7b { [hashtable] static $x; }

        # Field using type defined in this scope
        class C8a { [C1] $c1; }
        class C8b { [c1] $c1; }

        # Field referring to self type
        class C9 { [C9] $c9; }

        # Hidden fields
        class C10a { hidden $x }
        class C10b { hidden [int] $x }
        class C10c { hidden static $x; static hidden $y }
        class C10d { hidden static [int] $x; static hidden [int] $y }
    }

    It 'Positive Parse Methods Tests' {
        # Just a bunch of random basic things here
        # This test doesn't need to check anything, if there are
        # any parse errors, the entire suite will fail because the
        # script will fail to parse.

        # No members
        class C1 {}

        # Simple method
        class C2 { f() {} }

        # Simple method with return type
        class C3 { [int] f() { return 1 } }

        # Multiple methods, one line
        class C4 { f() {} f1() {} }

        # Multiple methods w/ overloads
        class C5
        {
            f1() {}
            f1($a) {}
        }

        # Static method
        class C6 { static f() {} }

        # Static method w/ return type
        class C7 { static [hashtable] f1() { return @{} } }

        # Method using return type defined in this scope
        class C8a { [C1] f1() { return [C1]::new() } }
        class C8b { [c1] f1() { return [c1]::new() } }

        # Hidden methods
        class C10a { hidden F() { } }
        class C10b { hidden [void] F() { } }
        class C10c { hidden static F() { } static hidden G() { } }
        class C10d { hidden static [void] F() { } static hidden [void] G() { } }

         # return analysis
        class C11a { [int]foo() { try {throw "foo"} catch {throw $_} } }
        class C11b { [int]foo() { try {throw "foo"} finally {}; try {} catch {} } }
        class C11c { [int]foo() { try {throw "foo"} catch [ArgumentException] {throw $_} catch {throw $_} } }
        class C11d { [int]foo() { try {if (1 -eq 2) { throw "1"} else {throw "2"}} finally {} } }
        class C11e { [int]foo() { try {throw "foo"} catch [ArgumentException] {throw $_} catch {return 1} } }
        class C11f { [int]foo() { try {} finally {throw "bar"} } }
        class C11g { [int]foo() { do { return 1 } while ($false) } }
        class C11h { [int]foo() { try {throw "foo"} finally {} } }

        # local variables
        class C12a { static f() { [bigint]$foo = 42 } }
        class C12b { [void] f() { [bigint]$foo = 42 } }
        class C12c { [void] f() { [System.Management.Automation.Host.Rectangle]$foo = [System.Management.Automation.Host.Rectangle]::new(0, 0, 0, 0) } }
    }

    context "Positive ParseMethods return type Test" {
        # Method with return type of self
        class C9 { [C9] f() { return [C9]::new() } }
        $c9 = [C9]::new().f()
        It "Expected a C9 returned" { $c9.GetType().Name | Should -Be C9 }
        class C9a { [C9a[]] f() { return [C9a]::new() } }
        $c9a = [C9a]::new().f()
        It "Expected a C9a[] returned" { $c9a.GetType().Name | Should -Be C9a[] }
        class C9b { [System.Collections.Generic.List[C9b]] f() { return [C9b]::new() } }
        $c9b = [C9b]::new().f()
        It "Expected a System.Collections.Generic.List[C9b] returned" {  $c9b -is [System.Collections.Generic.List[C9b]] | Should -BeTrue }
        It 'Methods returning object should return $null if no output was produced' {
            class Foo {
                [object] Bar1() { return & {} }
                static [object] Bar2() { return & {} }
            }
            # Test instance method
            [Foo]::new().Bar1() | Should -BeNullOrEmpty
            # Test static method
            [foo]::Bar2() | Should -BeNullOrEmpty
        }
    }

    It 'Positive ParseProperty Attributes Test' {
        class C1a { [ValidateNotNull()][int]$x; }
        class C1b
        {
            [ValidateNotNull()]
            [int]
            $x
        }
        class C1c
        {
            [ValidateNotNull()]
            [int]$x
        }
        class C1d
        {
            [ValidateNotNull()][int]
            $x
        }
    }

    It 'PositiveParseMethodAttributesTest' {
        class C1a { [Obsolete()][int] f() { return 0 } }
        class C1b
        {
            [Obsolete()]
            [int]
            f() { return 1 }
        }
        class C1c
        {
            [Obsolete("Message")]
            [int] f() { return 0 }
        }
        class C1d
        {
            [Obsolete()][int]
            f(){ return -1 }
        }
    }

    It 'Class Method Reference ConstantVars' {
        class C1
        {
            [bool] f1() { return $true }
            [bool] f2() { return $false }
            [object] f3() { return $null }
        }
    }

    It 'Positive Parsing Of DscResource' {
        [DscResource()]
        class C1
        {
            [DscProperty(Key)][string]$Key
            [bool] Test() { return $false }
            [C1] Get() { return $this }
            Set() {}
        }

        [DscResource()]
        class C2
        {
            [DscProperty(Key)][int]$Key = 1
            [bool] Test() { return $false }
            [C2] Get() { return $this }
            Set() {}
        }

        [DscResource()]
        class C4
        {
            [DscProperty(Key)][byte]$Key=1
            C4() { }
            C4($a) { }
            [bool] Test() { return $false }
            [C4] Get() { return $this }
            Set() {}
        }

        [DscResource()]
        class C5
        {
            [DscProperty(Key)][int]$Key = 1
            C5() { }
            static C5() { }
            C5($a) { }
            [bool] Test() { return $false }
            [C5] Get() { return $this }
            Set() {}
        }
    }

    It 'allows some useful implicit variables inside methods' {
        class C {
            [void] m()
            {
                $LASTEXITCODE
                $lastexitcode
                '111' -Match '1'
                $Matches
                $mAtches
                $Error[0]
                $error
                $pwd
                foreach ($i in 1..10) {$foreach}
                switch ($i)
                {
                    '1' {
                        $switch
                    }
                }
            }
        }
    }

    It 'allowes [ordered] attribute inside methods' {
        class A
        {
            $h
            A()
            {
                $this.h = [ordered] @{}
            }
        }
        [A]::new().h.GetType().Name | Should -BeExactly 'OrderedDictionary'
    }
}

Describe 'Negative Parsing Tests' -Tags "CI" {
    ShouldBeParseError 'class' MissingNameAfterKeyword 5
    ShouldBeParseError 'class foo' MissingTypeBody 9
    ShouldBeParseError 'class foo {' MissingEndCurlyBrace 11
    ShouldBeParseError 'class foo { [int] }' IncompleteMemberDefinition 17
    ShouldBeParseError 'class foo { $private: }' InvalidVariableReference 12
    ShouldBeParseError 'class foo { [int]$global: }' InvalidVariableReference 17
    ShouldBeParseError 'class foo { [Attr()] }' IncompleteMemberDefinition 20
    ShouldBeParseError 'class foo {} class foo {}' MemberAlreadyDefined 13
    ShouldBeParseError 'class foo { $x; $x; }' MemberAlreadyDefined 16 -SkipAndCheckRuntimeError
    ShouldBeParseError 'class foo { [int][string]$x; }' TooManyTypes 17
    ShouldBeParseError 'class foo { static static $x; }' DuplicateQualifier 19
    ShouldBeParseError 'class foo { [zz]$x; }' TypeNotFound 13
    ShouldBeParseError 'class foo { [zz]f() { return 0 } }' TypeNotFound 13
    ShouldBeParseError 'class foo { f([zz]$x) {} }' TypeNotFound 15

    ShouldBeParseError 'class C {} class C {}' MemberAlreadyDefined 11
    ShouldBeParseError 'class C { f(){} f(){} }' MemberAlreadyDefined 16 -SkipAndCheckRuntimeError
    ShouldBeParseError 'class C { F(){} F($o){} [int] F($o) {return 1} }' MemberAlreadyDefined 24 -SkipAndCheckRuntimeError
    ShouldBeParseError 'class C { f(){} f($a){} f(){} }' MemberAlreadyDefined 24 -SkipAndCheckRuntimeError
    ShouldBeParseError 'class C { f([int]$a){} f([int]$b){} }' MemberAlreadyDefined 23 -SkipAndCheckRuntimeError
    ShouldBeParseError 'class C { $x; [int]$x; }' MemberAlreadyDefined 14 -SkipAndCheckRuntimeError
    ShouldBeParseError 'class C { static C($x) {} }' StaticConstructorCantHaveParameters 19 -SkipAndCheckRuntimeError
    ShouldBeParseError 'class C { static C([int]$x = 100) {} }' StaticConstructorCantHaveParameters 19 -SkipAndCheckRuntimeError

    ShouldBeParseError 'class C {f(){ return 1} }' VoidMethodHasReturn 14
    ShouldBeParseError 'class C {[int] f(){ return } }' NonVoidMethodMissingReturnValue 20
    ShouldBeParseError 'class C {[int] f(){} }' MethodHasCodePathNotReturn 15
    ShouldBeParseError 'class C {f(){ $x=1; if($x -lt 2){ return } elseif($x -gt 0 ) {return 1} else{return 2} return 3 } }' @("VoidMethodHasReturn", "VoidMethodHasReturn", "VoidMethodHasReturn") @(62,77,87)

    ShouldBeParseError 'class foo { [int] bar() { $y = $z; return $y} }' VariableNotLocal 31
    ShouldBeParseError 'class foo { bar() { foreach ($zz in $yy) { } } }' VariableNotLocal 36
    ShouldBeParseError 'class foo { bar() { foreach ($zz in $global:yy) { $abc = $zzzzz } } }' VariableNotLocal 57
    ShouldBeParseError 'class foo { bar() { try { $zz = 42 } finally { } $zz } }' VariableNotLocal 49
    ShouldBeParseError 'class foo { bar() { try { $zz = 42 } catch { } $zz } }' VariableNotLocal 47
    ShouldBeParseError 'class foo { bar() { switch (@()) { default { $aa = 42 } } $aa } }' VariableNotLocal 58
    ShouldBeParseError 'class C { $x; static bar() { $this.x = 1 } }' NonStaticMemberAccessInStaticMember 29
    ShouldBeParseError 'class C { $x; static $y = $this.x }' NonStaticMemberAccessInStaticMember 26

    ShouldBeParseError 'class C { [void]foo() { try { throw "foo"} finally { return } } }' ControlLeavingFinally 53
    ShouldBeParseError 'class C { [int]foo() { return; return 1 } }' NonVoidMethodMissingReturnValue 23
    ShouldBeParseError 'class C { [int]foo() { try { throw "foo"} catch { } } }' MethodHasCodePathNotReturn 15
    ShouldBeParseError 'class C { [int]foo() { try { throw "foo"} catch [ArgumentException] {} catch {throw $_} } }' MethodHasCodePathNotReturn 15
    ShouldBeParseError 'class C { [int]foo() { try { throw "foo"} catch [ArgumentException] {return 1} catch {} } }' MethodHasCodePathNotReturn 15
    ShouldBeParseError 'class C { [int]foo() { while ($false) { return 1 } } }' MethodHasCodePathNotReturn 15
    ShouldBeParseError 'class C { [int]foo() { try { New-Item -ItemType Directory foo } finally { rm -rec foo } } }' MethodHasCodePathNotReturn 15
    ShouldBeParseError 'class C { [int]foo() { try { New-Item -ItemType Directory foo; return 1 } catch { } } }' MethodHasCodePathNotReturn 15
    ShouldBeParseError 'class C { [bool] Test() { if ($false) { return $true; } } }' MethodHasCodePathNotReturn 17

    ShouldBeParseError 'class C { [int]$i; [void] foo() {$i = 10} }' MissingThis 33
    ShouldBeParseError 'class C { static [int]$i; [void] foo() {$i = 10} }' MissingTypeInStaticPropertyAssignment 40

    ShouldBeParseError 'class C : B' MissingTypeBody 11

    ShouldBeParseError 'Class foo { q(){} w(){}' MissingEndCurlyBrace 11
}

Describe 'Negative methods Tests' -Tags "CI" {
    ShouldBeParseError 'class foo { f() { param($x) } }' ParamBlockNotAllowedInMethod 18
    ShouldBeParseError 'class foo { f() { dynamicparam {} } }' NamedBlockNotAllowedInMethod 18
    ShouldBeParseError 'class foo { f() { begin {} } }' NamedBlockNotAllowedInMethod 18
    ShouldBeParseError 'class foo { f() { process {} } }' NamedBlockNotAllowedInMethod 18
    ShouldBeParseError 'class foo { f() { end {} } }' NamedBlockNotAllowedInMethod 18
    ShouldBeParseError 'class foo { f([Parameter()]$a) {} }' AttributeNotAllowedOnDeclaration 14
    ShouldBeParseError 'class foo { [int] foo() { return 1 }}' ConstructorCantHaveReturnType 12
    ShouldBeParseError 'class foo { [void] bar($a, [string][int]$b, $c) {} }' MultipleTypeConstraintsOnMethodParam 35
}

Describe 'Negative Assignment Tests' -Tags "CI" {
    ShouldBeParseError 'class foo { [string ]$path; f() { $path="" } }' MissingThis 34
    ShouldBeParseError 'class foo { [string ]$path; f() { [string] $path="" } }' MissingThis 43
    ShouldBeParseError 'class foo { [string ]$path; f() { [int] [string] $path="" } }' MissingThis 49
}

Describe 'Negative Assignment Tests' -Tags "CI" {
    ShouldBeParseError '[DscResource()]class C { [bool] Test() { return $false } [C] Get() { return $this } Set() {} }' DscResourceMissingKeyProperty 0

    # Test method
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [C] Get() { return $this } Set() {} }' DscResourceMissingTestMethod 0
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [C] Get() { return $this } Set() {} Test() { } }' DscResourceMissingTestMethod 0
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [C] Get() { return $this } Set() {} [int] Test() { return 1 } }' DscResourceMissingTestMethod 0
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [C] Get() { return $this } Set() {} [bool] Test($a) { return $false } }' DscResourceMissingTestMethod 0

    # Get method
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [bool] Test() { return $false } Set() {} }' DscResourceMissingGetMethod 0
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [bool] Test() { return $false } Set() {} Get() { } }' DscResourceInvalidGetMethod 98
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [bool] Test() { return $false } Set() {} [int] Get() { return 1 } }' DscResourceInvalidGetMethod 98
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [bool] Test() { return $false } Set() {} [C] Get($a) { return $this } }' DscResourceMissingGetMethod 0

    # Set method
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [bool] Test() { return $false } [C] Get() { return $this } }' DscResourceMissingSetMethod 0
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [bool] Test() { return $false } [C] Get() { return $this } [int] Set() { return 1 } }' DscResourceMissingSetMethod 0
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [bool] Test() { return $false } [C] Get() { return $this } Set($a) { } }' DscResourceMissingSetMethod 0

    # Default ctor
    ShouldBeParseError '[DscResource()]class C { [DscProperty(Key)][string]$Key; [bool] Test() { return $false } [C] Get() { return $this } Set() {} C($a) { } }' DscResourceMissingDefaultConstructor 0
}

Describe 'Negative DscResources Tests' -Tags "CI" {
        # Usage errors
        ShouldBeParseError '[Flags()]class C{}' AttributeNotAllowedOnDeclaration 0
        ShouldBeParseError 'class C { [Flags()]$field; }' AttributeNotAllowedOnDeclaration 10
        ShouldBeParseError 'class C { [Flags()]foo(){} }' AttributeNotAllowedOnDeclaration 10

        # Errors related to construction of the attribute
        ShouldBeParseError '[UnknownAttr()]class C{}' CustomAttributeTypeNotFound 1
        ShouldBeParseError '[System.Management.Automation.Cmdlet()]class C{}' MethodCountCouldNotFindBest 0 -SkipAndCheckRuntimeError
        ShouldBeParseError '[System.Management.Automation.Cmdlet("zz")]class C{}' MethodCountCouldNotFindBest 0 -SkipAndCheckRuntimeError
        ShouldBeParseError '[System.Management.Automation.Cmdlet("Get", "Thing", Prop=1)]class C{}' PropertyNotFoundForAttribute 53
        ShouldBeParseError '[System.Management.Automation.Cmdlet("Get", "Thing", ConfirmImpact="foo")]class C{}' CannotConvertValue 67 -SkipAndCheckRuntimeError
        ShouldBeParseError '[System.Management.Automation.Cmdlet("Get", "Thing", NounName="foo")]class C{}' ReadOnlyProperty 53
        ShouldBeParseError '[System.Management.Automation.Cmdlet("Get", "Thing", ConfirmImpact=$zed)]class C{}' ParameterAttributeArgumentNeedsToBeConstant 67
        ShouldBeParseError 'class C{ [ValidateScript({})]$p; }' ParameterAttributeArgumentNeedsToBeConstant 25
}

Describe 'Negative ClassAttributes Tests' -Tags "CI" {
    [System.Management.Automation.Cmdlet("Get", "Thing")]class C{}
    $t = [C].GetCustomAttributes($false)

    It "Should have one attribute (class C)" {$t.Count | Should -Be 1}
    It "Should have instance of CmdletAttribute (class C)" {$t[0] | Should -BeOfType System.Management.Automation.CmdletAttribute }

    [System.Management.Automation.CmdletAttribute]$c = $t[0]
    It "Verb should be Get (class C)" {$c.VerbName | Should -BeExactly 'Get'}
    It "Noun should be Thing (class C)" {$c.NounName | Should -BeExactly 'Thing'}

    [System.Management.Automation.Cmdlet("Get", "Thing", SupportsShouldProcess = $true, SupportsPaging = $true)]class C2{}
    $t = [C2].GetCustomAttributes($false)
    It "Should have one attribute (class C2)" { $t.Count | should -Be 1 }
    It "Should have instance of CmdletAttribute (class C2)" { $t[0] | Should -BeOfType System.Management.Automation.CmdletAttribute }
    [System.Management.Automation.CmdletAttribute]$c = $t[0]
    It "Verb should be Get (class C2)" {$c.VerbName | Should -BeExactly 'Get'}
    It "Noun should be Thing (class C2)" {$c.NounName | Should -BeExactly 'Thing'}

    It  "SupportsShouldProcess should be $true" { $c.SupportsShouldProcess | Should -BeTrue }
    It  "SupportsPaging should be `$true" { $c.SupportsPaging | Should -BeTrue }
    Context "Support ConfirmImpact as an attribute" {
        It  "ConfirmImpact should be high" {
            [System.Management.Automation.Cmdlet("Get", "Thing", SupportsShouldProcess = $true, ConfirmImpact = 'High', SupportsPaging = $true)]class C3{}
            $t = [C3].GetCustomAttributes($false)
            $t.Count | Should -Be 1
            $t[0] | Should -BeOfType System.Management.Automation.CmdletAttribute
            [System.Management.Automation.CmdletAttribute]$c = $t[0]
            $c.ConfirmImpact | Should -BeExactly 'High'

        }
    }
}

Describe 'Property Attributes Test' -Tags "CI" {
        class C { [ValidateSet('a', 'b')]$p; }

        $t = [C].GetProperty('p').GetCustomAttributes($false)
        It "Should have one attribute" { $t.Count | Should -Be 1 }
        [ValidateSet]$v = $t[0]
        It "Should have 2 valid values" { $v.ValidValues.Count | Should -Be 2 }
        It "first value should be a" { $v.ValidValues[0] | Should -Be 'a' }
        It "second value should be b" { $v.ValidValues[1] | Should -Be 'b' }
}

Describe 'Method Attributes Test' -Tags "CI" {
        class C { [Obsolete("aaa")][int]f() { return 1 } }

        $t = [C].GetMethod('f').GetCustomAttributes($false)
        It "Should have one attribute" {$t.Count | Should -Be 1 }
        It "Attribute type should be ObsoleteAttribute" { $t[0].GetType().FullName | Should -Be System.ObsoleteAttribute }
}

Describe 'Positive SelfClass Type As Parameter Test' -Tags "CI" {
        class Point
        {
            Point($x, $y) { $this.x = $x; $this.y = $y }
            Point() {}

            [int] $x = 0
            [int] $y = 0
            Add([Point] $val) {  $this.x += $val.x; $this.y += $val.y;  }

            Print() { Write-Host "[`$x=$($this.x) `$y=$($this.y)]" }
            Set($x, $y) { $this.x = $x; $this.y = $y }
        }
        It  "[Point]::Add works construction via ::new" {
            $point = [Point]::new(100,200)
            $point2 = [Point]::new(1,2)
            $point.Add($point2)

            $point.x | Should -Be 101
            $point.y | Should -Be 202
        }

        It  "[Point]::Add works construction via new-object" {
            $point = New-Object Point 100,200
            $point2 = New-Object Point 1,2
            $point.Add($point2)

            $point.x | Should -Be 101
            $point.y | Should -Be 202
        }
}

Describe 'PositiveReturnSelfClassTypeFromMemberFunction Test' -Tags "CI" {
        class ReturnObjectFromMemberFunctionTest
        {
            [ReturnObjectFromMemberFunctionTest] CreateInstance()
            {
              return [ReturnObjectFromMemberFunctionTest]::new()
            }
            [string] SayHello()
            {
                return "Hello1"
            }
        }
        $f = [ReturnObjectFromMemberFunctionTest]::new()
        $z = $f.CreateInstance() # Line 13
        It "CreateInstance works" { $z.SayHello() | Should -BeExactly 'Hello1' }
}

Describe 'TestMultipleArguments Test' -Tags "CI" {
        if ( $IsCoreCLR ) { $maxCount = 14 } else { $maxCount = 16 }
        for ($i = 0; $i -lt $maxCount; $i++)
        {
            $properties = $(for ($j = 0; $j -le $i; $j++) {
                "        [int]`$Prop$j"
            }) -join "`n"

            $methodParameters = $(for ($j = 0; $j -le $i; $j++) {
                "[int]`$arg$j"
            }) -join ", "

            $ctorAssignments = $(for ($j = 0; $j -le $i; $j++) {
                "            `$this.Prop$j = `$arg$j"
            }) -join "`n"

            $methodReturnValue = $(for ($j = 0; $j -le $i; $j++) {
                "`$arg$j"
            }) -join " + "

            $methodArguments =  $(for ($j = 0; $j -le $i; $j++) {
                $j
            }) -join ", "

            $addUpProperties =  $(for ($j = 0; $j -le $i; $j++) {
                "`$inst.`Prop$j"
            }) -join " + "

            $expectedTotal = (0..$i | Measure-Object -Sum).Sum

            $class = @"
    class Foo
    {
$properties

        Foo($methodParameters)
        {
$ctorAssignments
        }

        [int] DoSomething($methodParameters)
        {
            return $methodReturnValue
        }
    }

    `$inst = [Foo]::new($methodArguments)
    `$sum = $addUpProperties
    It "ExpectedTotal: Sum should be $expectedTotal" { `$sum | Should -Be $expectedTotal }
    It "ExpectedTotal: Invocation should return $expectedTotal" { `$inst.DoSomething($methodArguments) | Should -Be $expectedTotal }
"@

            Invoke-Expression $class
        }
}

Describe 'Scopes Test' -Tags "CI" {
        class C1
        {
            static C1() {
                $global:foo = $script:foo
            }
            C1() {
                $script:bar = $global:foo
            }
            static [int] f1() {
                return $script:bar + $global:bar
            }
            [int] f2() {
                return $script:bar + $global:bar
            }
        }
}

Describe 'Check PS Class Assembly Test' -Tags "CI" {
        class C1 {}
        $assem = [C1].Assembly
        $attrs = @($assem.GetCustomAttributes($true))
        $expectedAttr = @($attrs | Where-Object { $_  -is [System.Management.Automation.DynamicClassImplementationAssemblyAttribute] })
        It "Expected a DynamicClassImplementationAssembly attribute" { $expectedAttr.Length | Should -Be 1}
}

Describe 'ScriptScopeAccessFromClassMethod' -Tags "CI" {
        Import-Module "$PSScriptRoot\MSFT_778492.psm1"
        try
        {
            $c = Get-MSFT_778492
            It "Method should have found variable in module scope" { $c.F() | Should -BeExactly 'MSFT_778492 script scope'}
        }
        finally
        {
            Remove-Module MSFT_778492
        }
}

Describe 'Hidden Members Test ' -Tags "CI" {
        class C1
        {
            [int]$visibleX
            [int]$visibleY
            hidden [int]$hiddenZ
        }

        # Create an instance
        $instance = [C1]@{ visibleX = 10; visibleY = 12; hiddenZ = 42 }

        It "Access hidden property should still work" { $instance.hiddenZ | Should -Be 42 }

        It "Table formatting should not include hidden member hiddenZ" {
            $expectedTable = @"

visibleX visibleY
-------- --------
      10       12


"@

            $tableOutput = $instance | Format-Table -AutoSize | Out-String
            $tableOutput.Replace("`r","") | Should -BeExactly $expectedTable.Replace("`r","")
        }

        # Get-Member should not include hidden members by default
        $member = $instance | Get-Member hiddenZ
        it "Get-Member should not find hidden member w/o -Force" { $member | Should -BeNullOrEmpty }

        # Get-Member should include hidden members with -Force
        $member = $instance | Get-Member hiddenZ -Force
        It "Get-Member should find hidden member w/ -Force" { $member | Should -Not -BeNullOrEmpty }

        # Tab completion should not return a hidden member
        $line = 'class C2 { hidden [int]$hiddenZ } [C2]::new().h'
        $completions = [System.Management.Automation.CommandCompletion]::CompleteInput($line, $line.Length, $null)
        It "Tab completion should not return a hidden member" { $completions.CompletionMatches.Count | Should -Be 0 }
}

Describe 'BaseMethodCall Test ' -Tags "CI" {
        It "Derived class method call" {"abc".ToString() | Should -BeExactly "abc" }
        # call [object] ToString() method as a base class method.
        It "Base class method call" {([object]"abc").ToString() | Should -BeExactly "System.String" }
}

Describe 'Scoped Types Test' -Tags "CI" {
        class C1 { [string] GetContext() { return "Test scope" } }

        filter f1
        {
            class C1 { [string] GetContext() { return "f1 scope" } }

            return [C1]::new().GetContext()
        }

        filter f2
        {
            class C1 { [string] GetContext() { return "f2 scope" } }

            return (new-object C1).GetContext()
        }

        It "New-Object at test scope" { (new-object C1).GetContext() | Should -BeExactly "Test scope" }
        It "[C1]::new() at test scope" { [C1]::new().GetContext() | Should -BeExactly "Test scope" }

        It "[C1]::new() in nested scope" { (f1) | Should -BeExactly "f1 scope" }
        It "'new-object C1' in nested scope" { (f2) | Should -BeExactly "f2 scope" }

        It "[C1]::new() in nested scope (in pipeline)" { (1 | f1 | f2 | f1) | Should -BeExactly "f1 scope" }
        It "'new-object C1' in nested scope (in pipeline)" { (1 | f2 | f1 | f2) | Should -BeExactly "f2 scope" }
}

Describe 'ParameterOfClassTypeInModule Test' -Tags "CI" {
        try
        {
            $sb = [scriptblock]::Create(@'
enum EE {one = 1}
function test-it([EE]$ee){$ee}
'@)
            $mod = New-Module $sb -Name MSFT_2081529 | Import-Module
            $result = test-it -ee one
            It "Parameter of class/enum type defined in module should work" { $result | Should -Be 1 }
        }
        finally
        {
            Remove-Module -ErrorAction ignore MSFT_2081529
        }
}

Describe 'Type building' -Tags "CI" {
    It 'should build the type only once for scriptblock' {
        $a = $null
        1..10 | ForEach-Object {
            class C {}
            if ($a) {
	         $a -eq [C] | Should -BeTrue
            }
            $a = [C]
        }
    }

    It 'should create a new type every time scriptblock executed?' -Pending {
        $sb = [scriptblock]::Create('class A {static [int] $a }; [A]::new()')
        1..2 | ForEach-Object {
        $a = $sb.Invoke()[0]
            ++$a::a | Should -Be 1
            ++$a::a | Should -Be 2
        }
    }

    It 'should get the script from a class type' {
        class C {}

        $a = [C].Assembly.GetCustomAttributes($false).Where{
            $_ -is [System.Management.Automation.DynamicClassImplementationAssemblyAttribute]}
        $a.ScriptFile | Should -BeExactly $PSCommandPath
    }
}

Describe 'RuntimeType created for TypeDefinitionAst' -Tags "CI" {

    It 'can make cast to the right RuntimeType in two different contexts' -pending {

        $ssfe = [System.Management.Automation.Runspaces.SessionStateFunctionEntry]::new("foo", @'
class Base
{
    [int] foo() { return 100 }
}

class Derived : Base
{
    [int] foo() { return 2 * ([Base]$this).foo() }
}

[Derived]::new().foo()
'@)

        $iss = [System.Management.Automation.Runspaces.initialsessionstate]::CreateDefault2()
        $iss.Commands.Add($ssfe)

        $ps = [powershell]::Create($iss)
        $ps.AddCommand("foo").Invoke() | Should -Be 200
        $ps.Streams.Error | Should -BeNullOrEmpty

        $ps1 = [powershell]::Create($iss)
        $ps1.AddCommand("foo").Invoke() | Should -Be 200
        $ps1.Streams.Error | Should -BeNullOrEmpty

        $ps.Commands.Clear()
        $ps.Streams.Error.Clear()
        $ps.AddScript(". foo").Invoke() | Should -Be 200
        $ps.Streams.Error | Should -BeNullOrEmpty
    }
}

Describe 'TypeTable lookups' -Tags "CI" {

    Context 'Call methods from a different thread' {
        $b = [powershell]::Create().AddScript(
@'
class A {}
class B
{
    [object] getA1() { return New-Object A }
    [object] getA2() { return [A]::new() }
}

[B]::new()

'@).Invoke()[0]

        It 'can do type lookup by name' {
            $b.getA1() | Should -BeExactly 'A'
        }

        It 'can do type lookup by [type]' {
            $b.getA2() | Should -BeExactly 'A'
        }
    }
}

Describe 'Protected method access' -Tags "CI" {

    Add-Type @'
namespace Foo
{
    public class Bar
    {
        protected int x {get; set;}
    }
}
'@

     It 'doesn''t allow protected methods access outside of inheritance chain' {
        $a = [scriptblock]::Create(@'
class A
{
    SetX([Foo.Bar]$bar, [int]$x)
    {
        $bar.x = $x
    }

    [int] GetX([Foo.Bar]$bar)
    {
        Set-StrictMode -Version latest
        return $bar.x
    }
}
[A]::new()

'@).Invoke()
        $bar = [Foo.Bar]::new()
        { $a.SetX($bar, 42) } | Should -Throw -ErrorId 'PropertyAssignmentException'
        { $a.GetX($bar) } | Should -Throw -ErrorId 'PropertyNotFoundStrict'
     }

     It 'can call protected methods sequentially from two different contexts' {
        $ssfe = [System.Management.Automation.Runspaces.SessionStateFunctionEntry]::new("foo", @'
class A : Foo.Bar
{
    SetX([int]$x)
    {
        $this.x = $x
    }

    [int] GetX()
    {
        return $this.x
    }
}
return [A]::new()
'@)

        $iss = [System.Management.Automation.Runspaces.initialsessionstate]::CreateDefault()
        $iss.Commands.Add($ssfe)

        $ps = [powershell]::Create($iss)
        $a = $ps.AddCommand("foo").Invoke()[0]
        $ps.Streams.Error | Should -BeNullOrEmpty

        $ps1 = [powershell]::Create($iss)
        $a1 = $ps1.AddCommand("foo").Invoke()[0]
        $ps1.Streams.Error | Should -BeNullOrEmpty

        $a.SetX(101)
        $a1.SetX(103)

        $a.GetX() | Should -Be 101
        $a1.GetX() | Should -Be 103
    }
}

Describe 'variable analysis' -Tags "CI" {
    It 'can specify type construct on the local variables' {
        class A { [string] getFoo() { return 'foo'} }

        class B
        {
            static [A] getA ()
            {
                [A] $var = [A]::new()
                return $var
            }
        }

        [B]::getA().getFoo() | Should -BeExactly 'foo'
    }
}
