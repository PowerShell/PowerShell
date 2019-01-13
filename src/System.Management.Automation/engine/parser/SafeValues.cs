// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.IO;
using System.Linq;
using System.Management.Automation.Internal;

/*
 *
 * This visitor makes a determination as to whether an operation is safe in a GetPowerShell API Context.
 * It is modeled on the ConstantValueVisitor with changes which allow those
 * operations which are deemed safe, rather than constant. The following are differences from
 * ConstantValueVisitor:
 *  o Because we are going to call for values in ScriptBlockToPowerShell, the
 *    Get*ValueVisitor class is removed
 *  o IsGetPowerShellSafeValueVisitor only needs to determine whether it is safe, we won't return
 *    anything but that determination (vs actually returning a value in the out constantValue parameter
 *    as is found in the ConstantValueVisitor).
 *  o the internal bool members (Checking* members in ConstantValues) aren't needed as those checks are not germane
 *  o VisitExpandableStringExpression may be safe under the proper circumstances
 *  o VisitIndexExpression may be safe under the proper circumstances
 *  o VisitStatementBlock is safe if its component statements are safe
 *  o VisitBinaryExpression is not safe as it allows for a DOS attack
 *  o VisitVariableExpression is generally safe, there are checks outside of this code for ensuring variables actually
 *    have provided references. Those other checks ensure that the variable isn't something like $PID or $HOME, etc.,
 *    otherwise it's a safe operation, such as reference to a variable such as $true, or passed parameters.
 *  o VisitTypeExpression is not safe as it enables determining what types are available on the system which
 *    can imply what software has been installed on the system.
 *  o VisitMemberExpression is not safe as allows for the same attack as VisitTypeExpression
 *  o VisitArrayExpression may be safe if its components are safe
 *  o VisitArrayLiteral may be safe if its components are safe
 *  o VisitHashtable may be safe if its components are safe
 *
 */

namespace System.Management.Automation.Language
{
    internal class IsSafeValueVisitor : ICustomAstVisitor
    {
        public static bool IsAstSafe(Ast ast, GetSafeValueVisitor.SafeValueContext safeValueContext)
        {
            IsSafeValueVisitor visitor = new IsSafeValueVisitor(safeValueContext);
            return visitor.IsAstSafe(ast);
        }

        internal IsSafeValueVisitor(GetSafeValueVisitor.SafeValueContext safeValueContext)
        {
            _safeValueContext = safeValueContext;
        }

        internal bool IsAstSafe(Ast ast)
        {
            if ((bool)ast.Accept(this) && _visitCount < MaxVisitCount)
            {
                return true;
            }

            return false;
        }

        // A readonly singleton with the default SafeValueContext.
        internal static readonly IsSafeValueVisitor Default = new IsSafeValueVisitor(GetSafeValueVisitor.SafeValueContext.Default);

        // This is a check of the number of visits
        private uint _visitCount = 0;
        private const uint MaxVisitCount = 5000;
        private const int MaxHashtableKeyCount = 500;

        // Used to determine if we are being called within a GetPowerShell() context,
        // which does some additional security verification outside of the scope of
        // what we can verify.
        private readonly GetSafeValueVisitor.SafeValueContext _safeValueContext;

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

        public object VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst) { return false; }

        public object VisitBlockStatement(BlockStatementAst blockStatementAst) { return false; }

        public object VisitInvokeMemberExpression(InvokeMemberExpressionAst invokeMemberExpressionAst) { return false; }

        public object VisitIndexExpression(IndexExpressionAst indexExpressionAst)
        {
            return (bool)indexExpressionAst.Index.Accept(this) && (bool)indexExpressionAst.Target.Accept(this);
        }

