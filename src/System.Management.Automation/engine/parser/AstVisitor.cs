// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Management.Automation.Language
{
    /// <summary>
    /// </summary>
    public interface ICustomAstVisitor
    {
        /// <summary/>
        object VisitErrorStatement(ErrorStatementAst errorStatementAst);
        /// <summary/>
        object VisitErrorExpression(ErrorExpressionAst errorExpressionAst);

        #region Script Blocks

        /// <summary/>
        object VisitScriptBlock(ScriptBlockAst scriptBlockAst);
        /// <summary/>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Param")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        object VisitParamBlock(ParamBlockAst paramBlockAst);
        /// <summary/>
        object VisitNamedBlock(NamedBlockAst namedBlockAst);
        /// <summary/>
        object VisitTypeConstraint(TypeConstraintAst typeConstraintAst);
        /// <summary/>
        object VisitAttribute(AttributeAst attributeAst);
        /// <summary/>
        object VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst);
        /// <summary/>
        object VisitParameter(ParameterAst parameterAst);

        #endregion Script Blocks

        #region Statements

        /// <summary/>
        object VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst);
        /// <summary/>
        object VisitStatementBlock(StatementBlockAst statementBlockAst);
        /// <summary/>
        object VisitIfStatement(IfStatementAst ifStmtAst);
        /// <summary/>
        object VisitTrap(TrapStatementAst trapStatementAst);
        /// <summary/>
        object VisitSwitchStatement(SwitchStatementAst switchStatementAst);
        /// <summary/>
        object VisitDataStatement(DataStatementAst dataStatementAst);
        /// <summary/>
        object VisitForEachStatement(ForEachStatementAst forEachStatementAst);
        /// <summary/>
        object VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst);
        /// <summary/>
        object VisitForStatement(ForStatementAst forStatementAst);
        /// <summary/>
        object VisitWhileStatement(WhileStatementAst whileStatementAst);
        /// <summary/>
        object VisitCatchClause(CatchClauseAst catchClauseAst);
        /// <summary/>
        object VisitTryStatement(TryStatementAst tryStatementAst);
        /// <summary/>
        object VisitBreakStatement(BreakStatementAst breakStatementAst);
        /// <summary/>
        object VisitContinueStatement(ContinueStatementAst continueStatementAst);
        /// <summary/>
        object VisitReturnStatement(ReturnStatementAst returnStatementAst);
        /// <summary/>
        object VisitExitStatement(ExitStatementAst exitStatementAst);
        /// <summary/>
        object VisitThrowStatement(ThrowStatementAst throwStatementAst);
        /// <summary/>
        object VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst);
        /// <summary/>
        object VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst);

        #endregion Statements

        #region Pipelines

        /// <summary/>
        object VisitPipeline(PipelineAst pipelineAst);
        /// <summary/>
        object VisitCommand(CommandAst commandAst);
        /// <summary/>
        object VisitCommandExpression(CommandExpressionAst commandExpressionAst);
        /// <summary/>
        object VisitCommandParameter(CommandParameterAst commandParameterAst);
        /// <summary/>
        object VisitFileRedirection(FileRedirectionAst fileRedirectionAst);
        /// <summary/>
        object VisitMergingRedirection(MergingRedirectionAst mergingRedirectionAst);
        #endregion Pipelines

        #region Expressions

        /// <summary/>
        object VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst);
        /// <summary/>
        object VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst);
        /// <summary/>
        object VisitConvertExpression(ConvertExpressionAst convertExpressionAst);
        /// <summary/>
        object VisitConstantExpression(ConstantExpressionAst constantExpressionAst);
        /// <summary/>
        object VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst);
        /// <summary/>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "SubExpression")]
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "subExpression")]
        object VisitSubExpression(SubExpressionAst subExpressionAst);
        /// <summary/>
        object VisitUsingExpression(UsingExpressionAst usingExpressionAst);
        /// <summary/>
        object VisitVariableExpression(VariableExpressionAst variableExpressionAst);
        /// <summary/>
        object VisitTypeExpression(TypeExpressionAst typeExpressionAst);
        /// <summary/>
        object VisitMemberExpression(MemberExpressionAst memberExpressionAst);
        /// <summary/>
        object VisitInvokeMemberExpression(InvokeMemberExpressionAst invokeMemberExpressionAst);
        /// <summary/>
        object VisitArrayExpression(ArrayExpressionAst arrayExpressionAst);
        /// <summary/>
        object VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst);
        /// <summary/>
        object VisitHashtable(HashtableAst hashtableAst);
        /// <summary/>
        object VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst);
        /// <summary/>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Paren")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "paren")]
        object VisitParenExpression(ParenExpressionAst parenExpressionAst);
        /// <summary/>
        object VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst);
        /// <summary/>
        object VisitIndexExpression(IndexExpressionAst indexExpressionAst);
        /// <summary/>
        object VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst);
        /// <summary/>
        object VisitBlockStatement(BlockStatementAst blockStatementAst);

        #endregion Expressions
    }

    /// <summary/>
    public interface ICustomAstVisitor2 : ICustomAstVisitor
    {
        /// <summary/>
        object VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst);

        /// <summary/>
        object VisitPropertyMember(PropertyMemberAst propertyMemberAst);

        /// <summary/>
        object VisitFunctionMember(FunctionMemberAst functionMemberAst);

        /// <summary/>
        object VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst);

        /// <summary/>
        object VisitUsingStatement(UsingStatementAst usingStatement);

        /// <summary/>
        object VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst);

        /// <summary/>
        object VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordAst);
    }

