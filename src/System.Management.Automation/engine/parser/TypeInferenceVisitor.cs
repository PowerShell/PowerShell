// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.PowerShell.Commands;

using CimClass = Microsoft.Management.Infrastructure.CimClass;
using CimInstance = Microsoft.Management.Infrastructure.CimInstance;

namespace System.Management.Automation
{
    /// <summary>
    /// Enum describing permissions to use runtime evaluation during type inference
    /// </summary>
    public enum TypeInferenceRuntimePermissions {
        /// <summary>
        /// No runtime use is allowed
        /// </summary>
        None = 0,
        /// <summary>
        /// Use of SafeExprEvaluator visitor is allowed
        /// </summary>
        AllowSafeEval = 1,
    }

    /// <summary>
    /// static class containing methods to work with type inference of abstract syntax trees
    /// </summary>
    internal static class AstTypeInference
    {
        /// <summary>
        /// Infers the type that the result of executing a statement would have without using runtime safe eval
        /// </summary>
        /// <param name="ast">the ast to infer the type from</param>
        /// <returns></returns>
        public static IList<PSTypeName> InferTypeOf(Ast ast)
        {
            return InferTypeOf(ast, TypeInferenceRuntimePermissions.None);
        }

        /// <summary>
        /// Infers the type that the result of executing a statement would have
        /// </summary>
        /// <param name="ast">the ast to infer the type from</param>
        /// <param name="evalPermissions">The runtime usage permissions allowed during type inference</param>
        /// <returns></returns>
        public static IList<PSTypeName> InferTypeOf(Ast ast, TypeInferenceRuntimePermissions evalPermissions)
        {
            return InferTypeOf(ast, PowerShell.Create(RunspaceMode.CurrentRunspace), evalPermissions);
        }

        /// <summary>
        /// Infers the type that the result of executing a statement would have without using runtime safe eval
        /// </summary>
        /// <param name="ast">the ast to infer the type from</param>
        /// <param name="powerShell">the instance of powershell to user for expression evalutaion needed for type inference</param>
        /// <returns></returns>
        public static IList<PSTypeName> InferTypeOf(Ast ast, PowerShell powerShell)
        {
            return InferTypeOf(ast, powerShell, TypeInferenceRuntimePermissions.None);
        }

        /// <summary>
        /// Infers the type that the result of executing a statement would have
        /// </summary>
        /// <param name="ast">the ast to infer the type from</param>
        /// <param name="powerShell">the instance of powershell to user for expression evalutaion needed for type inference</param>
        /// <param name="evalPersmissions">The runtime usage permissions allowed during type inference</param>
        /// <returns></returns>
        public static IList<PSTypeName> InferTypeOf(Ast ast, PowerShell powerShell, TypeInferenceRuntimePermissions evalPersmissions)
        {
            var context = new TypeInferenceContext(powerShell);
            return InferTypeOf(ast, context, evalPersmissions);
        }

        /// <summary>
        /// Infers the type that the result of executing a statement would have
        /// </summary>
        /// <param name="ast">the ast to infer the type from</param>
        /// <param name="context">The current type inference context</param>
        /// <param name="evalPersmissions">The runtime usage permissions allowed during type inference</param>
        /// <returns></returns>
        internal static IList<PSTypeName> InferTypeOf(Ast ast, TypeInferenceContext context, TypeInferenceRuntimePermissions evalPersmissions = TypeInferenceRuntimePermissions.None)
        {
            var originalRuntimePermissions = context.RuntimePermissions;
            try
            {
                context.RuntimePermissions = evalPersmissions;
                return context.InferType(ast, new TypeInferenceVisitor(context)).Distinct(new PSTypeNameComparer()).ToList();
            }
            finally
            {
                context.RuntimePermissions = originalRuntimePermissions;
            }
        }
    }

    class PSTypeNameComparer : IEqualityComparer<PSTypeName>
    {
        public bool Equals(PSTypeName x, PSTypeName y)
        {
            return x.Name.Equals(y.Name);
        }

        public int GetHashCode(PSTypeName obj)
        {
            return obj.Name.GetHashCode();
        }
    }

    internal class TypeInferenceContext
    {
        public static readonly PSTypeName[] EmptyPSTypeNameArray = Utils.EmptyArray<PSTypeName>();
        private readonly PowerShell _powerShell;

        public TypeInferenceContext() : this(PowerShell.Create(RunspaceMode.CurrentRunspace))
        {
        }

        /// <summary>
        /// Create a new Type Inference context.
        /// The powerShell instance passed need to have a non null Runspace
        /// </summary>
        /// <param name="powerShell"></param>
        public TypeInferenceContext(PowerShell powerShell)
        {
            Diagnostics.Assert(powerShell.Runspace != null, "Callers are required to ensure we have a runspace");
            _powerShell = powerShell;

            Helper = new PowerShellExecutionHelper(powerShell);
        }

        // used to infer types in script properties attached to an object,
        // to be able to determine the type of $this in the scripts properties
        public PSTypeName CurrentThisType { get; set; }

        public TypeDefinitionAst CurrentTypeDefinitionAst { get; set; }

        public TypeInferenceRuntimePermissions RuntimePermissions { get; set; }

        internal PowerShellExecutionHelper Helper { get; }

        internal ExecutionContext ExecutionContext => _powerShell.Runspace.ExecutionContext;

        public bool TryGetRepresentativeTypeNameFromExpressionSafeEval(ExpressionAst expression, out PSTypeName typeName)
        {
            typeName = null;
            if (RuntimePermissions != TypeInferenceRuntimePermissions.AllowSafeEval)
            {
                return false;
            }
            object value;
            return expression != null &&
                   SafeExprEvaluator.TrySafeEval(expression, ExecutionContext, out value) &&
                   TryGetRepresentativeTypeNameFromValue(value, out typeName);
        }

        internal IList<object> GetMembersByInferredType(PSTypeName typename, bool isStatic, Func<object, bool> filter)
        {
            List<object> results = new List<object>();

            Func<object, bool> filterToCall = filter;
            if (typename.Type != null)
            {
                AddMembersByInferredTypesClrType(typename, isStatic, filter, filterToCall, results);
            }
            else if (typename.TypeDefinitionAst != null)
            {
                AddMembersByInferredTypeDefinitionAst(typename, isStatic, filter, filterToCall, results);
            }
            else
            {
                // Look in the type table first.
                if (!isStatic)
                {
                    var consolidatedString = new ConsolidatedString(new[] { typename.Name });
                    results.AddRange(ExecutionContext.TypeTable.GetMembers<PSMemberInfo>(consolidatedString));
                }

                AddMembersByInferredTypeCimType(typename, results, filterToCall);
            }

            return results;
        }

