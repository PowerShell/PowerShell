# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

using namespace System.Management.Automation
using namespace System.Management.Automation.Language

Describe "Ternary Operator" -Tags CI {
    Context "Parsing of ternary operator" {
        BeforeAll {
            $testCases_1 = @(
                @{ Script = '$true?2:3'; TokenKind = [TokenKind]::Variable; }
                @{ Script = '$false?';   TokenKind = [TokenKind]::Variable; }
                @{ Script = '$:abc';     TokenKind = [TokenKind]::Variable; }
                @{ Script = '$env:abc';  TokenKind = [TokenKind]::Variable; }
                @{ Script = '$env:123';  TokenKind = [TokenKind]::Variable; }
                @{ Script = 'a?2:2';     TokenKind = [TokenKind]::Generic;  }
                @{ Script = '1?2:3';     TokenKind = [TokenKind]::Generic;  }
                @{ Script = 'a?';        TokenKind = [TokenKind]::Generic;  }
                @{ Script = 'a?b';       TokenKind = [TokenKind]::Generic;  }
                @{ Script = '1?';        TokenKind = [TokenKind]::Generic;  }
                @{ Script = '?2:3';      TokenKind = [TokenKind]::Generic;  }
            )

            $testCases_2 = @(
                @{ Script = '$true ?';     ErrorId = "ExpectedValueExpression";         AstType = [ErrorExpressionAst] }
                @{ Script = '$true ? 3';   ErrorId = "MissingColonInTernaryExpression"; AstType = [ErrorExpressionAst] }
                @{ Script = '$true ? 3 :'; ErrorId = "ExpectedValueExpression";         AstType = [TernaryExpressionAst] }
            )
        }

        It "Question-mark and colon parsed correctly in '<Script>' when not in ternary expression context" -TestCases $testCases_1 {
            param($Script, $TokenKind)

            $tks = $null
            $ers = $null
            $result = [Parser]::ParseInput($Script, [ref]$tks, [ref]$ers)

            $tks[0].Kind | Should -BeExactly $TokenKind
            $tks[0].Text | Should -BeExactly $Script

            if ($TokenKind -eq "Variable") {
                $result.EndBlock.Statements[0].PipelineElements[0].Expression | Should -BeOfType 'System.Management.Automation.Language.VariableExpressionAst'
                $result.EndBlock.Statements[0].PipelineElements[0].Expression.Extent.Text | Should -BeExactly $Script
            } else {
                $result.EndBlock.Statements[0].PipelineElements[0].CommandElements[0] | Should -BeOfType 'System.Management.Automation.Language.StringConstantExpressionAst'
                $result.EndBlock.Statements[0].PipelineElements[0].CommandElements[0].Extent.Text | Should -BeExactly $Script
            }
        }

        It "Question-mark and colon can be used as command names" {
            function a?b:c { 'a?b:c' }
            function 2?3:4 { '2?3:4' }

            a?b:c | Should -BeExactly 'a?b:c'
            2?3:4 | Should -BeExactly '2?3:4'
        }

        It "Generate incomplete parsing error properly for '<Script>'" -TestCases $testCases_2 {
            param($Script, $ErrorId, $AstType)

            $ers = $null
            $result = [Parser]::ParseInput($Script, [ref]$null, [ref]$ers)

            $ers.Count | Should -Be 1
            $ers.IncompleteInput | Should -BeTrue
            $ers.ErrorId | Should -BeExactly $ErrorId

            $result.EndBlock.Statements[0].PipelineElements[0].Expression | Should -BeOfType $AstType
        }

        It "Generate ternary ast when possible" {
            $ers = $null
            $result = [Parser]::ParseInput('$true ? :', [ref]$null, [ref]$ers)
            $ers.Count | Should -Be 2

            $ers[0].IncompleteInput | Should -BeFalse
            $ers[0].ErrorId | Should -BeExactly 'ExpectedValueExpression'
            $ers[1].IncompleteInput | Should -BeTrue
            $ers[1].ErrorId | Should -BeExactly 'ExpectedValueExpression'

            $expr = $result.EndBlock.Statements[0].PipelineElements[0].Expression
            $expr | Should -BeOfType 'System.Management.Automation.Language.TernaryExpressionAst'
            $expr.IfTrue | Should -BeOfType 'System.Management.Automation.Language.ErrorExpressionAst'
            $expr.IfFalse | Should -BeOfType 'System.Management.Automation.Language.ErrorExpressionAst'

            $ers = $null
            $result = [Parser]::ParseInput('$true ? : 3', [ref]$null, [ref]$ers)
            $ers.Count | Should -Be 1

            $ers.IncompleteInput | Should -BeFalse
            $ers.ErrorId | Should -BeExactly "ExpectedValueExpression"
            $expr = $result.EndBlock.Statements[0].PipelineElements[0].Expression
            $expr | Should -BeOfType 'System.Management.Automation.Language.TernaryExpressionAst'
            $expr.IfTrue | Should -BeOfType 'System.Management.Automation.Language.ErrorExpressionAst'
            $expr.IfFalse | Should -BeOfType 'System.Management.Automation.Language.ConstantExpressionAst'
        }
    }

    Context "Using of ternary operator" {
        BeforeAll {
            $testCases_1 = @(
                ## Condition: variable and constant expressions
                @{ Script = { $true ? 1 : 2 };  ExpectedValue = 1 }
                @{ Script = { $true? ?1 :2 };   ExpectedValue = 2 }
                @{ Script = { ${true}?1:2 };    ExpectedValue = 1 }
                @{ Script = { 1 ? 1kb : 0xf };  ExpectedValue = 1kb }
                @{ Script = { 0 ?1kb:0xf };     ExpectedValue = 15 }
                @{ Script = { 's' ?1kb:0xf };   ExpectedValue = 1kb }
                @{ Script = { $null ?1kb:0xf }; ExpectedValue = 15 }
                @{ Script = { '' ?1kb:0xf };    ExpectedValue = 15 }

                ## Condition: other primary expressions
                @{ Script = { 1,2,3,4 ? 'Core' : 'Desktop' };          ExpectedValue = 'Core' }
                @{ Script = { @(1,2,3,4) ? 'Core' : 'Desktop' };       ExpectedValue = 'Core' }
                @{ Script = { @{name = 'name'} ? 'Core' : 'Desktop' };  ExpectedValue = 'Core' }
                @{ Script = { @{name = 'name'}.name ? 'Core' : 'Desktop' }; ExpectedValue = 'Core' }
                @{ Script = { @{name = 'name'}.Contains('name') ? 'Core' : 'Desktop' }; ExpectedValue = 'Core' }
                @{ Script = { (Test-Path Env:\NonExist) ? 'true' : 'false' };     ExpectedValue = 'false' }
                @{ Script = { (Test-Path Env:\PSModulePath) ? 'true' : 'false' }; ExpectedValue = 'true' }
                @{ Script = { $($p = Get-Process -Id $PID; $p.Name -eq 'pwsh') ? 'Core' : 'Desktop' }; ExpectedValue = 'Core' }

                ## Condition: unary and binary expression expressions
                @{ Script = { -not $IsCoreCLR ? 'Desktop' : 'Core' };             ExpectedValue = 'Core' }
                @{ Script = { $PSEdition -eq 'Core' ? 'Core' : 'Desktop' };       ExpectedValue = 'Core' }
                @{ Script = { $IsCoreCLR -and (Get-Process -Id $PID).Name -eq 'pwsh' ? 'Core' : 'Desktop' }; ExpectedValue = 'Core' }
                @{ Script = { $IsCoreCLR -and 'pwsh' -match 'p.*h' ? 'Core' : 'Desktop' }; ExpectedValue = 'Core' }
                @{ Script = { 1,2,3 -contains 2 ? 'Core' : 'Desktop' }; ExpectedValue = 'Core' }

                ## Nested ternary expressions
                @{ Script = { $IsCoreCLR ? $false ? 'nested-if-true' : 'nested-if-false' : 'if-false' }; ExpectedValue = 'nested-if-false' }
                @{ Script = { $IsCoreCLR ? $false ? 'nested-if-true' : $true ? 'nested-nested-if-true' : 'nested-nested-if-false' : 'if-false' }; ExpectedValue = 'nested-nested-if-true' }

                ## Binary operator has higher precedence order than ternary
                @{ Script = { !$IsCoreCLR ? 'Core' : 'Desktop' -eq 'Core' };  ExpectedValue = !$IsCoreCLR ? 'Core' : ('Desktop' -eq 'Core') }
                @{ Script = { ($IsCoreCLR ? 'Core' : 'Desktop') -eq 'Core' }; ExpectedValue = $true }
            )
        }

        It "Basic uses of ternary operator - '<Script>'" -TestCases $testCases_1 {
            param($Script, $ExpectedValue)
            & $Script | Should -BeExactly $ExpectedValue
        }

        It "Use ternary operator in parameter default values" {
            function testFunc {
                param($psExec = $IsCoreCLR ? 'pwsh' : 'powershell.exe')
                $psExec
            }
            testFunc | Should -BeExactly 'pwsh'
        }

        It "Use ternary operator with assignments" {
            $IsCoreCLR ? ([string]$var = 'string') : 'blah' > $null
            $var = [System.IO.FileInfo]::new('abc')
            $var | Should -BeOfType [string]
            $var | Should -BeExactly 'abc'
        }

        It "Use ternary operator in pipeline" {
            $result = $IsCoreCLR ? 'Core' : 'Desktop' | ForEach-Object { $_ + '-Pipe' }
            $result | Should -BeExactly 'Core-Pipe'
        }

        It "Return script block from ternary expression" {
            $result = ${IsCoreCLR}?{'Core'}:{'Desktop'}
            $result | Should -BeOfType [scriptblock]
            & $result | Should -BeExactly 'Core'
        }

        It "Tab completion for variables assigned with ternary expression" {
            ## Type inference for the ternary expression should aggregate the inferred values from both branches
            $text1 = '$var1 = $IsCoreCLR ? (Get-Item $PSHome) : (Get-Process -Id $PID); $var1.Full'
            $result = TabExpansion2 -inputScript $text1 -cursorColumn $text1.Length
            $result.CompletionMatches[0].CompletionText | Should -BeExactly FullName

            $text2 = '$var1 = $IsCoreCLR ? (Get-Item $PSHome) : (Get-Process -Id $PID); $var1.Proce'
            $result = TabExpansion2 -inputScript $text2 -cursorColumn $text2.Length
            $result.CompletionMatches[0].CompletionText | Should -BeExactly ProcessName

            $text3 = '$IsCoreCLR ? ($var2 = Get-Item $PSHome) : "blah"; $var2.Full'
            $result = TabExpansion2 -inputScript $text3 -cursorColumn $text3.Length
            $result.CompletionMatches[0].CompletionText | Should -BeExactly FullName
        }
    }
}
