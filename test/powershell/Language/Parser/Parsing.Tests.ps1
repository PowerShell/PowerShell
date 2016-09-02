Import-Module $PSScriptRoot\..\LanguageTestSupport.psm1 -force
set-strictmode -v 2    

    
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
    ShouldBeParseError 'dynamicparam' MissingNamedStatementBlock 12
    ShouldBeParseError 'begin process {}' MissingNamedStatementBlock 6 -CheckColumnNumber
    ShouldBeParseError 'end process {}' MissingNamedStatementBlock 4 -CheckColumnNumber
    ShouldBeParseError 'dynamicparam process {}' MissingNamedStatementBlock 13 -CheckColumnNumber
    ShouldBeParseError 'process begin {}' MissingNamedStatementBlock 8 -CheckColumnNumber
    ShouldBeParseError 'begin process end' MissingNamedStatementBlock,MissingNamedStatementBlock,MissingNamedStatementBlock 6,14,18 -CheckColumnNumber

    Test-Ast 'begin' 'begin' 'begin'
    Test-Ast 'begin end' 'begin end' 'begin' 'end'
    Test-Ast 'begin end process' 'begin end process' 'begin' 'end' 'process'
    Test-Ast 'begin {} end' 'begin {} end' 'begin {}' 'end'
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


Describe 'splatting parsing' -Tags "CI" {
    ShouldBeParseError '@a' SplattingNotPermitted 0
    ShouldBeParseError 'foreach (@a in $b) {}' SplattingNotPermitted 9
    ShouldBeParseError 'param(@a)' SplattingNotPermitted 6
    ShouldBeParseError 'function foo (@a) {}' SplattingNotPermitted 14
}

Describe 'Pipes parsing' -Tags "CI" {
    ShouldBeParseError 'gps|' EmptyPipeElement 4
    ShouldBeParseError '1|1' ExpressionsMustBeFirstInPipeline 2
    ShouldBeParseError '$a=' ExpectedValueExpression 3
    ShouldBeParseError '1 &' UnexpectedToken,MissingExpression 2,2
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