        internal void AddMembersByInferredTypesClrType(PSTypeName typename, bool isStatic, Func<object, bool> filter, Func<object, bool> filterToCall, List<object> results)
        {
            if (CurrentTypeDefinitionAst == null || CurrentTypeDefinitionAst.Type != typename.Type)
            {
                if (filterToCall == null)
                {
                    filterToCall = o => !IsMemberHidden(o);
                }
                else
                {
                    filterToCall = o => !IsMemberHidden(o) && filter(o);
                }
            }
            IEnumerable<Type> elementTypes;
            if (typename.Type.IsArray)
            {
                elementTypes = new[] { typename.Type.GetElementType() };
            }
            else
            {
                elementTypes = typename.Type.GetInterfaces().Where(
                    t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            }
            foreach (var type in elementTypes.Prepend(typename.Type))
            {
                // Look in the type table first.
                if (!isStatic)
                {
                    var consolidatedString = DotNetAdapter.GetInternedTypeNameHierarchy(type);
                    results.AddRange(ExecutionContext.TypeTable.GetMembers<PSMemberInfo>(consolidatedString));
                }

                var members = isStatic
                    ? PSObject.dotNetStaticAdapter.BaseGetMembers<PSMemberInfo>(type)
                    : PSObject.dotNetInstanceAdapter.GetPropertiesAndMethods(type, false);
                results.AddRange(filterToCall != null ? members.Where(filterToCall) : members);
            }
        }

        internal void AddMembersByInferredTypeDefinitionAst(PSTypeName typename, bool isStatic,
            Func<object, bool> filter, Func<object, bool> filterToCall, List<object> results)
        {
            if (CurrentTypeDefinitionAst != typename.TypeDefinitionAst)
            {
                if (filterToCall == null)
                    filterToCall = o => !IsMemberHidden(o);
                else
                    filterToCall = o => !IsMemberHidden(o) && filter(o);
            }

            bool foundConstructor = false;
            foreach (var member in typename.TypeDefinitionAst.Members)
            {
                bool add;
                var propertyMember = member as PropertyMemberAst;
                if (propertyMember != null)
                {
                    add = propertyMember.IsStatic == isStatic;
                }
                else
                {
                    var functionMember = (FunctionMemberAst) member;
                    add = functionMember.IsStatic == isStatic;
                    foundConstructor |= functionMember.IsConstructor;
                }

                if (filterToCall != null && add)
                {
                    add = filterToCall(member);
                }

                if (add)
                {
                    results.Add(member);
                }
            }

            //iterate through bases/interfaces
            foreach (var baseType in typename.TypeDefinitionAst.BaseTypes)
            {
                var baseTypeName = baseType.TypeName as TypeName;
                if (baseTypeName == null) continue;
                var baseTypeDefinitionAst = baseTypeName._typeDefinitionAst;
                results.AddRange(GetMembersByInferredType(new PSTypeName(baseTypeDefinitionAst), isStatic, filterToCall));
            }

            // Add stuff from our base class System.Object.
            if (isStatic)
            {
                // Don't add base class constructors
                if (filter == null)
                {
                    filterToCall = o => !IsConstructor(o);
                }

                else
                {
                    filterToCall = o => !IsConstructor(o) && filter(o);
                }

                if (!foundConstructor)
                {
                    results.Add(
                        new CompilerGeneratedMemberFunctionAst(PositionUtilities.EmptyExtent, typename.TypeDefinitionAst,
                            SpecialMemberFunctionType.DefaultConstructor));
                }
            }
            else
            {
                // Reset the filter because the recursive call will add IsHidden back if necessary.
                filterToCall = filter;
            }
            results.AddRange(GetMembersByInferredType(new PSTypeName(typeof(object)), isStatic, filterToCall));
        }

        internal void AddMembersByInferredTypeCimType(PSTypeName typename, List<object> results, Func<object, bool> filterToCall)
        {
            string cimNamespace;
            string className;
            if (ParseCimCommandsTypeName(typename, out cimNamespace, out className))
            {
                var powerShellExecutionHelper = Helper;
                powerShellExecutionHelper
                    .AddCommandWithPreferenceSetting("CimCmdlets\\Get-CimClass")
                    .AddParameter("Namespace", cimNamespace)
                    .AddParameter("Class", className);
                var classes = powerShellExecutionHelper.ExecuteCurrentPowerShell(out _);
                foreach (var cimClass in classes.Select(PSObject.Base).OfType<CimClass>())
                {
                    if (filterToCall == null)
                    {
                        results.AddRange(cimClass.CimClassProperties);
                    }
                    else
                    {
                        foreach (var prop in cimClass.CimClassProperties)
                        {
                            if (filterToCall(prop))
                            {
                                results.Add(prop);
                            }
                        }
                    }
                }
            }
        }

        internal IEnumerable<PSTypeName> InferType(Ast ast, TypeInferenceVisitor visitor)
        {
            var res = ast.Accept(visitor);
            Diagnostics.Assert(res != null, "Fix visit methods to not return null");
            return (IEnumerable<PSTypeName>)res;
        }

        private static bool TryGetRepresentativeTypeNameFromValue(object value, out PSTypeName type)
        {
            type = null;
            if (value != null)
            {
                var list = value as IList;
                if (list != null && list.Count > 0)
                {
                    value = list[0];
                }
                value = PSObject.Base(value);
                if (value != null)
                {
                    type = new PSTypeName(value.GetType());
                    return true;
                }
            }
            return false;
        }

        internal static bool ParseCimCommandsTypeName(PSTypeName typename, out string cimNamespace, out string className)
        {
            cimNamespace = null;
            className = null;
            if (typename == null)
            {
                return false;
            }
            if (typename.Type != null)
            {
                return false;
            }

            var match = Regex.Match(typename.Name, "(?<NetTypeName>.*)#(?<CimNamespace>.*)[/\\\\](?<CimClassName>.*)");
            if (!match.Success)
            {
                return false;
            }

            if (!match.Groups["NetTypeName"].Value.EqualsOrdinalIgnoreCase(typeof(CimInstance).FullName))
            {
                return false;
            }

            cimNamespace = match.Groups["CimNamespace"].Value;
            className = match.Groups["CimClassName"].Value;
            return true;
        }

        private static bool IsMemberHidden(object member)
        {
            switch (member)
            {
                case PSMemberInfo psMemberInfo:
                    return psMemberInfo.IsHidden;
                case MemberInfo memberInfo:
                    return memberInfo.GetCustomAttributes(typeof(HiddenAttribute), false).Any();
                case PropertyMemberAst propertyMember:
                    return propertyMember.IsHidden;
                case FunctionMemberAst functionMember:
                    return functionMember.IsHidden;
            }

            return false;
        }

        private static bool IsConstructor(object member)
        {
            var psMethod = member as PSMethod;
            var methodCacheEntry = psMethod?.adapterData as DotNetAdapter.MethodCacheEntry;
            return methodCacheEntry != null && methodCacheEntry.methodInformationStructures[0].method.IsConstructor;
        }
    }

    internal class TypeInferenceVisitor : ICustomAstVisitor2
    {
        private readonly TypeInferenceContext _context;

        private static readonly PSTypeName StringPSTypeName = new PSTypeName(typeof(string));

        public TypeInferenceVisitor(TypeInferenceContext context)
        {
            _context = context;
        }

