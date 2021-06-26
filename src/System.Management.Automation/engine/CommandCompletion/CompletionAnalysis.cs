// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Management.Automation.Subsystem;
using System.Management.Automation.Subsystem.DSC;

namespace System.Management.Automation
{
    internal class CompletionContext
    {
        internal List<Ast> RelatedAsts { get; set; }

        // Only one of TokenAtCursor or TokenBeforeCursor is set
        // This is how we can tell if we're trying to complete part of something (like a member)
        // or complete an argument, where TokenBeforeCursor could be a parameter name.
        internal Token TokenAtCursor { get; set; }

        internal Token TokenBeforeCursor { get; set; }

        internal IScriptPosition CursorPosition { get; set; }

        internal PowerShellExecutionHelper Helper { get; set; }

        internal Hashtable Options { get; set; }

        internal Dictionary<string, ScriptBlock> CustomArgumentCompleters { get; set; }

        internal Dictionary<string, ScriptBlock> NativeArgumentCompleters { get; set; }

        internal string WordToComplete { get; set; }

        internal int ReplacementIndex { get; set; }

        internal int ReplacementLength { get; set; }

        internal ExecutionContext ExecutionContext { get; set; }

        internal PseudoBindingInfo PseudoBindingInfo { get; set; }

        internal TypeInferenceContext TypeInferenceContext { get; set; }

        internal bool GetOption(string option, bool @default)
        {
            if (Options == null || !Options.ContainsKey(option))
            {
                return @default;
            }

            return LanguagePrimitives.ConvertTo<bool>(Options[option]);
        }
    }

    internal class CompletionAnalysis
    {
        private readonly Ast _ast;
        private readonly Token[] _tokens;
        private readonly IScriptPosition _cursorPosition;
        private readonly Hashtable _options;

        internal CompletionAnalysis(Ast ast, Token[] tokens, IScriptPosition cursorPosition, Hashtable options)
        {
            _ast = ast;
            _tokens = tokens;
            _cursorPosition = cursorPosition;
            _options = options;
        }

        private static bool IsInterestingToken(Token token)
        {
            return token.Kind != TokenKind.NewLine && token.Kind != TokenKind.EndOfInput;
        }

        private static bool IsCursorWithinOrJustAfterExtent(IScriptPosition cursor, IScriptExtent extent)
        {
            return cursor.Offset > extent.StartOffset && cursor.Offset <= extent.EndOffset;
        }

        private static bool IsCursorRightAfterExtent(IScriptPosition cursor, IScriptExtent extent)
        {
            return cursor.Offset == extent.EndOffset;
        }

        private static bool IsCursorAfterExtentAndInTheSameLine(IScriptPosition cursor, IScriptExtent extent)
        {
            return cursor.Offset >= extent.EndOffset && extent.EndLineNumber == cursor.LineNumber;
        }

        private static bool IsCursorBeforeExtent(IScriptPosition cursor, IScriptExtent extent)
        {
            return cursor.Offset < extent.StartOffset;
        }

        private static bool IsCursorAfterExtent(IScriptPosition cursor, IScriptExtent extent)
        {
            return extent.EndOffset < cursor.Offset;
        }

        private static bool IsCursorOutsideOfExtent(IScriptPosition cursor, IScriptExtent extent)
        {
            return cursor.Offset < extent.StartOffset || cursor.Offset > extent.EndOffset;
        }

        internal readonly struct AstAnalysisContext
        {
            internal AstAnalysisContext(Token tokenAtCursor, Token tokenBeforeCursor, List<Ast> relatedAsts, int replacementIndex)
            {
                TokenAtCursor = tokenAtCursor;
                TokenBeforeCursor = tokenBeforeCursor;
                RelatedAsts = relatedAsts;
                ReplacementIndex = replacementIndex;
            }

            internal readonly Token TokenAtCursor;
            internal readonly Token TokenBeforeCursor;
            internal readonly List<Ast> RelatedAsts;
            internal readonly int ReplacementIndex;
        }

        internal static AstAnalysisContext ExtractAstContext(Ast inputAst, Token[] inputTokens, IScriptPosition cursor)
        {
            bool adjustLineAndColumn = false;
            IScriptPosition positionForAstSearch = cursor;

            Token tokenBeforeCursor = null;
            Token tokenAtCursor = InterstingTokenAtCursorOrDefault(inputTokens, cursor);
            if (tokenAtCursor == null)
            {
                tokenBeforeCursor = InterstingTokenBeforeCursorOrDefault(inputTokens, cursor);
                if (tokenBeforeCursor != null)
                {
                    positionForAstSearch = tokenBeforeCursor.Extent.EndScriptPosition;
                    adjustLineAndColumn = true;
                }
            }
            else
            {
                var stringExpandableToken = tokenAtCursor as StringExpandableToken;
                if (stringExpandableToken?.NestedTokens != null)
                {
                    tokenAtCursor = InterstingTokenAtCursorOrDefault(stringExpandableToken.NestedTokens, cursor) ?? stringExpandableToken;
                }
            }

            int replacementIndex = adjustLineAndColumn ? cursor.Offset : 0;
            List<Ast> relatedAsts = AstSearcher.FindAll(
                inputAst,
                ast => IsCursorWithinOrJustAfterExtent(positionForAstSearch, ast.Extent),
                searchNestedScriptBlocks: true).ToList();

            Diagnostics.Assert(tokenAtCursor == null || tokenBeforeCursor == null, "Only one of these tokens can be non-null");

            return new AstAnalysisContext(tokenAtCursor, tokenBeforeCursor, relatedAsts, replacementIndex);
        }

        internal CompletionContext CreateCompletionContext(PowerShell powerShell)
        {
            var typeInferenceContext = new TypeInferenceContext(powerShell);
            return InitializeCompletionContext(typeInferenceContext);
        }

        internal CompletionContext CreateCompletionContext(TypeInferenceContext typeInferenceContext)
        {
            return InitializeCompletionContext(typeInferenceContext);
        }

        private CompletionContext InitializeCompletionContext(TypeInferenceContext typeInferenceContext)
        {
            var astContext = ExtractAstContext(_ast, _tokens, _cursorPosition);

            if (typeInferenceContext.CurrentTypeDefinitionAst == null)
            {
                typeInferenceContext.CurrentTypeDefinitionAst = Ast.GetAncestorTypeDefinitionAst(astContext.RelatedAsts.Last());
            }

            ExecutionContext executionContext = typeInferenceContext.ExecutionContext;

            return new CompletionContext
            {
                Options = _options,
                CursorPosition = _cursorPosition,
                TokenAtCursor = astContext.TokenAtCursor,
                TokenBeforeCursor = astContext.TokenBeforeCursor,
                RelatedAsts = astContext.RelatedAsts,
                ReplacementIndex = astContext.ReplacementIndex,
                ExecutionContext = executionContext,
                TypeInferenceContext = typeInferenceContext,
                Helper = typeInferenceContext.Helper,
                CustomArgumentCompleters = executionContext.CustomArgumentCompleters,
                NativeArgumentCompleters = executionContext.NativeArgumentCompleters,
            };
        }

        private static Token InterstingTokenAtCursorOrDefault(IReadOnlyList<Token> tokens, IScriptPosition cursorPosition)
        {
            for (int i = tokens.Count - 1; i >= 0; --i)
            {
                Token token = tokens[i];
                if (IsCursorWithinOrJustAfterExtent(cursorPosition, token.Extent) && IsInterestingToken(token))
                {
                    return token;
                }
            }

            return null;
        }

        private static Token InterstingTokenBeforeCursorOrDefault(IReadOnlyList<Token> tokens, IScriptPosition cursorPosition)
        {
            for (int i = tokens.Count - 1; i >= 0; --i)
            {
                Token token = tokens[i];
                if (IsCursorAfterExtent(cursorPosition, token.Extent) && IsInterestingToken(token))
                {
                    return token;
                }
            }

            return null;
        }

        private static Ast GetLastAstAtCursor(ScriptBlockAst scriptBlockAst, IScriptPosition cursorPosition)
        {
            var asts = AstSearcher.FindAll(scriptBlockAst, ast => IsCursorRightAfterExtent(cursorPosition, ast.Extent), searchNestedScriptBlocks: true);
            return asts.LastOrDefault();
        }

        #region Special Cases

        /// <summary>
        /// Check if we should complete file names for "switch -file"
        /// </summary>
        private static bool CompleteAgainstSwitchFile(Ast lastAst, Token tokenBeforeCursor)
        {
            Tuple<Token, Ast> fileConditionTuple;

            var errorStatement = lastAst as ErrorStatementAst;
            if (errorStatement != null && errorStatement.Flags != null && errorStatement.Kind != null && tokenBeforeCursor != null &&
                errorStatement.Kind.Kind.Equals(TokenKind.Switch) && errorStatement.Flags.TryGetValue("file", out fileConditionTuple))
            {
                // Handle "switch -file <tab>"
                return fileConditionTuple.Item1.Extent.EndOffset == tokenBeforeCursor.Extent.EndOffset;
            }

            if (lastAst.Parent is CommandExpressionAst)
            {
                // Handle "switch -file m<tab>" or "switch -file *.ps1<tab>"
                if (!(lastAst.Parent.Parent is PipelineAst pipeline))
                {
                    return false;
                }

                errorStatement = pipeline.Parent as ErrorStatementAst;
                if (errorStatement == null || errorStatement.Kind == null || errorStatement.Flags == null)
                {
                    return false;
                }

                return (errorStatement.Kind.Kind.Equals(TokenKind.Switch) &&
                        errorStatement.Flags.TryGetValue("file", out fileConditionTuple) && fileConditionTuple.Item2 == pipeline);
            }

            return false;
        }

        private static bool CompleteOperator(Token tokenAtCursor, Ast lastAst)
        {
            if (tokenAtCursor.Kind == TokenKind.Minus)
            {
                return lastAst is BinaryExpressionAst;
            }
            else if (tokenAtCursor.Kind == TokenKind.Parameter)
            {
                if (lastAst is CommandParameterAst)
                    return lastAst.Parent is ExpressionAst;
            }

            return false;
        }

