# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

using namespace System.Collections.Generic
using namespace System.Threading
using namespace System.Timers
using namespace SMA = System.Management.Automation
using type Timer = System.String
using type string = System.Management.Automation.Language.TokenKind
using type MyList = List[Timer]
using type MyNormalType = System.Collections.ArrayList
using type MyGenericType = System.Collections.Generic.Dictionary[string,int]
using type MyMultiDimArray = int[,]

Describe "Type resolution with type and namespace aliases" -Tags "CI" {
    It "Should resolve <Type> to <Expected>" -TestCases @(
        @{Type = 'Timer'; Expected = 'System.String'} #Type alias should have higher priority than "Using namespace"
        @{Type = 'string'; Expected = 'System.Management.Automation.Language.TokenKind'} #Type alias should have higher priority than type accelerators
        @{Type = 'MyList'; Expected = 'System.Collections.Generic.List[System.String]'} #Type alias declaration should be combinable with other using statements
        @{Type = 'MyNormalType'; Expected = 'System.Collections.ArrayList'} #Type alias should work with normal types
        @{Type = 'MyGenericType'; Expected = 'System.Collections.Generic.Dictionary[System.Management.Automation.Language.TokenKind,System.Int32]'} #Type alias should work with generics
        @{Type = 'MyMultiDimArray'; Expected = 'System.Int32[,]'} #Type alias should work with array types
        @{Type = 'SMA.ActionPreference'; Expected = 'System.Management.Automation.ActionPreference'}
        @{Type = 'SMA.Language.TokenKind'; Expected = 'System.Management.Automation.Language.TokenKind'} #Namespace alias should work with nested namespaces
    ) -Test {
        param($Type, $Expected)
        $ResolvedType = Invoke-Expression -Command "[$Type]"
        if ($ResolvedType.IsGenericType)
        {
            "$($ResolvedType.Namespace).$($ResolvedType.Name -replace '`\d+')[$($ResolvedType.GenericTypeArguments.FullName -join ',')]" | Should -Be $Expected
        }
        else
        {
            $ResolvedType.FullName | Should -Be $Expected
        }
    }
}
Describe "Type and namespace parsing errors" -Tags "CI" {
    It "Should fail to parse <TestString>" -TestCases @(
        @{ExpectedErrors = 'DuplicateTypeAliasClause';TestString = 'using type X = Y;using type X = Z'}
        @{ExpectedErrors = 'TypeAliasConflictsTypeDefinition';TestString = 'using type x = y;enum x{}'}
        @{ExpectedErrors = 'DuplicateNamespaceClause';TestString = 'using namespace X = Y;using namespace X = Y;'}
        @{ExpectedErrors = 'DuplicateNamespaceClause';TestString = 'using namespace X;using namespace X = Y;'}
        @{ExpectedErrors = 'TypeAliasContainsNamespace';TestString = 'using type x.y = z'}
        @{ExpectedErrors = 'NamespaceAliasContainsNamespace';TestString = 'using namespace x.y = z'}
        @{ExpectedErrors = 'MissingTypeAlias';TestString = 'using type x = ;'}
        @{ExpectedErrors = 'MissingNamespaceAlias';TestString = 'using namespace x ='}
        @{ExpectedErrors = 'MissingTypeAlias';TestString = "using type x = `n"}
        @{ExpectedErrors = 'InvalidValueForUsingItemName';TestString = 'using namespace x = $SomeVar'}
        @{ExpectedErrors = 'TypeNameExpected';TestString = 'using type x = [SomeType]'}
        @{ExpectedErrors = 'MissingTypename','EndSquareBracketExpectedAtEndOfAttribute';TestString = 'using type x = SomeGenericType[Arg1,'}
        @{ExpectedErrors = 'InvalidValueForUsingItemName';TestString = 'using namespace x = SomeArrayType[,]'}
    ) -Test {
        param([string[]]$ExpectedError, $TestString)
        $ParsedTokens = $null
        $ParsedErrors = $null
        $null = [System.Management.Automation.Language.Parser]::ParseInput($TestString, [ref]$ParsedTokens, [ref]$ParsedErrors)
        for ($i = 0; $i -lt $ExpectedErrors.Count; $i++)
        {
            $ParsedErrors[$i].ErrorId | Should -Be $ExpectedError[$i]
        }
    }
}
Describe "Type and namespace token tokenization" -Tags "CI" {
    It "Should properly tokenize <TestString>" -TestCases @(
        @{
            TestString = 'using type x = y'
            InterestingTokens = @(
                @{Index = 2; Text = "X"; Kind = "Identifier"; Flags = "TypeName"}
                @{Index = 4; Text = "Y"; Kind = "Identifier"; Flags = "TypeName"}
            )
        }
        @{
            TestString = 'using namespace x = y'
            InterestingTokens = @(
                @{Index = 4; Text = "Y"; Kind = "Identifier"; Flags = "None"}
            )
        }
        @{
            TestString = 'using type x'
            InterestingTokens = @(
                @{Index = 2; Text = "X"; Kind = "Identifier"; Flags = "TypeName"}
            )
        }
        @{
            TestString = 'using type =' #Do not mark invalid tokens as a typename
            InterestingTokens = @(
                @{Index = 2; Text = "="; Kind = "Generic"; Flags = "AssignmentOperator"}
            )
        }
        @{
            TestString = 'using type x = Y.Z[A,B]'
            InterestingTokens = @(
                @{Index = 4; Text = "Y.Z"; Kind = "Identifier"; Flags = "TypeName"}
                @{Index = 6; Text = "A"; Kind = "Identifier"; Flags = "TypeName"}
                @{Index = 8; Text = "B"; Kind = "Identifier"; Flags = "TypeName"}
            )
        }
    ) -Test {
        param($InterestingTokens, $TestString)
        $ParsedTokens = $null
        $ParsedErrors = $null
        $null = [System.Management.Automation.Language.Parser]::ParseInput($TestString, [ref]$ParsedTokens, [ref]$ParsedErrors)
        foreach ($TokenInfo in $InterestingTokens)
        {
            $Token = $ParsedTokens[$TokenInfo['Index']]
            $Token.Text | Should -Be $TokenInfo['Text']
            $Token.Kind | Should -Be $TokenInfo['Kind']
            $Token.Flags | Should -Be $TokenInfo['TypeName']
        }
    }
}
