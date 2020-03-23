# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
using Namespace System.Management.Automation.Language
Describe "The SafeGetValue method on AST returns safe values" -Tags "CI" {
    It "A hashtable is returned from a HashtableAst" {
        $HashtableAstType = [HashtableAst]
        $HtAst = {
            @{ one = 1 }
            }.ast.Find({$args[0] -is $HashtableAstType}, $true)
        $HtAst | Should -Not -BeNullOrEmpty
        $HtAst.SafeGetValue() | Should -BeOfType Hashtable
    }
    It "An Array is returned from a LiteralArrayAst" {
        $ArrayAstType = [ArrayLiteralAst]
        $ArrayAst = {
            @( 1,2,3,4)
            }.ast.Find({$args[0] -is $ArrayAstType}, $true)
        $ArrayAst | Should -Not -BeNullOrEmpty
        ,$ArrayAst.SafeGetValue() | Should -BeOfType Object[]
    }
    It "The proper error is returned when a variable is referenced" {
        $ast = { $a }.Ast.Find({$args[0] -is "VariableExpressionAst"},$true)
        { $ast.SafeGetValue() } | Should -Throw -ErrorId "InvalidOperationException"
    }
    It "A ScriptBlock AST fails with the proper error" {
        { { 1 }.Ast.SafeGetValue() } | Should -Throw -ErrorId "InvalidOperationException"
    }

}