        private static bool CompleteAgainstStatementFlags(Ast scriptAst, Ast lastAst, Token token, out TokenKind kind)
        {
            kind = TokenKind.Unknown;

            // Handle "switch -f<tab>"
            var errorStatement = lastAst as ErrorStatementAst;
            if (errorStatement != null && errorStatement.Kind != null)
            {
                switch (errorStatement.Kind.Kind)
                {
                    case TokenKind.Switch:
                        kind = TokenKind.Switch;
                        return true;
                    default:
                        break;
                }
            }

            // Handle "switch -<tab>". Skip cases like "switch ($a) {} -<tab> "
            var scriptBlockAst = scriptAst as ScriptBlockAst;
            if (token != null && token.Kind == TokenKind.Minus && scriptBlockAst != null)
            {
                var asts = AstSearcher.FindAll(scriptBlockAst, ast => IsCursorAfterExtent(token.Extent.StartScriptPosition, ast.Extent), searchNestedScriptBlocks: true);

                Ast last = asts.LastOrDefault();
                errorStatement = null;

                while (last != null)
                {
                    errorStatement = last as ErrorStatementAst;
                    if (errorStatement != null) { break; }

                    last = last.Parent;
                }

                if (errorStatement != null && errorStatement.Kind != null)
                {
                    switch (errorStatement.Kind.Kind)
                    {
                        case TokenKind.Switch:

                            Tuple<Token, Ast> value;
                            if (errorStatement.Flags != null && errorStatement.Flags.TryGetValue(Parser.VERBATIM_ARGUMENT, out value))
                            {
                                if (IsTokenTheSame(value.Item1, token))
                                {
                                    kind = TokenKind.Switch;
                                    return true;
                                }
                            }

                            break;

                        default:
                            break;
                    }
                }
            }

            return false;
        }

        private static bool IsTokenTheSame(Token x, Token y)
        {
            if (x.Kind == y.Kind && x.TokenFlags == y.TokenFlags &&
                x.Extent.StartLineNumber == y.Extent.StartLineNumber &&
                x.Extent.StartColumnNumber == y.Extent.StartColumnNumber &&
                x.Extent.EndLineNumber == y.Extent.EndLineNumber &&
                x.Extent.EndColumnNumber == y.Extent.EndColumnNumber)
            {
                return true;
            }

            return false;
        }

        #endregion Special Cases

        internal List<CompletionResult> GetResults(PowerShell powerShell, out int replacementIndex, out int replacementLength)
        {
            var completionContext = CreateCompletionContext(powerShell);

            PSLanguageMode? previousLanguageMode = null;
            try
            {
                // Tab expansion is called from a trusted function - we should apply ConstrainedLanguage if necessary.
                if (completionContext.ExecutionContext.HasRunspaceEverUsedConstrainedLanguageMode)
                {
                    previousLanguageMode = completionContext.ExecutionContext.LanguageMode;
                    completionContext.ExecutionContext.LanguageMode = PSLanguageMode.ConstrainedLanguage;
                }

                return GetResultHelper(completionContext, out replacementIndex, out replacementLength, false);
            }
            finally
            {
                if (previousLanguageMode.HasValue)
                {
                    completionContext.ExecutionContext.LanguageMode = previousLanguageMode.Value;
                }
            }
        }

