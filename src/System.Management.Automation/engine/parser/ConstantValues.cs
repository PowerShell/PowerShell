// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation.Internal;
using System.Reflection;

namespace System.Management.Automation.Language
{
    /*
     * The IsGetPowerShellSafeValueVisitor class in SafeValues.cs used this class as the basis for implementation.
     * There is a number of similarities between these two classes, and changes (fixes) in this code
     * may need to be reflected in that class and vice versa
     */
    internal class IsConstantValueVisitor : ICustomAstVisitor2
    {
        public static bool IsConstant(Ast ast, out object constantValue, bool forAttribute = false, bool forRequires = false)
        {
            try
            {
                if ((bool)ast.Accept(new IsConstantValueVisitor { CheckingAttributeArgument = forAttribute, CheckingRequiresArgument = forRequires }))
                {
                    Ast parent = ast.Parent;
                    while (parent != null)
                    {
                        if (parent is DataStatementAst)
                        {
                            break;
                        }

                        parent = parent.Parent;
                    }

                    if (parent == null)
                    {
                        constantValue = ast.Accept(new ConstantValueVisitor { AttributeArgument = forAttribute, RequiresArgument = forRequires });
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // If we get an exception, ignore it and assume the expression isn't constant.
                // This can happen, e.g. if a cast is invalid:
                //     [int]"zed"
            }

            constantValue = null;
            return false;
        }

        internal bool CheckingAttributeArgument { get; set; }

        internal bool CheckingClassAttributeArguments { get; set; }

        internal bool CheckingRequiresArgument { get; set; }

        public object VisitErrorStatement(ErrorStatementAst errorStatementAst) { return false; }

        public object VisitErrorExpression(ErrorExpressionAst errorExpressionAst) { return false; }

        public object VisitScriptBlock(ScriptBlockAst scriptBlockAst) { return false; }

        public object VisitParamBlock(ParamBlockAst paramBlockAst) { return false; }

        public object VisitNamedBlock(NamedBlockAst namedBlockAst) { return false; }

        public object VisitTypeConstraint(TypeConstraintAst typeConstraintAst) { return false; }

        public object VisitAttribute(AttributeAst attributeAst) { return false; }

        public object VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst) { return false; }

        public object VisitParameter(ParameterAst parameterAst) { return false; }

        public object VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst) { return false; }

        public object VisitIfStatement(IfStatementAst ifStmtAst) { return false; }

        public object VisitTrap(TrapStatementAst trapStatementAst) { return false; }

        public object VisitSwitchStatement(SwitchStatementAst switchStatementAst) { return false; }

        public object VisitDataStatement(DataStatementAst dataStatementAst) { return false; }

        public object VisitForEachStatement(ForEachStatementAst forEachStatementAst) { return false; }

        public object VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst) { return false; }

        public object VisitForStatement(ForStatementAst forStatementAst) { return false; }

        public object VisitWhileStatement(WhileStatementAst whileStatementAst) { return false; }

        public object VisitCatchClause(CatchClauseAst catchClauseAst) { return false; }

        public object VisitTryStatement(TryStatementAst tryStatementAst) { return false; }

        public object VisitBreakStatement(BreakStatementAst breakStatementAst) { return false; }

        public object VisitContinueStatement(ContinueStatementAst continueStatementAst) { return false; }

        public object VisitReturnStatement(ReturnStatementAst returnStatementAst) { return false; }

        public object VisitExitStatement(ExitStatementAst exitStatementAst) { return false; }

        public object VisitThrowStatement(ThrowStatementAst throwStatementAst) { return false; }

