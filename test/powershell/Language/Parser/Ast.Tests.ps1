using Namespace System.Management.Automation.Language
Describe "The SafeGetValue method on AST returns safe values" -Tags "DRT" {
    It "A hashtable is returned from a HashtableAst" {
        $HashtableAstType = [HashtableAst]
        $HtAst = { 
            @{ one = 1 } 
            }.ast.Find({$args[0] -is $HashtableAstType}, $true)
        $HtAst.SafeGetValue().GetType().Name | Should be Hashtable
    }
    It "An Array is returned from a LiteralArrayAst" {
        $ArrayAstType = [ArrayLiteralAst]
        $ArrayAst = {
            @( 1,2,3,4)
            }.ast.Find({$args[0] -is $ArrayAstType}, $true)
        $ArrayAst.SafeGetValue().GetType().Name | Should be "Object[]"
    }
    It "The proper error is returned when a variable is referenced" {
        $ast = { $a }.Ast.Find({$args[0] -is "VariableExpressionAst"},$true)
        try {
            $ast.SafeGetValue() | out-null
            Throw "Execution Succeeded"
        }
        catch {
            $_.FullyQualifiedErrorId | Should be "InvalidOperationException"
            $_.ToString() | Should Match '\$a'
        }
    }
    It "A ScriptBlock AST fails with the proper error" {
        try {
            { 1 }.Ast.SafeGetValue()
            Throw "Execution Succeeded"
        }
        catch {
            $_.FullyQualifiedErrorId | Should be "InvalidOperationException"
        }
    }

}