        internal List<CompletionResult> GetResultHelper(CompletionContext completionContext, out int replacementIndex, out int replacementLength, bool isQuotedString)
        {
            replacementIndex = -1;
            replacementLength = -1;

            var tokenAtCursor = completionContext.TokenAtCursor;
            var lastAst = completionContext.RelatedAsts.Last();
            List<CompletionResult> result = null;
            if (tokenAtCursor != null)
            {
                replacementIndex = tokenAtCursor.Extent.StartScriptPosition.Offset;
                replacementLength = tokenAtCursor.Extent.EndScriptPosition.Offset - replacementIndex;

                completionContext.ReplacementIndex = replacementIndex;
                completionContext.ReplacementLength = replacementLength;

                switch (tokenAtCursor.Kind)
                {
                    case TokenKind.Variable:
                    case TokenKind.SplattedVariable:
                        completionContext.WordToComplete = ((VariableToken)tokenAtCursor).VariablePath.UserPath;
                        result = CompletionCompleters.CompleteVariable(completionContext);
                        break;

                    case TokenKind.Multiply:
                    case TokenKind.Generic:
                    case TokenKind.MinusMinus: // for native commands '--'
                    case TokenKind.Identifier:
                        result = GetResultForIdentifier(completionContext, ref replacementIndex, ref replacementLength, isQuotedString);
                        break;

                    case TokenKind.Parameter:
                        // When it's the content of a quoted string, we only handle variable/member completion
                        if (isQuotedString)
                            break;

                        completionContext.WordToComplete = tokenAtCursor.Text;
                        var cmdAst = lastAst.Parent as CommandAst;
                        if (lastAst is StringConstantExpressionAst && cmdAst != null && cmdAst.CommandElements.Count == 1)
                        {
                            result = CompleteFileNameAsCommand(completionContext);
                            break;
                        }

                        TokenKind statementKind;
                        if (CompleteAgainstStatementFlags(null, lastAst, null, out statementKind))
                        {
                            result = CompletionCompleters.CompleteStatementFlags(statementKind, completionContext.WordToComplete);
                            break;
                        }

                        if (CompleteOperator(tokenAtCursor, lastAst))
                        {
                            result = CompletionCompleters.CompleteOperator(completionContext.WordToComplete);
                            break;
                        }

                        // Handle scenarios like this: dir -path:<tab>
                        if (completionContext.WordToComplete.EndsWith(':'))
                        {
                            replacementIndex = tokenAtCursor.Extent.EndScriptPosition.Offset;
                            replacementLength = 0;

                            completionContext.WordToComplete = string.Empty;
                            result = CompletionCompleters.CompleteCommandArgument(completionContext);
                        }
                        else
                        {
                            result = CompletionCompleters.CompleteCommandParameter(completionContext);
                        }

                        break;

                    case TokenKind.Dot:
                    case TokenKind.ColonColon:
                    case TokenKind.QuestionDot:
                        replacementIndex += tokenAtCursor.Text.Length;
                        replacementLength = 0;
                        result = CompletionCompleters.CompleteMember(completionContext, @static: tokenAtCursor.Kind == TokenKind.ColonColon);
                        break;

                    case TokenKind.Comment:
                        // When it's the content of a quoted string, we only handle variable/member completion
                        if (isQuotedString)
                            break;

                        completionContext.WordToComplete = tokenAtCursor.Text;
                        result = CompletionCompleters.CompleteComment(completionContext, ref replacementIndex, ref replacementLength);
                        break;

                    case TokenKind.StringExpandable:
                    case TokenKind.StringLiteral:
                        // Search to see if we're looking at an assignment
                        if (lastAst.Parent is CommandExpressionAst
                            && lastAst.Parent.Parent is AssignmentStatementAst assignmentAst)
                        {
                            // Handle scenarios like `$ErrorActionPreference = '<tab>`
                            if (TryGetCompletionsForVariableAssignment(completionContext, assignmentAst, out List<CompletionResult> completions))
                            {
                                return completions;
                            }
                        }

                        result = GetResultForString(completionContext, ref replacementIndex, ref replacementLength, isQuotedString);
                        break;

                    case TokenKind.RBracket:
                        if (lastAst is TypeExpressionAst)
                        {
                            var targetExpr = (TypeExpressionAst)lastAst;
                            var memberResult = new List<CompletionResult>();

                            CompletionCompleters.CompleteMemberHelper(
                                true,
                                "*",
                                targetExpr,
                                completionContext, memberResult);

                            if (memberResult.Count > 0)
                            {
                                replacementIndex++;
                                replacementLength = 0;
                                result = (from entry in memberResult
                                          let completionText = TokenKind.ColonColon.Text() + entry.CompletionText
                                          select new CompletionResult(completionText, entry.ListItemText, entry.ResultType, entry.ToolTip)).ToList();
                            }
                        }

                        break;

                    case TokenKind.Comma:
                        // Handle array elements such as dir .\cd,<tab> || dir -Path: .\cd,<tab>
                        if (lastAst is ErrorExpressionAst &&
                            (lastAst.Parent is CommandAst || lastAst.Parent is CommandParameterAst))
                        {
                            replacementIndex += replacementLength;
                            replacementLength = 0;

                            result = CompletionCompleters.CompleteCommandArgument(completionContext);
                        }
                        else if (lastAst is AttributeAst)
                        {
                            completionContext.ReplacementIndex = replacementIndex += tokenAtCursor.Text.Length;
                            completionContext.ReplacementLength = replacementLength = 0;
                            result = GetResultForAttributeArgument(completionContext, ref replacementIndex, ref replacementLength);
                        }
                        else
                        {
                            //
                            // Handle auto completion for enum/dependson property of DSC resource,
                            // cursor is right after ','
                            //
                            // Configuration config
                            // {
                            //     User test
                            //     {
                            //         DependsOn=@('[user]x',|)
                            //
                            bool unused;
                            result = GetResultForEnumPropertyValueOfDSCResource(completionContext, string.Empty, ref replacementIndex, ref replacementLength, out unused);
                        }

                        break;
                    case TokenKind.AtCurly:
                        // Handle scenarios such as 'Sort-Object @{<tab>' and  'gci | Format-Table @{'
                        result = GetResultForHashtable(completionContext);
                        replacementIndex += 2;
                        replacementLength = 0;
                        break;

                    case TokenKind.Semi:
                        // Handle scenarios such as 'gci | Format-Table @{Label=...;<tab>'
                        if (lastAst is HashtableAst)
                        {
                            result = GetResultForHashtable(completionContext);
                            replacementIndex += 1;
                            replacementLength = 0;
                        }

                        break;

                    case TokenKind.Number:
                        // Handle scenarios such as Get-Process -Id 5<tab> || Get-Process -Id 5210, 3<tab> || Get-Process -Id: 5210, 3<tab>
                        if (lastAst is ConstantExpressionAst &&
                            (lastAst.Parent is CommandAst || lastAst.Parent is CommandParameterAst ||
                            (lastAst.Parent is ArrayLiteralAst &&
                             (lastAst.Parent.Parent is CommandAst || lastAst.Parent.Parent is CommandParameterAst))))
                        {
                            completionContext.WordToComplete = tokenAtCursor.Text;
                            result = CompletionCompleters.CompleteCommandArgument(completionContext);

                            replacementIndex = completionContext.ReplacementIndex;
                            replacementLength = completionContext.ReplacementLength;
                        }
                        else if (lastAst.Parent is CommandExpressionAst
                            && lastAst.Parent.Parent is AssignmentStatementAst assignmentAst2)
                        {
                            // Handle scenarios like '[ValidateSet(11,22)][int]$i = 11; $i = 2<tab>'
                            if (TryGetCompletionsForVariableAssignment(completionContext, assignmentAst2, out List<CompletionResult> completions))
                            {
                                result = completions;
                            }
                        }

                        break;

                    case TokenKind.Redirection:
                        // Handle file name completion after the redirection operator: gps ><tab> || gps >><tab> || dir con 2><tab> || dir con 2>><tab>
                        if (lastAst is ErrorExpressionAst && lastAst.Parent is FileRedirectionAst)
                        {
                            completionContext.WordToComplete = string.Empty;
                            completionContext.ReplacementIndex = (replacementIndex += tokenAtCursor.Text.Length);
                            completionContext.ReplacementLength = replacementLength = 0;
                            result = new List<CompletionResult>(CompletionCompleters.CompleteFilename(completionContext));
                        }

                        break;

                    case TokenKind.Minus:
                        // Handle operator completion: 55 -<tab> || "string" -<tab> || (Get-Something) -<tab>
                        if (CompleteOperator(tokenAtCursor, lastAst))
                        {
                            result = CompletionCompleters.CompleteOperator(string.Empty);
                            break;
                        }

                        // Handle the flag completion for statements, such as the switch statement
                        if (CompleteAgainstStatementFlags(completionContext.RelatedAsts[0], null, tokenAtCursor, out statementKind))
                        {
                            completionContext.WordToComplete = tokenAtCursor.Text;
                            result = CompletionCompleters.CompleteStatementFlags(statementKind, completionContext.WordToComplete);
                            break;
                        }

                        break;

                    case TokenKind.DynamicKeyword:
                        {
                            DynamicKeywordStatementAst keywordAst;
                            ConfigurationDefinitionAst configureAst = GetAncestorConfigurationAstAndKeywordAst(
                                completionContext.CursorPosition, lastAst, out keywordAst);
                            Diagnostics.Assert(configureAst != null, "ConfigurationDefinitionAst should never be null");
                            bool matched = false;
                            completionContext.WordToComplete = tokenAtCursor.Text.Trim();
                            // Current token is within ConfigurationDefinitionAst or DynamicKeywordStatementAst
                            return GetResultForIdentifierInConfiguration(completionContext, configureAst, null, out matched);
                        }
                    case TokenKind.Equals:
                    case TokenKind.AtParen:
                    case TokenKind.LParen:
                        {
                            if (lastAst is AttributeAst)
                            {
                                completionContext.ReplacementIndex = replacementIndex += tokenAtCursor.Text.Length;
                                completionContext.ReplacementLength = replacementLength = 0;
                                result = GetResultForAttributeArgument(completionContext, ref replacementIndex, ref replacementLength);
                            }
                            else if (lastAst is HashtableAst hashTableAst && lastAst.Parent is not DynamicKeywordStatementAst && CheckForPendingAssignment(hashTableAst))
                            {
                                // Handle scenarios such as 'gci | Format-Table @{Label=<tab>' if incomplete parsing of the assignment.
                                return null;
                            }
                            else if (lastAst is AssignmentStatementAst assignmentAst2)
                            {
                                completionContext.ReplacementIndex = replacementIndex += tokenAtCursor.Text.Length;
                                completionContext.ReplacementLength = replacementLength = 0;

                                // Handle scenarios like '$ErrorActionPreference =<tab>'
                                if (TryGetCompletionsForVariableAssignment(completionContext, assignmentAst2, out List<CompletionResult> completions))
                                {
                                    return completions;
                                }
                            }
                            else
                            {
                                // Handle scenarios such as 'configuration foo { File ab { Attributes ='
                                // (auto completion for enum/dependson property of DSC resource),
                                // cursor is right after '=', '(' or '@('
                                //
                                // Configuration config
                                // {
                                //     User test
                                //     {
                                //         DependsOn=|
                                //         DependsOn=@(|)
                                //         DependsOn=(|
                                //
                                bool unused;
                                result = GetResultForEnumPropertyValueOfDSCResource(completionContext, string.Empty, ref replacementIndex, ref replacementLength, out unused);
                            }

                            break;
                        }
                    default:
                        if ((tokenAtCursor.TokenFlags & TokenFlags.Keyword) != 0)
                        {
                            completionContext.WordToComplete = tokenAtCursor.Text;

                            // Handle the file name completion
                            result = CompleteFileNameAsCommand(completionContext);

                            // Handle the command name completion
                            var commandNameResult = CompletionCompleters.CompleteCommand(completionContext);
                            if (commandNameResult != null && commandNameResult.Count > 0)
                            {
                                result.AddRange(commandNameResult);
                            }
                        }
                        else
                        {
                            replacementIndex = -1;
                            replacementLength = -1;
                        }

                        break;
                }
            }
            else
            {
                IScriptPosition cursor = completionContext.CursorPosition;
                bool isCursorLineEmpty = string.IsNullOrWhiteSpace(cursor.Line);
                var tokenBeforeCursor = completionContext.TokenBeforeCursor;
                bool isLineContinuationBeforeCursor = false;
                if (tokenBeforeCursor != null)
                {
                    //
                    // Handle following scenario, cursor is in next line and after a command call,
                    // we need to skip the command call autocompletion if there is no backtick character
                    // in the end of the previous line, since backtick means command call continues to the next line
                    //
                    // Configuration config
                    // {
                    //     User test
                    //     {
                    //         DependsOn=zzz
                    //         |
                    //
                    isLineContinuationBeforeCursor = completionContext.TokenBeforeCursor.Kind == TokenKind.LineContinuation;
                }

                bool skipAutoCompleteForCommandCall = isCursorLineEmpty && !isLineContinuationBeforeCursor;
                bool lastAstIsExpressionAst = lastAst is ExpressionAst;
                if (!isQuotedString &&
                    !skipAutoCompleteForCommandCall &&
                    (lastAst is CommandParameterAst || lastAst is CommandAst ||
                    (lastAstIsExpressionAst && lastAst.Parent is CommandAst) ||
                    (lastAstIsExpressionAst && lastAst.Parent is CommandParameterAst) ||
                    (lastAstIsExpressionAst && lastAst.Parent is ArrayLiteralAst &&
                    (lastAst.Parent.Parent is CommandAst || lastAst.Parent.Parent is CommandParameterAst))))
                {
                    completionContext.WordToComplete = string.Empty;

                    var hashTableAst = lastAst as HashtableAst;

                    // Do not do any tab completion if we have a hash table
                    // and an assignment is pending.  For cases like:
                    //   new-object System.Drawing.Point -prop @{ X=  -> Tab should not complete
                    // Note: This check works when all statements preceding the last are complete,
                    //       but if a preceding statement is incomplete this test fails because
                    //       the Ast mixes the statements due to incomplete parsing.
                    //   e.g.,
                    //   new-object System.Drawing.Point -prop @{ X = 100; Y =      <- Incomplete line
                    //   new-object new-object System.Drawing.Point -prop @{ X =    <- Tab will yield hash properties.
                    if (hashTableAst != null &&
                        CheckForPendingAssignment(hashTableAst))
                    {
                        return result;
                    }

                    if (hashTableAst != null)
                    {
                        completionContext.ReplacementIndex = replacementIndex = completionContext.CursorPosition.Offset;
                        completionContext.ReplacementLength = replacementLength = 0;
                        result = CompletionCompleters.CompleteHashtableKey(completionContext, hashTableAst);
                    }
                    else
                    {
                        result = CompletionCompleters.CompleteCommandArgument(completionContext);
                        replacementIndex = completionContext.ReplacementIndex;
                        replacementLength = completionContext.ReplacementLength;
                    }
                }
                else if (!isQuotedString)
                {
                    //
                    // Handle completion of empty line within configuration statement
                    // Ignore the auto completion if there is a backtick character in previous line
                    //
                    bool cursorAtLineContinuation;
                    if ((tokenAtCursor != null && tokenAtCursor.Kind == TokenKind.LineContinuation) ||
                        (tokenBeforeCursor != null && tokenBeforeCursor.Kind == TokenKind.LineContinuation))
                        cursorAtLineContinuation = true;
                    else
                        cursorAtLineContinuation = false;
                    if (isCursorLineEmpty && !cursorAtLineContinuation)
                    {
                        //
                        // Handle following scenario, both Configuration and DSC resource 'User' are not complete
                        // Check Hashtable first, and then fallback to configuration
                        //
                        // Configuration config
                        // {
                        //     User test
                        //     {
                        //         DependsOn=''
                        //         |
                        result = GetResultForHashtable(completionContext);
                        if (result == null || result.Count == 0)
                        {
                            DynamicKeywordStatementAst keywordAst;
                            ConfigurationDefinitionAst configAst = GetAncestorConfigurationAstAndKeywordAst(cursor, lastAst, out keywordAst);
                            if (configAst != null)
                            {
                                bool matched;
                                result = GetResultForIdentifierInConfiguration(completionContext, configAst, keywordAst, out matched);
                            }
                        }
                    }
                    else if (completionContext.TokenAtCursor == null)
                    {
                        if (tokenBeforeCursor != null)
                        {
                            //
                            // Handle auto completion for enum/dependson property of DSC resource,
                            // cursor is after '=', ',', '(', or '@('
                            //
                            // Configuration config
                            // {
                            //     User test
                            //     {
                            //         DependsOn= |
                            //         DependsOn=@('[user]x', |)
                            //         DependsOn=@( |)
                            //         DependsOn=(|
                            //
                            switch (tokenBeforeCursor.Kind)
                            {
                                case TokenKind.Equals:
                                case TokenKind.Comma:
                                case TokenKind.AtParen:
                                case TokenKind.LParen:
                                    {
                                        if (lastAst is AssignmentStatementAst assignmentAst)
                                        {
                                            // Handle scenarios like '$ErrorActionPreference = <tab>'
                                            if (TryGetCompletionsForVariableAssignment(completionContext, assignmentAst, out result))
                                            {
                                                break;
                                            }
                                        }

                                        if (lastAst is AttributeAst)
                                        {
                                            completionContext.ReplacementLength = replacementLength = 0;
                                            result = GetResultForAttributeArgument(completionContext, ref replacementIndex, ref replacementLength);
                                            break;
                                        }

                                        bool unused;
                                        result = GetResultForEnumPropertyValueOfDSCResource(completionContext, string.Empty, ref replacementIndex, ref replacementLength, out unused);
                                        break;
                                    }
                                default:
                                    break;
                            }
                        }
                    }

                    if (result != null && result.Count > 0)
                    {
                        completionContext.ReplacementIndex = replacementIndex = completionContext.CursorPosition.Offset;
                        completionContext.ReplacementLength = replacementLength = 0;
                    }
                    else
                    {
                        bool needFileCompletion = false;
                        if (lastAst is ErrorExpressionAst && lastAst.Parent is FileRedirectionAst)
                        {
                            // Handle file name completion after redirection operator: gps > <tab>
                            needFileCompletion = true;
                        }
                        else if (lastAst is ErrorStatementAst && CompleteAgainstSwitchFile(lastAst, completionContext.TokenBeforeCursor))
                        {
                            // Handle file name completion after "switch -file": switch -file <tab>
                            needFileCompletion = true;
                        }

                        if (needFileCompletion)
                        {
                            completionContext.WordToComplete = string.Empty;
                            result = new List<CompletionResult>(CompletionCompleters.CompleteFilename(completionContext));

                            replacementIndex = completionContext.ReplacementIndex;
                            replacementLength = completionContext.ReplacementLength;
                        }
                    }
                }
            }

            if (result == null || result.Count == 0)
            {
                var typeAst = completionContext.RelatedAsts.OfType<TypeExpressionAst>().FirstOrDefault();
                TypeName typeNameToComplete = null;
                if (typeAst != null)
                {
                    typeNameToComplete = FindTypeNameToComplete(typeAst.TypeName, _cursorPosition);
                }
                else
                {
                    var typeConstraintAst = completionContext.RelatedAsts.OfType<TypeConstraintAst>().FirstOrDefault();
                    if (typeConstraintAst != null)
                    {
                        typeNameToComplete = FindTypeNameToComplete(typeConstraintAst.TypeName, _cursorPosition);
                    }
                }

                if (typeNameToComplete != null)
                {
                    // See if the typename to complete really is within the typename, and if so, which one, in the case of generics.

                    replacementIndex = typeNameToComplete.Extent.StartOffset;
                    replacementLength = typeNameToComplete.Extent.EndOffset - replacementIndex;
                    completionContext.WordToComplete = typeNameToComplete.FullName;
                    result = CompletionCompleters.CompleteType(completionContext);
                }
            }

            if (result == null || result.Count == 0)
            {
                result = GetResultForHashtable(completionContext);
            }

            if (result == null || result.Count == 0)
            {
                // Handle special file completion scenarios: .\+file.txt -> +<tab>
                string input = completionContext.RelatedAsts[0].Extent.Text;
                if (Regex.IsMatch(input, @"^[\S]+$") && completionContext.RelatedAsts.Count > 0 && completionContext.RelatedAsts[0] is ScriptBlockAst)
                {
                    replacementIndex = completionContext.RelatedAsts[0].Extent.StartScriptPosition.Offset;
                    replacementLength = completionContext.RelatedAsts[0].Extent.EndScriptPosition.Offset - replacementIndex;

                    completionContext.WordToComplete = input;
                    result = CompleteFileNameAsCommand(completionContext);
                }
            }

            return result;
        }

