# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Set-StrictMode -v 2

Describe 'for statement parsing' -Tags "CI" {
    ShouldBeParseError 'for' MissingOpenParenthesisAfterKeyword 4 -CheckColumnNumber
    ShouldBeParseError 'for(' MissingEndParenthesisAfterStatement 5 -CheckColumnNumber
    ShouldBeParseError 'for(;' MissingEndParenthesisAfterStatement 6 -CheckColumnNumber
    ShouldBeParseError 'for(;;' MissingEndParenthesisAfterStatement 7 -CheckColumnNumber
    ShouldBeParseError 'for($a' MissingEndParenthesisAfterStatement 7 -CheckColumnNumber
    ShouldBeParseError 'for($a;' MissingEndParenthesisAfterStatement 8 -CheckColumnNumber
    ShouldBeParseError 'for($a;$b' MissingEndParenthesisAfterStatement 10 -CheckColumnNumber
    ShouldBeParseError 'for($a;$b;' MissingEndParenthesisAfterStatement 11 -CheckColumnNumber
    ShouldBeParseError 'for($a;$b;$c' MissingEndParenthesisAfterStatement 13 -CheckColumnNumber
    ShouldBeParseError 'for($a;$b;$c)' MissingLoopStatement 14 -CheckColumnNumber
    ShouldBeParseError ':lab for' MissingOpenParenthesisAfterKeyword 9 -CheckColumnNumber
    ShouldBeParseError ':lab for(' MissingEndParenthesisAfterStatement 9
    ShouldBeParseError ':lab for(;' MissingEndParenthesisAfterStatement 11 -CheckColumnNumber
    ShouldBeParseError ':lab for(;;' MissingEndParenthesisAfterStatement 12 -CheckColumnNumber
    ShouldBeParseError ':lab for($a' MissingEndParenthesisAfterStatement 12 -CheckColumnNumber
    ShouldBeParseError ':lab for($a;' MissingEndParenthesisAfterStatement 12
    ShouldBeParseError ':lab for($a;$b' MissingEndParenthesisAfterStatement 14
    ShouldBeParseError ':lab for($a;$b;' MissingEndParenthesisAfterStatement 15
    ShouldBeParseError ':lab for($a;$b;$c' MissingEndParenthesisAfterStatement 17
    ShouldBeParseError ':lab for($a;$b;$c)' MissingLoopStatement 18

    Test-ErrorStmt 'for z' 'for'
    Test-ErrorStmt 'for {}' 'for'
    Test-ErrorStmt 'for ()' 'for ()'
    Test-ErrorStmt 'for(' 'for('
    Test-ErrorStmt 'for(;' 'for(;'
    Test-ErrorStmt 'for(;;' 'for(;;'
    Test-ErrorStmt 'for($a' 'for($a' '$a' '$a' '$a'
    Test-ErrorStmt 'for($a;$b' 'for($a;$b' '$a' '$a' '$a' '$b' '$b' '$b'
    Test-ErrorStmt 'for($a;$b;$c' 'for($a;$b;$c' '$a' '$a' '$a' '$b' '$b' '$b' '$c' '$c' '$c'
    Test-ErrorStmt 'for($a;$b;$c)' 'for($a;$b;$c)' '$a' '$a' '$a' '$b' '$b' '$b' '$c' '$c' '$c'
    Test-ErrorStmt 'for()zzzz' 'for()'

    Test-ErrorStmt ':lab for z'         ':lab for'
    Test-ErrorStmt ':lab for {}'        ':lab for'
    Test-ErrorStmt ':lab for ()'        ':lab for ()'
    Test-ErrorStmt ':lab for('          ':lab for('
    Test-ErrorStmt ':lab for(;'         ':lab for(;'
    Test-ErrorStmt ':lab for(;;'        ':lab for(;;'
    Test-ErrorStmt ':lab for($a'        ':lab for($a' '$a' '$a' '$a'
    Test-ErrorStmt ':lab for($a;$b'     ':lab for($a;$b' '$a' '$a' '$a' '$b' '$b' '$b'
    Test-ErrorStmt ':lab for($a;$b;$c'  ':lab for($a;$b;$c' '$a' '$a' '$a' '$b' '$b' '$b' '$c' '$c' '$c'
    Test-ErrorStmt ':lab for($a;$b;$c)' ':lab for($a;$b;$c)' '$a' '$a' '$a' '$b' '$b' '$b' '$c' '$c' '$c'
    Test-ErrorStmt ':lab for()zzzz'     ':lab for()'
}