        private IEnumerable<PSTypeName> InferTypes(Ast ast)
        {
            return _context.InferType(ast, this);
        }

        object ICustomAstVisitor.VisitTypeExpression(TypeExpressionAst typeExpressionAst)
        {
            return new[] { new PSTypeName(typeExpressionAst.StaticType) };
        }

        object ICustomAstVisitor.VisitMemberExpression(MemberExpressionAst memberExpressionAst)
        {
            return InferTypesFrom(memberExpressionAst);
        }

        object ICustomAstVisitor.VisitInvokeMemberExpression(InvokeMemberExpressionAst invokeMemberExpressionAst)
        {
            return InferTypesFrom(invokeMemberExpressionAst);
        }

        object ICustomAstVisitor.VisitArrayExpression(ArrayExpressionAst arrayExpressionAst)
        {
            return new[] { new PSTypeName(typeof(object[])) };
        }

        object ICustomAstVisitor.VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst)
        {
            return new[] { new PSTypeName(typeof(object[])) };
        }

        object ICustomAstVisitor.VisitHashtable(HashtableAst hashtableAst)
        {
            return new[] { new PSTypeName(typeof(Hashtable)) };
        }

        object ICustomAstVisitor.VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
        {
            return new[] { new PSTypeName(typeof(ScriptBlock)) };
        }

        object ICustomAstVisitor.VisitParenExpression(ParenExpressionAst parenExpressionAst)
        {
            return parenExpressionAst.Pipeline.Accept(this);
        }

        object ICustomAstVisitor.VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst)
        {
            return new[] { StringPSTypeName };
        }

        object ICustomAstVisitor.VisitIndexExpression(IndexExpressionAst indexExpressionAst)
        {
            return InferTypeFrom(indexExpressionAst);
        }