        // Helper method to auto complete hashtable key
        private static List<CompletionResult> GetResultForHashtable(CompletionContext completionContext)
        {
            var lastAst = completionContext.RelatedAsts.Last();
            HashtableAst tempHashtableAst = null;
            IScriptPosition cursor = completionContext.CursorPosition;
            var hashTableAst = lastAst as HashtableAst;
            if (hashTableAst != null)
            {
                // Check if the cursor within the hashtable
                if (cursor.Offset < hashTableAst.Extent.EndOffset)
                {
                    tempHashtableAst = hashTableAst;
                }
                else if (cursor.Offset == hashTableAst.Extent.EndOffset)
                {
                    // Exclude the scenario that cursor at the end of hashtable, i.e. after '}'
                    if (completionContext.TokenAtCursor == null ||
                        completionContext.TokenAtCursor.Kind != TokenKind.RCurly)
                    {
                        tempHashtableAst = hashTableAst;
                    }
                }
            }
            else
            {
                // Handle property completion on a blank line for DynamicKeyword statement
                Ast lastChildofHashtableAst;
                hashTableAst = Ast.GetAncestorHashtableAst(lastAst, out lastChildofHashtableAst);

                // Check if the hashtable within a DynamicKeyword statement
                if (hashTableAst != null)
                {
                    var keywordAst = Ast.GetAncestorAst<DynamicKeywordStatementAst>(hashTableAst);
                    if (keywordAst != null)
                    {
                        // Handle only empty line
                        if (string.IsNullOrWhiteSpace(cursor.Line))
                        {
                            // Check if the cursor outside of last child of hashtable and within the hashtable
                            if (cursor.Offset > lastChildofHashtableAst.Extent.EndOffset &&
                                cursor.Offset <= hashTableAst.Extent.EndOffset)
                            {
                                tempHashtableAst = hashTableAst;
                            }
                        }
                    }
                }
            }

            hashTableAst = tempHashtableAst;
            if (hashTableAst != null)
            {
                completionContext.ReplacementIndex = completionContext.CursorPosition.Offset;
                completionContext.ReplacementLength = 0;
                return CompletionCompleters.CompleteHashtableKey(completionContext, hashTableAst);
            }

            return null;
        }

        // Helper method to look for an incomplete assignment pair in hash table.
        private static bool CheckForPendingAssignment(HashtableAst hashTableAst)
        {
            foreach (var keyValue in hashTableAst.KeyValuePairs)
            {
                if (keyValue.Item2 is ErrorStatementAst)
                {
                    // This indicates the assignment has not completed.
                    return true;
                }
            }

            return false;
        }

        internal static TypeName FindTypeNameToComplete(ITypeName type, IScriptPosition cursor)
        {
            var typeName = type as TypeName;
            if (typeName != null)
            {
                // If the cursor is at the start offset, it's not really inside, so return null.
                // If the cursor is at the end offset, it's not really inside, but it's just before the cursor,
                // we don want to complete it.
                return (cursor.Offset > type.Extent.StartOffset && cursor.Offset <= type.Extent.EndOffset)
                           ? typeName
                           : null;
            }

            var genericTypeName = type as GenericTypeName;
            if (genericTypeName != null)
            {
                typeName = FindTypeNameToComplete(genericTypeName.TypeName, cursor);
                if (typeName != null)
                    return typeName;
                foreach (var t in genericTypeName.GenericArguments)
                {
                    typeName = FindTypeNameToComplete(t, cursor);
                    if (typeName != null)
                        return typeName;
                }

                return null;
            }

            var arrayTypeName = type as ArrayTypeName;
            if (arrayTypeName != null)
            {
                return FindTypeNameToComplete(arrayTypeName.ElementType, cursor) ?? null;
            }

            return null;
        }

        private static string GetFirstLineSubString(string stringToComplete, out bool hasNewLine)
        {
            hasNewLine = false;
            if (!string.IsNullOrEmpty(stringToComplete))
            {
                var index = stringToComplete.IndexOfAny(Utils.Separators.CrLf);
                if (index >= 0)
                {
                    stringToComplete = stringToComplete.Substring(0, index);
                    hasNewLine = true;
                }
            }

            return stringToComplete;
        }

        private static Tuple<ExpressionAst, StatementAst> GetHashEntryContainsCursor(
            IScriptPosition cursor,
            HashtableAst hashTableAst,
            bool isCursorInString)
        {
            Tuple<ExpressionAst, StatementAst> keyValuePairWithCursor = null;
            foreach (var kvp in hashTableAst.KeyValuePairs)
            {
                if (IsCursorWithinOrJustAfterExtent(cursor, kvp.Item2.Extent))
                {
                    keyValuePairWithCursor = kvp;
                    break;
                }

                if (!isCursorInString)
                {
                    //
                    // Handle following case, cursor is after '=' but before next key value pair,
                    // next key value pair will be treated as kvp.Item2 of 'Ensure' key
                    //
                    //    configuration foo
                    //    {
                    //        File foo
                    //        {
                    //            DestinationPath = "\foo.txt"
                    //            Ensure = |
                    //            DependsOn =@("[User]x")
                    //        }
                    //    }
                    //
                    if (kvp.Item2.Extent.StartLineNumber > kvp.Item1.Extent.EndLineNumber &&
                        IsCursorAfterExtentAndInTheSameLine(cursor, kvp.Item1.Extent))
                    {
                        keyValuePairWithCursor = kvp;
                        break;
                    }

                    //
                    // If cursor is not within a string, then handle following two cases,
                    //
                    //  #1) cursor is after '=', in the same line of previous key value pair
                    //      configuration test{File testfile{DestinationPath='c:\test'; Ensure = |
                    //
                    //  #2) cursor is after '=', in the separate line of previous key value pair
                    //      configuration test{File testfile{DestinationPath='c:\test';
                    //        Ensure = |
                    //
                    if (!IsCursorBeforeExtent(cursor, kvp.Item1.Extent) &&
                        IsCursorAfterExtentAndInTheSameLine(cursor, kvp.Item2.Extent))
                    {
                        keyValuePairWithCursor = kvp;
                    }
                }
            }

            return keyValuePairWithCursor;
        }