Describe 'foreach statement parsing' -Tags "CI" {
    ShouldBeParseError 'foreach' MissingOpenParenthesisAfterKeyword 7
    ShouldBeParseError 'foreach (' MissingVariableNameAfterForeach 9
    ShouldBeParseError 'foreach ($a' MissingInInForeach 11
    ShouldBeParseError 'foreach ($a into' MissingInInForeach 11
    ShouldBeParseError 'foreach ($a in' MissingForeachExpression 14
    ShouldBeParseError 'foreach ($a in $x' MissingEndParenthesisAfterForeach 17
    ShouldBeParseError 'foreach ($a in $x)' MissingForeachStatement 18
    ShouldBeParseError ':lab foreach' MissingOpenParenthesisAfterKeyword 12
    ShouldBeParseError ':lab foreach (' MissingVariableNameAfterForeach 14
    ShouldBeParseError ':lab foreach ($a' MissingInInForeach 16
    ShouldBeParseError ':lab foreach ($a into' MissingInInForeach 17 -CheckColumnNumber
    ShouldBeParseError ':lab foreach ($a in' MissingForeachExpression 20 -CheckColumnNumber
    ShouldBeParseError ':lab foreach ($a in $x' MissingEndParenthesisAfterForeach 23 -CheckColumnNumber
    ShouldBeParseError ':lab foreach ($a in $x)' MissingForeachStatement 24 -CheckColumnNumber

    Test-ErrorStmt 'foreach'                    'foreach'
    Test-ErrorStmt 'foreach z'                  'foreach'
    Test-ErrorStmt 'foreach ('                  'foreach ('
    Test-ErrorStmt 'foreach ($a'                'foreach ($a' '$a'
    Test-ErrorStmt 'foreach ($a zz'             'foreach ($a' '$a'
    Test-ErrorStmt 'foreach ($a # line comment' 'foreach ($a' '$a'
    Test-ErrorStmt 'foreach ($a $zz'            'foreach ($a' '$a'
    Test-ErrorStmt 'foreach ($a in'             'foreach ($a in' '$a'
    Test-ErrorStmt 'foreach ($a in $b'          'foreach ($a in $b' '$a' '$b' '$b' '$b'
    Test-ErrorStmt 'foreach ($a in $b <#hmm#>)' 'foreach ($a in $b <#hmm#>)' '$a' '$b' '$b' '$b'
    Test-ErrorStmt ':lab foreach'                    ':lab foreach'
    Test-ErrorStmt ':lab foreach z'                  ':lab foreach'
    Test-ErrorStmt ':lab foreach ('                  ':lab foreach ('
    Test-ErrorStmt ':lab foreach ($a'                ':lab foreach ($a' '$a'
    Test-ErrorStmt ':lab foreach ($a zz'             ':lab foreach ($a' '$a'
    Test-ErrorStmt ':lab foreach ($a # line comment' ':lab foreach ($a' '$a'
    Test-ErrorStmt ':lab foreach ($a $zz'            ':lab foreach ($a' '$a'
    Test-ErrorStmt ':lab foreach ($a in'             ':lab foreach ($a in' '$a'
    Test-ErrorStmt ':lab foreach ($a in $b'          ':lab foreach ($a in $b' '$a' '$b' '$b' '$b'
    Test-ErrorStmt ':lab foreach ($a in $b <#hmm#>)' ':lab foreach ($a in $b <#hmm#>)' '$a' '$b' '$b' '$b'
}

Describe 'do/while statement statement parsing' -Tags "CI" {
    ShouldBeParseError 'do' MissingLoopStatement 2
    ShouldBeParseError 'do {}' MissingWhileOrUntilInDoWhile 5
    ShouldBeParseError 'do {} while' MissingOpenParenthesisAfterKeyword 11
    ShouldBeParseError 'do {} while (' MissingExpressionAfterKeyword 13
    ShouldBeParseError 'do {} while (1' MissingEndParenthesisAfterStatement 14
    ShouldBeParseError ':lab do' MissingLoopStatement 8 -CheckColumnNumber
    ShouldBeParseError ':lab do {}' MissingWhileOrUntilInDoWhile 11 -CheckColumnNumber
    ShouldBeParseError ':lab do {} while' MissingOpenParenthesisAfterKeyword 17 -CheckColumnNumber
    ShouldBeParseError ':lab do {} while (' MissingExpressionAfterKeyword 19 -CheckColumnNumber
    ShouldBeParseError ':lab do {} while (1' MissingEndParenthesisAfterStatement 20 -CheckColumnNumber

    Test-ErrorStmt 'do'                  'do'
    Test-ErrorStmt 'do {1}'              'do {1}' '{1}' '1' '1' '1'
    Test-ErrorStmt 'do {1} while'        'do {1} while' '{1}' '1' '1' '1'
    Test-ErrorStmt 'do {1} while('       'do {1} while(' '{1}' '1' '1' '1'
    Test-ErrorStmt 'do {1} while($false' 'do {1} while($false' '{1}' '1' '1' '1' '$false' '$false' '$false'
    Test-ErrorStmt ':lab do'                  ':lab do'
    Test-ErrorStmt ':lab do {1}'              ':lab do {1}' '{1}' '1' '1' '1'
    Test-ErrorStmt ':lab do {1} while'        ':lab do {1} while' '{1}' '1' '1' '1'
    Test-ErrorStmt ':lab do {1} while('       ':lab do {1} while(' '{1}' '1' '1' '1'
    Test-ErrorStmt ':lab do {1} while($false' ':lab do {1} while($false' '{1}' '1' '1' '1' '$false' '$false' '$false'
}

Describe 'do/while statement statement parsing' -Tags "CI" {
    ShouldBeParseError 'do' MissingLoopStatement 3 -CheckColumnNumber
    ShouldBeParseError 'do {}' MissingWhileOrUntilInDoWhile 6 -CheckColumnNumber
    ShouldBeParseError 'do {} until' MissingOpenParenthesisAfterKeyword 12 -CheckColumnNumber
    ShouldBeParseError 'do {} until (' MissingExpressionAfterKeyword 14 -CheckColumnNumber
    ShouldBeParseError 'do {} until (1' MissingEndParenthesisAfterStatement 15 -CheckColumnNumber
    ShouldBeParseError ':lab do' MissingLoopStatement 8 -CheckColumnNumber
    ShouldBeParseError ':lab do {}' MissingWhileOrUntilInDoWhile 10
    ShouldBeParseError ':lab do {} until' MissingOpenParenthesisAfterKeyword 16
    ShouldBeParseError ':lab do {} until (' MissingExpressionAfterKeyword 18
    ShouldBeParseError ':lab do {} until (1' MissingEndParenthesisAfterStatement 19

    Test-ErrorStmt 'do'                  'do'
    Test-ErrorStmt 'do {1}'              'do {1}' '{1}' '1' '1' '1'
    Test-ErrorStmt 'do {1} until'        'do {1} until' '{1}' '1' '1' '1'
    Test-ErrorStmt 'do {1} until('       'do {1} until(' '{1}' '1' '1' '1'
    Test-ErrorStmt 'do {1} until($false' 'do {1} until($false' '{1}' '1' '1' '1' '$false' '$false' '$false'
    Test-ErrorStmt ':lab do'                  ':lab do'
    Test-ErrorStmt ':lab do {1}'              ':lab do {1}' '{1}' '1' '1' '1'
    Test-ErrorStmt ':lab do {1} until'        ':lab do {1} until' '{1}' '1' '1' '1'
    Test-ErrorStmt ':lab do {1} until('       ':lab do {1} until(' '{1}' '1' '1' '1'
    Test-ErrorStmt ':lab do {1} until($false' ':lab do {1} until($false' '{1}' '1' '1' '1' '$false' '$false' '$false'
}