        object ICustomAstVisitor.VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst)
        {
            return attributedExpressionAst.Child.Accept(this);
        }

        object ICustomAstVisitor.VisitBlockStatement(BlockStatementAst blockStatementAst)
        {
            return blockStatementAst.Body.Accept(this);
        }

        object ICustomAstVisitor.VisitUsingExpression(UsingExpressionAst usingExpressionAst)
        {
            return usingExpressionAst.SubExpression.Accept(this);
        }

        object ICustomAstVisitor.VisitVariableExpression(VariableExpressionAst ast)
        {
            return InferTypeFrom(ast);
        }

        object ICustomAstVisitor.VisitMergingRedirection(MergingRedirectionAst mergingRedirectionAst)
        {
            return TypeInferenceContext.EmptyPSTypeNameArray;
        }

        object ICustomAstVisitor.VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst)
        {
            return InferTypes(binaryExpressionAst.Left);
        }

        object ICustomAstVisitor.VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst)
        {
            var tokenKind = unaryExpressionAst.TokenKind;
            return (tokenKind == TokenKind.Not || tokenKind == TokenKind.Exclaim)
                ? BinaryExpressionAst.BoolTypeNameArray
                : unaryExpressionAst.Child.Accept(this);
        }

        object ICustomAstVisitor.VisitConvertExpression(ConvertExpressionAst convertExpressionAst)
        {
            var type = convertExpressionAst.Type.TypeName.GetReflectionType();
            var psTypeName = type != null ? new PSTypeName(type) : new PSTypeName(convertExpressionAst.Type.TypeName.FullName);
            return new[] { psTypeName };
        }

        object ICustomAstVisitor.VisitConstantExpression(ConstantExpressionAst constantExpressionAst)
        {
            var value = constantExpressionAst.Value;
            return value != null ? new[] { new PSTypeName(value.GetType()) } : TypeInferenceContext.EmptyPSTypeNameArray;
        }

        object ICustomAstVisitor.VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst)
        {
            return new[] { StringPSTypeName };
        }

        object ICustomAstVisitor.VisitSubExpression(SubExpressionAst subExpressionAst)
        {
            return subExpressionAst.SubExpression.Accept(this);
        }

        object ICustomAstVisitor.VisitErrorStatement(ErrorStatementAst errorStatementAst)
        {
            return errorStatementAst.Conditions.Concat(errorStatementAst.Bodies).Concat(errorStatementAst.NestedAst).SelectMany(InferTypes);
        }

        object ICustomAstVisitor.VisitErrorExpression(ErrorExpressionAst errorExpressionAst)
        {
            return errorExpressionAst.NestedAst.SelectMany(InferTypes);
        }

        object ICustomAstVisitor.VisitScriptBlock(ScriptBlockAst scriptBlockAst)
        {
            var res = new List<PSTypeName>(10);
            var beginBlock = scriptBlockAst.BeginBlock;
            var processBlock = scriptBlockAst.ProcessBlock;
            var endBlock = scriptBlockAst.EndBlock;
            // The following is used when we don't find OutputType, which is checked elsewhere.
            if (beginBlock != null)
            {
                res.AddRange(InferTypes(beginBlock));
            }
            if (processBlock != null)
            {
                res.AddRange(InferTypes(processBlock));
            }
            if (endBlock != null)
            {
                res.AddRange(InferTypes(endBlock));
            }
            return res;
        }

        object ICustomAstVisitor.VisitParamBlock(ParamBlockAst paramBlockAst)
        {
            return TypeInferenceContext.EmptyPSTypeNameArray;
        }

        object ICustomAstVisitor.VisitNamedBlock(NamedBlockAst namedBlockAst)
        {
            return namedBlockAst.Statements.SelectMany(InferTypes);
        }

        object ICustomAstVisitor.VisitTypeConstraint(TypeConstraintAst typeConstraintAst)
        {
            return TypeInferenceContext.EmptyPSTypeNameArray;
        }

        object ICustomAstVisitor.VisitAttribute(AttributeAst attributeAst)
        {
            return TypeInferenceContext.EmptyPSTypeNameArray;
        }

        object ICustomAstVisitor.VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst)
        {
            return TypeInferenceContext.EmptyPSTypeNameArray;
        }

        object ICustomAstVisitor.VisitParameter(ParameterAst parameterAst)
        {
            var res = new List<PSTypeName>();
            var attributes = parameterAst.Attributes;
            var typeConstraint = attributes.OfType<TypeConstraintAst>().FirstOrDefault();
            if (typeConstraint != null)
            {
                res.Add(new PSTypeName(typeConstraint.TypeName));
            }
            foreach (var attributeAst in attributes.OfType<AttributeAst>())
            {
                PSTypeNameAttribute attribute = null;
                try
                {
                    attribute = attributeAst.GetAttribute() as PSTypeNameAttribute;
                }
                catch (RuntimeException) { }
                if (attribute != null)
                {
                    res.Add(new PSTypeName(attribute.PSTypeName));
                }
            }
            return res;
        }

        object ICustomAstVisitor.VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst)
        {
            return TypeInferenceContext.EmptyPSTypeNameArray;
        }

        object ICustomAstVisitor.VisitStatementBlock(StatementBlockAst statementBlockAst)
        {
            return statementBlockAst.Statements.SelectMany(InferTypes);
        }

        object ICustomAstVisitor.VisitIfStatement(IfStatementAst ifStmtAst)
        {
            var res = new List<PSTypeName>();

            res.AddRange(ifStmtAst.Clauses.SelectMany(clause => InferTypes(clause.Item2)));

            var elseClause = ifStmtAst.ElseClause;
            if (elseClause != null)
            {
                res.AddRange(InferTypes(elseClause));
            }
            return res;
        }

        object ICustomAstVisitor.VisitTrap(TrapStatementAst trapStatementAst)
        {
            return trapStatementAst.Body.Accept(this);
        }

        object ICustomAstVisitor.VisitSwitchStatement(SwitchStatementAst switchStatementAst)
        {
            var res = new List<PSTypeName>(8);
            var clauses = switchStatementAst.Clauses;
            var defaultStatement = switchStatementAst.Default;

            res.AddRange(clauses.SelectMany(clause => InferTypes(clause.Item2)));

            if (defaultStatement != null)
            {
                res.AddRange(InferTypes(defaultStatement));
            }
            return res;
        }

        object ICustomAstVisitor.VisitDataStatement(DataStatementAst dataStatementAst)
        {
            return dataStatementAst.Body.Accept(this);
        }

        object ICustomAstVisitor.VisitForEachStatement(ForEachStatementAst forEachStatementAst)
        {
            return forEachStatementAst.Body.Accept(this);
        }

        object ICustomAstVisitor.VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst)
        {
            return doWhileStatementAst.Body.Accept(this);
        }

        object ICustomAstVisitor.VisitForStatement(ForStatementAst forStatementAst)
        {
            return forStatementAst.Body.Accept(this);
        }

        object ICustomAstVisitor.VisitWhileStatement(WhileStatementAst whileStatementAst)
        {
            return whileStatementAst.Body.Accept(this);
        }

        object ICustomAstVisitor.VisitCatchClause(CatchClauseAst catchClauseAst)
        {
            return catchClauseAst.Body.Accept(this);
        }

        object ICustomAstVisitor.VisitTryStatement(TryStatementAst tryStatementAst)
        {
            var res = new List<PSTypeName>(5);
            res.AddRange(InferTypes(tryStatementAst.Body));
            foreach (var catchClauseAst in tryStatementAst.CatchClauses)
            {
                res.AddRange(InferTypes(catchClauseAst));
            }
            if (tryStatementAst.Finally != null)
            {
                res.AddRange(InferTypes(tryStatementAst.Finally));
            }
            return res;
        }

        object ICustomAstVisitor.VisitBreakStatement(BreakStatementAst breakStatementAst)
        {
            return TypeInferenceContext.EmptyPSTypeNameArray;
        }

        object ICustomAstVisitor.VisitContinueStatement(ContinueStatementAst continueStatementAst)
        {
            return TypeInferenceContext.EmptyPSTypeNameArray;
        }

        object ICustomAstVisitor.VisitReturnStatement(ReturnStatementAst returnStatementAst)
        {
            return returnStatementAst.Pipeline.Accept(this);
        }

        object ICustomAstVisitor.VisitExitStatement(ExitStatementAst exitStatementAst)
        {
            return TypeInferenceContext.EmptyPSTypeNameArray;
        }

        object ICustomAstVisitor.VisitThrowStatement(ThrowStatementAst throwStatementAst)
        {
            return TypeInferenceContext.EmptyPSTypeNameArray;
        }

        object ICustomAstVisitor.VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst)
        {
            return doUntilStatementAst.Body.Accept(this);
        }

        object ICustomAstVisitor.VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst)
        {
            return assignmentStatementAst.Left.Accept(this);
        }

        object ICustomAstVisitor.VisitPipeline(PipelineAst pipelineAst)
        {
            return pipelineAst.PipelineElements.Last().Accept(this);
        }

        object ICustomAstVisitor.VisitCommand(CommandAst commandAst)
        {
            return InferTypesFrom(commandAst);
        }

        object ICustomAstVisitor.VisitCommandExpression(CommandExpressionAst commandExpressionAst)
        {
            return commandExpressionAst.Expression.Accept(this);
        }

        object ICustomAstVisitor.VisitCommandParameter(CommandParameterAst commandParameterAst)
        {
            return TypeInferenceContext.EmptyPSTypeNameArray;
        }

        object ICustomAstVisitor.VisitFileRedirection(FileRedirectionAst fileRedirectionAst)
        {
            return TypeInferenceContext.EmptyPSTypeNameArray;
        }

        private IEnumerable<PSTypeName> InferTypesFrom(CommandAst commandAst)
        {
            PseudoBindingInfo pseudoBinding = new PseudoParameterBinder()
                                    .DoPseudoParameterBinding(commandAst, null, null, PseudoParameterBinder.BindingType.ParameterCompletion);
            if (pseudoBinding?.CommandInfo == null)
            {
                yield break;
            }

            AstParameterArgumentPair pathArgument;
            string pathParameterName = "Path";
            if (!pseudoBinding.BoundArguments.TryGetValue(pathParameterName, out pathArgument))
            {
                pathParameterName = "LiteralPath";
                pseudoBinding.BoundArguments.TryGetValue(pathParameterName, out pathArgument);
            }

            // The OutputType on cmdlets like Get-ChildItem may depend on the path.
            // The CmdletInfo returned based on just the command name will specify returning all possibilities, e.g.certificates, environment, registry, etc.
            // If you specified - Path, the list of OutputType can be refined, but we have to make a copy of the CmdletInfo object this way to get that refinement.
            var commandInfo = pseudoBinding.CommandInfo;
            var pathArgumentPair = pathArgument as AstPair;
            if (pathArgumentPair?.Argument is StringConstantExpressionAst)
            {
                var pathValue = ((StringConstantExpressionAst)pathArgumentPair.Argument).Value;
                try
                {
                    commandInfo = commandInfo.CreateGetCommandCopy(new object[] { "-" + pathParameterName, pathValue });
                }
                catch (InvalidOperationException) { }
            }

            var cmdletInfo = commandInfo as CmdletInfo;
            if (cmdletInfo != null)
            {
                // Special cases
                var inferTypesFromObjectCmdlets = InferTypesFromObjectCmdlets(commandAst, cmdletInfo, pseudoBinding).ToArray();
                if (inferTypesFromObjectCmdlets.Length > 0)
                {
                    foreach (var objectCmdletTypes in inferTypesFromObjectCmdlets)
                    {
                        yield return objectCmdletTypes;
                    }
                    yield break;
                }
            }

            // The OutputType property ignores the parameter set specified in the OutputTypeAttribute.
            // With psuedo-binding, we actually know the candidate parameter sets, so we could take
            // advantage of it here, but I opted for the simpler code because so few cmdlets use
            // ParameterSetName in OutputType and of the ones I know about, it isn't that useful.
            foreach (var result in commandInfo.OutputType)
            {
                yield return result;
            }
        }

        /// <summary>
        /// Infer types from the well-known object cmdlets, like foreach-object, where-object, sort-object etc
        /// </summary>
        /// <param name="commandAst"></param>
        /// <param name="cmdletInfo"></param>
        /// <param name="pseudoBinding"></param>
        /// <returns></returns>
        private IEnumerable<PSTypeName> InferTypesFromObjectCmdlets(CommandAst commandAst, CmdletInfo cmdletInfo, PseudoBindingInfo pseudoBinding)
        {
            // new-object - yields an instance of whatever -Type is bound to
            if (cmdletInfo.ImplementingType.FullName.EqualsOrdinalIgnoreCase("Microsoft.PowerShell.Commands.NewObjectCommand"))
            {
                var newObjectType = InferTypesFromNewObjectCommand(pseudoBinding);
                if (newObjectType != null)
                {
                    yield return newObjectType;
                }

                yield break; // yield break;
            }

            // Get-CimInstance/New-CimInstance - yields a CimInstance with ETS type based on its arguments for -Namespace and -ClassName parameters
            if (
                cmdletInfo.ImplementingType.FullName.EqualsOrdinalIgnoreCase("Microsoft.Management.Infrastructure.CimCmdlets.GetCimInstanceCommand") ||
                cmdletInfo.ImplementingType.FullName.EqualsOrdinalIgnoreCase("Microsoft.Management.Infrastructure.CimCmdlets.NewCimInstanceCommand"))
            {
                foreach (var cimType in InferTypesFromCimCommand(pseudoBinding))
                {
                    yield return cimType;
                }
                yield break; // yield break;
            }

            // where-object - yields whatever we saw before where-object in the pipeline.
            // same for sort-object
            if (cmdletInfo.ImplementingType == typeof(WhereObjectCommand)
                ||
                cmdletInfo.ImplementingType.FullName.EqualsOrdinalIgnoreCase("Microsoft.PowerShell.Commands.SortObjectCommand"))
            {
                foreach (var whereOrSortType in InferTypesFromWhereAndSortCommand(commandAst))
                {
                    yield return whereOrSortType;
                }

                // We could also check -InputObject, but that is rarely used.  But don't bother continuing.
                yield break; // yield break;
            }

            // foreach-object - yields the type of it's script block parameters
            if (cmdletInfo.ImplementingType == typeof(ForEachObjectCommand))
            {
                foreach (var foreachType in InferTypesFromForeachCommand(pseudoBinding, commandAst))
                {
                    yield return foreachType;
                }
            }
        }

        private static IEnumerable<PSTypeName> InferTypesFromCimCommand(PseudoBindingInfo pseudoBinding)
        {
            string pseudoboundNamespace =
                CompletionCompleters.NativeCommandArgumentCompletion_ExtractSecondaryArgument(pseudoBinding.BoundArguments,
                    "Namespace").FirstOrDefault();
            string pseudoboundClassName =
                CompletionCompleters.NativeCommandArgumentCompletion_ExtractSecondaryArgument(pseudoBinding.BoundArguments,
                    "ClassName").FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(pseudoboundClassName))
            {
                yield return new PSTypeName(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}#{1}/{2}",
                    typeof(CimInstance).FullName,
                    pseudoboundNamespace ?? "root/cimv2",
                    pseudoboundClassName));
            }
            yield return new PSTypeName(typeof(CimInstance));
        }

        private IEnumerable<PSTypeName> InferTypesFromForeachCommand(PseudoBindingInfo pseudoBinding, CommandAst commandAst)
        {
            AstParameterArgumentPair argument;
            if (pseudoBinding.BoundArguments.TryGetValue("MemberName", out argument))
            {
                var previousPipelineElement = GetPreviousPipelineCommand(commandAst);
                if (previousPipelineElement == null)
                {
                    yield break;
                }
                foreach (var t in InferTypes(previousPipelineElement))
                {
                    var memberName = (((AstPair)argument).Argument as StringConstantExpressionAst)?.Value;

                    if (memberName != null)
                    {
                        var members = _context.GetMembersByInferredType(t, false, null);
                        bool maybeWantDefaultCtor = false;
                        foreach (var type in GetTypesOfMembers(t, memberName, members, ref maybeWantDefaultCtor, isInvokeMemberExpressionAst: false))
                        {
                            yield return type;
                        }
                    }
                }
            }

            if (pseudoBinding.BoundArguments.TryGetValue("Begin", out argument))
            {
                foreach (var type in GetInferredTypeFromScriptBlockParameter(argument))
                {
                    yield return type;
                }
            }

            if (pseudoBinding.BoundArguments.TryGetValue("Process", out argument))
            {
                foreach (var type in GetInferredTypeFromScriptBlockParameter(argument))
                {
                    yield return type;
                }
            }

            if (pseudoBinding.BoundArguments.TryGetValue("End", out argument))
            {
                foreach (var type in GetInferredTypeFromScriptBlockParameter(argument))
                {
                    yield return type;
                }
            }
        }

        private IEnumerable<PSTypeName> InferTypesFromWhereAndSortCommand(CommandAst commandAst)
        {
            var parentPipeline = commandAst.Parent as PipelineAst;
            if (parentPipeline != null)
            {
                int i;
                for (i = 0; i < parentPipeline.PipelineElements.Count; i++)
                {
                    if (parentPipeline.PipelineElements[i] == commandAst)
                        break;
                }
                if (i > 0)
                {
                    foreach (var typename in InferTypes(parentPipeline.PipelineElements[i - 1]))
                    {
                        yield return typename;
                    }
                }
            }
        }

        private static PSTypeName InferTypesFromNewObjectCommand(PseudoBindingInfo pseudoBinding)
        {
            AstParameterArgumentPair typeArgument;
            if (pseudoBinding.BoundArguments.TryGetValue("TypeName", out typeArgument))
            {
                var typeArgumentPair = typeArgument as AstPair;
                var stringConstantExpr = typeArgumentPair?.Argument as StringConstantExpressionAst;
                if (stringConstantExpr != null)
                {
                     return new PSTypeName(stringConstantExpr.Value);
                }
            }
            return null;
        }

        private IEnumerable<PSTypeName> InferTypesFrom(MemberExpressionAst memberExpressionAst)
        {
            var memberCommandElement = memberExpressionAst.Member;
            var isStatic = memberExpressionAst.Static;
            var expression = memberExpressionAst.Expression;

            // If the member name isn't simple, don't even try.
            var memberAsStringConst = memberCommandElement as StringConstantExpressionAst;
            if (memberAsStringConst == null)
                return Utils.EmptyArray<PSTypeName>();

            var exprType = GetExpressionType(expression, isStatic);
            if (exprType == null || exprType.Length == 0)
            {
                return Utils.EmptyArray<PSTypeName>();
            }

            var res = new List<PSTypeName>(10);
            bool isInvokeMemberExpressionAst = memberExpressionAst is InvokeMemberExpressionAst;
            var maybeWantDefaultCtor = isStatic
                && isInvokeMemberExpressionAst
                && memberAsStringConst.Value.EqualsOrdinalIgnoreCase("new");

            // We use a list of member names because we might discover aliases properties
            // and if we do, we'll add to the list.
            var memberNameList = new List<string> { memberAsStringConst.Value };
            foreach (var type in exprType)
            {
                if (type.Type == typeof(PSObject))
                {
                    continue;
                }
                var members = _context.GetMembersByInferredType(type, isStatic, filter: null);

                AddTypesOfMembers(type, memberNameList, members, ref maybeWantDefaultCtor, isInvokeMemberExpressionAst, res);

                // We didn't find any constructors but they used [T]::new() syntax
                if (maybeWantDefaultCtor)
                {
                    res.Add(type);
                }
            }
            return res;
        }

        private List<PSTypeName> GetTypesOfMembers(PSTypeName thisType, string memberName, IList<object> members, ref bool maybeWantDefaultCtor, bool isInvokeMemberExpressionAst)
        {
            var memberNamesToCheck = new List<string> { memberName };
            var res = new List<PSTypeName>(10);

            AddTypesOfMembers(thisType, memberNamesToCheck, members, ref maybeWantDefaultCtor, isInvokeMemberExpressionAst, res);
            return res;
        }

        private void AddTypesOfMembers(PSTypeName currentType, List<string> memberNamesToCheck, IList<object> members, ref bool maybeWantDefaultCtor, bool isInvokeMemberExpressionAst, List<PSTypeName> result)
        {
            for (int i = 0; i < memberNamesToCheck.Count; i++)
            {
                string memberNameToCheck = memberNamesToCheck[i];
                foreach (var member in members)
                {
                    if (TryGetTypeFromMember(currentType, member, memberNameToCheck, ref maybeWantDefaultCtor, isInvokeMemberExpressionAst, result, memberNamesToCheck))
                    {
                        break;
                    }
                }
            }
        }

        private bool TryGetTypeFromMember(PSTypeName currentType, object member, string memberName, ref bool maybeWantDefaultCtor, bool isInvokeMemberExpressionAst, List<PSTypeName> result, List<string> memberNamesToCheck)
        {
            switch (member)
            {
                case PropertyInfo propertyInfo: // .net property
                {
                    if (propertyInfo.Name.EqualsOrdinalIgnoreCase(memberName) && !isInvokeMemberExpressionAst)
                    {
                        result.Add(new PSTypeName(propertyInfo.PropertyType));
                        return true;
                    }
                    return false;
                }
                case FieldInfo fieldInfo:       // .net field
                {
                    if (fieldInfo.Name.EqualsOrdinalIgnoreCase(memberName) && !isInvokeMemberExpressionAst)
                    {
                        result.Add(new PSTypeName(fieldInfo.FieldType));
                        return true;
                    }
                    return false;
                }
                case DotNetAdapter.MethodCacheEntry methodCacheEntry: // .net method
                {
                    if (methodCacheEntry[0].method.Name.Equals(memberName, StringComparison.OrdinalIgnoreCase))
                    {
                        maybeWantDefaultCtor = false;
                        if (isInvokeMemberExpressionAst)
                        {
                            var res = (from method in methodCacheEntry.methodInformationStructures
                                select method.method as MethodInfo into methodInfo
                                where methodInfo != null && !methodInfo.ReturnType.ContainsGenericParameters
                                select new PSTypeName(methodInfo.ReturnType));
                            result.AddRange(res);
                            return true;
                        }
                        else
                        {
                            // Accessing a method as a property, we'd return a wrapper over the method.
                            result.Add(new PSTypeName(typeof(PSMethod)));
                            return true;
                        }
                    }
                    return false;
                }
                case MemberAst memberAst: // this is for members defined by PowerShell classes
                {
                    if (memberAst.Name.Equals(memberName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (isInvokeMemberExpressionAst)
                        {
                            if (memberAst is FunctionMemberAst functionMemberAst && !functionMemberAst.IsReturnTypeVoid())
                            {
                                result.Add(new PSTypeName(functionMemberAst.ReturnType.TypeName));
                                return true;
                            }
                        }
                        else
                        {
                            if (memberAst is PropertyMemberAst propertyMemberAst)
                            {
                                result.Add(propertyMemberAst.PropertyType != null
                                    ? new PSTypeName(propertyMemberAst.PropertyType.TypeName)
                                    : new PSTypeName(typeof(object)));
                                return true;
                            }
                            else
                            {
                                // Accessing a method as a property, we'd return a wrapper over the method.
                                result.Add(new PSTypeName(typeof(PSMethod)));
                                return true;
                            }
                        }
                    }
                    return false;
                }
                case PSMemberInfo memberInfo:
                {
                    if (!memberInfo.Name.Equals(memberName, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    ScriptBlock scriptBlock = null;
                    switch (memberInfo)
                    {
                        case PSProperty p:
                        {
                            result.Add(new PSTypeName(p.Value.GetType()));
                            return true;
                        }
                        case PSNoteProperty noteProperty:
                        {
                            result.Add(new PSTypeName(noteProperty.Value.GetType()));
                            return true;
                        }
                        case PSAliasProperty aliasProperty:
                        {
                            memberNamesToCheck.Add(aliasProperty.ReferencedMemberName);
                            return true;
                        }
                        case PSCodeProperty codeProperty:
                        {
                            if (codeProperty.GetterCodeReference != null)
                            {
                                result.Add(new PSTypeName(codeProperty.GetterCodeReference.ReturnType));
                            }
                            return true;
                        }
                        case PSScriptProperty scriptProperty:
                        {
                            scriptBlock = scriptProperty.GetterScript;
                            break;
                        }
                        case PSScriptMethod scriptMethod:
                        {
                            scriptBlock = scriptMethod.Script;
                            break;
                        }
                    }
                    if (scriptBlock != null)
                    {
                        var thisToRestore = _context.CurrentThisType;
                        try
                        {
                            _context.CurrentThisType = currentType;
                            var outputType = scriptBlock.OutputType;
                            if (outputType != null && outputType.Count != 0)
                            {
                                result.AddRange(outputType);
                                return true;
                            }
                            else
                            {
                                result.AddRange(InferTypes(scriptBlock.Ast).ToArray());
                                return true;
                            }
                        }
                        finally
                        {
                            _context.CurrentThisType = thisToRestore;
                        }
                    }
                }
                return false;
            }
            return false;
        }

        private PSTypeName[] GetExpressionType(ExpressionAst expression, bool isStatic)
        {
            PSTypeName[] exprType;
            if (isStatic)
            {
                var exprAsType = expression as TypeExpressionAst;
                if (exprAsType == null)
                    return null;
                var type = exprAsType.TypeName.GetReflectionType();
                if (type == null)
                {
                    var typeName = exprAsType.TypeName as TypeName;
                    if (typeName?._typeDefinitionAst == null)
                        return null;

                    exprType = new[] {new PSTypeName(typeName._typeDefinitionAst)};
                }
                else
                {
                    exprType = new[] {new PSTypeName(type)};
                }
            }
            else
            {
                exprType = InferTypes(expression).ToArray();
                if (exprType.Length == 0)
                {
                    if (_context.TryGetRepresentativeTypeNameFromExpressionSafeEval(expression, out PSTypeName _))
                    {
                        return exprType;
                    }
                    return exprType;
                }
            }
            return exprType;
        }

        private IEnumerable<PSTypeName> InferTypeFrom(VariableExpressionAst variableExpressionAst)
        {
            // We don't need to handle drive qualified variables, we can usually get those values
            // without needing to "guess" at the type.
            var astVariablePath = variableExpressionAst.VariablePath;
            if (!astVariablePath.IsVariable)
            {
                // Not a variable - the caller should have already tried going to session state
                // to get the item and hence it's type, but that must have failed.  Don't try again.
                yield break;
            }

            Ast parent = variableExpressionAst.Parent;
            if (astVariablePath.IsUnqualified &&
                (SpecialVariables.IsUnderbar(astVariablePath.UserPath)
                 || astVariablePath.UserPath.EqualsOrdinalIgnoreCase(SpecialVariables.PSItem)))
            {
                // $_ is special, see if we're used in a script block in some pipeline.
                while (parent != null)
                {
                    if (parent is ScriptBlockExpressionAst)
                        break;
                    parent = parent.Parent;
                }

                if (parent != null)
                {
                    if (parent.Parent is CommandExpressionAst && parent.Parent.Parent is PipelineAst)
                    {
                        // Script block in a hash table, could be something like:
                        //     dir | ft @{ Expression = { $_ } }
                        if (parent.Parent.Parent.Parent is HashtableAst)
                        {
                            parent = parent.Parent.Parent.Parent;
                        }
                        else if (parent.Parent.Parent.Parent is ArrayLiteralAst && parent.Parent.Parent.Parent.Parent is HashtableAst)
                        {
                            parent = parent.Parent.Parent.Parent.Parent;
                        }
                    }
                    if (parent.Parent is CommandParameterAst)
                    {
                        parent = parent.Parent;
                    }

                    var commandAst = parent.Parent as CommandAst;
                    if (commandAst != null)
                    {
                        // We found a command, see if there is a previous command in the pipeline.
                        PipelineAst pipelineAst = (PipelineAst)commandAst.Parent;
                        var previousCommandIndex = pipelineAst.PipelineElements.IndexOf(commandAst) - 1;
                        if (previousCommandIndex < 0) yield break;
                        foreach (var result in InferTypes(pipelineAst.PipelineElements[0]))
                        {
                            if (result.Type != null)
                            {
                                // Assume (because we're looking at $_ and we're inside a script block that is an
                                // argument to some command) that the type we're getting is actually unrolled.
                                // This might not be right in all cases, but with our simple analysis, it's
                                // right more often than it's wrong.
                                if (result.Type.IsArray)
                                {
                                    yield return new PSTypeName(result.Type.GetElementType());
                                    continue;
                                }

                                if (typeof(IEnumerable).IsAssignableFrom(result.Type))
                                {
                                    // We can't deduce much from IEnumerable, but we can if it's generic.
                                    var enumerableInterfaces = result.Type.GetInterfaces().Where(
                                        t =>
                                            t.IsGenericType &&
                                            t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                                    foreach (var i in enumerableInterfaces)
                                    {
                                        yield return new PSTypeName(i.GetGenericArguments()[0]);
                                    }
                                    continue;
                                }
                            }
                            yield return result;
                        }
                        yield break;
                    }
                }
            }

            // For certain variables, we always know their type, well at least we can assume we know.
            if (astVariablePath.IsUnqualified)
            {
                var isThis = astVariablePath.UserPath.EqualsOrdinalIgnoreCase(SpecialVariables.This);
                if (!isThis || (_context.CurrentTypeDefinitionAst == null && _context.CurrentThisType == null))
                {
                    for (int i = 0; i < SpecialVariables.AutomaticVariables.Length; i++)
                    {
                        if (!astVariablePath.UserPath.EqualsOrdinalIgnoreCase(SpecialVariables.AutomaticVariables[i]))
                            continue;
                        var type = SpecialVariables.AutomaticVariableTypes[i];
                        if (type != typeof(object))
                            yield return new PSTypeName(type);
                        break;
                    }
                }
                else
                {
                    yield return _context.CurrentThisType != null
                        ? _context.CurrentThisType
                        : new PSTypeName(_context.CurrentTypeDefinitionAst);
                    yield break;
                }
            }
                else
                {
                    yield return new PSTypeName(_context.CurrentTypeDefinitionAst);
                    yield break;
                }

            // Look for our variable as a parameter or on the lhs of an assignment - hopefully we'll find either
            // a type constraint or at least we can use the rhs to infer the type.

            while (parent.Parent != null)
            {
                parent = parent.Parent;
            }

            if (parent.Parent is FunctionDefinitionAst)
            {
                parent = parent.Parent;
            }

            int startOffset = variableExpressionAst.Extent.StartOffset;
            var targetAsts = (List<Ast>)AstSearcher.FindAll(parent,
                ast => (ast is ParameterAst || ast is AssignmentStatementAst || ast is ForEachStatementAst || ast is CommandAst)
                       && variableExpressionAst.AstAssignsToSameVariable(ast)
                       && ast.Extent.EndOffset < startOffset,
                searchNestedScriptBlocks: true);

            var parameterAst = targetAsts.OfType<ParameterAst>().FirstOrDefault();
            if (parameterAst != null)
            {
                var parameterTypes = InferTypes(parameterAst).ToArray();
                if (parameterTypes.Length > 0)
                {
                    foreach (var parameterType in parameterTypes)
                    {
                        yield return parameterType;
                    }
                    yield break;
                }
            }

            var assignAsts = targetAsts.OfType<AssignmentStatementAst>().ToArray();

            // If any of the assignments lhs use a type constraint, then we use that.
            // Otherwise, we use the rhs of the "nearest" assignment
            foreach (var assignAst in assignAsts)
            {
                var lhsConvert = assignAst.Left as ConvertExpressionAst;
                if (lhsConvert != null)
                {
                    yield return new PSTypeName(lhsConvert.Type.TypeName);
                    yield break;
                }
            }

            var foreachAst = targetAsts.OfType<ForEachStatementAst>().FirstOrDefault();
            if (foreachAst != null)
            {
                foreach (var typeName in InferTypes(foreachAst.Condition))
                {
                    yield return typeName;
                }
                yield break;
            }

            var commandCompletionAst = targetAsts.OfType<CommandAst>().FirstOrDefault();
            if (commandCompletionAst != null)
            {
                foreach (var typeName in InferTypes(commandCompletionAst))
                {
                    yield return typeName;
                }
                yield break;
            }

            int smallestDiff = int.MaxValue;
            AssignmentStatementAst closestAssignment = null;
            foreach (var assignAst in assignAsts)
            {
                var endOffset = assignAst.Extent.EndOffset;
                if ((startOffset - endOffset) < smallestDiff)
                {
                    smallestDiff = startOffset - endOffset;
                    closestAssignment = assignAst;
                }
            }

            if (closestAssignment != null)
            {
                foreach (var type in InferTypes(closestAssignment.Right))
                {
                    yield return type;
                }
            }

            PSTypeName evalTypeName;
            if (_context.TryGetRepresentativeTypeNameFromExpressionSafeEval(variableExpressionAst, out evalTypeName))
            {
                yield return evalTypeName;
            }

        }

        private IEnumerable<PSTypeName> InferTypeFrom(IndexExpressionAst indexExpressionAst)
        {
            var targetTypes = InferTypes(indexExpressionAst.Target);
            bool foundAny = false;
            foreach (var psType in targetTypes)
            {
                var type = psType.Type;
                if (type != null)
                {
                    if (type.IsArray)
                    {
                        yield return new PSTypeName(type.GetElementType());
                        continue;
                    }

                    foreach (var iface in type.GetInterfaces())
                    {
                        var isGenericType = iface.IsGenericType;
                        if (isGenericType && iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                        {
                            var valueType = iface.GetGenericArguments()[1];
                            if (!valueType.ContainsGenericParameters)
                            {
                                foundAny = true;
                                yield return new PSTypeName(valueType);
                            }
                        }
                        else if (isGenericType && iface.GetGenericTypeDefinition() == typeof(IList<>))
                        {
                            var valueType = iface.GetGenericArguments()[0];
                            if (!valueType.ContainsGenericParameters)
                            {
                                foundAny = true;
                                yield return new PSTypeName(valueType);
                            }
                        }
                    }

                    var defaultMember = type.GetCustomAttributes<DefaultMemberAttribute>(true).FirstOrDefault();
                    if (defaultMember != null)
                    {
                        var indexers = type.GetGetterProperty(defaultMember.MemberName);
                        foreach (var indexer in indexers)
                        {
                            foundAny = true;
                            yield return new PSTypeName(indexer.ReturnType);
                        }
                    }
                }

                if (!foundAny)
                {
                    // Inferred type of target wasn't indexable.  Assume (perhaps incorrectly)
                    // that it came from OutputType and that more than one object was returned
                    // and that we're indexing because of that, in which case, OutputType really
                    // is the inferred type.
                    yield return psType;
                }
            }
        }

        private IEnumerable<PSTypeName> GetInferredTypeFromScriptBlockParameter(AstParameterArgumentPair argument)
        {
            var argumentPair = argument as AstPair;
            var scriptBlockExpressionAst = argumentPair?.Argument as ScriptBlockExpressionAst;
            if (scriptBlockExpressionAst == null) yield break;
            foreach (var type in InferTypes(scriptBlockExpressionAst.ScriptBlock))
            {
                yield return type;
            }
        }

        object ICustomAstVisitor2.VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst)
        {
            return TypeInferenceContext.EmptyPSTypeNameArray;
        }

        object ICustomAstVisitor2.VisitPropertyMember(PropertyMemberAst propertyMemberAst)
        {
            return TypeInferenceContext.EmptyPSTypeNameArray;
        }

        object ICustomAstVisitor2.VisitFunctionMember(FunctionMemberAst functionMemberAst)
        {
            return TypeInferenceContext.EmptyPSTypeNameArray;
        }

        object ICustomAstVisitor2.VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst)
        {
            return ((ICustomAstVisitor)this).VisitInvokeMemberExpression(baseCtorInvokeMemberExpressionAst);
        }

        object ICustomAstVisitor2.VisitUsingStatement(UsingStatementAst usingStatement)
        {
            return TypeInferenceContext.EmptyPSTypeNameArray;
        }

        object ICustomAstVisitor2.VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst)
        {
            return configurationDefinitionAst.Body.Accept(this);
        }

        object ICustomAstVisitor2.VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordAst)
        {
            // TODO: What is the right InferredType for the AST
            return dynamicKeywordAst.CommandElements[0].Accept(this);
        }

        private static CommandBaseAst GetPreviousPipelineCommand(CommandAst commandAst)
        {
            var pipe = (PipelineAst)commandAst.Parent;
            var i = pipe.PipelineElements.IndexOf(commandAst);
            return i != 0 ? pipe.PipelineElements[i - 1] : null;
        }
    }

    static class TypeInferenceExtension
    {
        public static bool EqualsOrdinalIgnoreCase(this string s, string t)
        {
            return string.Equals(s, t, StringComparison.OrdinalIgnoreCase);
        }

        public static IEnumerable<MethodInfo> GetGetterProperty(this Type type, string propertyName)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(
                m =>
                {
                    var name = m.Name;
                    // Equals without string allocation
                    return name.Length == propertyName.Length + 4 &&
                           name.StartsWith("get_") && propertyName.IndexOf(name, 4, StringComparison.Ordinal) == 4;
                }
            );
        }

        public static bool AstAssignsToSameVariable(this VariableExpressionAst variableAst, Ast ast)
        {
            var parameterAst = ast as ParameterAst;
            var variableAstVariablePath = variableAst.VariablePath;
            if (parameterAst != null)
            {
                return variableAstVariablePath.IsUnscopedVariable &&
                       parameterAst.Name.VariablePath.UnqualifiedPath.Equals(variableAstVariablePath.UnqualifiedPath, StringComparison.OrdinalIgnoreCase);
            }

            var foreachAst = ast as ForEachStatementAst;
            if (foreachAst != null)
            {
                return variableAstVariablePath.IsUnscopedVariable &&
                       foreachAst.Variable.VariablePath.UnqualifiedPath.Equals(variableAstVariablePath.UnqualifiedPath, StringComparison.OrdinalIgnoreCase);
            }

            var commandAst = ast as CommandAst;
            if (commandAst != null)
            {
                string[] variableParameters = new string[] { "PV", "PipelineVariable", "OV", "OutVariable" };
                StaticBindingResult bindingResult = StaticParameterBinder.BindCommand(commandAst, false, variableParameters);

                if (bindingResult != null)
                {
                    foreach (string commandVariableParameter in variableParameters)
                    {
                        if (bindingResult.BoundParameters.TryGetValue(commandVariableParameter, out ParameterBindingResult parameterBindingResult))
                        {
                            if (String.Equals(variableAstVariablePath.UnqualifiedPath, (string)parameterBindingResult.ConstantValue, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            var assignmentAst = (AssignmentStatementAst)ast;
            var lhs = assignmentAst.Left;
            var convertExpr = lhs as ConvertExpressionAst;
            if (convertExpr != null)
            {
                lhs = convertExpr.Child;
            }

            var varExpr = lhs as VariableExpressionAst;
            if (varExpr == null)
                return false;

            var candidateVarPath = varExpr.VariablePath;
            if (candidateVarPath.UserPath.Equals(variableAstVariablePath.UserPath, StringComparison.OrdinalIgnoreCase))
                return true;

            // The following condition is making an assumption that at script scope, we didn't use $script:, but in the local scope, we did
            // If we are searching anything other than script scope, this is wrong.
            if (variableAstVariablePath.IsScript && variableAstVariablePath.UnqualifiedPath.Equals(candidateVarPath.UnqualifiedPath, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}