        // Pulls the variable out of an assignment's LHS expression
        // Also brings back the innermost type constraint if there is one
        private static VariableExpressionAst GetVariableFromExpressionAst(
            ExpressionAst expression,
            ref Type typeConstraint,
            ref ValidateSetAttribute setConstraint)
        {
            switch (expression)
            {
                // $x = ...
                case VariableExpressionAst variableExpression:
                    return variableExpression;

                // [type]$x = ...
                case ConvertExpressionAst convertExpression:
                    typeConstraint = convertExpression.Type.TypeName.GetReflectionType();
                    return GetVariableFromExpressionAst(convertExpression.Child, ref typeConstraint, ref setConstraint);

                // [attribute()][type]$x = ...
                case AttributedExpressionAst attributedExpressionAst:

                    try
                    {
                        setConstraint = attributedExpressionAst.Attribute.GetAttribute() as ValidateSetAttribute;
                    }
                    catch
                    {
                        // Do nothing, just prevent fallout from an unsuccessful attribute conversion
                    }

                    return GetVariableFromExpressionAst(attributedExpressionAst.Child, ref typeConstraint, ref setConstraint);

                // Something else, like `MemberExpressionAst` $a.p = <tab> which isn't currently handled
                default:
                    return null;
            }
        }

        // Gets any type constraints or validateset constraints on a given variable
        private static bool TryGetTypeConstraintOnVariable(
            CompletionContext completionContext,
            string variableName,
            out Type typeConstraint,
            out ValidateSetAttribute setConstraint)
        {
            typeConstraint = null;
            setConstraint = null;

            PSVariable variable = completionContext.ExecutionContext.EngineSessionState.GetVariable(variableName);

            if (variable == null || variable.Attributes.Count == 0)
            {
                return false;
            }

            foreach (Attribute attribute in variable.Attributes)
            {
                if (attribute is ArgumentTypeConverterAttribute typeConverterAttribute)
                {
                    typeConstraint = typeConverterAttribute.TargetType;
                    continue;
                }

                if (attribute is ValidateSetAttribute validateSetAttribute)
                {
                    setConstraint = validateSetAttribute;
                }
            }

            return typeConstraint != null || setConstraint != null;
        }

        private static bool TryGetCompletionsForVariableAssignment(
            CompletionContext completionContext,
            AssignmentStatementAst assignmentAst,
            out List<CompletionResult> completions)
        {
            bool TryGetResultForEnum(Type typeConstraint, CompletionContext completionContext, out List<CompletionResult> completions)
            {
                completions = null;

                if (typeConstraint != null && typeConstraint.IsEnum)
                {
                    completions = GetResultForEnum(typeConstraint, completionContext);
                    return true;
                }

                return false;
            }

            bool TryGetResultForSet(Type typeConstraint, ValidateSetAttribute setConstraint, CompletionContext completionContext1, out List<CompletionResult> completions)
            {
                completions = null;

                if (setConstraint?.ValidValues != null)
                {
                    completions = GetResultForSet(typeConstraint, setConstraint.ValidValues, completionContext);
                    return true;
                }

                return false;
            }

            completions = null;

            // Try to get the variable from the assignment, plus any type constraint on it
            Type typeConstraint = null;
            ValidateSetAttribute setConstraint = null;
            VariableExpressionAst variableAst = GetVariableFromExpressionAst(assignmentAst.Left, ref typeConstraint, ref setConstraint);

            if (variableAst == null)
            {
                return false;
            }

            // Assignment constraints override any existing ones, so try them first

            // Check any [ValidateSet()] constraint first since it's likely to be narrow
            if (TryGetResultForSet(typeConstraint, setConstraint, completionContext, out completions))
            {
                return true;
            }

            // Then try to complete for an enum type
            if (TryGetResultForEnum(typeConstraint, completionContext, out completions))
            {
                return true;
            }

            // If the assignment itself was unconstrained, the variable still might be
            if (!TryGetTypeConstraintOnVariable(completionContext, variableAst.VariablePath.UserPath, out typeConstraint, out setConstraint))
            {
                return false;
            }

            // Again try the [ValidateSet()] constraint first
            if (TryGetResultForSet(typeConstraint, setConstraint, completionContext, out completions))
            {
                return true;
            }

            // Then try to complete for an enum type again
            if (TryGetResultForEnum(typeConstraint, completionContext, out completions))
            {
                return true;
            }

            return false;
        }

        private static List<CompletionResult> GetResultForSet(
            Type typeConstraint,
            IList<string> validValues,
            CompletionContext completionContext)
        {
            var allValues = new List<string>();
            foreach (string value in validValues)
            {
                if (typeConstraint != null && (typeConstraint == typeof(string) || typeConstraint.IsEnum))
                {
                    allValues.Add(GetQuotedString(value, completionContext));
                }
                else
                {
                    allValues.Add(value);
                }
            }

            return GetMatchedResults(allValues, completionContext);
        }

        private static List<CompletionResult> GetMatchedResults(
            List<string> allValues,
            CompletionContext completionContext)
        {
            var stringToComplete = string.Empty;
            if (completionContext.TokenAtCursor != null && completionContext.TokenAtCursor.Kind != TokenKind.Equals)
            {
                stringToComplete = completionContext.TokenAtCursor.Text;
            }

            IEnumerable<string> matchedResults = null;

            if (!string.IsNullOrEmpty(stringToComplete))
            {
                string matchString = stringToComplete + "*";
                var wildcardPattern = WildcardPattern.Get(matchString, WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant);

                matchedResults = allValues.Where(r => wildcardPattern.IsMatch(r));
            }
            else
            {
                matchedResults = allValues;
            }

            var result = new List<CompletionResult>();
            foreach (var match in matchedResults)
            {
                result.Add(new CompletionResult(match));
            }

            return result;
        }

        private static string GetQuotedString(
            string value,
            CompletionContext completionContext)
        {
            var stringToComplete = string.Empty;
            if (completionContext.TokenAtCursor != null)
            {
                stringToComplete = completionContext.TokenAtCursor.Text;
            }

            var quote = stringToComplete.StartsWith('"') ? "\"" : "'";
            return quote + value + quote;
        }

        private static List<CompletionResult> GetResultForEnum(
            Type type,
            CompletionContext completionContext)
        {
            var allNames = new List<string>();
            foreach (var name in Enum.GetNames(type))
            {
                allNames.Add(GetQuotedString(name, completionContext));
            }

            allNames.Sort();

            return GetMatchedResults(allNames, completionContext);
        }