Describe 'trap statement parsing' -Tags "CI" {

    ShouldBeParseError 'trap' MissingTrapStatement 4
    ShouldBeParseError 'trap [int]' MissingTrapStatement 11 -CheckColumnNumber

    Test-ErrorStmt 'trap' 'trap'
    Test-ErrorStmt 'trap [int]' 'trap [int]' '[int]'
}

Describe 'named blocks parsing' -Tags "CI" {
    ShouldBeParseError 'begin' MissingNamedStatementBlock 5
    ShouldBeParseError 'process' MissingNamedStatementBlock 7
    ShouldBeParseError 'end' MissingNamedStatementBlock 3
    ShouldBeParseError 'clean' MissingNamedStatementBlock 5
    ShouldBeParseError 'dynamicparam' MissingNamedStatementBlock 12
    ShouldBeParseError 'begin process {}' MissingNamedStatementBlock 6 -CheckColumnNumber
    ShouldBeParseError 'end process {}' MissingNamedStatementBlock 4 -CheckColumnNumber
    ShouldBeParseError 'clean process {}' MissingNamedStatementBlock 6 -CheckColumnNumber
    ShouldBeParseError 'dynamicparam process {}' MissingNamedStatementBlock 13 -CheckColumnNumber
    ShouldBeParseError 'process begin {}' MissingNamedStatementBlock 8 -CheckColumnNumber
    ShouldBeParseError 'begin process end clean' MissingNamedStatementBlock, MissingNamedStatementBlock, MissingNamedStatementBlock, MissingNamedStatementBlock 6, 14, 18, 24 -CheckColumnNumber

    Test-Ast 'begin' 'begin' 'begin'
    Test-Ast 'begin end' 'begin end' 'begin' 'end'
    Test-Ast 'begin end process' 'begin end process' 'begin' 'end' 'process'
    Test-Ast 'begin {} end' 'begin {} end' 'begin {}' 'end'
    Test-Ast 'begin process end clean' 'begin process end clean' 'begin' 'clean' 'end' 'process'
    Test-Ast 'begin {} process end clean {}' 'begin {} process end clean {}' 'begin {}' 'clean {}' 'end' 'process'
}

#
# data statement
#
Describe 'data statement parsing' -Tags "CI" {
    ShouldBeParseError 'data' MissingStatementBlockForDataSection 5 -CheckColumnNumber
    ShouldBeParseError 'data foo' MissingStatementBlockForDataSection 9 -CheckColumnNumber
    ShouldBeParseError 'data -abc' InvalidParameterForDataSectionStatement 6 -CheckColumnNumber
    ShouldBeParseError 'data -abc foo {}' InvalidParameterForDataSectionStatement 6 -CheckColumnNumber
    ShouldBeParseError 'data -abc & {}' InvalidParameterForDataSectionStatement 5
    ShouldBeParseError 'data -s & {}' MissingValueForSupportedCommandInDataSectionStatement 7
    ShouldBeParseError 'data -s ) {}' MissingValueForSupportedCommandInDataSectionStatement,UnexpectedToken 8,9 -CheckColumnNumber
    ShouldBeParseError 'data -s } {}' MissingValueForSupportedCommandInDataSectionStatement,UnexpectedToken 7,8
    ShouldBeParseError 'data -s ; {}' MissingValueForSupportedCommandInDataSectionStatement 7
    ShouldBeParseError 'data -sup a,' MissingValueForSupportedCommandInDataSectionStatement 13 -CheckColumnNumber
    ShouldBeParseError 'data -sup a,b' MissingStatementBlockForDataSection 14 -CheckColumnNumber

    Test-ErrorStmt 'data' 'data'
    Test-ErrorStmt 'data -s & {}' 'data -s'
    Test-ErrorStmt 'data -s ) {}' 'data -s'
    Test-ErrorStmt 'data -s abc' 'data -s abc' 'abc'
    Test-ErrorStmt 'data -s abc,' 'data -s abc,' 'abc'
    Test-ErrorStmt 'data -s a,b' 'data -s a,b' 'a' 'b'
}

