// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace System.Management.Automation.Language
{
    /// <summary>
    /// Each Visit* method in <see ref="AstVisitor"/> returns one of these values to control
    /// how visiting nodes in the AST should proceed.
    /// </summary>
    public enum AstVisitAction
    {
        /// <summary>
        /// Continue visiting all nodes the ast.
        /// </summary>
        Continue,

        /// <summary>
        /// Skip visiting child nodes of currently visited node, but continue visiting other nodes.
        /// </summary>
        SkipChildren,

        /// <summary>
        /// Stop visiting all nodes.
        /// </summary>
        StopVisit,
    }

    /// <summary>
    /// AstVisitor is used for basic scenarios requiring traversal of the nodes in an Ast.
    /// An implementation of AstVisitor does not explicitly traverse the Ast, instead,
    /// the engine traverses all nodes in the Ast and calls the appropriate method on each node.
    /// </summary>
    public abstract class AstVisitor
    {
        internal AstVisitAction CheckForPostAction(Ast ast, AstVisitAction action)
        {
            var postActionHandler = this as IAstPostVisitHandler;
            postActionHandler?.PostVisit(ast);

            return action;
        }

        /// <summary/>
        public virtual AstVisitAction DefaultVisit(Ast ast) => AstVisitAction.Continue;

        /// <summary/>
        public virtual AstVisitAction VisitErrorStatement(ErrorStatementAst errorStatementAst) => DefaultVisit(errorStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitErrorExpression(ErrorExpressionAst errorExpressionAst) => DefaultVisit(errorExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitScriptBlock(ScriptBlockAst scriptBlockAst) => DefaultVisit(scriptBlockAst);

        /// <summary/>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Param")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public virtual AstVisitAction VisitParamBlock(ParamBlockAst paramBlockAst) => DefaultVisit(paramBlockAst);

        /// <summary/>
        public virtual AstVisitAction VisitNamedBlock(NamedBlockAst namedBlockAst) => DefaultVisit(namedBlockAst);

        /// <summary/>
        public virtual AstVisitAction VisitTypeConstraint(TypeConstraintAst typeConstraintAst) => DefaultVisit(typeConstraintAst);

        /// <summary/>
        public virtual AstVisitAction VisitAttribute(AttributeAst attributeAst) => DefaultVisit(attributeAst);

        /// <summary/>
        public virtual AstVisitAction VisitParameter(ParameterAst parameterAst) => DefaultVisit(parameterAst);

        /// <summary/>
        public virtual AstVisitAction VisitTypeExpression(TypeExpressionAst typeExpressionAst) => DefaultVisit(typeExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst) => DefaultVisit(functionDefinitionAst);

        /// <summary/>
        public virtual AstVisitAction VisitStatementBlock(StatementBlockAst statementBlockAst) => DefaultVisit(statementBlockAst);

        /// <summary/>
        public virtual AstVisitAction VisitIfStatement(IfStatementAst ifStmtAst) => DefaultVisit(ifStmtAst);

        /// <summary/>
        public virtual AstVisitAction VisitTrap(TrapStatementAst trapStatementAst) => DefaultVisit(trapStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitSwitchStatement(SwitchStatementAst switchStatementAst) => DefaultVisit(switchStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitDataStatement(DataStatementAst dataStatementAst) => DefaultVisit(dataStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitForEachStatement(ForEachStatementAst forEachStatementAst) => DefaultVisit(forEachStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst) => DefaultVisit(doWhileStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitForStatement(ForStatementAst forStatementAst) => DefaultVisit(forStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitWhileStatement(WhileStatementAst whileStatementAst) => DefaultVisit(whileStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitCatchClause(CatchClauseAst catchClauseAst) => DefaultVisit(catchClauseAst);

        /// <summary/>
        public virtual AstVisitAction VisitTryStatement(TryStatementAst tryStatementAst) => DefaultVisit(tryStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitBreakStatement(BreakStatementAst breakStatementAst) => DefaultVisit(breakStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitContinueStatement(ContinueStatementAst continueStatementAst) => DefaultVisit(continueStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitReturnStatement(ReturnStatementAst returnStatementAst) => DefaultVisit(returnStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitExitStatement(ExitStatementAst exitStatementAst) => DefaultVisit(exitStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitThrowStatement(ThrowStatementAst throwStatementAst) => DefaultVisit(throwStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst) => DefaultVisit(doUntilStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst) => DefaultVisit(assignmentStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitPipeline(PipelineAst pipelineAst) => DefaultVisit(pipelineAst);

        /// <summary/>
        public virtual AstVisitAction VisitCommand(CommandAst commandAst) => DefaultVisit(commandAst);

        /// <summary/>
        public virtual AstVisitAction VisitCommandExpression(CommandExpressionAst commandExpressionAst) => DefaultVisit(commandExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitCommandParameter(CommandParameterAst commandParameterAst) => DefaultVisit(commandParameterAst);

        /// <summary/>
        public virtual AstVisitAction VisitMergingRedirection(MergingRedirectionAst redirectionAst) => DefaultVisit(redirectionAst);

        /// <summary/>
        public virtual AstVisitAction VisitFileRedirection(FileRedirectionAst redirectionAst) => DefaultVisit(redirectionAst);

        /// <summary/>
        public virtual AstVisitAction VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst) => DefaultVisit(binaryExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst) => DefaultVisit(unaryExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitConvertExpression(ConvertExpressionAst convertExpressionAst) => DefaultVisit(convertExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitConstantExpression(ConstantExpressionAst constantExpressionAst) => DefaultVisit(constantExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst) => DefaultVisit(stringConstantExpressionAst);

        /// <summary/>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "SubExpression")]
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "subExpression")]
        public virtual AstVisitAction VisitSubExpression(SubExpressionAst subExpressionAst) => DefaultVisit(subExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitUsingExpression(UsingExpressionAst usingExpressionAst) => DefaultVisit(usingExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitVariableExpression(VariableExpressionAst variableExpressionAst) => DefaultVisit(variableExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitMemberExpression(MemberExpressionAst memberExpressionAst) => DefaultVisit(memberExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitInvokeMemberExpression(InvokeMemberExpressionAst methodCallAst) => DefaultVisit(methodCallAst);

        /// <summary/>
        public virtual AstVisitAction VisitArrayExpression(ArrayExpressionAst arrayExpressionAst) => DefaultVisit(arrayExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst) => DefaultVisit(arrayLiteralAst);

        /// <summary/>
        public virtual AstVisitAction VisitHashtable(HashtableAst hashtableAst) => DefaultVisit(hashtableAst);

        /// <summary/>
        public virtual AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst) => DefaultVisit(scriptBlockExpressionAst);

        /// <summary/>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Paren")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "paren")]
        public virtual AstVisitAction VisitParenExpression(ParenExpressionAst parenExpressionAst) => DefaultVisit(parenExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst) => DefaultVisit(expandableStringExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitIndexExpression(IndexExpressionAst indexExpressionAst) => DefaultVisit(indexExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst) => DefaultVisit(attributedExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitBlockStatement(BlockStatementAst blockStatementAst) => DefaultVisit(blockStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst) => DefaultVisit(namedAttributeArgumentAst);
    }

    /// <summary>
    /// AstVisitor for new Ast node types.
    /// </summary>
    public abstract class AstVisitor2 : AstVisitor
    {
        /// <summary/>
        public virtual AstVisitAction VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst) => DefaultVisit(typeDefinitionAst);

        /// <summary/>
        public virtual AstVisitAction VisitPropertyMember(PropertyMemberAst propertyMemberAst) => DefaultVisit(propertyMemberAst);

        /// <summary/>
        public virtual AstVisitAction VisitFunctionMember(FunctionMemberAst functionMemberAst) => DefaultVisit(functionMemberAst);

        /// <summary/>
        public virtual AstVisitAction VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst) => DefaultVisit(baseCtorInvokeMemberExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitUsingStatement(UsingStatementAst usingStatementAst) => DefaultVisit(usingStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst) => DefaultVisit(configurationDefinitionAst);

        /// <summary/>
        public virtual AstVisitAction VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordStatementAst) => DefaultVisit(dynamicKeywordStatementAst);

        /// <summary/>
        public virtual AstVisitAction VisitTernaryExpression(TernaryExpressionAst ternaryExpressionAst) => DefaultVisit(ternaryExpressionAst);

        /// <summary/>
        public virtual AstVisitAction VisitPipelineChain(PipelineChainAst statementChain) => DefaultVisit(statementChain);
    }

    /// <summary>
    /// Implement this interface when you implement <see cref="AstVisitor"/> or <see cref="AstVisitor2"/> when
    /// you want to do something after possibly visiting the children of the ast.
    /// </summary>
#nullable enable
    public interface IAstPostVisitHandler
    {
        /// <summary>
        /// The function called on each ast node after processing it's children.
        /// </summary>
        /// <param name="ast">The ast whose children have all been processed and whose siblings
        /// and parents are about to be processed.</param>
        void PostVisit(Ast ast);
    }
}