        private static List<CompletionResult> GetResultForEnumPropertyValueOfDSCResource(
            CompletionContext completionContext,
            string stringToComplete,
            ref int replacementIndex,
            ref int replacementLength,
            out bool shouldContinue)
        {
            shouldContinue = true;
            bool isCursorInString = completionContext.TokenAtCursor is StringToken;
            List<CompletionResult> result = null;
            var lastAst = completionContext.RelatedAsts.Last();
            Ast lastChildofHashtableAst;
            var hashTableAst = Ast.GetAncestorHashtableAst(lastAst, out lastChildofHashtableAst);
            Diagnostics.Assert(stringToComplete != null, "stringToComplete should never be null");
            // Check if the hashtable within a DynamicKeyword statement
            if (hashTableAst != null)
            {
                var keywordAst = Ast.GetAncestorAst<DynamicKeywordStatementAst>(hashTableAst);
                if (keywordAst != null)
                {
                    IScriptPosition cursor = completionContext.CursorPosition;
                    var keyValuePairWithCursor = GetHashEntryContainsCursor(cursor, hashTableAst, isCursorInString);
                    if (keyValuePairWithCursor != null)
                    {
                        var propertyNameAst = keyValuePairWithCursor.Item1 as StringConstantExpressionAst;
                        if (propertyNameAst != null)
                        {
                            DynamicKeywordProperty property;
                            if (keywordAst.Keyword.Properties.TryGetValue(propertyNameAst.Value, out property))
                            {
                                List<string> existingValues = null;
                                WildcardPattern wildcardPattern = null;
                                bool isDependsOnProperty = string.Equals(property.Name, @"DependsOn", StringComparison.OrdinalIgnoreCase);
                                bool hasNewLine = false;
                                string stringQuote = (completionContext.TokenAtCursor is StringExpandableToken) ? "\"" : "'";
                                if ((property.ValueMap != null && property.ValueMap.Count > 0) || isDependsOnProperty)
                                {
                                    shouldContinue = false;
                                    existingValues = new List<string>();
                                    if (string.Equals(property.TypeConstraint, "StringArray", StringComparison.OrdinalIgnoreCase))
                                    {
                                        var arrayAst = Ast.GetAncestorAst<ArrayLiteralAst>(lastAst);
                                        if (arrayAst != null && arrayAst.Elements.Count > 0)
                                        {
                                            foreach (ExpressionAst expression in arrayAst.Elements)
                                            {
                                                //
                                                // stringAst can be null in following case
                                                //      DependsOn='[user]x',|
                                                //
                                                var stringAst = expression as StringConstantExpressionAst;
                                                if (stringAst != null && IsCursorOutsideOfExtent(cursor, expression.Extent))
                                                {
                                                    existingValues.Add(stringAst.Value);
                                                }
                                            }
                                        }
                                    }
                                    //
                                    // Make sure only auto-complete string value in current line
                                    //
                                    stringToComplete = GetFirstLineSubString(stringToComplete, out hasNewLine);
                                    completionContext.WordToComplete = stringToComplete;
                                    replacementLength = completionContext.ReplacementLength = stringToComplete.Length;
                                    //
                                    // Calculate the replacementIndex based on cursor location (relative to the string token)
                                    //
                                    if (completionContext.TokenAtCursor is StringToken)
                                    {
                                        replacementIndex = completionContext.TokenAtCursor.Extent.StartOffset + 1;
                                    }
                                    else
                                    {
                                        replacementIndex = completionContext.CursorPosition.Offset - replacementLength;
                                    }

                                    completionContext.ReplacementIndex = replacementIndex;
                                    string matchString = stringToComplete + "*";
                                    wildcardPattern = WildcardPattern.Get(matchString, WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant);
                                    result = new List<CompletionResult>();
                                }

                                Diagnostics.Assert(isCursorInString || (!hasNewLine), "hasNoQuote and hasNewLine cannot be true at the same time");
                                if (property.ValueMap != null && property.ValueMap.Count > 0)
                                {
                                    IEnumerable<string> orderedValues = property.ValueMap.Keys.OrderBy(static x => x).Where(v => !existingValues.Contains(v, StringComparer.OrdinalIgnoreCase));
                                    var matchedResults = orderedValues.Where(v => wildcardPattern.IsMatch(v));
                                    if (matchedResults == null || !matchedResults.Any())
                                    {
                                        // Fallback to all allowed values
                                        matchedResults = orderedValues;
                                    }

                                    foreach (var value in matchedResults)
                                    {
                                        string completionText = isCursorInString ? value : stringQuote + value + stringQuote;
                                        if (hasNewLine)
                                            completionText += stringQuote;
                                        result.Add(new CompletionResult(
                                            completionText,
                                            value,
                                            CompletionResultType.Text,
                                            value));
                                    }
                                }
                                else if (isDependsOnProperty)
                                {
                                    var configAst = Ast.GetAncestorAst<ConfigurationDefinitionAst>(keywordAst);
                                    if (configAst != null)
                                    {
                                        var namedBlockAst = Ast.GetAncestorAst<NamedBlockAst>(keywordAst);
                                        if (namedBlockAst != null)
                                        {
                                            List<string> allResources = new List<string>();
                                            foreach (var statementAst in namedBlockAst.Statements)
                                            {
                                                var dynamicKeywordAst = statementAst as DynamicKeywordStatementAst;
                                                if (dynamicKeywordAst != null &&
                                                    dynamicKeywordAst != keywordAst &&
                                                    !string.Equals(dynamicKeywordAst.Keyword.Keyword, @"Node", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    if (!string.IsNullOrEmpty(dynamicKeywordAst.ElementName))
                                                    {
                                                        StringBuilder sb = new StringBuilder("[", 50);
                                                        sb.Append(dynamicKeywordAst.Keyword.Keyword);
                                                        sb.Append(']');
                                                        sb.Append(dynamicKeywordAst.ElementName);
                                                        var resource = sb.ToString();
                                                        if (!existingValues.Contains(resource, StringComparer.OrdinalIgnoreCase) &&
                                                            !allResources.Contains(resource, StringComparer.OrdinalIgnoreCase))
                                                        {
                                                            allResources.Add(resource);
                                                        }
                                                    }
                                                }
                                            }

                                            var matchedResults = allResources.Where(r => wildcardPattern.IsMatch(r));
                                            if (matchedResults == null || !matchedResults.Any())
                                            {
                                                // Fallback to all allowed values
                                                matchedResults = allResources;
                                            }

                                            foreach (var resource in matchedResults)
                                            {
                                                string completionText = isCursorInString ? resource : stringQuote + resource + stringQuote;
                                                if (hasNewLine)
                                                    completionText += stringQuote;
                                                result.Add(new CompletionResult(
                                                    completionText,
                                                    resource,
                                                    CompletionResultType.Text,
                                                    resource));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private List<CompletionResult> GetResultForString(CompletionContext completionContext, ref int replacementIndex, ref int replacementLength, bool isQuotedString)
        {
            // When it's the content of a quoted string, we only handle variable/member completion
            if (isQuotedString) { return null; }

            var tokenAtCursor = completionContext.TokenAtCursor;
            var lastAst = completionContext.RelatedAsts.Last();

            List<CompletionResult> result = null;
            var expandableString = lastAst as ExpandableStringExpressionAst;
            var constantString = lastAst as StringConstantExpressionAst;
            if (constantString == null && expandableString == null) { return null; }

            string strValue = constantString != null ? constantString.Value : expandableString.Value;
            StringConstantType strType = constantString != null ? constantString.StringConstantType : expandableString.StringConstantType;
            string subInput = null;

            bool shouldContinue;
            result = GetResultForEnumPropertyValueOfDSCResource(completionContext, strValue, ref replacementIndex, ref replacementLength, out shouldContinue);
            if (!shouldContinue || (result != null && result.Count > 0))
            {
                return result;
            }

            if (strType == StringConstantType.DoubleQuoted)
            {
                var match = Regex.Match(strValue, @"(\$[\w\d]+\.[\w\d\*]*)$");
                if (match.Success)
                {
                    subInput = match.Groups[1].Value;
                }
                else if ((match = Regex.Match(strValue, @"(\[[\w\d\.]+\]::[\w\d\*]*)$")).Success)
                {
                    subInput = match.Groups[1].Value;
                }
            }

            // Handle variable/member completion
            if (subInput != null)
            {
                int stringStartIndex = tokenAtCursor.Extent.StartScriptPosition.Offset;
                int cursorIndexInString = _cursorPosition.Offset - stringStartIndex - 1;
                if (cursorIndexInString >= strValue.Length)
                    cursorIndexInString = strValue.Length;

                var analysis = new CompletionAnalysis(_ast, _tokens, _cursorPosition, _options);
                var subContext = analysis.CreateCompletionContext(completionContext.TypeInferenceContext);

                var subResult = analysis.GetResultHelper(subContext, out int subReplaceIndex, out _, true);

                if (subResult != null && subResult.Count > 0)
                {
                    result = new List<CompletionResult>();
                    replacementIndex = stringStartIndex + 1 + (cursorIndexInString - subInput.Length);
                    replacementLength = subInput.Length;
                    ReadOnlySpan<char> prefix = subInput.AsSpan(0, subReplaceIndex);

                    foreach (CompletionResult entry in subResult)
                    {
                        string completionText = string.Concat(prefix, entry.CompletionText.AsSpan());
                        if (entry.ResultType == CompletionResultType.Property)
                        {
                            completionText = TokenKind.DollarParen.Text() + completionText + TokenKind.RParen.Text();
                        }
                        else if (entry.ResultType == CompletionResultType.Method)
                        {
                            completionText = TokenKind.DollarParen.Text() + completionText;
                        }

                        completionText += "\"";
                        result.Add(new CompletionResult(completionText, entry.ListItemText, entry.ResultType, entry.ToolTip));
                    }
                }
            }
            else
            {
                var commandElementAst = lastAst as CommandElementAst;
                string wordToComplete =
                    CompletionCompleters.ConcatenateStringPathArguments(commandElementAst, string.Empty, completionContext);

                if (wordToComplete != null)
                {
                    completionContext.WordToComplete = wordToComplete;

                    // Handle scenarios like this: cd 'c:\windows\win'<tab>
                    if (lastAst.Parent is CommandAst || lastAst.Parent is CommandParameterAst)
                    {
                        result = CompletionCompleters.CompleteCommandArgument(completionContext);
                        replacementIndex = completionContext.ReplacementIndex;
                        replacementLength = completionContext.ReplacementLength;
                    }
                    // Handle scenarios like this: "c:\wind"<tab>. Treat the StringLiteral/StringExpandable as path/command
                    else
                    {
                        // Handle path/commandname completion for quoted string
                        result = new List<CompletionResult>(CompletionCompleters.CompleteFilename(completionContext));

                        // Try command name completion only if the text contains '-'
                        if (wordToComplete.Contains('-'))
                        {
                            var commandNameResult = CompletionCompleters.CompleteCommand(completionContext);
                            if (commandNameResult != null && commandNameResult.Count > 0)
                            {
                                result.AddRange(commandNameResult);
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Find the configuration statement contains current cursor.
        /// </summary>
        /// <param name="cursorPosition"></param>
        /// <param name="ast"></param>
        /// <param name="keywordAst"></param>
        /// <returns></returns>
        private static ConfigurationDefinitionAst GetAncestorConfigurationAstAndKeywordAst(
            IScriptPosition cursorPosition,
            Ast ast,
            out DynamicKeywordStatementAst keywordAst)
        {
            ConfigurationDefinitionAst configureAst = Ast.GetAncestorConfigurationDefinitionAstAndDynamicKeywordStatementAst(ast, out keywordAst);
            // Find the configuration statement contains current cursor
            // Note: cursorPosition.Offset < configureAst.Extent.EndOffset means cursor locates inside the configuration
            //       cursorPosition.Offset = configureAst.Extent.EndOffset means cursor locates at the end of the configuration
            while (configureAst != null && cursorPosition.Offset > configureAst.Extent.EndOffset)
            {
                configureAst = Ast.GetAncestorAst<ConfigurationDefinitionAst>(configureAst.Parent);
            }

            return configureAst;
        }

        /// <summary>
        /// Generate auto complete results for identifier within configuration.
        /// Results are generated based on DynamicKeywords matches given identifier.
        /// For example, following "Fi" matches "File", and "Us" matches "User"
        ///
        ///     Configuration
        ///     {
        ///         Fi^
        ///         Node("TargetMachine")
        ///         {
        ///             Us^
        ///         }
        ///     }
        /// </summary>
        /// <param name="completionContext"></param>
        /// <param name="configureAst"></param>
        /// <param name="keywordAst"></param>
        /// <param name="matched"></param>
        /// <returns></returns>
        private static List<CompletionResult> GetResultForIdentifierInConfiguration(
            CompletionContext completionContext,
            ConfigurationDefinitionAst configureAst,
            DynamicKeywordStatementAst keywordAst,
            out bool matched)
        {
            List<CompletionResult> results = null;
            matched = false;

            IEnumerable<DynamicKeyword> keywords = configureAst.DefinedKeywords.Where(
                k => // Node is special case, legal in both Resource and Meta configuration
                    string.Equals(k.Keyword, @"Node", StringComparison.OrdinalIgnoreCase) ||
                    (
                        // Check compatibility between Resource and Configuration Type
                        k.IsCompatibleWithConfigurationType(configureAst.ConfigurationType) &&
                        !DynamicKeyword.IsHiddenKeyword(k.Keyword) &&
                        !k.IsReservedKeyword
                    )
            );

            if (keywordAst != null && completionContext.CursorPosition.Offset < keywordAst.Extent.EndOffset)
                keywords = keywordAst.Keyword.GetAllowedKeywords(keywords);

            if (keywords != null && keywords.Any())
            {
                string commandName = (completionContext.WordToComplete ?? string.Empty) + "*";
                var wildcardPattern = WildcardPattern.Get(commandName, WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant);

                // Filter by name
                var matchedResults = keywords.Where(k => wildcardPattern.IsMatch(k.Keyword));
                if (matchedResults == null || !matchedResults.Any())
                {
                    // Fallback to all legal keywords in the configuration statement
                    matchedResults = keywords;
                }
                else
                {
                    matched = true;
                }

                foreach (var keyword in matchedResults)
                {
                    string usageString = string.Empty;
                    ICrossPlatformDsc dscSubsystem = SubsystemManager.GetSubsystem<ICrossPlatformDsc>();
                    if (dscSubsystem != null)
                    {
                        usageString = dscSubsystem.GetDSCResourceUsageString(keyword);
                    }
                    else
                    {
                        usageString = Microsoft.PowerShell.DesiredStateConfiguration.Internal.DscClassCache.GetDSCResourceUsageString(keyword);
                    }

                    if (results == null)
                    {
                        results = new List<CompletionResult>();
                    }

                    results.Add(new CompletionResult(
                        keyword.Keyword,
                        keyword.Keyword,
                        CompletionResultType.DynamicKeyword,
                        usageString));
                }
            }

            return results;
        }

        private List<CompletionResult> GetResultForIdentifier(CompletionContext completionContext, ref int replacementIndex, ref int replacementLength, bool isQuotedString)
        {
            var tokenAtCursor = completionContext.TokenAtCursor;
            var lastAst = completionContext.RelatedAsts.Last();

            List<CompletionResult> result = null;
            var tokenAtCursorText = tokenAtCursor.Text;
            completionContext.WordToComplete = tokenAtCursorText;

            var strConst = lastAst as StringConstantExpressionAst;
            if (strConst != null)
            {
                if (strConst.Value.Equals("$", StringComparison.Ordinal))
                {
                    completionContext.WordToComplete = string.Empty;
                    return CompletionCompleters.CompleteVariable(completionContext);
                }
                else
                {
                    UsingStatementAst usingState = strConst.Parent as UsingStatementAst;
                    if (usingState != null)
                    {
                        completionContext.ReplacementIndex = strConst.Extent.StartOffset;
                        completionContext.ReplacementLength = strConst.Extent.EndOffset - replacementIndex;
                        completionContext.WordToComplete = strConst.Extent.Text;
                        switch (usingState.UsingStatementKind)
                        {
                            case UsingStatementKind.Assembly:
                                break;
                            case UsingStatementKind.Command:
                                break;
                            case UsingStatementKind.Module:
                                var moduleExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                                {
                                    StringLiterals.PowerShellModuleFileExtension,
                                    StringLiterals.PowerShellDataFileExtension,
                                    StringLiterals.PowerShellNgenAssemblyExtension,
                                    StringLiterals.PowerShellILAssemblyExtension,
                                    StringLiterals.PowerShellILExecutableExtension,
                                    StringLiterals.PowerShellCmdletizationFileExtension
                                };
                                result = CompletionCompleters.CompleteFilename(completionContext, false, moduleExtensions).ToList();
                                if (completionContext.WordToComplete.IndexOfAny(Utils.Separators.DirectoryOrDrive) != -1)
                                {
                                    // The partial input is a path, then we don't iterate modules under $ENV:PSModulePath
                                    return result;
                                }

                                var moduleResults = CompletionCompleters.CompleteModuleName(completionContext, false);
                                if (moduleResults != null && moduleResults.Count > 0)
                                    result.AddRange(moduleResults);
                                return result;
                            case UsingStatementKind.Namespace:
                                result = CompletionCompleters.CompleteNamespace(completionContext);
                                return result;
                            case UsingStatementKind.Type:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("UsingStatementKind");
                        }
                    }
                }
            }

            result = GetResultForAttributeArgument(completionContext, ref replacementIndex, ref replacementLength);
            if (result != null) return result;

            if ((tokenAtCursor.TokenFlags & TokenFlags.CommandName) != 0)
            {
                // Handle completion for a path with variable, such as: $PSHOME\ty<tab>
                if (completionContext.RelatedAsts.Count > 0 && completionContext.RelatedAsts[0] is ScriptBlockAst)
                {
                    Ast cursorAst = null;
                    var cursorPosition = (InternalScriptPosition)_cursorPosition;
                    int offsetBeforeCmdName = cursorPosition.Offset - tokenAtCursorText.Length;
                    if (offsetBeforeCmdName >= 0)
                    {
                        var cursorBeforeCmdName = cursorPosition.CloneWithNewOffset(offsetBeforeCmdName);
                        var scriptBlockAst = (ScriptBlockAst)completionContext.RelatedAsts[0];
                        cursorAst = GetLastAstAtCursor(scriptBlockAst, cursorBeforeCmdName);
                    }

                    if (cursorAst != null &&
                        cursorAst.Extent.EndLineNumber == tokenAtCursor.Extent.StartLineNumber &&
                        cursorAst.Extent.EndColumnNumber == tokenAtCursor.Extent.StartColumnNumber)
                    {
                        if (tokenAtCursorText.IndexOfAny(Utils.Separators.Directory) == 0)
                        {
                            string wordToComplete =
                                CompletionCompleters.ConcatenateStringPathArguments(cursorAst as CommandElementAst, tokenAtCursorText, completionContext);
                            if (wordToComplete != null)
                            {
                                completionContext.WordToComplete = wordToComplete;
                                result = new List<CompletionResult>(CompletionCompleters.CompleteFilename(completionContext));
                                if (result.Count > 0)
                                {
                                    replacementIndex = cursorAst.Extent.StartScriptPosition.Offset;
                                    replacementLength += cursorAst.Extent.Text.Length;
                                }

                                return result;
                            }
                            else
                            {
                                var variableAst = cursorAst as VariableExpressionAst;
                                string fullPath = variableAst != null
                                    ? CompletionCompleters.CombineVariableWithPartialPath(
                                        variableAst: variableAst,
                                        extraText: tokenAtCursorText,
                                        executionContext: completionContext.ExecutionContext)
                                    : null;

                                if (fullPath == null) { return result; }

                                // Continue trying the filename/commandname completion for scenarios like this: $aa\d<tab>
                                completionContext.WordToComplete = fullPath;
                                replacementIndex = cursorAst.Extent.StartScriptPosition.Offset;
                                replacementLength += cursorAst.Extent.Text.Length;

                                completionContext.ReplacementIndex = replacementIndex;
                                completionContext.ReplacementLength = replacementLength;
                            }
                        }
                        // Continue trying the filename/commandname completion for scenarios like this: $aa[get-<tab>
                        else if (cursorAst is not ErrorExpressionAst || cursorAst.Parent is not IndexExpressionAst)
                        {
                            return result;
                        }
                    }
                }

                // When it's the content of a quoted string, we only handle variable/member completion
                if (isQuotedString) { return result; }

                // Handle the StringExpandableToken;
                var strToken = tokenAtCursor as StringExpandableToken;
                if (strToken != null && strToken.NestedTokens != null && strConst != null)
                {
                    try
                    {
                        string expandedString = null;
                        var expandableStringAst = new ExpandableStringExpressionAst(strConst.Extent, strConst.Value, StringConstantType.BareWord);
                        if (CompletionCompleters.IsPathSafelyExpandable(expandableStringAst: expandableStringAst,
                                                                        extraText: string.Empty,
                                                                        executionContext: completionContext.ExecutionContext,
                                                                        expandedString: out expandedString))
                        {
                            completionContext.WordToComplete = expandedString;
                        }
                        else
                        {
                            return result;
                        }
                    }
                    catch (Exception)
                    {
                        return result;
                    }
                }

                //
                // Handle completion of DSC resources within Configuration
                //
                DynamicKeywordStatementAst keywordAst;
                ConfigurationDefinitionAst configureAst = GetAncestorConfigurationAstAndKeywordAst(completionContext.CursorPosition, lastAst, out keywordAst);
                bool matched = false;
                List<CompletionResult> keywordResult = null;
                if (configureAst != null)
                {
                    // Current token is within ConfigurationDefinitionAst or DynamicKeywordStatementAst
                    keywordResult = GetResultForIdentifierInConfiguration(completionContext, configureAst, keywordAst, out matched);
                }

                // Handle the file completion before command name completion
                result = CompleteFileNameAsCommand(completionContext);
                // Handle the command name completion
                var commandNameResult = CompletionCompleters.CompleteCommand(completionContext);

                if (commandNameResult != null && commandNameResult.Count > 0)
                {
                    result.AddRange(commandNameResult);
                }

                if (matched && keywordResult != null)
                {
                    result.InsertRange(0, keywordResult);
                }
                else if (!matched && keywordResult != null && commandNameResult.Count == 0)
                {
                    result.AddRange(keywordResult);
                }

                return result;
            }

            var isSingleDash = tokenAtCursorText.Length == 1 && tokenAtCursorText[0].IsDash();
            var isDoubleDash = tokenAtCursorText.Length == 2 && tokenAtCursorText[0].IsDash() && tokenAtCursorText[1].IsDash();
            var isParentCommandOrDynamicKeyword = (lastAst.Parent is CommandAst || lastAst.Parent is DynamicKeywordStatementAst);
            if ((isSingleDash || isDoubleDash) && isParentCommandOrDynamicKeyword)
            {
                // When it's the content of a quoted string, we only handle variable/member completion
                if (isSingleDash)
                {
                    if (isQuotedString) { return result; }

                    var res = CompletionCompleters.CompleteCommandParameter(completionContext);
                    if (res.Count != 0)
                    {
                        return res;
                    }
                }

                return CompletionCompleters.CompleteCommandArgument(completionContext);
            }

            TokenKind memberOperator = TokenKind.Unknown;
            bool isMemberCompletion = (lastAst.Parent is MemberExpressionAst);
            bool isStatic = isMemberCompletion && ((MemberExpressionAst)lastAst.Parent).Static;
            bool isWildcard = false;

            if (!isMemberCompletion)
            {
                // Still might be member completion, something like: echo $member.
                // We need to know if the previous element before the token is adjacent because
                // we don't have a MemberExpressionAst, we might have 2 command arguments.

                if (tokenAtCursorText.Equals(TokenKind.Dot.Text(), StringComparison.Ordinal))
                {
                    memberOperator = TokenKind.Dot;
                    isMemberCompletion = true;
                }
                else if (tokenAtCursorText.Equals(TokenKind.ColonColon.Text(), StringComparison.Ordinal))
                {
                    memberOperator = TokenKind.ColonColon;
                    isMemberCompletion = true;
                }
                else if (tokenAtCursor.Kind.Equals(TokenKind.Multiply) && lastAst is BinaryExpressionAst)
                {
                    // Handle member completion with wildcard(wildcard is at the end): $a.p*
                    var binaryExpressionAst = (BinaryExpressionAst)lastAst;
                    var memberExpressionAst = binaryExpressionAst.Left as MemberExpressionAst;
                    var errorPosition = binaryExpressionAst.ErrorPosition;

                    if (memberExpressionAst != null && binaryExpressionAst.Operator == TokenKind.Multiply &&
                        errorPosition.StartOffset == memberExpressionAst.Member.Extent.EndOffset)
                    {
                        isStatic = memberExpressionAst.Static;
                        memberOperator = isStatic ? TokenKind.ColonColon : TokenKind.Dot;
                        isMemberCompletion = true;
                        isWildcard = true;

                        // Member completion will add back the '*', so pretend it wasn't there, at least from the "related asts" point of view,
                        // but add the member expression that we are really completing.
                        completionContext.RelatedAsts.Remove(binaryExpressionAst);
                        completionContext.RelatedAsts.Add(memberExpressionAst);

                        var memberAst = memberExpressionAst.Member as StringConstantExpressionAst;
                        if (memberAst != null)
                        {
                            replacementIndex = memberAst.Extent.StartScriptPosition.Offset;
                            replacementLength += memberAst.Extent.Text.Length;
                        }
                    }
                }
            }

            if (isMemberCompletion)
            {
                result = CompletionCompleters.CompleteMember(completionContext, @static: (isStatic || memberOperator == TokenKind.ColonColon));

                // If the last token was just a '.', we tried to complete members.  That may
                // have failed because it wasn't really an attempt to complete a member, in
                // which case we should try to complete as an argument.
                if (result.Count > 0)
                {
                    if (!isWildcard && memberOperator != TokenKind.Unknown)
                    {
                        replacementIndex += tokenAtCursorText.Length;
                        replacementLength = 0;
                    }

                    return result;
                }
            }

            if (lastAst.Parent is HashtableAst)
            {
                result = CompletionCompleters.CompleteHashtableKey(completionContext, (HashtableAst)lastAst.Parent);
                if (result != null && result.Count > 0)
                {
                    return result;
                }
            }

            // When it's the content of a quoted string, we only handle variable/member completion
            if (isQuotedString) { return result; }

            bool needFileCompletion = false;
            if (lastAst.Parent is FileRedirectionAst || CompleteAgainstSwitchFile(lastAst, completionContext.TokenBeforeCursor))
            {
                string wordToComplete =
                    CompletionCompleters.ConcatenateStringPathArguments(lastAst as CommandElementAst, string.Empty, completionContext);
                if (wordToComplete != null)
                {
                    needFileCompletion = true;
                    completionContext.WordToComplete = wordToComplete;
                }
            }
            else if (tokenAtCursorText.IndexOfAny(Utils.Separators.Directory) == 0)
            {
                var command = lastAst.Parent as CommandBaseAst;
                if (command != null && command.Redirections.Count > 0)
                {
                    var fileRedirection = command.Redirections[0] as FileRedirectionAst;
                    if (fileRedirection != null &&
                        fileRedirection.Extent.EndLineNumber == lastAst.Extent.StartLineNumber &&
                        fileRedirection.Extent.EndColumnNumber == lastAst.Extent.StartColumnNumber)
                    {
                        string wordToComplete =
                            CompletionCompleters.ConcatenateStringPathArguments(fileRedirection.Location, tokenAtCursorText, completionContext);

                        if (wordToComplete != null)
                        {
                            needFileCompletion = true;
                            completionContext.WordToComplete = wordToComplete;
                            replacementIndex = fileRedirection.Location.Extent.StartScriptPosition.Offset;
                            replacementLength += fileRedirection.Location.Extent.EndScriptPosition.Offset - replacementIndex;

                            completionContext.ReplacementIndex = replacementIndex;
                            completionContext.ReplacementLength = replacementLength;
                        }
                    }
                }
            }

            if (needFileCompletion)
            {
                return new List<CompletionResult>(CompletionCompleters.CompleteFilename(completionContext));
            }
            else
            {
                string wordToComplete =
                    CompletionCompleters.ConcatenateStringPathArguments(lastAst as CommandElementAst, string.Empty, completionContext);
                if (wordToComplete != null)
                {
                    completionContext.WordToComplete = wordToComplete;
                }
            }

            result = CompletionCompleters.CompleteCommandArgument(completionContext);
            replacementIndex = completionContext.ReplacementIndex;
            replacementLength = completionContext.ReplacementLength;
            return result;
        }

        private static List<CompletionResult> GetResultForAttributeArgument(CompletionContext completionContext, ref int replacementIndex, ref int replacementLength)
        {
            // Attribute member arguments
            Type attributeType = null;
            string argName = string.Empty;
            Ast argAst = completionContext.RelatedAsts.Find(static ast => ast is NamedAttributeArgumentAst);
            NamedAttributeArgumentAst namedArgAst = argAst as NamedAttributeArgumentAst;
            if (argAst != null && namedArgAst != null)
            {
                attributeType = ((AttributeAst)namedArgAst.Parent).TypeName.GetReflectionAttributeType();
                argName = namedArgAst.ArgumentName;
                replacementIndex = namedArgAst.Extent.StartOffset;
                replacementLength = argName.Length;
            }
            else
            {
                Ast astAtt = completionContext.RelatedAsts.Find(static ast => ast is AttributeAst);
                AttributeAst attAst = astAtt as AttributeAst;
                if (astAtt != null && attAst != null)
                {
                    attributeType = attAst.TypeName.GetReflectionAttributeType();
                }
            }

            if (attributeType != null)
            {
                PropertyInfo[] propertyInfos = attributeType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                List<CompletionResult> result = new List<CompletionResult>();
                foreach (PropertyInfo property in propertyInfos)
                {
                    // Ignore getter-only properties, including 'TypeId' (all attributes inherit it).
                    if (!property.CanWrite) { continue; }

                    if (property.Name.StartsWith(argName, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(new CompletionResult(property.Name, property.Name, CompletionResultType.Property,
                            property.PropertyType.ToString() + " " + property.Name));
                    }
                }

                return result;
            }

            return null;
        }

        /// <summary>
        /// Complete file name as command.
        /// </summary>
        /// <param name="completionContext"></param>
        /// <returns></returns>
        private static List<CompletionResult> CompleteFileNameAsCommand(CompletionContext completionContext)
        {
            var addAmpersandIfNecessary = CompletionCompleters.IsAmpersandNeeded(completionContext, true);
            var result = new List<CompletionResult>();
            var clearLiteralPathsKey = false;

            if (completionContext.Options == null)
            {
                completionContext.Options = new Hashtable { { "LiteralPaths", true } };
            }
            else if (!completionContext.Options.ContainsKey("LiteralPaths"))
            {
                // Dont escape '[',']','`' when the file name is treated as command name
                completionContext.Options.Add("LiteralPaths", true);
                clearLiteralPathsKey = true;
            }

            try
            {
                var fileNameResult = CompletionCompleters.CompleteFilename(completionContext);
                foreach (var entry in fileNameResult)
                {
                    // Add '&' to file names that are quoted
                    var completionText = entry.CompletionText;
                    var len = completionText.Length;
                    if (addAmpersandIfNecessary && len > 2 && completionText[0].IsSingleQuote() && completionText[len - 1].IsSingleQuote())
                    {
                        completionText = "& " + completionText;
                        result.Add(new CompletionResult(completionText, entry.ListItemText, entry.ResultType, entry.ToolTip));
                    }
                    else
                    {
                        result.Add(entry);
                    }
                }
            }
            finally
            {
                if (clearLiteralPathsKey)
                    completionContext.Options.Remove("LiteralPaths");
            }

            return result;
        }
    }
}