#
# try/catch/finally statement
#
Describe 'try/catch/finally statement parsing' -Tags "CI" {
    ShouldBeParseError 'try' MissingTryStatementBlock 3
    ShouldBeParseError 'try {}' MissingCatchOrFinally 6
    ShouldBeParseError 'try {} catch' MissingCatchHandlerBlock 12
    ShouldBeParseError 'try {} finally' MissingFinallyStatementBlock 15 -CheckColumnNumber
    ShouldBeParseError 'try {} catch [int]' MissingCatchHandlerBlock 19 -CheckColumnNumber
    ShouldBeParseError 'try {} catch {} finally' MissingFinallyStatementBlock 23
    ShouldBeParseError 'try {} catch {} catch' MissingCatchHandlerBlock 21
    ShouldBeParseError 'try {} catch [int],' MissingTypeLiteralToken 19
    ShouldBeParseError 'try {} finally { return }' ControlLeavingFinally 17
    ShouldBeParseError 'try {} finally { break }' ControlLeavingFinally 17
    ShouldBeParseError 'try {} finally { continue }' ControlLeavingFinally 17

    Test-ErrorStmt 'try'                                   'try'
    Test-ErrorStmt 'try {1}'                               'try {1}' '{1}' '1' '1' '1'
    Test-ErrorStmt 'try {1} catch'                         'try {1} catch'  '{1}' '1' '1' '1'
    Test-ErrorStmt 'try {1} finally'                       'try {1} finally'  '{1}' '1' '1' '1'
    Test-ErrorStmt 'try {1} catch [int]'                   'try {1} catch [int]' '{1}' '1' '1' '1' '[int]'
    Test-ErrorStmt 'try {1} catch {2} finally'             'try {1} catch {2} finally' '{1}' '1' '1' '1' 'catch {2}' '{2}' '2' '2' '2'
    Test-ErrorStmt 'try {1} catch {2} catch'               'try {1} catch {2} catch' '{1}' '1' '1' '1' 'catch {2}' '{2}' '2' '2' '2'
    Test-ErrorStmt 'try {1} catch [int],[char] {2} catch'  'try {1} catch [int],[char] {2} catch' '{1}' '1' '1' '1' 'catch [int],[char] {2}' '{2}' '2' '2' '2' '[int]' '[char]'
    Test-ErrorStmt 'try {1} catch [int],'                  'try {1} catch [int],' '{1}' '1' '1' '1' '[int]'
}

Describe 'switch statement parsing' -Tags "CI" {
    ShouldBeParseError 'switch' PipelineValueRequired 6
    ShouldBeParseError 'switch -abc' InvalidSwitchFlag,PipelineValueRequired 7,11
    ShouldBeParseError 'switch -file' MissingFilenameOption 12
    ShouldBeParseError 'switch -file a (1)' PipelineValueRequired,MissingCurlyBraceInSwitchStatement 15,18
    ShouldBeParseError 'switch (' PipelineValueRequired 8
    ShouldBeParseError 'switch ()' PipelineValueRequired,MissingCurlyBraceInSwitchStatement 8,9
    ShouldBeParseError 'switch ("abc")  ' MissingCurlyBraceInSwitchStatement 14
    ShouldBeParseError 'switch ("abc")  {' MissingSwitchConditionExpression 17
    ShouldBeParseError 'switch ("abc")  { 1' MissingSwitchStatementClause 19
    ShouldBeParseError 'switch ("abc")  { 1 }' MissingSwitchStatementClause 19
    ShouldBeParseError 'switch (1) {default {} default {}}' MultipleSwitchDefaultClauses 23

    Test-ErrorStmt 'switch'                'switch'
    Test-ErrorStmt 'switch -abc'           'switch -abc'
    Test-ErrorStmt 'switch ('              'switch ('
    Test-ErrorStmt 'switch ()'             'switch ()'
    Test-ErrorStmt 'switch -file'          'switch -file'
    Test-ErrorStmt              'switch -file a'         'switch -file a'
    Test-ErrorStmtForSwitchFlag 'switch -file a'         'file'  'a' 'a' 'a'
    Test-ErrorStmt              'switch -file a (1)'     'switch -file a (1)'  '1' '1' '1'
    Test-ErrorStmtForSwitchFlag 'switch -file a (1)'     'file'  'a' 'a' 'a'
    Test-ErrorStmt 'switch (1) {foo'       'switch (1) {foo' 'foo' '1' '1' '1'
    Test-ErrorStmt 'switch (1) {foo}'      'switch (1) {foo' 'foo' '1' '1' '1'
    Test-ErrorStmt 'switch (1) {foo {bar}' 'switch (1) {foo {bar}' 'foo' '{bar}' 'bar' 'bar' 'bar' '1' '1' '1'
    Test-ErrorStmt 'switch (1) {default {9} default{2}' 'switch (1) {default {9} default{2}' 'default' '{9}' '9' '9' '9' 'default' '{2}' '2' '2' '2' '1' '1' '1'
}

Describe 'function statement parsing' -Tags "CI" {
    ShouldBeParseError 'function' MissingNameAfterKeyword 8
    ShouldBeParseError 'function foo' MissingFunctionBody 12
    ShouldBeParseError 'function foo(' MissingEndParenthesisInFunctionParameterList 13
    ShouldBeParseError 'function foo {' MissingEndCurlyBrace 13
    ShouldBeParseError 'function foo { function bar { if (1) {} }' MissingEndCurlyBrace 13
    ShouldBeParseError 'function f { param($a,[int]$a) }' DuplicateFormalParameter 27
    ShouldBeParseError 'function f($a,[int]$a){}' DuplicateFormalParameter 19
    ShouldBeParseError 'function foo {param(}' MissingEndParenthesisInFunctionParameterList 20

    Test-ErrorStmt 'function foo()' 'function foo()'
    Test-ErrorStmt 'function foo($a)' 'function foo($a)' '$a' '$a'
    Test-ErrorStmt 'function foo($a = 1)' 'function foo($a = 1)' '$a = 1' '1' '$a'
    Test-ErrorStmt 'function foo($a' 'function foo($a' '$a' '$a'
    Test-ErrorStmt 'function foo($a 1' 'function foo($a' '$a' '$a'
    Test-ErrorStmt 'function foo($a = 1' 'function foo($a = 1' '$a = 1' '1' '$a'
}

Describe 'assignment statement parsing' -Tags "CI" {
    ShouldBeParseError '$a,$b += 1,2' InvalidLeftHandSide 0
}