        public object VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst)
        {
            bool isSafe = true;
            foreach (var nestedExpression in expandableStringExpressionAst.NestedExpressions)
            {
                _visitCount++;
                if (!(bool)nestedExpression.Accept(this))
                {
                    isSafe = false;
                    break;
                }
            }

            return isSafe;
        }

        public object VisitStatementBlock(StatementBlockAst statementBlockAst)
        {
            bool isSafe = true;
            foreach (var statement in statementBlockAst.Statements)
            {
                _visitCount++;
                if (statement == null)
                {
                    isSafe = false;
                    break;
                }

                if (!(bool)statement.Accept(this))
                {
                    isSafe = false;
                    break;
                }
            }

            return isSafe;
        }

        public object VisitPipeline(PipelineAst pipelineAst)
        {
            var expr = pipelineAst.GetPureExpression();
            return expr != null && (bool)expr.Accept(this);
        }

        public object VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst)
        {
            // This can be used for a denial of service
            // Write-Output (((((("AAAAAAAAAAAAAAAAAAAAAA"*2)*2)*2)*2)*2)*2)
            // Keep on going with that pattern, and we're generating gigabytes of strings.
            return false;
        }

        public object VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst)
        {
            bool unaryExpressionIsSafe = unaryExpressionAst.TokenKind.HasTrait(TokenFlags.CanConstantFold) &&
                !unaryExpressionAst.TokenKind.HasTrait(TokenFlags.DisallowedInRestrictedMode) &&
                (bool)unaryExpressionAst.Child.Accept(this);
            if (unaryExpressionIsSafe)
            {
                _visitCount++;
            }

            return unaryExpressionIsSafe;
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

            _visitCount++;
            return (bool)convertExpressionAst.Child.Accept(this);
        }

        public object VisitConstantExpression(ConstantExpressionAst constantExpressionAst)
        {
            _visitCount++;
            return true;
        }

        public object VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst)
        {
            _visitCount++;
            return true;
        }

        public object VisitSubExpression(SubExpressionAst subExpressionAst)
        {
            return subExpressionAst.SubExpression.Accept(this);
        }

        public object VisitUsingExpression(UsingExpressionAst usingExpressionAst)
        {
            // $using:true should be safe - it's silly to write that, but not harmful.
            _visitCount++;
            return usingExpressionAst.SubExpression.Accept(this);
        }

        public object VisitVariableExpression(VariableExpressionAst variableExpressionAst)
        {
            _visitCount++;

            if (_safeValueContext == GetSafeValueVisitor.SafeValueContext.GetPowerShell)
            {
                // GetPowerShell does its own validation of allowed variables in the
                // context of the entire script block, and then supplies this visitor
                // with the CommandExpressionAst directly. This
                // prevents us from evaluating variable safety in this visitor,
                // so we rely on GetPowerShell's implementation.
                return true;
            }

            if (_safeValueContext == GetSafeValueVisitor.SafeValueContext.ModuleAnalysis)
            {
                return variableExpressionAst.IsConstantVariable() ||
                       (variableExpressionAst.VariablePath.IsUnqualified &&
                        variableExpressionAst.VariablePath.UnqualifiedPath.Equals(SpecialVariables.PSScriptRoot, StringComparison.OrdinalIgnoreCase));
            }

            bool unused = false;
            return variableExpressionAst.IsSafeVariableReference(null, ref unused);
        }

        public object VisitTypeExpression(TypeExpressionAst typeExpressionAst)
        {
            // Type expressions are not safe as they allow fingerprinting by providing
            // a set of types, you can inspect the types in the AppDomain implying which assemblies are in use
            // and their version
            return false;
        }

        public object VisitMemberExpression(MemberExpressionAst memberExpressionAst)
        {
            return false;
        }

        public object VisitArrayExpression(ArrayExpressionAst arrayExpressionAst)
        {
            // An Array expression *may* be safe, if its components are safe
            return arrayExpressionAst.SubExpression.Accept(this);
        }

        public object VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst)
        {
            bool isSafe = arrayLiteralAst.Elements.All(e => (bool)e.Accept(this));
            // An array literal is safe
            return isSafe;
        }

        public object VisitHashtable(HashtableAst hashtableAst)
        {
            if (hashtableAst.KeyValuePairs.Count > MaxHashtableKeyCount)
            {
                return false;
            }

            return hashtableAst.KeyValuePairs.All(pair => (bool)pair.Item1.Accept(this) && (bool)pair.Item2.Accept(this));
        }

        public object VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
        {
            // Returning a ScriptBlock instance itself is OK, bad stuff only happens
            // when invoking one (which is blocked)
            return true;
        }

        public object VisitParenExpression(ParenExpressionAst parenExpressionAst)
        {
            return parenExpressionAst.Pipeline.Accept(this);
        }
    }

    /*
     * This implementation retrieves the safe value without directly calling the compiler
     * except in the case of handling the unary operator
     * ExecutionContext is provided to ensure we can resolve variables
     */
    internal class GetSafeValueVisitor : ICustomAstVisitor
    {
        internal enum SafeValueContext
        {
            Default,
            GetPowerShell,
            ModuleAnalysis
        }

        // future proofing
        private GetSafeValueVisitor() { }

        public static object GetSafeValue(Ast ast, ExecutionContext context, SafeValueContext safeValueContext)
        {
            s_context = context;
            if (IsSafeValueVisitor.IsAstSafe(ast, safeValueContext))
            {
                return ast.Accept(new GetSafeValueVisitor());
            }

            if (safeValueContext == SafeValueContext.ModuleAnalysis)
            {
                return null;
            }

            throw PSTraceSource.NewArgumentException("ast");
        }

        private static ExecutionContext s_context;

        public object VisitErrorStatement(ErrorStatementAst errorStatementAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitErrorExpression(ErrorExpressionAst errorExpressionAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitScriptBlock(ScriptBlockAst scriptBlockAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitParamBlock(ParamBlockAst paramBlockAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitNamedBlock(NamedBlockAst namedBlockAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitTypeConstraint(TypeConstraintAst typeConstraintAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitAttribute(AttributeAst attributeAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitParameter(ParameterAst parameterAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitIfStatement(IfStatementAst ifStmtAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitTrap(TrapStatementAst trapStatementAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitSwitchStatement(SwitchStatementAst switchStatementAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitDataStatement(DataStatementAst dataStatementAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitForEachStatement(ForEachStatementAst forEachStatementAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitForStatement(ForStatementAst forStatementAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitWhileStatement(WhileStatementAst whileStatementAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitCatchClause(CatchClauseAst catchClauseAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitTryStatement(TryStatementAst tryStatementAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitBreakStatement(BreakStatementAst breakStatementAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitContinueStatement(ContinueStatementAst continueStatementAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitReturnStatement(ReturnStatementAst returnStatementAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitExitStatement(ExitStatementAst exitStatementAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitThrowStatement(ThrowStatementAst throwStatementAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitCommand(CommandAst commandAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitCommandExpression(CommandExpressionAst commandExpressionAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitCommandParameter(CommandParameterAst commandParameterAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitFileRedirection(FileRedirectionAst fileRedirectionAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitMergingRedirection(MergingRedirectionAst mergingRedirectionAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitBlockStatement(BlockStatementAst blockStatementAst) { throw PSTraceSource.NewArgumentException("ast"); }

        public object VisitInvokeMemberExpression(InvokeMemberExpressionAst invokeMemberExpressionAst) { throw PSTraceSource.NewArgumentException("ast"); }

        //
        // This is similar to logic used deep in the engine for slicing something that can be sliced
        // It's recreated here because there isn't really a simple API which can be called for this case.
        // this can throw, but there really isn't useful information we can add, as the
        // offending expression will be presented in the case of any failure
        //
        private object GetSingleValueFromTarget(object target, object index)
        {
            var targetString = target as string;
            if (targetString != null)
            {
                var offset = (int)index;
                if (Math.Abs(offset) >= targetString.Length)
                {
                    return null;
                }

                return offset >= 0 ? targetString[offset] : targetString[targetString.Length + offset];
            }

            var targetArray = target as object[];
            if (targetArray != null)
            {
                // this can throw, that just gets percolated back
                var offset = (int)index;
                if (Math.Abs(offset) >= targetArray.Length)
                {
                    return null;
                }

                return offset >= 0 ? targetArray[offset] : targetArray[targetArray.Length + offset];
            }

            var targetHashtable = target as Hashtable;
            if (targetHashtable != null)
            {
                return targetHashtable[index];
            }
            // The actual exception doesn't really matter because the caller in ScriptBlockToPowerShell
            // will present the user with the offending script segment
            throw new Exception();
        }

        private object GetIndexedValueFromTarget(object target, object index)
        {
            var indexArray = index as object[];
            return indexArray != null ? ((object[])indexArray).Select(i => GetSingleValueFromTarget(target, i)).ToArray() : GetSingleValueFromTarget(target, index);
        }

        public object VisitIndexExpression(IndexExpressionAst indexExpressionAst)
        {
            // Get the value of the index and value and call the compiler
            var index = indexExpressionAst.Index.Accept(this);
            var target = indexExpressionAst.Target.Accept(this);
            if (index == null || target == null)
            {
                throw new ArgumentNullException("indexExpressionAst");
            }

            return GetIndexedValueFromTarget(target, index);
        }

        public object VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst)
        {
            object[] safeValues = new object[expandableStringExpressionAst.NestedExpressions.Count];
            // retrieve OFS, and if it doesn't exist set it to space
            string ofs = null;
            if (s_context != null)
            {
                ofs = s_context.SessionState.PSVariable.GetValue("OFS") as string;
            }

            if (ofs == null)
            {
                ofs = " ";
            }

            for (int offset = 0; offset < safeValues.Length; offset++)
            {
                var result = expandableStringExpressionAst.NestedExpressions[offset].Accept(this);
                // depending on the nested expression we may retrieve a variable, or even need to
                // execute a sub-expression. The result of which may be returned
                // as a scalar, array or nested array. If the unwrap of first array doesn't contain a nested
                // array we can then pass it to string.Join. If it *does* contain an array,
                // we need to unwrap the inner array and pass *that* to string.Join.
                //
                // This means we get the same answer with GetPowerShell() as in the command-line
                // { echo "abc $true $(1) $(2,3) def" }.Invoke() gives the same answer as
                // { echo "abc $true $(1) $(2,3) def" }.GetPowerShell().Invoke()
                // abc True 1 2 3 def
                // as does { echo "abc $true $(1) $(@(1,2),@(3,4)) def"
                // which is
                // abc True 1 System.Object[] System.Object[] def
                // fortunately, at this point, we're dealing with strings, so whatever the result
                // from the ToString method of the array (or scalar) elements, that's symmetrical with
                // a standard scriptblock invocation behavior
                var resultArray = result as object[];

                // In this environment, we can't use $OFS as we might expect. Retrieving OFS
                // might possibly leak server side info which we don't want, so we'll
                // assign ' ' as our OFS for purposes of GetPowerShell
                // Also, this will not call any script implementations of ToString (ala types.clixml)
                // This *will* result in a different result in those cases. However, to execute some
                // arbitrary script at this stage would be opening ourselves up to an attack
                if (resultArray != null)
                {
                    object[] subExpressionResult = new object[resultArray.Length];
                    for (int subExpressionOffset = 0;
                        subExpressionOffset < subExpressionResult.Length;
                        subExpressionOffset++)
                    {
                        // check to see if there is an array in our array,
                        object[] subResult = resultArray[subExpressionOffset] as object[];
                        if (subResult != null)
                        {
                            subExpressionResult[subExpressionOffset] = string.Join(ofs, subResult);
                        }
                        else // it is a scalar, so we can just add it to our collections
                        {
                            subExpressionResult[subExpressionOffset] = resultArray[subExpressionOffset];
                        }
                    }

                    safeValues[offset] = string.Join(ofs, subExpressionResult);
                }
                else
                {
                    safeValues[offset] = result;
                }
            }

            return StringUtil.Format(expandableStringExpressionAst.FormatExpression, safeValues);
        }

        public object VisitStatementBlock(StatementBlockAst statementBlockAst)
        {
            ArrayList statementList = new ArrayList();
            foreach (var statement in statementBlockAst.Statements)
            {
                if (statement != null)
                {
                    var obj = statement.Accept(this);
                    var enumerator = LanguagePrimitives.GetEnumerator(obj);
                    if (enumerator != null)
                    {
                        while (enumerator.MoveNext())
                        {
                            statementList.Add(enumerator.Current);
                        }
                    }
                    else
                    {
                        statementList.Add(obj);
                    }
                }
                else
                {
                    throw PSTraceSource.NewArgumentException("ast");
                }
            }

            return statementList.ToArray();
        }

        public object VisitPipeline(PipelineAst pipelineAst)
        {
            var expr = pipelineAst.GetPureExpression();
            if (expr != null)
            {
                return expr.Accept(this);
            }

            throw PSTraceSource.NewArgumentException("ast");
        }

        public object VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst)
        {
            // This can be used for a denial of service
            // Write-Output (((((("AAAAAAAAAAAAAAAAAAAAAA"*2)*2)*2)*2)*2)*2)
            // Keep on going with that pattern, and we're generating gigabytes of strings.
            throw PSTraceSource.NewArgumentException("ast");
        }

        public object VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst)
        {
            if (s_context != null)
            {
                return Compiler.GetExpressionValue(unaryExpressionAst, true, s_context, null);
            }
            else
            {
                throw PSTraceSource.NewArgumentException("ast");
            }
        }

        public object VisitConvertExpression(ConvertExpressionAst convertExpressionAst)
        {
            // at this point, we know we're safe because we checked both the type and the child,
            // so now we can just call the compiler and indicate that it's trusted (at this point)
            if (s_context != null)
            {
                return Compiler.GetExpressionValue(convertExpressionAst, true, s_context, null);
            }
            else
            {
                throw PSTraceSource.NewArgumentException("ast");
            }
        }

        public object VisitConstantExpression(ConstantExpressionAst constantExpressionAst)
        {
            return constantExpressionAst.Value;
        }

        public object VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst)
        {
            return stringConstantExpressionAst.Value;
        }

        public object VisitSubExpression(SubExpressionAst subExpressionAst)
        {
            return subExpressionAst.SubExpression.Accept(this);
        }

        public object VisitUsingExpression(UsingExpressionAst usingExpressionAst)
        {
            // $using:true should be safe - it's silly to write that, but not harmful.
            return usingExpressionAst.SubExpression.Accept(this);
        }

        public object VisitVariableExpression(VariableExpressionAst variableExpressionAst)
        {
            // There are earlier checks to be sure that we are not using unreferenced variables
            // this ensures that we only use what was declared in the param block
            // other variables such as true/false/args etc have been already vetted
            string name = variableExpressionAst.VariablePath.UnqualifiedPath;
            if (variableExpressionAst.IsConstantVariable())
            {
                if (name.Equals(SpecialVariables.True, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (name.Equals(SpecialVariables.False, StringComparison.OrdinalIgnoreCase))
                    return false;

                Diagnostics.Assert(name.Equals(SpecialVariables.Null, StringComparison.OrdinalIgnoreCase), "Unexpected constant variable");
                return null;
            }

            if (name.Equals(SpecialVariables.PSScriptRoot, StringComparison.OrdinalIgnoreCase))
            {
                var scriptFileName = variableExpressionAst.Extent.File;
                if (scriptFileName == null)
                    return null;

                return Path.GetDirectoryName(scriptFileName);
            }

            if (s_context != null)
            {
                return VariableOps.GetVariableValue(variableExpressionAst.VariablePath, s_context, variableExpressionAst);
            }

            throw PSTraceSource.NewArgumentException("ast");
        }

        public object VisitTypeExpression(TypeExpressionAst typeExpressionAst)
        {
            // Type expressions are not safe as they allow fingerprinting by providing
            // a set of types, you can inspect the types in the AppDomain implying which assemblies are in use
            // and their version
            throw PSTraceSource.NewArgumentException("ast");
        }

        public object VisitMemberExpression(MemberExpressionAst memberExpressionAst)
        {
            throw PSTraceSource.NewArgumentException("ast");
        }

        public object VisitArrayExpression(ArrayExpressionAst arrayExpressionAst)
        {
            // An Array expression *may* be safe, if its components are safe
            var arrayExpressionAstResult = (object[])arrayExpressionAst.SubExpression.Accept(this);
            return arrayExpressionAstResult;
        }

        public object VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst)
        {
            // An array literal is safe
            ArrayList arrayElements = new ArrayList();
            foreach (var element in arrayLiteralAst.Elements)
            {
                arrayElements.Add(element.Accept(this));
            }

            return arrayElements.ToArray();
        }

        public object VisitHashtable(HashtableAst hashtableAst)
        {
            Hashtable hashtable = new Hashtable(StringComparer.CurrentCultureIgnoreCase);
            foreach (var pair in hashtableAst.KeyValuePairs)
            {
                var key = pair.Item1.Accept(this);
                var value = pair.Item2.Accept(this);
                hashtable.Add(key, value);
            }

            return hashtable;
        }

        public object VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
        {
            return ScriptBlock.Create(scriptBlockExpressionAst.Extent.Text);
        }

        public object VisitParenExpression(ParenExpressionAst parenExpressionAst)
        {
            return parenExpressionAst.Pipeline.Accept(this);
        }
    }
}
