Describe 'Positive Parse Properties Tests' -Tags "CI" {
    It 'PositiveParsePropertiesTest' {
        # Just a bunch of random basic things here
        # This test doesn't need to check anything, if there are
        # any parse errors, the entire suite will fail because the
        # script will fail to parse.

        # No members
        interface I1 {}

        # Simple field
        interface I2 { $x; }

        # Simple typed field
        interface I3 { [int] $x; }

        # Multiple fields, one line, last w/o semicolon
        interface I4 { $x; $y }

        # Multiple fields, multiple lines
        interface I5
        {
            $x
            $y
        }

        # Field using type defined in this scope
        interface I8a { [I1] $i1; }
        interface I8b { [i1] $i1; }

        # Field referring to self type
        interface I9 { [I9] $i9; }
    }

    It 'Positive Parse Methods Tests' {
        # Just a bunch of random basic things here
        # This test doesn't need to check anything, if there are
        # any parse errors, the entire suite will fail because the
        # script will fail to parse.

        # No members
        interface I1 {}

        # Simple method
        interface I2 { f(); }

        # Simple method with return type
        interface I3 { [int] f(); }

        # Multiple methods, one line
        <# let's keep test for later
        interface I4 { f() f1() }
        #>

        # Multiple methods w/ overloads
        interface I5
        {
            f1()
            f1($a)
        }

        # Method using return type defined in this scope
        interface I8a { [I1] f1() }
        interface I8b { [i1] f1() }
    }
}

Describe 'Negative Parsing Tests' -Tags "CI" {
    ShouldBeParseError 'interface' MissingNameAfterKeyword 5
    ShouldBeParseError 'interface IFoo' MissingTypeBody 9
    ShouldBeParseError 'interface IFoo {' MissingEndCurlyBrace 11
    ShouldBeParseError 'interface IFoo { [int] }' IncompleteMemberDefinition 17
    ShouldBeParseError 'interface IFoo { $private: }' InvalidVariableReference 12
    ShouldBeParseError 'interface IFoo { [int]$global: }' InvalidVariableReference 17
    ShouldBeParseError 'interface IFoo {} interface IFoo {}' MemberAlreadyDefined 13
    ShouldBeParseError 'interface IFoo { $x; $x; }' MemberAlreadyDefined 16 -SkipAndCheckRuntimeError
    ShouldBeParseError 'interface IFoo { [int][string]$x; }' TooManyTypes 17
    ShouldBeParseError 'interface IFoo { static static $x; }' DuplicateQualifier 19
    ShouldBeParseError 'interface IFoo { [zz]$x; }' TypeNotFound 13
    ShouldBeParseError 'interface IFoo { [zz]f() }' TypeNotFound 13
    ShouldBeParseError 'interface IFoo { f([zz]$x) }' TypeNotFound 15

    ShouldBeParseError 'interface I {} interface I {}' MemberAlreadyDefined 11
    ShouldBeParseError 'interface I { f(); f() }' MemberAlreadyDefined 16 -SkipAndCheckRuntimeError
    ShouldBeParseError 'interface I { F(); F($o); [int] F($o) }' MemberAlreadyDefined 24 -SkipAndCheckRuntimeError
    ShouldBeParseError 'interface I { f(); f($a); f(); }' MemberAlreadyDefined 24 -SkipAndCheckRuntimeError
    ShouldBeParseError 'interface I { f([int]$a); f([int]$b); }' MemberAlreadyDefined 23 -SkipAndCheckRuntimeError
    ShouldBeParseError 'interface I { $x; [int]$x; }' MemberAlreadyDefined 14 -SkipAndCheckRuntimeError
    ShouldBeParseError 'interface I { static C($x) }' StaticConstructorCantHaveParameters 19 -SkipAndCheckRuntimeError
    ShouldBeParseError 'interface I { static C([int]$x = 100) }' StaticConstructorCantHaveParameters 19 -SkipAndCheckRuntimeError

    ShouldBeParseError 'interface I : B' MissingTypeBody 11

    ShouldBeParseError 'interface IFoo { q(); w()' MissingEndCurlyBrace 11
}

Describe 'Positive SelfClass Type As Parameter Test' -Tags "CI" {
        interface IPoint
        {
            [int] $x;
            [int] $y;
            Add([IPoint]$val)
        }
        It  "[IPoint]::Add accepts a parameter of its own type" {
            [IPoint].GetMember('Add').GetParameters[0].ParameterType -eq [IPoint]
        }
}