Describe 'null coalescing assignment statement parsing' -Tag 'CI' {
    ShouldBeParseError '1 ??= 1' InvalidLeftHandSide 0
    ShouldBeParseError '@() ??= 1' InvalidLeftHandSide 0
    ShouldBeParseError '@{} ??= 1' InvalidLeftHandSide 0
    ShouldBeParseError '1..2 ??= 1' InvalidLeftHandSide 0
    ShouldBeParseError '[int] ??= 1' InvalidLeftHandSide 0
    ShouldBeParseError '$cricket ?= $soccer' ExpectedValueExpression,InvalidLeftHandSide 10,0
}

Describe 'null coalescing statement parsing' -Tag "CI" {
    ShouldBeParseError '$x??=' ExpectedValueExpression 5
    ShouldBeParseError '$x ??Get-Thing' ExpectedValueExpression,UnexpectedToken 5,5
    ShouldBeParseError '$??=$false' ExpectedValueExpression,InvalidLeftHandSide 3,0
    ShouldBeParseError '$hello ??? $what' ExpectedValueExpression,MissingColonInTernaryExpression 9,17
}

Describe 'null conditional member access statement parsing' -Tag 'CI' {
    ShouldBeParseError '[datetime]?::now' ExpectedValueExpression, UnexpectedToken 11, 11
    ShouldBeParseError '$x ?.name' ExpectedValueExpression, UnexpectedToken 4, 4
    ShouldBeParseError 'Get-Date ?.ToString()' ExpectedExpression 20
    ShouldBeParseError '${x}?.' MissingPropertyName 6
    ShouldBeParseError '${x}?.name = "value"' InvalidLeftHandSide 0

    ShouldBeParseError '[datetime]?[0]' MissingTypename, ExpectedValueExpression, UnexpectedToken 12, 11, 11
    ShouldBeParseError '${x} ?[1]' MissingTypename, ExpectedValueExpression, UnexpectedToken 7, 6, 6
    ShouldBeParseError '${x}?[]' MissingArrayIndexExpression 6
    ShouldBeParseError '${x}?[-]' MissingExpressionAfterOperator 7
    ShouldBeParseError '${x}?[             ]' MissingArrayIndexExpression 6
    ShouldBeParseError '${x}?[0] = 1' InvalidLeftHandSide 0
}

Describe 'splatting parsing' -Tags "CI" {
    ShouldBeParseError '@a' SplattingNotPermitted 0
    ShouldBeParseError 'foreach (@a in $b) {}' SplattingNotPermitted 9
    ShouldBeParseError 'param(@a)' SplattingNotPermitted 6
    ShouldBeParseError 'function foo (@a) {}' SplattingNotPermitted 14
}

Describe 'Pipes parsing' -Tags "CI" {
    ShouldBeParseError '|gps' EmptyPipeElement 0
    ShouldBeParseError 'gps|' EmptyPipeElement 4
    ShouldBeParseError 'gps| |foreach name' EmptyPipeElement 4
    ShouldBeParseError '1|1' ExpressionsMustBeFirstInPipeline 2
    ShouldBeParseError '$a=' ExpectedValueExpression 3
}

Describe 'commands parsing' -Tags "CI" {
    ShouldBeParseError 'gcm -a:' ParameterRequiresArgument 7
    ShouldBeParseError 'gcm -a: 1,' MissingExpression 11 -CheckColumnNumber
    ShouldBeParseError 'gcm ,' MissingArgument 4
}

Describe 'tokens parsing' -Tags "CI" {
    ShouldBeParseError '   )' UnexpectedToken 3
    ShouldBeParseError '   }' UnexpectedToken 4 -CheckColumnNumber
}

Describe 'expressions parsing' -Tags "CI" {
    ShouldBeParseError '1+' ExpectedValueExpression 2
    ShouldBeParseError '[a()][b]' UnexpectedAttribute 0
    ShouldBeParseError '[a()][b]2' UnexpectedAttribute 0
    ShouldBeParseError '[ref][ref]$x' ReferenceNeedsToBeByItselfInTypeSequence 5
    ShouldBeParseError '[int][ref]$x' ReferenceNeedsToBeLastTypeInTypeConversion 5
    ShouldBeParseError '[int][ref]$x = 42' ReferenceNeedsToBeByItselfInTypeConstraint 5
}

Describe 'Hash Expression parsing' -Tags "CI" {
    ShouldBeParseError '@{ a=1;b=2;c=3;' MissingEndCurlyBrace 2
}

Describe 'Unicode escape sequence parsing' -Tag "CI" {
    ShouldBeParseError '"`u{}"' InvalidUnicodeEscapeSequence 1                 # error span is >>`u{}<<
    ShouldBeParseError '"`u{219z}"' InvalidUnicodeEscapeSequence 7             # error offset is "`u{219>>z<<}"
    ShouldBeParseError '"`u{12345z}"' InvalidUnicodeEscapeSequence 9           # error offset is "`u{12345>>z<<}"
    ShouldBeParseError '"`u{1234567}"' TooManyDigitsInUnicodeEscapeSequence 10 # error offset is "`u{123456>>7<<}"
    ShouldBeParseError '"`u{110000}"' InvalidUnicodeEscapeSequenceValue 4      # error offset is "`u{>>1<<10000}"
    ShouldBeParseError '"`u2195}"' InvalidUnicodeEscapeSequence 1
    ShouldBeParseError '"`u{' InvalidUnicodeEscapeSequence,TerminatorExpectedAtEndOfString 4,0
    ShouldBeParseError '"`u{1' InvalidUnicodeEscapeSequence,TerminatorExpectedAtEndOfString 5,0
    ShouldBeParseError '"`u{123456' MissingUnicodeEscapeSequenceTerminator,TerminatorExpectedAtEndOfString 10,0
    ShouldBeParseError '"`u{1234567' TooManyDigitsInUnicodeEscapeSequence,TerminatorExpectedAtEndOfString 10,0
}