        public object VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst) { return false; }

        public object VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst) { return false; }

        public object VisitCommand(CommandAst commandAst) { return false; }

        public object VisitCommandExpression(CommandExpressionAst commandExpressionAst) { return false; }

        public object VisitCommandParameter(CommandParameterAst commandParameterAst) { return false; }

        public object VisitFileRedirection(FileRedirectionAst fileRedirectionAst) { return false; }

        public object VisitMergingRedirection(MergingRedirectionAst mergingRedirectionAst) { return false; }

        public object VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst) { return false; }

        public object VisitIndexExpression(IndexExpressionAst indexExpressionAst) { return false; }

        public object VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst) { return false; }

        public object VisitBlockStatement(BlockStatementAst blockStatementAst) { return false; }

        public object VisitInvokeMemberExpression(InvokeMemberExpressionAst invokeMemberExpressionAst) { return false; }

        public object VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst) { return false; }

        public object VisitPropertyMember(PropertyMemberAst propertyMemberAst) { return false; }

        public object VisitFunctionMember(FunctionMemberAst functionMemberAst) { return false; }

        public object VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst) { return false; }

        public object VisitUsingStatement(UsingStatementAst usingStatement) { return false; }

        public object VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst) { return false; }

        public object VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordAst) { return false; }

        public object VisitStatementBlock(StatementBlockAst statementBlockAst)
        {
            if (statementBlockAst.Traps != null) 
            {
                return false;
            }

            if (statementBlockAst.Statements.Count > 1)
            {
                return false;
            }

            var pipeline = statementBlockAst.Statements.FirstOrDefault();
            return pipeline != null && (bool)pipeline.Accept(this);
        }

        public object VisitPipeline(PipelineAst pipelineAst)
        {
            var expr = pipelineAst.GetPureExpression();
            return expr != null && (bool)expr.Accept(this);
        }

        private static bool IsNullDivisor(ExpressionAst operand)
        {
            if (!(operand is VariableExpressionAst varExpr))
            {
                return false;
            }

            var parent = operand.Parent as BinaryExpressionAst;
            if (parent == null || parent.Right != operand)
            {
                return false;
            }

            switch (parent.Operator)
            {
                case TokenKind.Divide:
                case TokenKind.DivideEquals:
                case TokenKind.Rem:
                case TokenKind.RemainderEquals:
                    string name = varExpr.VariablePath.UnqualifiedPath;
                    return (name.Equals(SpecialVariables.False, StringComparison.OrdinalIgnoreCase) ||
                            name.Equals(SpecialVariables.Null, StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }

        public object VisitTernaryExpression(TernaryExpressionAst ternaryExpressionAst)
        {
            return (bool)ternaryExpressionAst.Condition.Accept(this) &&
                   (bool)ternaryExpressionAst.IfTrue.Accept(this) &&
                   (bool)ternaryExpressionAst.IfFalse.Accept(this);
        }

        public object VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst)
        {
            return binaryExpressionAst.Operator.HasTrait(TokenFlags.CanConstantFold) &&
                (bool)binaryExpressionAst.Left.Accept(this) && (bool)binaryExpressionAst.Right.Accept(this)
                && !IsNullDivisor(binaryExpressionAst.Right);
        }

        public object VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst)
        {
            return unaryExpressionAst.TokenKind.HasTrait(TokenFlags.CanConstantFold) &&
                (bool)unaryExpressionAst.Child.Accept(this);
        }

        public object VisitConvertExpression(ConvertExpressionAst convertExpressionAst)
        {
            var type = convertExpressionAst.Type.TypeName.GetReflectionType();
            if (type == null)
            {
                return false;
            }

            if (!type.IsSafePrimitive())
            {
                // Only do conversions to built-in types - other conversions might not
                // be safe to optimize.
                return false;
            }

            return (bool)convertExpressionAst.Child.Accept(this);
        }

        public object VisitConstantExpression(ConstantExpressionAst constantExpressionAst)
        {
            return true;
        }

        public object VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst)
        {
            return true;
        }

        public object VisitSubExpression(SubExpressionAst subExpressionAst)
        {
            return subExpressionAst.SubExpression.Accept(this);
        }

        public object VisitUsingExpression(UsingExpressionAst usingExpressionAst)
        {
            // $using:true should be constant - it's silly to write that, but not harmful.
            return usingExpressionAst.SubExpression.Accept(this);
        }

        public object VisitVariableExpression(VariableExpressionAst variableExpressionAst)
        {
            return variableExpressionAst.IsConstantVariable();
        }

        public object VisitTypeExpression(TypeExpressionAst typeExpressionAst)
        {
            // We defer trying to resolve a type expression as an attribute argument
            // until the script/function is first run, so it's OK if a type expression
            // as an attribute argument cannot be resolved yet.
            return CheckingAttributeArgument ||
                typeExpressionAst.TypeName.GetReflectionType() != null;
        }

        public object VisitMemberExpression(MemberExpressionAst memberExpressionAst)
        {
            if (!memberExpressionAst.Static || memberExpressionAst.Expression is not TypeExpressionAst)
            {
                return false;
            }

            var type = ((TypeExpressionAst)memberExpressionAst.Expression).TypeName.GetReflectionType();
            if (type == null)
            {
                return false;
            }

            if (!(memberExpressionAst.Member is StringConstantExpressionAst member))
            {
                return false;
            }

            var memberInfo = type.GetMember(member.Value, MemberTypes.Field,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (memberInfo.Length != 1)
            {
                return false;
            }

            return (((FieldInfo)memberInfo[0]).Attributes & FieldAttributes.Literal) != 0;
        }

        public object VisitArrayExpression(ArrayExpressionAst arrayExpressionAst)
        {
            return false;
        }

        public object VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst)
        {
            // An array literal is a constant when we're generating metadata, but when
            // we're generating code, we need to create new arrays or we'd have an aliasing problem.
            return (CheckingAttributeArgument || CheckingRequiresArgument) && arrayLiteralAst.Elements.All(e => (bool)e.Accept(this));
        }

        public object VisitHashtable(HashtableAst hashtableAst)
        {
            return CheckingRequiresArgument &&
                   hashtableAst.KeyValuePairs.All(pair => (bool)pair.Item1.Accept(this) && (bool)pair.Item2.Accept(this));
        }

        public object VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
        {
            // A script block expression is a constant when we're generating metadata, but when
            // we're generating code, we need to create new script blocks so we can't use a constant.
            // Also - we have no way to describe a script block when generating .Net metadata, so
            // we must disallow script blocks as attribute arguments on/inside a class.
            return CheckingAttributeArgument && !CheckingClassAttributeArguments;
        }

        public object VisitParenExpression(ParenExpressionAst parenExpressionAst)
        {
            return parenExpressionAst.Pipeline.Accept(this);
        }
    }

    internal class ConstantValueVisitor : ICustomAstVisitor2
    {
        internal bool AttributeArgument { get; set; }

        internal bool RequiresArgument { get; set; }

        [Conditional("DEBUG")]
        [Conditional("ASSERTIONS_TRACE")]
        private void CheckIsConstant(Ast ast, string msg)
        {
            Diagnostics.Assert(
                (bool)ast.Accept(new IsConstantValueVisitor { CheckingAttributeArgument = this.AttributeArgument, CheckingRequiresArgument = RequiresArgument }), msg);
        }

        private static object CompileAndInvoke(Ast ast)
        {
            try
            {
                var compiler = new Compiler { CompilingConstantExpression = true };
                return Expression.Lambda((Expression)ast.Accept(compiler)).Compile().DynamicInvoke();
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        public object VisitErrorStatement(ErrorStatementAst errorStatementAst) { return AutomationNull.Value; }

        public object VisitErrorExpression(ErrorExpressionAst errorExpressionAst) { return AutomationNull.Value; }

        public object VisitScriptBlock(ScriptBlockAst scriptBlockAst) { return AutomationNull.Value; }

        public object VisitParamBlock(ParamBlockAst paramBlockAst) { return AutomationNull.Value; }

        public object VisitNamedBlock(NamedBlockAst namedBlockAst) { return AutomationNull.Value; }

        public object VisitTypeConstraint(TypeConstraintAst typeConstraintAst) { return AutomationNull.Value; }

        public object VisitAttribute(AttributeAst attributeAst) { return AutomationNull.Value; }

        public object VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst) { return AutomationNull.Value; }

        public object VisitParameter(ParameterAst parameterAst) { return AutomationNull.Value; }

        public object VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst) { return AutomationNull.Value; }

        public object VisitIfStatement(IfStatementAst ifStmtAst) { return AutomationNull.Value; }

        public object VisitTrap(TrapStatementAst trapStatementAst) { return AutomationNull.Value; }

        public object VisitSwitchStatement(SwitchStatementAst switchStatementAst) { return AutomationNull.Value; }

        public object VisitDataStatement(DataStatementAst dataStatementAst) { return AutomationNull.Value; }

        public object VisitForEachStatement(ForEachStatementAst forEachStatementAst) { return AutomationNull.Value; }

        public object VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst) { return AutomationNull.Value; }

        public object VisitForStatement(ForStatementAst forStatementAst) { return AutomationNull.Value; }

        public object VisitWhileStatement(WhileStatementAst whileStatementAst) { return AutomationNull.Value; }

        public object VisitCatchClause(CatchClauseAst catchClauseAst) { return AutomationNull.Value; }

        public object VisitTryStatement(TryStatementAst tryStatementAst) { return AutomationNull.Value; }

        public object VisitBreakStatement(BreakStatementAst breakStatementAst) { return AutomationNull.Value; }

        public object VisitContinueStatement(ContinueStatementAst continueStatementAst) { return AutomationNull.Value; }

        public object VisitReturnStatement(ReturnStatementAst returnStatementAst) { return AutomationNull.Value; }

        public object VisitExitStatement(ExitStatementAst exitStatementAst) { return AutomationNull.Value; }

        public object VisitThrowStatement(ThrowStatementAst throwStatementAst) { return AutomationNull.Value; }

        public object VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst) { return AutomationNull.Value; }

        public object VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst) { return AutomationNull.Value; }

        public object VisitCommand(CommandAst commandAst) { return AutomationNull.Value; }

        public object VisitCommandExpression(CommandExpressionAst commandExpressionAst) { return AutomationNull.Value; }

        public object VisitCommandParameter(CommandParameterAst commandParameterAst) { return AutomationNull.Value; }

        public object VisitFileRedirection(FileRedirectionAst fileRedirectionAst) { return AutomationNull.Value; }

        public object VisitMergingRedirection(MergingRedirectionAst mergingRedirectionAst) { return AutomationNull.Value; }

        public object VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst) { return AutomationNull.Value; }

        public object VisitIndexExpression(IndexExpressionAst indexExpressionAst) { return AutomationNull.Value; }

        public object VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst) { return AutomationNull.Value; }

        public object VisitBlockStatement(BlockStatementAst blockStatementAst) { return AutomationNull.Value; }

        public object VisitInvokeMemberExpression(InvokeMemberExpressionAst invokeMemberExpressionAst) { return AutomationNull.Value; }

        public object VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst) { return AutomationNull.Value; }

        public object VisitPropertyMember(PropertyMemberAst propertyMemberAst) { return AutomationNull.Value; }

        public object VisitFunctionMember(FunctionMemberAst functionMemberAst) { return AutomationNull.Value; }

        public object VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst) { return AutomationNull.Value; }

        public object VisitUsingStatement(UsingStatementAst usingStatement) { return AutomationNull.Value; }

        public object VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst) { return AutomationNull.Value; }

        public object VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordAst) { return AutomationNull.Value; }

        public object VisitStatementBlock(StatementBlockAst statementBlockAst)
        {
            CheckIsConstant(statementBlockAst, "Caller to verify ast is constant");
            return statementBlockAst.Statements[0].Accept(this);
        }

        public object VisitPipeline(PipelineAst pipelineAst)
        {
            CheckIsConstant(pipelineAst, "Caller to verify ast is constant");
            return pipelineAst.GetPureExpression().Accept(this);
        }

        public object VisitTernaryExpression(TernaryExpressionAst ternaryExpressionAst)
        {
            CheckIsConstant(ternaryExpressionAst, "Caller to verify ast is constant");

            object condition = ternaryExpressionAst.Condition.Accept(this);
            return LanguagePrimitives.IsTrue(condition)
                ? ternaryExpressionAst.IfTrue.Accept(this)
                : ternaryExpressionAst.IfFalse.Accept(this);
        }

        public object VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst)
        {
            CheckIsConstant(binaryExpressionAst, "Caller to verify ast is constant");
            return CompileAndInvoke(binaryExpressionAst);
        }

        public object VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst)
        {
            CheckIsConstant(unaryExpressionAst, "Caller to verify ast is constant");
            return CompileAndInvoke(unaryExpressionAst);
        }

        public object VisitConvertExpression(ConvertExpressionAst convertExpressionAst)
        {
            CheckIsConstant(convertExpressionAst, "Caller to verify ast is constant");
            return CompileAndInvoke(convertExpressionAst);
        }

        public object VisitConstantExpression(ConstantExpressionAst constantExpressionAst)
        {
            CheckIsConstant(constantExpressionAst, "Caller to verify ast is constant");
            return constantExpressionAst.Value;
        }

        public object VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst)
        {
            CheckIsConstant(stringConstantExpressionAst, "Caller to verify ast is constant");
            return stringConstantExpressionAst.Value;
        }

        public object VisitSubExpression(SubExpressionAst subExpressionAst)
        {
            CheckIsConstant(subExpressionAst, "Caller to verify ast is constant");
            return subExpressionAst.SubExpression.Accept(this);
        }

        public object VisitUsingExpression(UsingExpressionAst usingExpressionAst)
        {
            CheckIsConstant(usingExpressionAst.SubExpression, "Caller to verify ast is constant");
            return usingExpressionAst.SubExpression.Accept(this);
        }

        public object VisitVariableExpression(VariableExpressionAst variableExpressionAst)
        {
            CheckIsConstant(variableExpressionAst, "Caller to verify ast is constant");
            string name = variableExpressionAst.VariablePath.UnqualifiedPath;
            if (name.Equals(SpecialVariables.True, StringComparison.OrdinalIgnoreCase))
                return true;

            if (name.Equals(SpecialVariables.False, StringComparison.OrdinalIgnoreCase))
                return false;

            Diagnostics.Assert(name.Equals(SpecialVariables.Null, StringComparison.OrdinalIgnoreCase), "Unexpected constant variable");
            return null;
        }

        public object VisitTypeExpression(TypeExpressionAst typeExpressionAst)
        {
            CheckIsConstant(typeExpressionAst, "Caller to verify ast is constant");
            return typeExpressionAst.TypeName.GetReflectionType();
        }

        public object VisitMemberExpression(MemberExpressionAst memberExpressionAst)
        {
            CheckIsConstant(memberExpressionAst, "Caller to verify ast is constant");

            var type = ((TypeExpressionAst)memberExpressionAst.Expression).TypeName.GetReflectionType();
            var member = ((StringConstantExpressionAst)memberExpressionAst.Member).Value;
            var memberInfo = type.GetMember(member, MemberTypes.Field,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            return ((FieldInfo)memberInfo[0]).GetValue(null);
        }

        public object VisitArrayExpression(ArrayExpressionAst arrayExpressionAst)
        {
            CheckIsConstant(arrayExpressionAst, "Caller to verify ast is constant");
            return arrayExpressionAst.SubExpression.Accept(this);
        }

        public object VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst)
        {
            CheckIsConstant(arrayLiteralAst, "Caller to verify ast is constant");
            return arrayLiteralAst.Elements.Select(e => e.Accept(this)).ToArray();
        }

        public object VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
        {
            CheckIsConstant(scriptBlockExpressionAst, "Caller to verify ast is constant");
            return new ScriptBlock(scriptBlockExpressionAst.ScriptBlock, isFilter: false);
        }

        public object VisitParenExpression(ParenExpressionAst parenExpressionAst)
        {
            CheckIsConstant(parenExpressionAst, "Caller to verify ast is constant");
            return parenExpressionAst.Pipeline.Accept(this);
        }

        public object VisitHashtable(HashtableAst hashtableAst)
        {
            CheckIsConstant(hashtableAst, "Caller to verify ast is constant");
            var result = new Hashtable();
            foreach (var pair in hashtableAst.KeyValuePairs)
            {
                result.Add(pair.Item1.Accept(this), pair.Item2.Accept(this));
            }

            return result;
        }
    }
}