#if DEBUG
    class CheckAllParentsSet : AstVisitor2
    {
        internal CheckAllParentsSet(Ast root)
        {
            this.Root = root;
        }

        private Ast Root { get; set; }

        internal AstVisitAction CheckParent(Ast ast)
        {
            if (ast != Root)
            {
                Diagnostics.Assert(ast.Parent != null, "Parent not set");
            }

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitErrorStatement(ErrorStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitErrorExpression(ErrorExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitScriptBlock(ScriptBlockAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitParamBlock(ParamBlockAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitNamedBlock(NamedBlockAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitTypeConstraint(TypeConstraintAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitAttribute(AttributeAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitParameter(ParameterAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitTypeExpression(TypeExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitStatementBlock(StatementBlockAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitIfStatement(IfStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitTrap(TrapStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitSwitchStatement(SwitchStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitDataStatement(DataStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitForEachStatement(ForEachStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitDoWhileStatement(DoWhileStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitForStatement(ForStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitWhileStatement(WhileStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitCatchClause(CatchClauseAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitTryStatement(TryStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitBreakStatement(BreakStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitContinueStatement(ContinueStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitReturnStatement(ReturnStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitExitStatement(ExitStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitThrowStatement(ThrowStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitDoUntilStatement(DoUntilStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitAssignmentStatement(AssignmentStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitPipeline(PipelineAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitCommand(CommandAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitCommandExpression(CommandExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitCommandParameter(CommandParameterAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitMergingRedirection(MergingRedirectionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitFileRedirection(FileRedirectionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitBinaryExpression(BinaryExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitUnaryExpression(UnaryExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitConvertExpression(ConvertExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitConstantExpression(ConstantExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitStringConstantExpression(StringConstantExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitSubExpression(SubExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitUsingExpression(UsingExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitVariableExpression(VariableExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitMemberExpression(MemberExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitInvokeMemberExpression(InvokeMemberExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitArrayExpression(ArrayExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitArrayLiteral(ArrayLiteralAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitHashtable(HashtableAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitParenExpression(ParenExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitExpandableStringExpression(ExpandableStringExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitIndexExpression(IndexExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitAttributedExpression(AttributedExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitBlockStatement(BlockStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitNamedAttributeArgument(NamedAttributeArgumentAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitTypeDefinition(TypeDefinitionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitFunctionMember(FunctionMemberAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitPropertyMember(PropertyMemberAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitUsingStatement(UsingStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitConfigurationDefinition(ConfigurationDefinitionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitDynamicKeywordStatement(DynamicKeywordStatementAst ast) { return CheckParent(ast); }
    }

    /// <summary>
    /// Check if <see cref="TypeConstraintAst"/> contains <see cref="TypeBuilder "/> type.
    /// </summary>
    class CheckTypeBuilder : AstVisitor2
    {
        public override AstVisitAction VisitTypeConstraint(TypeConstraintAst ast)
        {
            Type type = ast.TypeName.GetReflectionType();
            if (type != null)
            {
                Diagnostics.Assert(!(type is TypeBuilder), "ReflectionType can never be TypeBuilder");
            }

            return AstVisitAction.Continue;
        }
    }
#endif

    /// <summary>
    /// Searches an AST, using the evaluation function provided by either of the constructors.
    /// </summary>
    internal class AstSearcher : AstVisitor2
    {
        #region External interface

        internal static IEnumerable<Ast> FindAll(Ast ast, Func<Ast, bool> predicate, bool searchNestedScriptBlocks)
        {
            Diagnostics.Assert(ast != null && predicate != null, "caller to verify arguments");

            var searcher = new AstSearcher(predicate, stopOnFirst: false, searchNestedScriptBlocks: searchNestedScriptBlocks);
            ast.InternalVisit(searcher);
            return searcher.Results;
        }

        internal static Ast FindFirst(Ast ast, Func<Ast, bool> predicate, bool searchNestedScriptBlocks)
        {
            Diagnostics.Assert(ast != null && predicate != null, "caller to verify arguments");

            var searcher = new AstSearcher(predicate, stopOnFirst: true, searchNestedScriptBlocks: searchNestedScriptBlocks);
            ast.InternalVisit(searcher);
            return searcher.Results.FirstOrDefault();
        }

        internal static bool Contains(Ast ast, Func<Ast, bool> predicate, bool searchNestedScriptBlocks)
        {
            Diagnostics.Assert(ast != null && predicate != null, "caller to verify arguments");

            var searcher = new AstSearcher(predicate, stopOnFirst: true, searchNestedScriptBlocks: searchNestedScriptBlocks);
            ast.InternalVisit(searcher);
            return searcher.Results.FirstOrDefault() != null;
        }

        internal static bool IsUsingDollarInput(Ast ast)
        {
            return (AstSearcher.Contains(
                ast,
                ast_ =>
                {
                    var varAst = ast_ as VariableExpressionAst;
                    if (varAst != null)
                    {
                        return varAst.VariablePath.IsVariable &&
                               varAst.VariablePath.UnqualifiedPath.Equals(SpecialVariables.Input,
                                                                          StringComparison.OrdinalIgnoreCase);
                    }

                    return false;
                },
                searchNestedScriptBlocks: false));
        }

        #endregion External interface

        protected AstSearcher(Func<Ast, bool> callback, bool stopOnFirst, bool searchNestedScriptBlocks)
        {
            _callback = callback;
            _stopOnFirst = stopOnFirst;
            _searchNestedScriptBlocks = searchNestedScriptBlocks;
            this.Results = new List<Ast>();
        }

        private readonly Func<Ast, bool> _callback;
        private readonly bool _stopOnFirst;
        private readonly bool _searchNestedScriptBlocks;
        protected readonly List<Ast> Results;

        protected AstVisitAction Check(Ast ast)
        {
            if (_callback(ast))
            {
                Results.Add(ast);
                if (_stopOnFirst)
                {
                    return AstVisitAction.StopVisit;
                }
            }

            return AstVisitAction.Continue;
        }

        protected AstVisitAction CheckScriptBlock(Ast ast)
        {
            var action = Check(ast);
            if (action == AstVisitAction.Continue && !_searchNestedScriptBlocks)
            {
                action = AstVisitAction.SkipChildren;
            }

            return action;
        }

        public override AstVisitAction VisitErrorStatement(ErrorStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitErrorExpression(ErrorExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitScriptBlock(ScriptBlockAst ast) { return Check(ast); }

        public override AstVisitAction VisitParamBlock(ParamBlockAst ast) { return Check(ast); }

        public override AstVisitAction VisitNamedBlock(NamedBlockAst ast) { return Check(ast); }

        public override AstVisitAction VisitTypeConstraint(TypeConstraintAst ast) { return Check(ast); }

        public override AstVisitAction VisitAttribute(AttributeAst ast) { return Check(ast); }

        public override AstVisitAction VisitParameter(ParameterAst ast) { return Check(ast); }

        public override AstVisitAction VisitTypeExpression(TypeExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst ast) { return CheckScriptBlock(ast); }

        public override AstVisitAction VisitStatementBlock(StatementBlockAst ast) { return Check(ast); }

        public override AstVisitAction VisitIfStatement(IfStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitTrap(TrapStatementAst ast) { return CheckScriptBlock(ast); }

        public override AstVisitAction VisitSwitchStatement(SwitchStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitDataStatement(DataStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitForEachStatement(ForEachStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitDoWhileStatement(DoWhileStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitForStatement(ForStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitWhileStatement(WhileStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitCatchClause(CatchClauseAst ast) { return Check(ast); }

        public override AstVisitAction VisitTryStatement(TryStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitBreakStatement(BreakStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitContinueStatement(ContinueStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitReturnStatement(ReturnStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitExitStatement(ExitStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitThrowStatement(ThrowStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitDoUntilStatement(DoUntilStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitAssignmentStatement(AssignmentStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitPipeline(PipelineAst ast) { return Check(ast); }

        public override AstVisitAction VisitCommand(CommandAst ast) { return Check(ast); }

        public override AstVisitAction VisitCommandExpression(CommandExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitCommandParameter(CommandParameterAst ast) { return Check(ast); }

        public override AstVisitAction VisitMergingRedirection(MergingRedirectionAst ast) { return Check(ast); }

        public override AstVisitAction VisitFileRedirection(FileRedirectionAst ast) { return Check(ast); }

        public override AstVisitAction VisitBinaryExpression(BinaryExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitUnaryExpression(UnaryExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitConvertExpression(ConvertExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitConstantExpression(ConstantExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitStringConstantExpression(StringConstantExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitSubExpression(SubExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitUsingExpression(UsingExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitVariableExpression(VariableExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitMemberExpression(MemberExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitInvokeMemberExpression(InvokeMemberExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitArrayExpression(ArrayExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitArrayLiteral(ArrayLiteralAst ast) { return Check(ast); }

        public override AstVisitAction VisitHashtable(HashtableAst ast) { return Check(ast); }

        public override AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst ast) { return CheckScriptBlock(ast); }

        public override AstVisitAction VisitParenExpression(ParenExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitExpandableStringExpression(ExpandableStringExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitIndexExpression(IndexExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitAttributedExpression(AttributedExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitNamedAttributeArgument(NamedAttributeArgumentAst ast) { return Check(ast); }

        public override AstVisitAction VisitTypeDefinition(TypeDefinitionAst ast) { return Check(ast); }

        public override AstVisitAction VisitPropertyMember(PropertyMemberAst ast) { return Check(ast); }

        public override AstVisitAction VisitFunctionMember(FunctionMemberAst ast) { return Check(ast); }

        public override AstVisitAction VisitUsingStatement(UsingStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitBlockStatement(BlockStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitConfigurationDefinition(ConfigurationDefinitionAst ast) { return Check(ast); }

        public override AstVisitAction VisitDynamicKeywordStatement(DynamicKeywordStatementAst ast) { return Check(ast); }
    }

    /// <summary>
    /// Default implementation of <see cref="ICustomAstVisitor"/> interface.
    /// </summary>
    public abstract class DefaultCustomAstVisitor : ICustomAstVisitor
    {
        /// <summary/>
        public virtual object VisitErrorStatement(ErrorStatementAst errorStatementAst) { return null; }
        /// <summary/>
        public virtual object VisitErrorExpression(ErrorExpressionAst errorExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitScriptBlock(ScriptBlockAst scriptBlockAst) { return null; }
        /// <summary/>
        public virtual object VisitParamBlock(ParamBlockAst paramBlockAst) { return null; }
        /// <summary/>
        public virtual object VisitNamedBlock(NamedBlockAst namedBlockAst) { return null; }
        /// <summary/>
        public virtual object VisitTypeConstraint(TypeConstraintAst typeConstraintAst) { return null; }
        /// <summary/>
        public virtual object VisitAttribute(AttributeAst attributeAst) { return null; }
        /// <summary/>
        public virtual object VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst) { return null; }
        /// <summary/>
        public virtual object VisitParameter(ParameterAst parameterAst) { return null; }
        /// <summary/>
        public virtual object VisitStatementBlock(StatementBlockAst statementBlockAst) { return null; }
        /// <summary/>
        public virtual object VisitIfStatement(IfStatementAst ifStmtAst) { return null; }
        /// <summary/>
        public virtual object VisitTrap(TrapStatementAst trapStatementAst) { return null; }
        /// <summary/>
        public virtual object VisitSwitchStatement(SwitchStatementAst switchStatementAst) { return null; }
        /// <summary/>
        public virtual object VisitDataStatement(DataStatementAst dataStatementAst) { return null; }
        /// <summary/>
        public virtual object VisitForEachStatement(ForEachStatementAst forEachStatementAst) { return null; }
        /// <summary/>
        public virtual object VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst) { return null; }
        /// <summary/>
        public virtual object VisitForStatement(ForStatementAst forStatementAst) { return null; }
        /// <summary/>
        public virtual object VisitWhileStatement(WhileStatementAst whileStatementAst) { return null; }
        /// <summary/>
        public virtual object VisitCatchClause(CatchClauseAst catchClauseAst) { return null; }
        /// <summary/>
        public virtual object VisitTryStatement(TryStatementAst tryStatementAst) { return null; }
        /// <summary/>
        public virtual object VisitBreakStatement(BreakStatementAst breakStatementAst) { return null; }
        /// <summary/>
        public virtual object VisitContinueStatement(ContinueStatementAst continueStatementAst) { return null; }
        /// <summary/>
        public virtual object VisitReturnStatement(ReturnStatementAst returnStatementAst) { return null; }
        /// <summary/>
        public virtual object VisitExitStatement(ExitStatementAst exitStatementAst) { return null; }
        /// <summary/>
        public virtual object VisitThrowStatement(ThrowStatementAst throwStatementAst) { return null; }
        /// <summary/>
        public virtual object VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst) { return null; }
        /// <summary/>
        public virtual object VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst) { return null; }
        /// <summary/>
        public virtual object VisitPipeline(PipelineAst pipelineAst) { return null; }
        /// <summary/>
        public virtual object VisitCommand(CommandAst commandAst) { return null; }
        /// <summary/>
        public virtual object VisitCommandExpression(CommandExpressionAst commandExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitCommandParameter(CommandParameterAst commandParameterAst) { return null; }
        /// <summary/>
        public virtual object VisitFileRedirection(FileRedirectionAst fileRedirectionAst) { return null; }
        /// <summary/>
        public virtual object VisitMergingRedirection(MergingRedirectionAst mergingRedirectionAst) { return null; }
        /// <summary/>
        public virtual object VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitConvertExpression(ConvertExpressionAst convertExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitConstantExpression(ConstantExpressionAst constantExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitSubExpression(SubExpressionAst subExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitUsingExpression(UsingExpressionAst usingExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitVariableExpression(VariableExpressionAst variableExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitTypeExpression(TypeExpressionAst typeExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitMemberExpression(MemberExpressionAst memberExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitInvokeMemberExpression(InvokeMemberExpressionAst invokeMemberExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitArrayExpression(ArrayExpressionAst arrayExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst) { return null; }
        /// <summary/>
        public virtual object VisitHashtable(HashtableAst hashtableAst) { return null; }
        /// <summary/>
        public virtual object VisitParenExpression(ParenExpressionAst parenExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitIndexExpression(IndexExpressionAst indexExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitBlockStatement(BlockStatementAst blockStatementAst) { return null; }
        /// <summary/>
        public virtual object VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst) { return null; }
        /// <summary/>
        public virtual object VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst) { return null; }
    }

    /// <summary>
    /// Default implementation of <see cref="ICustomAstVisitor2"/> interface.
    /// </summary>
    public abstract class DefaultCustomAstVisitor2 : DefaultCustomAstVisitor, ICustomAstVisitor2
    {
        /// <summary/>
        public virtual object VisitPropertyMember(PropertyMemberAst propertyMemberAst) { return null; }
        /// <summary/>
        public virtual object VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst) { return null; }
        /// <summary/>
        public virtual object VisitUsingStatement(UsingStatementAst usingStatement) { return null; }
        /// <summary/>
        public virtual object VisitConfigurationDefinition(ConfigurationDefinitionAst configurationAst) { return null; }
        /// <summary/>
        public virtual object VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordAst) { return null; }
        /// <summary/>
        public virtual object VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst) { return null; }
        /// <summary/>
        public virtual object VisitFunctionMember(FunctionMemberAst functionMemberAst) { return null; }
    }
}