Describe "Ternary Operator parsing" -Tags CI {
    BeforeAll {
        $testCases_basic = @(
            @{ Script = '$true?2:3'; TokenKind = [System.Management.Automation.Language.TokenKind]::Variable; }
            @{ Script = '$false?';   TokenKind = [System.Management.Automation.Language.TokenKind]::Variable; }
            @{ Script = '$:abc';     TokenKind = [System.Management.Automation.Language.TokenKind]::Variable; }
            @{ Script = '$env:abc';  TokenKind = [System.Management.Automation.Language.TokenKind]::Variable; }
            @{ Script = '$env:123';  TokenKind = [System.Management.Automation.Language.TokenKind]::Variable; }
            @{ Script = 'a?2:2';     TokenKind = [System.Management.Automation.Language.TokenKind]::Generic;  }
            @{ Script = '1?2:3';     TokenKind = [System.Management.Automation.Language.TokenKind]::Generic;  }
            @{ Script = 'a?';        TokenKind = [System.Management.Automation.Language.TokenKind]::Generic;  }
            @{ Script = 'a?b';       TokenKind = [System.Management.Automation.Language.TokenKind]::Generic;  }
            @{ Script = '1?';        TokenKind = [System.Management.Automation.Language.TokenKind]::Generic;  }
            @{ Script = '?2:3';      TokenKind = [System.Management.Automation.Language.TokenKind]::Generic;  }
        )

        $testCases_incomplete = @(
            @{ Script = '$true ?';     ErrorId = "ExpectedValueExpression";         AstType = [System.Management.Automation.Language.ErrorExpressionAst] }
            @{ Script = '$true ? 3';   ErrorId = "MissingColonInTernaryExpression"; AstType = [System.Management.Automation.Language.ErrorExpressionAst] }
            @{ Script = '$true ? 3 :'; ErrorId = "ExpectedValueExpression";         AstType = [System.Management.Automation.Language.TernaryExpressionAst] }
            @{ Script = "`$true`t?";     ErrorId = "ExpectedValueExpression";         AstType = [System.Management.Automation.Language.ErrorExpressionAst] }
            @{ Script = "`$true`t?`t3";   ErrorId = "MissingColonInTernaryExpression"; AstType = [System.Management.Automation.Language.ErrorExpressionAst] }
            @{ Script = "`$true`t?`t3`t:"; ErrorId = "ExpectedValueExpression";         AstType = [System.Management.Automation.Language.TernaryExpressionAst] }
        )
    }

    It "Question-mark and colon parsed correctly in <Script> when not in ternary expression context" -TestCases $testCases_basic {
        param($Script, $TokenKind)

        $tks = $null
        $ers = $null
        $result = [System.Management.Automation.Language.Parser]::ParseInput($Script, [ref]$tks, [ref]$ers)

        $tks[0].Kind | Should -BeExactly $TokenKind
        $tks[0].Text | Should -BeExactly $Script

        if ($TokenKind -eq "Variable") {
            $result.EndBlock.Statements[0].PipelineElements[0].Expression | Should -BeOfType System.Management.Automation.Language.VariableExpressionAst
            $result.EndBlock.Statements[0].PipelineElements[0].Expression.Extent.Text | Should -BeExactly $Script
        } else {
            $result.EndBlock.Statements[0].PipelineElements[0].CommandElements[0] | Should -BeOfType System.Management.Automation.Language.StringConstantExpressionAst
            $result.EndBlock.Statements[0].PipelineElements[0].CommandElements[0].Extent.Text | Should -BeExactly $Script
        }
    }

    It "Question-mark and colon can be used as command names" {
        function a?b:c { 'a?b:c' }
        function 2?3:4 { '2?3:4' }

        a?b:c | Should -BeExactly 'a?b:c'
        2?3:4 | Should -BeExactly '2?3:4'
    }

    It "Incomplete ternary expression <Script> should generate correct error" -TestCases $testCases_incomplete {
        param($Script, $ErrorId, $AstType)

        $ers = $null
        $result = [System.Management.Automation.Language.Parser]::ParseInput($Script, [ref]$null, [ref]$ers)

        $ers.Count | Should -Be 1
        $ers.IncompleteInput | Should -BeTrue
        $ers.ErrorId | Should -BeExactly $ErrorId

        $result.EndBlock.Statements[0].PipelineElements[0].Expression | Should -BeOfType $AstType
    }

    It "Generate ternary AST when operands are missing - '`$true ? :'" {
        $ers = $null
        $result = [System.Management.Automation.Language.Parser]::ParseInput('$true ? :', [ref]$null, [ref]$ers)
        $ers.Count | Should -Be 2

        $ers[0].IncompleteInput | Should -BeFalse
        $ers[0].ErrorId | Should -BeExactly 'ExpectedValueExpression'
        $ers[1].IncompleteInput | Should -BeTrue
        $ers[1].ErrorId | Should -BeExactly 'ExpectedValueExpression'

        $expr = $result.EndBlock.Statements[0].PipelineElements[0].Expression
        $expr | Should -BeOfType System.Management.Automation.Language.TernaryExpressionAst
        $expr.IfTrue | Should -BeOfType System.Management.Automation.Language.ErrorExpressionAst
        $expr.IfFalse | Should -BeOfType System.Management.Automation.Language.ErrorExpressionAst
    }

    It "Generate ternary AST when operands are missing - '`$true ? : 3'" {
        $ers = $null
        $result = [System.Management.Automation.Language.Parser]::ParseInput('$true ? : 3', [ref]$null, [ref]$ers)
        $ers.Count | Should -Be 1

        $ers.IncompleteInput | Should -BeFalse
        $ers.ErrorId | Should -BeExactly "ExpectedValueExpression"
        $expr = $result.EndBlock.Statements[0].PipelineElements[0].Expression
        $expr | Should -BeOfType System.Management.Automation.Language.TernaryExpressionAst
        $expr.IfTrue | Should -BeOfType System.Management.Automation.Language.ErrorExpressionAst
        $expr.IfFalse | Should -BeOfType System.Management.Automation.Language.ConstantExpressionAst
    }
}

