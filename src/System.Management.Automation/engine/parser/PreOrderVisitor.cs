// Copyright (c) Microsoft Corporation. All rights reserved.
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
            if (postActionHandler != null)
            {
                postActionHandler.PostVisit(ast);
            }

            return action;
        }

        /// <summary/>
        public virtual AstVisitAction VisitErrorStatement(ErrorStatementAst errorStatementAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitErrorExpression(ErrorExpressionAst errorExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitScriptBlock(ScriptBlockAst scriptBlockAst) { return AstVisitAction.Continue; }
        /// <summary/>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Param")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public virtual AstVisitAction VisitParamBlock(ParamBlockAst paramBlockAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitNamedBlock(NamedBlockAst namedBlockAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitTypeConstraint(TypeConstraintAst typeConstraintAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitAttribute(AttributeAst attributeAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitParameter(ParameterAst parameterAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitTypeExpression(TypeExpressionAst typeExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitStatementBlock(StatementBlockAst statementBlockAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitIfStatement(IfStatementAst ifStmtAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitTrap(TrapStatementAst trapStatementAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitSwitchStatement(SwitchStatementAst switchStatementAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitDataStatement(DataStatementAst dataStatementAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitForEachStatement(ForEachStatementAst forEachStatementAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitForStatement(ForStatementAst forStatementAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitWhileStatement(WhileStatementAst whileStatementAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitCatchClause(CatchClauseAst catchClauseAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitTryStatement(TryStatementAst tryStatementAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitBreakStatement(BreakStatementAst breakStatementAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitContinueStatement(ContinueStatementAst continueStatementAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitReturnStatement(ReturnStatementAst returnStatementAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitExitStatement(ExitStatementAst exitStatementAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitThrowStatement(ThrowStatementAst throwStatementAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitPipeline(PipelineAst pipelineAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitCommand(CommandAst commandAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitCommandExpression(CommandExpressionAst commandExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitCommandParameter(CommandParameterAst commandParameterAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitMergingRedirection(MergingRedirectionAst redirectionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitFileRedirection(FileRedirectionAst redirectionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitConvertExpression(ConvertExpressionAst convertExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitConstantExpression(ConstantExpressionAst constantExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "SubExpression")]
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "subExpression")]
        public virtual AstVisitAction VisitSubExpression(SubExpressionAst subExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitUsingExpression(UsingExpressionAst usingExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitVariableExpression(VariableExpressionAst variableExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitMemberExpression(MemberExpressionAst memberExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitInvokeMemberExpression(InvokeMemberExpressionAst methodCallAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitArrayExpression(ArrayExpressionAst arrayExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitHashtable(HashtableAst hashtableAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Paren")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "paren")]
        public virtual AstVisitAction VisitParenExpression(ParenExpressionAst parenExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitIndexExpression(IndexExpressionAst indexExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitBlockStatement(BlockStatementAst blockStatementAst) { return AstVisitAction.Continue; }
        /// <summary/>
        public virtual AstVisitAction VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst) { return AstVisitAction.Continue; }
    }

    /// <summary>
    /// AstVisitor for new Ast node types.
    /// </summary>
    public abstract class AstVisitor2 : AstVisitor
    {
        /// <summary/>
        public virtual AstVisitAction VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst) { return AstVisitAction.Continue; }

        /// <summary/>
        public virtual AstVisitAction VisitPropertyMember(PropertyMemberAst propertyMemberAst) { return AstVisitAction.Continue; }

        /// <summary/>
        public virtual AstVisitAction VisitFunctionMember(FunctionMemberAst functionMemberAst) { return AstVisitAction.Continue; }

        /// <summary/>
        public virtual AstVisitAction VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst) { return AstVisitAction.Continue; }

        /// <summary/>
        public virtual AstVisitAction VisitUsingStatement(UsingStatementAst usingStatementAst) { return AstVisitAction.Continue; }

        /// <summary/>
        public virtual AstVisitAction VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst) { return AstVisitAction.Continue; }

        /// <summary/>
        public virtual AstVisitAction VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordStatementAst) { return AstVisitAction.Continue; }
    }

    /// <summary>
    /// Implement this interface when you implement <see cref="AstVisitor"/> or <see cref="AstVisitor2"/> when
    /// you want to do something after possibly visiting the children of the ast.
    /// </summary>
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