Describe "ParserError type tests" -Tag CI {
    # This test was added because there use to be a hardcoded newline in the ToString() method of
    # the ParseError class. This makes sure the proper newlines are used.
    It "Should use consistent newline depending on OS" {
        $ers = $null
        [System.Management.Automation.Language.Parser]::ParseInput('$x =', [ref]$null, [ref]$ers) | Out-Null
        $measureResult = $ers[0].ToString() -split [System.Environment]::NewLine | Measure-Object

        # We expect the string to have 4 lines. That means that if we split by NewLine for that platform,
        # We should have 4 as the count.
        $measureResult.Count | Should -BeExactly 4

        # Just checking the above is not enough. We should also make sure that on non-Windows, there are no
        # `r`n
        if (!$IsWindows) {
            $measureResult = $ers[0].ToString() | Should -Not -Contain "`r`n"
        }
    }
}

Describe "Keywords 'default', 'hidden', 'in', 'static' Token parsing" -Tags CI {
    BeforeAll {
        $testCases_basic = @(
            @{
                Script = 'switch (1) {default {0} 1 {1}}'
                TokensToCheck = @{
                    5 = @{
                        TokenKind = [System.Management.Automation.Language.TokenKind]::Default
                        TokenFlags_Mask = [System.Management.Automation.Language.TokenFlags]::Keyword
                        TokenFlags_Value = [System.Management.Automation.Language.TokenFlags]::Keyword
                    }
                }
            }
            @{
                Script = 'switch (1) {"default" {0} 1 {1}}'
                TokensToCheck = @{
                    5 = @{
                        TokenKind = [System.Management.Automation.Language.TokenKind]::StringExpandable
                        TokenFlags_Mask = [System.Management.Automation.Language.TokenFlags]::Keyword
                        TokenFlags_Value = [System.Management.Automation.Language.TokenFlags]::None
                    }
                }
            }
            @{
                Script = 'switch (1) {adefault {0} 1 {1}}'
                TokensToCheck = @{
                    5 = @{
                        TokenKind = [System.Management.Automation.Language.TokenKind]::Identifier
                        TokenFlags_Mask = [System.Management.Automation.Language.TokenFlags]::Keyword
                        TokenFlags_Value = [System.Management.Automation.Language.TokenFlags]::None
                    }
                }
            }
            @{
                Script = 'foreach ($i in 1..2) {$i}'
                TokensToCheck = @{
                    3 = @{
                        TokenKind = [System.Management.Automation.Language.TokenKind]::In
                        TokenFlags_Mask = [System.Management.Automation.Language.TokenFlags]::Keyword
                        TokenFlags_Value = [System.Management.Automation.Language.TokenFlags]::Keyword
                    }
                }
            }
            @{
                Script = 'class test {hidden $a; static aMethod () {return $this.a} }'
                TokensToCheck = @{
                    3 = @{
                        TokenKind = [System.Management.Automation.Language.TokenKind]::Hidden
                        TokenFlags_Mask = [System.Management.Automation.Language.TokenFlags]::Keyword
                        TokenFlags_Value = [System.Management.Automation.Language.TokenFlags]::Keyword
                    }
                    6 = @{
                        TokenKind = [System.Management.Automation.Language.TokenKind]::Static
                        TokenFlags_Mask = [System.Management.Automation.Language.TokenFlags]::Keyword
                        TokenFlags_Value = [System.Management.Automation.Language.TokenFlags]::Keyword
                    }
                }
            }
            @{
                Script = 'echo default hidden in static'
                TokensToCheck = @{
                    1 = @{
                        TokenKind = [System.Management.Automation.Language.TokenKind]::Generic
                        TokenFlags_Mask = [System.Management.Automation.Language.TokenFlags]::Keyword
                        TokenFlags_Value = [System.Management.Automation.Language.TokenFlags]::None
                    }
                    2 = @{
                        TokenKind = [System.Management.Automation.Language.TokenKind]::Generic
                        TokenFlags_Mask = [System.Management.Automation.Language.TokenFlags]::Keyword
                        TokenFlags_Value = [System.Management.Automation.Language.TokenFlags]::None
                    }
                    3 = @{
                        TokenKind = [System.Management.Automation.Language.TokenKind]::Generic
                        TokenFlags_Mask = [System.Management.Automation.Language.TokenFlags]::Keyword
                        TokenFlags_Value = [System.Management.Automation.Language.TokenFlags]::None
                    }
                    4 = @{
                        TokenKind = [System.Management.Automation.Language.TokenKind]::Generic
                        TokenFlags_Mask = [System.Management.Automation.Language.TokenFlags]::Keyword
                        TokenFlags_Value = [System.Management.Automation.Language.TokenFlags]::None
                    }
                }
            }
            @{
                Script = 'default'
                TokensToCheck = @{
                    0 = @{
                        TokenKind = [System.Management.Automation.Language.TokenKind]::Default
                        TokenFlags_Mask = [System.Management.Automation.Language.TokenFlags]::Keyword -bor [System.Management.Automation.Language.TokenFlags]::CommandName
                        TokenFlags_Value = [System.Management.Automation.Language.TokenFlags]::CommandName
                    }
                }
            }
            @{
                Script = 'hidden'
                TokensToCheck = @{
                    0 = @{
                        TokenKind = [System.Management.Automation.Language.TokenKind]::Hidden
                        TokenFlags_Mask = [System.Management.Automation.Language.TokenFlags]::Keyword -bor [System.Management.Automation.Language.TokenFlags]::CommandName
                        TokenFlags_Value = [System.Management.Automation.Language.TokenFlags]::CommandName
                    }
                }
            }
            @{
                Script = 'in'
                TokensToCheck = @{
                    0 = @{
                        TokenKind = [System.Management.Automation.Language.TokenKind]::In
                        TokenFlags_Mask = [System.Management.Automation.Language.TokenFlags]::Keyword -bor [System.Management.Automation.Language.TokenFlags]::CommandName
                        TokenFlags_Value = [System.Management.Automation.Language.TokenFlags]::CommandName
                    }
                }
            }
            @{
                Script = 'static'
                TokensToCheck = @{
                    0 = @{
                        TokenKind = [System.Management.Automation.Language.TokenKind]::Static
                        TokenFlags_Mask = [System.Management.Automation.Language.TokenFlags]::Keyword -bor [System.Management.Automation.Language.TokenFlags]::CommandName
                        TokenFlags_Value = [System.Management.Automation.Language.TokenFlags]::CommandName
                    }
                }
            }
        )
    }

    AfterAll {
    }

    It "Keywords 'default', 'hidden', 'in', 'static' in {<Script>} correctly tokenized." -TestCases $testCases_basic {
        param($Script, $TokensToCheck)

        $tks = $null
        $ers = $null
        $result = [System.Management.Automation.Language.Parser]::ParseInput($Script, [ref]$tks, [ref]$ers)

        foreach ($token in $TokensToCheck.Keys ) {
            if ($TokensToCheck[$Token].ContainsKey('TokenKind')) {
                $tks[$token].Kind | Should -Be $TokensToCheck[$token].TokenKind -Because 'because TokenKind must be as expected'
            }
            if ($TokensToCheck[$Token].ContainsKey('TokenFlags_Value')) {
                $tks[$token].TokenFlags -band $TokensToCheck[$token].TokenFlags_Mask | Should -Be $TokensToCheck[$token].TokenFlags_Value -Because 'because TokenFlags must be as expected after masking'
            }
        }
    }

    $testKeywordsAsCmds = @(
        @{ Keyword = 'default' }
        @{ Keyword = 'hidden' }
        @{ Keyword = 'in' }         # Note: this overwrites Pester's `In` function.
        @{ Keyword = 'static' }
    )

    It "<Keyword> can be used as command name" -TestCases $testKeywordsAsCmds {
        param($Keyword)

        Invoke-Expression "function $Keyword { '$Keyword' }"

        . $Keyword | Should -BeExactly $Keyword
    }
}

Describe "Parsing array that has too many dimensions" -Tag CI {
    It "ParseError for '<Script>'" -TestCases @(
        @{ Script = '[int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,'; ErrorId = @('ArrayHasTooManyDimensions', 'EndSquareBracketExpectedAtEndOfAttribute'); StartOffset = @(5, 37); EndOffset = @(37, 37) }
        @{ Script = '[int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]'; ErrorId = @('ArrayHasTooManyDimensions', 'EndSquareBracketExpectedAtEndOfAttribute'); StartOffset = @(5, 38); EndOffset = @(37, 38) }
        @{ Script = '[int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]]'; ErrorId = @('ArrayHasTooManyDimensions'); StartOffset = @(5); EndOffset = @(37) }
    ) {
        param($Script, $ErrorId, $StartOffset, $EndOffset)

        $errs = Get-ParseResults -src $Script
        $errs.Count | Should -Be $ErrorId.Count
        for ($i = 0; $i -lt $errs.Count; $i++) {
            $errs[$i].ErrorId | Should -BeExactly $ErrorId[$i]
            $errs[$i].Extent.StartScriptPosition.Offset | Should -Be $StartOffset[$i]
            $errs[$i].Extent.EndScriptPosition.Offset | Should -Be $EndOffset[$i]
        }
    }
}

Describe "Parsing using statement with alias and linebreak and comma" -Tag CI {
    It "ParseError for '<Script>'" -TestCases @(
        @{ Script = "using namespace x =`n"; ErrorId = @('MissingNamespaceAlias'); StartOffset = @(19); EndOffset = @(19) }
        @{ Script = "using namespace x = `n"; ErrorId = @('MissingNamespaceAlias'); StartOffset = @(19); EndOffset = @(19) }
        @{ Script = "using namespace x = ;"; ErrorId = @('MissingNamespaceAlias'); StartOffset = @(19); EndOffset = @(19) }
        @{ Script = "using namespace x = ,"; ErrorId = @('UnexpectedUnaryOperator'); StartOffset = @(20); EndOffset = @(21) }
        @{ Script = "using namespace x = &"; ErrorId = @('InvalidValueForUsingItemName','MissingExpression'); StartOffset = @(20, 20); EndOffset = @(21, 21) }
    ) {
        param($Script, $ErrorId, $StartOffset, $EndOffset)

        $errs = Get-ParseResults -src $Script
        $errs.Count | Should -Be $ErrorId.Count
        for ($i = 0; $i -lt $errs.Count; $i++) {
            $errs[$i].ErrorId | Should -BeExactly $ErrorId[$i]
            $errs[$i].Extent.StartScriptPosition.Offset | Should -Be $StartOffset[$i]
            $errs[$i].Extent.EndScriptPosition.Offset | Should -Be $EndOffset[$i]
        }
    }
}
