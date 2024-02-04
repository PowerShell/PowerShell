// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.PowerShell.Commands;

using CimClass = Microsoft.Management.Infrastructure.CimClass;
using CimInstance = Microsoft.Management.Infrastructure.CimInstance;

namespace System.Management.Automation
{
    /// <summary>
    /// Enum describing permissions to use runtime evaluation during type inference.
    /// </summary>
    public enum TypeInferenceRuntimePermissions
    {
        /// <summary>
        /// No runtime use is allowed.
        /// </summary>
        None = 0,

        /// <summary>
        /// Use of SafeExprEvaluator visitor is allowed.
        /// </summary>
        AllowSafeEval = 1,
    }

    /// <summary>
    /// Static class containing methods to work with type inference of abstract syntax trees.
    /// </summary>
    internal static class AstTypeInference
    {
        /// <summary>
        /// Infers the type that the result of executing a statement would have without using runtime safe eval.
        /// </summary>
        /// <param name="ast">The ast to infer the type from.</param>
        /// <returns>List of inferred typenames.</returns>
        public static IList<PSTypeName> InferTypeOf(Ast ast)
        {
            return InferTypeOf(ast, TypeInferenceRuntimePermissions.None);
        }

        /// <summary>
        /// Infers the type that the result of executing a statement would have.
        /// </summary>
        /// <param name="ast">The ast to infer the type from.</param>
        /// <param name="evalPermissions">The runtime usage permissions allowed during type inference.</param>
        /// <returns>List of inferred typenames.</returns>
        public static IList<PSTypeName> InferTypeOf(Ast ast, TypeInferenceRuntimePermissions evalPermissions)
        {
            return InferTypeOf(ast, PowerShell.Create(RunspaceMode.CurrentRunspace), evalPermissions);
        }

        /// <summary>
        /// Infers the type that the result of executing a statement would have without using runtime safe eval.
        /// </summary>
        /// <param name="ast">The ast to infer the type from.</param>
        /// <param name="powerShell">The instance of powershell to use for expression evaluation needed for type inference.</param>
        /// <returns>List of inferred typenames.</returns>
        public static IList<PSTypeName> InferTypeOf(Ast ast, PowerShell powerShell)
        {
            return InferTypeOf(ast, powerShell, TypeInferenceRuntimePermissions.None);
        }

        /// <summary>
        /// Infers the type that the result of executing a statement would have.
        /// </summary>
        /// <param name="ast">The ast to infer the type from.</param>
        /// <param name="powerShell">The instance of powershell to user for expression evaluation needed for type inference.</param>
        /// <param name="evalPersmissions">The runtime usage permissions allowed during type inference.</param>
        /// <returns>List of inferred typenames.</returns>
        public static IList<PSTypeName> InferTypeOf(Ast ast, PowerShell powerShell, TypeInferenceRuntimePermissions evalPersmissions)
        {
            var context = new TypeInferenceContext(powerShell);
            return InferTypeOf(ast, context, evalPersmissions);
        }

        /// <summary>
        /// Infers the type that the result of executing a statement would have.
        /// </summary>
        /// <param name="ast">The ast to infer the type from.</param>
        /// <param name="context">The current type inference context.</param>
        /// <param name="evalPersmissions">The runtime usage permissions allowed during type inference.</param>
        /// <returns>List of inferred typenames.</returns>
        internal static IList<PSTypeName> InferTypeOf(
            Ast ast,
            TypeInferenceContext context,
            TypeInferenceRuntimePermissions evalPersmissions = TypeInferenceRuntimePermissions.None)
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

    internal class PSTypeNameComparer : IEqualityComparer<PSTypeName>
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
        public static readonly PSTypeName[] EmptyPSTypeNameArray = Array.Empty<PSTypeName>();
        private readonly PowerShell _powerShell;

        public TypeInferenceContext() : this(PowerShell.Create(RunspaceMode.CurrentRunspace))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeInferenceContext"/> class.
        /// The powerShell instance passed need to have a non null Runspace.
        /// </summary>
        /// <param name="powerShell">The instance of powershell to use for expression evaluation needed for type inference.</param>
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

            return expression != null &&
                   SafeExprEvaluator.TrySafeEval(expression, ExecutionContext, out var value) &&
                   TryGetRepresentativeTypeNameFromValue(value, out typeName);
        }

        internal IList<object> GetMembersByInferredType(PSTypeName typename, bool isStatic, Func<object, bool> filter)
        {
            List<object> results = new List<object>();

            Func<object, bool> filterToCall = filter;
            if (typename is PSSyntheticTypeName synthetic)
            {
                foreach (var mem in synthetic.Members)
                {
                    results.Add(new PSInferredProperty(mem.Name, mem.PSTypeName));
                }
            }

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
                var elementList = new List<Type>();
                foreach (var t in typename.Type.GetInterfaces())
                {
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        elementList.Add(t);
                    }
                }

                elementTypes = elementList;
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
                              ? PSObject.DotNetStaticAdapter.BaseGetMembers<PSMemberInfo>(type)
                              : PSObject.DotNetInstanceAdapter.GetPropertiesAndMethods(type, false);

                if (filterToCall != null)
                {
                    foreach (var member in members)
                    {
                        if (filterToCall(member))
                        {
                            results.Add(member);
                        }
                    }
                }
                else
                {
                    results.AddRange(members);
                }
            }
        }

        internal void AddMembersByInferredTypeDefinitionAst(
            PSTypeName typename,
            bool isStatic,
            Func<object, bool> filter,
            Func<object, bool> filterToCall,
            List<object> results)
        {
            if (CurrentTypeDefinitionAst != typename.TypeDefinitionAst)
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

            bool foundConstructor = false;
            foreach (var member in typename.TypeDefinitionAst.Members)
            {
                bool add;
                if (member is PropertyMemberAst propertyMember)
                {
                    add = propertyMember.IsStatic == isStatic;
                }
                else
                {
                    var functionMember = (FunctionMemberAst)member;
                    add = (functionMember.IsConstructor && isStatic) || (!functionMember.IsConstructor && functionMember.IsStatic == isStatic);
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

            // iterate through bases/interfaces
            foreach (var baseType in typename.TypeDefinitionAst.BaseTypes)
            {
                if (!(baseType.TypeName is TypeName baseTypeName))
                {
                    continue;
                }

                var baseTypeDefinitionAst = baseTypeName._typeDefinitionAst;
                if (baseTypeDefinitionAst is null)
                {
                    var baseReflectionType = baseTypeName.GetReflectionType();
                    if (baseReflectionType is not null)
                    {
                        results.AddRange(GetMembersByInferredType(new PSTypeName(baseReflectionType), isStatic, filterToCall));
                    }
                }
                else
                {
                    results.AddRange(GetMembersByInferredType(new PSTypeName(baseTypeDefinitionAst), isStatic, filterToCall));
                }
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
                        new CompilerGeneratedMemberFunctionAst(
                            PositionUtilities.EmptyExtent,
                            typename.TypeDefinitionAst,
                            SpecialMemberFunctionType.DefaultConstructor));
                }
            }
            else
            {
                // Reset the filter because the recursive call will add IsHidden back if necessary.
                filterToCall = filter;
            }

            PSTypeName baseMembersType;
            if (typename.TypeDefinitionAst.IsEnum)
            {
                if (!isStatic)
                {
                    results.Add(new PSInferredProperty("value__", new PSTypeName(typeof(int))));
                }

                baseMembersType = new PSTypeName(typeof(Enum));
            }
            else
            {
                baseMembersType = new PSTypeName(typeof(object));
            }

            results.AddRange(GetMembersByInferredType(baseMembersType, isStatic, filterToCall));
        }

        internal void AddMembersByInferredTypeCimType(PSTypeName typename, List<object> results, Func<object, bool> filterToCall)
        {
            if (ParseCimCommandsTypeName(typename, out var cimNamespace, out var className))
            {
                var powerShellExecutionHelper = Helper;
                powerShellExecutionHelper.AddCommandWithPreferenceSetting("CimCmdlets\\Get-CimClass")
                                         .AddParameter("Namespace", cimNamespace)
                                         .AddParameter("Class", className);

                var classes = powerShellExecutionHelper.ExecuteCurrentPowerShell(out _);
                var cimClasses = new List<CimClass>();
                foreach (var c in classes)
                {
                    if (PSObject.Base(c) is CimClass cc)
                    {
                        cimClasses.Add(cc);
                    }
                }

                foreach (var cimClass in cimClasses)
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
                if (value is IList list
                    && list.Count > 0)
                {
                    value = list[0];
                }

                value = PSObject.Base(value);
                if (value != null)
                {
                    var typeObject = value.GetType();
                    
                    if (typeObject.FullName.Equals("System.Management.Automation.PSObject", StringComparison.Ordinal))
                    {
                        var psobjectPropertyList = new List<PSMemberNameAndType>();
                        foreach (var property in ((PSObject)value).Properties)
                        {
                            if (property.IsHidden)
                            {
                                continue;
                            }

                            var propertyTypeName = new PSTypeName(property.TypeNameOfValue);
                            psobjectPropertyList.Add(new PSMemberNameAndType(property.Name, propertyTypeName, property.Value));
                        }

                        type = PSSyntheticTypeName.Create(typeObject, psobjectPropertyList);
                    }
                    else
                    {
                        type = new PSTypeName(typeObject);
                    }
                    
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
                    return memberInfo.GetCustomAttributes(typeof(HiddenAttribute), false).Length != 0;
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
            if (arrayExpressionAst.SubExpression.Statements.Count == 0)
            {
                return new[] { new PSTypeName(typeof(object[])) };
            }

            return new[] { GetArrayType(InferTypes(arrayExpressionAst.SubExpression)) };
        }

        object ICustomAstVisitor.VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst)
        {
            var inferredElementTypes = new List<PSTypeName>();
            foreach (ExpressionAst expression in arrayLiteralAst.Elements)
            {
                inferredElementTypes.AddRange(InferTypes(expression));
            }

            return new[] { GetArrayType(inferredElementTypes) };
        }

        object ICustomAstVisitor.VisitHashtable(HashtableAst hashtableAst)
        {
            if (hashtableAst.KeyValuePairs.Count > 0)
            {
                var properties = new List<PSMemberNameAndType>();
                void AddInferredTypes(Ast ast, string keyName)
                {
                    bool foundAnyTypes = false;
                    foreach (PSTypeName item in InferTypes(ast))
                    {
                        foundAnyTypes = true;
                        properties.Add(new PSMemberNameAndType(keyName, item));
                    }

                    if (!foundAnyTypes)
                    {
                        properties.Add(new PSMemberNameAndType(keyName, new PSTypeName("System.Object")));
                    }
                }

                foreach (var kv in hashtableAst.KeyValuePairs)
                {
                    string name = null;
                    if (kv.Item1 is StringConstantExpressionAst stringConstantExpressionAst)
                    {
                        name = stringConstantExpressionAst.Value;
                    }
                    else if (kv.Item1 is ConstantExpressionAst constantExpressionAst)
                    {
                        name = constantExpressionAst.Value.ToString();
                    }
                    else if (SafeExprEvaluator.TrySafeEval(kv.Item1, _context.ExecutionContext, out object nameValue))
                    {
                        name = nameValue.ToString();
                    }

                    if (name is not null)
                    {
                        if (kv.Item2 is PipelineAst pipelineAst && pipelineAst.GetPureExpression() is ExpressionAst expression)
                        {
                            object value;
                            if (expression is ConstantExpressionAst constant)
                            {
                                value = constant.Value;
                            }
                            else
                            {
                                _ = SafeExprEvaluator.TrySafeEval(expression, _context.ExecutionContext, out value);
                            }

                            if (value is null)
                            {
                                AddInferredTypes(expression, name);
                                continue;
                            }

                            PSTypeName valueType = new(value.GetType());
                            properties.Add(new PSMemberNameAndType(name, valueType, value));
                        }
                        else
                        {
                            AddInferredTypes(kv.Item2, name);
                        }
                    }
                }

                return new[] { PSSyntheticTypeName.Create(typeof(Hashtable), properties) };
            }

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
            var inferredTypes = new List<PSTypeName>();
            InferTypeFrom(ast, inferredTypes);
            return inferredTypes;
        }

        object ICustomAstVisitor.VisitMergingRedirection(MergingRedirectionAst mergingRedirectionAst)
        {
            return TypeInferenceContext.EmptyPSTypeNameArray;
        }

        object ICustomAstVisitor.VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst)
        {
            // TODO: Handle other kinds of expressions on the right side.
            if (binaryExpressionAst.Operator == TokenKind.As && binaryExpressionAst.Right is TypeExpressionAst typeExpression)
            {
                var type = typeExpression.TypeName.GetReflectionType();
                var psTypeName = type != null ? new PSTypeName(type) : new PSTypeName(typeExpression.TypeName.FullName);
                return new[] { psTypeName };
            }
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
            // The reflection type of PSCustomObject is PSObject, so this covers both the
            // [PSObject] @{ Key = "Value" } and the [PSCustomObject] @{ Key = "Value" } case.
            var type = convertExpressionAst.Type.TypeName.GetReflectionType();

            if (type is null && convertExpressionAst.Type.TypeName is TypeName unavailableType && unavailableType._typeDefinitionAst is not null)
            {
                return new[] { new PSTypeName(unavailableType._typeDefinitionAst) };
            }

            if (type == typeof(PSObject) && convertExpressionAst.Child is HashtableAst hashtableAst)
            {
                if (InferTypes(hashtableAst).FirstOrDefault() is PSSyntheticTypeName syntheticTypeName)
                {
                    return new[] { PSSyntheticTypeName.Create(type, syntheticTypeName.Members) };
                }
            }

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
            var inferredTypes = new List<PSTypeName>();
            if (errorStatementAst.Conditions is not null)
            {
                foreach (var ast in errorStatementAst.Conditions)
                {
                    inferredTypes.AddRange(InferTypes(ast));
                }
            }

            if (errorStatementAst.Bodies is not null)
            {
                foreach (var ast in errorStatementAst.Bodies)
                {
                    inferredTypes.AddRange(InferTypes(ast));
                }
            }

            if (errorStatementAst.NestedAst is not null)
            {
                foreach (var ast in errorStatementAst.NestedAst)
                {
                    inferredTypes.AddRange(InferTypes(ast));
                }
            }

            return inferredTypes;
        }

        object ICustomAstVisitor.VisitErrorExpression(ErrorExpressionAst errorExpressionAst)
        {
            var inferredTypes = new List<PSTypeName>();
            foreach (var ast in errorExpressionAst.NestedAst)
            {
                inferredTypes.AddRange(InferTypes(ast));
            }

            return inferredTypes;
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
            var inferredTypes = new List<PSTypeName>();
            for (var index = 0; index < namedBlockAst.Statements.Count; index++)
            {
                var ast = namedBlockAst.Statements[index];
                inferredTypes.AddRange(InferTypes(ast));
            }

            return inferredTypes;
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
            bool typeConstraintAdded = false;
            foreach (var attrib in attributes)
            {
                switch (attrib)
                {
                    case TypeConstraintAst typeConstraint:
                        if (!typeConstraintAdded)
                        {
                            res.Add(new PSTypeName(typeConstraint.TypeName));
                            typeConstraintAdded = true;
                        }

                        break;
                    case AttributeAst attributeAst:
                        PSTypeNameAttribute attribute = null;
                        try
                        {
                            attribute = attributeAst.GetAttribute() as PSTypeNameAttribute;
                        }
                        catch (RuntimeException)
                        {
                        }

                        if (attribute != null)
                        {
                            res.Add(new PSTypeName(attribute.PSTypeName));
                        }

                        break;
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
            var inferredTypes = new List<PSTypeName>();
            foreach (var ast in statementBlockAst.Statements)
            {
                inferredTypes.AddRange(InferTypes(ast));
            }

            return inferredTypes;
        }

        object ICustomAstVisitor.VisitIfStatement(IfStatementAst ifStmtAst)
        {
            var res = new List<PSTypeName>();

            foreach (var clause in ifStmtAst.Clauses)
            {
                res.AddRange(InferTypes(clause.Item2));
            }

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

            foreach (var clause in clauses)
            {
                res.AddRange(InferTypes(clause.Item2));
            }

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
            if (returnStatementAst.Pipeline is null)
            {
                return TypeInferenceContext.EmptyPSTypeNameArray;
            }

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
            var pipelineAstPipelineElements = pipelineAst.PipelineElements;
            return pipelineAstPipelineElements[pipelineAstPipelineElements.Count - 1].Accept(this);
        }

        object ICustomAstVisitor.VisitCommand(CommandAst commandAst)
        {
            var inferredTypes = new List<PSTypeName>();
            InferTypesFrom(commandAst, inferredTypes);
            return inferredTypes;
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

        private void InferTypesFrom(CommandAst commandAst, List<PSTypeName> inferredTypes)
        {
            PseudoBindingInfo pseudoBinding = new PseudoParameterBinder()
            .DoPseudoParameterBinding(commandAst, null, null, PseudoParameterBinder.BindingType.ParameterCompletion);

            if (pseudoBinding?.CommandInfo is null)
            {
                var commandName = commandAst.GetCommandName();
                if (string.IsNullOrEmpty(commandName))
                {
                    return;
                }

                try
                {
                    var foundCommand = CommandDiscovery.LookupCommandInfo(
                        commandName,
                        CommandTypes.Application,
                        SearchResolutionOptions.ResolveLiteralThenPathPatterns,
                        CommandOrigin.Internal,
                        _context.ExecutionContext);

                    // There's no way to know whether or not an application outputs anything
                    // but when they do, PowerShell will treat it as string data.
                    inferredTypes.Add(new PSTypeName(typeof(string)));
                }
                catch
                {
                    // The command wasn't found so we can't infer anything.
                }

                return;
            }

            string pathParameterName = "Path";
            if (!pseudoBinding.BoundArguments.TryGetValue(pathParameterName, out var pathArgument))
            {
                pathParameterName = "LiteralPath";
                pseudoBinding.BoundArguments.TryGetValue(pathParameterName, out pathArgument);
            }

            // The OutputType on cmdlets like Get-ChildItem may depend on the path.
            // The CmdletInfo returned based on just the command name will specify returning all possibilities, e.g.certificates, environment, registry, etc.
            // If you specified - Path, the list of OutputType can be refined, but we have to make a copy of the CmdletInfo object this way to get that refinement.
            var commandInfo = pseudoBinding.CommandInfo;
            var pathArgumentPair = pathArgument as AstPair;
            if (pathArgumentPair?.Argument is StringConstantExpressionAst ast)
            {
                var pathValue = ast.Value;
                try
                {
                    commandInfo = commandInfo.CreateGetCommandCopy(new object[] { "-" + pathParameterName, pathValue });
                }
                catch (InvalidOperationException)
                {
                }
            }

            if (commandInfo is CmdletInfo cmdletInfo)
            {
                // Special cases
                var inferTypesFromObjectCmdlets = InferTypesFromObjectCmdlets(commandAst, cmdletInfo, pseudoBinding);
                if (inferTypesFromObjectCmdlets.Count > 0)
                {
                    inferredTypes.AddRange(inferTypesFromObjectCmdlets);
                    return;
                }

                if (cmdletInfo.ImplementingType.FullName.EqualsOrdinalIgnoreCase("Microsoft.PowerShell.Commands.GetRandomCommand")
                    && pseudoBinding.BoundArguments.TryGetValue("InputObject", out var value))
                {
                    if (value.ParameterArgumentType == AstParameterArgumentType.PipeObject)
                    {
                        InferTypesFromPreviousCommand(commandAst, inferredTypes);
                    }
                    else if (value.ParameterArgumentType == AstParameterArgumentType.AstPair)
                    {
                        inferredTypes.AddRange(InferTypes(((AstPair)value).Argument));
                    }

                    return;
                }
            }

            // The OutputType property ignores the parameter set specified in the OutputTypeAttribute.
            // With pseudo-binding, we actually know the candidate parameter sets, so we could take
            // advantage of it here, but I opted for the simpler code because so few cmdlets use
            // ParameterSetName in OutputType and of the ones I know about, it isn't that useful.
            inferredTypes.AddRange(commandInfo.OutputType);
        }

        /// <summary>
        /// Infer types from the well-known object cmdlets, like foreach-object, where-object, sort-object etc.
        /// </summary>
        /// <param name="commandAst">The ast to infer types from.</param>
        /// <param name="cmdletInfo">The cmdletInfo.</param>
        /// <param name="pseudoBinding">Pseudo bindings of parameters.</param>
        /// <returns>List of inferred type names.</returns>
        private List<PSTypeName> InferTypesFromObjectCmdlets(CommandAst commandAst, CmdletInfo cmdletInfo, PseudoBindingInfo pseudoBinding)
        {
            var inferredTypes = new List<PSTypeName>(16);

            if (cmdletInfo.ImplementingType.FullName.EqualsOrdinalIgnoreCase("Microsoft.PowerShell.Commands.NewObjectCommand"))
            {
                // new - object - yields an instance of whatever -Type is bound to
                var newObjectType = InferTypesFromNewObjectCommand(pseudoBinding);
                if (newObjectType != null)
                {
                    inferredTypes.Add(newObjectType);
                }
            }
            else if (
                cmdletInfo.ImplementingType.FullName.EqualsOrdinalIgnoreCase("Microsoft.Management.Infrastructure.CimCmdlets.GetCimInstanceCommand") ||
                cmdletInfo.ImplementingType.FullName.EqualsOrdinalIgnoreCase("Microsoft.Management.Infrastructure.CimCmdlets.NewCimInstanceCommand"))
            {
                // Get-CimInstance/New-CimInstance - adds a CimInstance with ETS type based on its arguments for -Namespace and -ClassName parameters
                InferTypesFromCimCommand(pseudoBinding, inferredTypes);
            }
            else if (cmdletInfo.ImplementingType == typeof(WhereObjectCommand) ||
                     cmdletInfo.ImplementingType.FullName.EqualsOrdinalIgnoreCase("Microsoft.PowerShell.Commands.SortObjectCommand"))
            {
                // where-object - adds whatever we saw before where-object in the pipeline.
                // same for sort-object
                InferTypesFromWhereAndSortCommand(commandAst, inferredTypes);
            }
            else if (cmdletInfo.ImplementingType == typeof(ForEachObjectCommand))
            {
                // foreach-object - adds the type of it's script block parameters
                InferTypesFromForeachCommand(pseudoBinding, commandAst, inferredTypes);
            }
            else if (cmdletInfo.ImplementingType.FullName.EqualsOrdinalIgnoreCase("Microsoft.PowerShell.Commands.SelectObjectCommand"))
            {
                // Select-object - adds whatever we saw before where-object in the pipeline.
                // unless -property or -excludeproperty
                InferTypesFromSelectCommand(pseudoBinding, commandAst, inferredTypes);
            }
            else if (cmdletInfo.ImplementingType.FullName.EqualsOrdinalIgnoreCase("Microsoft.PowerShell.Commands.GroupObjectCommand"))
            {
                // Group-object - annotates the types of Group and Value based on whatever we saw before Group-Object in the pipeline.
                InferTypesFromGroupCommand(pseudoBinding, commandAst, inferredTypes);
            }

            return inferredTypes;
        }

        private static void InferTypesFromCimCommand(PseudoBindingInfo pseudoBinding, List<PSTypeName> inferredTypes)
        {
            string pseudoboundNamespace =
            CompletionCompleters.NativeCommandArgumentCompletion_ExtractSecondaryArgument(
                                    pseudoBinding.BoundArguments,
                                    "Namespace")
                                .FirstOrDefault();

            string pseudoboundClassName =
            CompletionCompleters.NativeCommandArgumentCompletion_ExtractSecondaryArgument(
                                    pseudoBinding.BoundArguments,
                                    "ClassName")
                                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(pseudoboundClassName))
            {
                var typeName = new PSTypeName(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}#{1}/{2}",
                        typeof(CimInstance).FullName,
                        pseudoboundNamespace ?? "root/cimv2",
                        pseudoboundClassName));

                inferredTypes.Add(typeName);
            }

            inferredTypes.Add(new PSTypeName(typeof(CimInstance)));
        }

        private void InferTypesFromForeachCommand(PseudoBindingInfo pseudoBinding, CommandAst commandAst, List<PSTypeName> inferredTypes)
        {
            if (pseudoBinding.BoundArguments.TryGetValue("MemberName", out AstParameterArgumentPair argument))
            {
                var previousPipelineElement = GetPreviousPipelineCommand(commandAst);
                if (previousPipelineElement == null)
                {
                    return;
                }

                foreach (var t in InferTypes(previousPipelineElement))
                {
                    var memberName = (((AstPair)argument).Argument as StringConstantExpressionAst)?.Value;

                    if (memberName != null)
                    {
                        var members = _context.GetMembersByInferredType(t, false, null);
                        bool maybeWantDefaultCtor = false;
                        GetTypesOfMembers(t, memberName, members, ref maybeWantDefaultCtor, isInvokeMemberExpressionAst: false, inferredTypes);
                    }
                }
            }

            if (pseudoBinding.BoundArguments.TryGetValue("Begin", out argument))
            {
                GetInferredTypeFromScriptBlockParameter(argument, inferredTypes);
            }

            if (pseudoBinding.BoundArguments.TryGetValue("Process", out argument))
            {
                GetInferredTypeFromScriptBlockParameter(argument, inferredTypes);
            }

            if (pseudoBinding.BoundArguments.TryGetValue("End", out argument))
            {
                GetInferredTypeFromScriptBlockParameter(argument, inferredTypes);
            }
        }

        private void InferTypesFromGroupCommand(PseudoBindingInfo pseudoBinding, CommandAst commandAst, List<PSTypeName> inferredTypes)
        {
            if (pseudoBinding.BoundArguments.TryGetValue("AsHashTable", out AstParameterArgumentPair _))
            {
                inferredTypes.Add(new PSTypeName(typeof(Hashtable)));
                return;
            }

            var noElement = pseudoBinding.BoundArguments.TryGetValue("NoElement", out AstParameterArgumentPair _);

            string[] properties = null;
            bool scriptBlockProperty = false;
            if (pseudoBinding.BoundArguments.TryGetValue("Property", out AstParameterArgumentPair propertyArgumentPair))
            {
                if (propertyArgumentPair is AstPair astPair)
                {
                    switch (astPair.Argument)
                    {
                        case StringConstantExpressionAst stringConstant:
                            properties = new[] { stringConstant.Value };
                            break;
                        case ArrayLiteralAst arrayLiteral:
                            properties = arrayLiteral.Elements.OfType<StringConstantExpressionAst>().Select(static c => c.Value).ToArray();
                            scriptBlockProperty = arrayLiteral.Elements.OfType<StringConstantExpressionAst>().Any();
                            break;
                        case CommandElementAst _:
                            scriptBlockProperty = true;
                            break;
                    }
                }
            }

            bool IsInPropertyArgument(object o)
            {
                if (properties == null)
                {
                    return true;
                }

                string name;
                switch (o)
                {
                    case string s:
                        name = s;
                        break;
                    default:
                        name = GetMemberName(o);
                        break;
                }

                foreach (var propertyName in properties)
                {
                    if (name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            var previousPipelineElement = GetPreviousPipelineCommand(commandAst);
            const string typeName = "Microsoft.PowerShell.Commands.GroupInfo";
            var members = new List<PSMemberNameAndType>();
            foreach (var prevType in InferTypes(previousPipelineElement))
            {
                members.Clear();
                if (noElement)
                {
                    members.Add(new PSMemberNameAndType("Values", new PSTypeName(prevType.Name)));
                    inferredTypes.Add(PSSyntheticTypeName.Create(typeName, members));
                    continue;
                }

                var memberNameAndTypes = GetMemberNameAndTypeFromProperties(prevType, IsInPropertyArgument);
                if (memberNameAndTypes.Count == 0)
                {
                    continue;
                }

                if (properties != null)
                {
                    foreach (var memType in memberNameAndTypes)
                    {
                        members.Clear();
                        members.Add(new PSMemberNameAndType("Group", new PSTypeName(prevType.Name)));
                        members.Add(new PSMemberNameAndType("Values", new PSTypeName(memType.PSTypeName.Name)));
                        inferredTypes.Add(PSSyntheticTypeName.Create(typeName, members));
                    }
                }
                else
                {
                    // No Property parameter given
                    // group infers to IList<PrevType>
                    members.Add(new PSMemberNameAndType("Group", new PSTypeName(prevType.Name)));
                    // Value infers to IList<PrevType>
                    if (!scriptBlockProperty)
                    {
                        members.Add(new PSMemberNameAndType("Values", new PSTypeName(prevType.Name)));
                    }
                }

                inferredTypes.Add(PSSyntheticTypeName.Create(typeName, members));
            }
        }

        private void InferTypesFromWhereAndSortCommand(CommandAst commandAst, List<PSTypeName> inferredTypes)
        {
            InferTypesFromPreviousCommand(commandAst, inferredTypes);
        }

        private void InferTypesFromPreviousCommand(CommandAst commandAst, List<PSTypeName> inferredTypes)
        {
            if (commandAst.Parent is PipelineAst parentPipeline)
            {
                int i;
                for (i = 0; i < parentPipeline.PipelineElements.Count; i++)
                {
                    if (parentPipeline.PipelineElements[i] == commandAst)
                    {
                        break;
                    }
                }

                if (i > 0)
                {
                    inferredTypes.AddRange(GetInferredEnumeratedTypes(InferTypes(parentPipeline.PipelineElements[i - 1])));
                }
            }
        }

        private void InferTypesFromSelectCommand(PseudoBindingInfo pseudoBinding, CommandAst commandAst, List<PSTypeName> inferredTypes)
        {
            void InferFromSelectProperties(AstParameterArgumentPair astParameterArgumentPair, CommandBaseAst previousPipelineElementAst, bool includeMatchedProperties = true)
            {
                if (astParameterArgumentPair is AstPair astPair)
                {
                    static object ToWildCardOrString(string value) => WildcardPattern.ContainsWildcardCharacters(value) ? (object)new WildcardPattern(value) : value;
                    object[] properties = null;
                    switch (astPair.Argument)
                    {
                        case StringConstantExpressionAst stringConstant:
                            properties = new[] { ToWildCardOrString(stringConstant.Value) };
                            break;
                        case ArrayLiteralAst arrayLiteral:
                            properties = arrayLiteral.Elements.OfType<StringConstantExpressionAst>().Select(static c => ToWildCardOrString(c.Value)).ToArray();
                            break;
                    }

                    if (properties == null)
                    {
                        return;
                    }

                    bool IsInPropertyArgument(object o)
                    {
                        string name;
                        switch (o)
                        {
                            case string s:
                                name = s;
                                break;
                            default:
                                name = GetMemberName(o);
                                break;
                        }

                        foreach (var propertyNameOrPattern in properties)
                        {
                            switch (propertyNameOrPattern)
                            {
                                case string propertyName:
                                    if (string.Equals(name, propertyName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        return includeMatchedProperties;
                                    }

                                    break;
                                case WildcardPattern pattern:
                                    if (pattern.IsMatch(name))
                                    {
                                        return includeMatchedProperties;
                                    }

                                    break;
                            }
                        }

                        return !includeMatchedProperties;
                    }

                    foreach (var t in InferTypes(previousPipelineElementAst))
                    {
                        var list = GetMemberNameAndTypeFromProperties(t, IsInPropertyArgument);
                        inferredTypes.Add(PSSyntheticTypeName.Create(typeof(PSObject), list));
                    }
                }
            }

            var previousPipelineElement = GetPreviousPipelineCommand(commandAst);
            if (previousPipelineElement == null)
            {
                return;
            }

            if (pseudoBinding.BoundArguments.TryGetValue("Property", out var property))
            {
                InferFromSelectProperties(property, previousPipelineElement);

                return;
            }

            if (pseudoBinding.BoundArguments.TryGetValue("ExcludeProperty", out var excludeProperty))
            {
                InferFromSelectProperties(excludeProperty, previousPipelineElement, includeMatchedProperties: false);

                return;
            }

            if (pseudoBinding.BoundArguments.TryGetValue("ExpandProperty", out var expandedPropertyArgument))
            {
                foreach (var t in InferTypes(previousPipelineElement))
                {
                    var memberName = (((AstPair)expandedPropertyArgument).Argument as StringConstantExpressionAst)?.Value;

                    if (memberName != null)
                    {
                        var members = _context.GetMembersByInferredType(t, false, null);
                        bool maybeWantDefaultCtor = false;
                        GetTypesOfMembers(t, memberName, members, ref maybeWantDefaultCtor, isInvokeMemberExpressionAst: false, inferredTypes);
                    }
                }

                return;
            }

            InferTypesFromPreviousCommand(commandAst, inferredTypes);
        }

        private List<PSMemberNameAndType> GetMemberNameAndTypeFromProperties(PSTypeName t, Func<object, bool> isInPropertyList)
        {
            var list = new List<PSMemberNameAndType>(8);
            var members = _context.GetMembersByInferredType(t, false, isInPropertyList);
            var memberTypes = new List<PSTypeName>();
            foreach (var mem in members)
            {
                if (!IsProperty(mem))
                {
                    continue;
                }

                var memberName = GetMemberName(mem);
                if (!isInPropertyList(memberName))
                {
                    continue;
                }

                bool maybeWantDefaultCtor = false;
                memberTypes.Clear();
                GetTypesOfMembers(t, memberName, members, ref maybeWantDefaultCtor, isInvokeMemberExpressionAst: false, memberTypes);
                if (memberTypes.Count > 0)
                {
                    list.Add(new PSMemberNameAndType(memberName, memberTypes[0]));
                }
            }

            return list;
        }

        private static bool IsProperty(object member)
        {
            switch (member)
            {
                case PropertyInfo _:
                    return true;
                case PSMemberInfo memberInfo:
                    return (memberInfo.MemberType & PSMemberTypes.Properties) == memberInfo.MemberType;
                default:
                    return false;
            }
        }

        private static string GetMemberName(object member)
        {
            var name = string.Empty;
            switch (member)
            {
                case PSMemberInfo psMemberInfo:
                    name = psMemberInfo.Name;
                    break;
                case MemberInfo memberInfo:
                    name = memberInfo.Name;
                    break;
                case PropertyMemberAst propertyMember:
                    name = propertyMember.Name;
                    break;
                case FunctionMemberAst functionMember:
                    name = functionMember.Name;
                    break;
                case DotNetAdapter.MethodCacheEntry methodCacheEntry:
                    name = methodCacheEntry[0].method.Name;
                    break;
            }

            return name;
        }

        private static PSTypeName InferTypesFromNewObjectCommand(PseudoBindingInfo pseudoBinding)
        {
            if (pseudoBinding.BoundArguments.TryGetValue("TypeName", out var typeArgument))
            {
                var typeArgumentPair = typeArgument as AstPair;
                if (typeArgumentPair?.Argument is StringConstantExpressionAst stringConstantExpr)
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
            if (!(memberCommandElement is StringConstantExpressionAst memberAsStringConst))
            {
                return Array.Empty<PSTypeName>();
            }

            var exprType = GetExpressionType(expression, isStatic);
            if (exprType == null || exprType.Length == 0)
            {
                return Array.Empty<PSTypeName>();
            }

            bool isInvokeMemberExpressionAst = false;
            var res = new List<PSTypeName>(10);
            IList<ITypeName> genericTypeArguments = null;

            if (memberExpressionAst is InvokeMemberExpressionAst invokeMemberExpression)
            {
                isInvokeMemberExpressionAst = true;
                genericTypeArguments = invokeMemberExpression.GenericTypeArguments;
            }

            var maybeWantDefaultCtor = isStatic
                                       && isInvokeMemberExpressionAst
                                       && memberAsStringConst.Value.EqualsOrdinalIgnoreCase("new");

            // We use a list of member names because we might discover aliases properties
            // and if we do, we'll add to the list.
            var memberNameList = new List<string> { memberAsStringConst.Value };
            foreach (var type in exprType)
            {
                if (type.Type == typeof(PSObject) && type is not PSSyntheticTypeName)
                {
                    continue;
                }

                var members = _context.GetMembersByInferredType(type, isStatic, filter: null);

                AddTypesOfMembers(type, memberNameList, members, ref maybeWantDefaultCtor, isInvokeMemberExpressionAst, genericTypeArguments, res);

                // We didn't find any constructors but they used [T]::new() syntax
                if (maybeWantDefaultCtor)
                {
                    res.Add(type);
                }
            }

            return res;
        }

        private void GetTypesOfMembers(
            PSTypeName thisType,
            string memberName,
            IList<object> members,
            ref bool maybeWantDefaultCtor,
            bool isInvokeMemberExpressionAst,
            List<PSTypeName> inferredTypes)
        {
            var memberNamesToCheck = new List<string> { memberName };
            AddTypesOfMembers(thisType, memberNamesToCheck, members, ref maybeWantDefaultCtor, isInvokeMemberExpressionAst, genericTypeArguments: null, inferredTypes);
        }

        private void AddTypesOfMembers(
            PSTypeName currentType,
            List<string> memberNamesToCheck,
            IList<object> members,
            ref bool maybeWantDefaultCtor,
            bool isInvokeMemberExpressionAst,
            IList<ITypeName> genericTypeArguments,
            List<PSTypeName> result)
        {
            for (int i = 0; i < memberNamesToCheck.Count; i++)
            {
                string memberNameToCheck = memberNamesToCheck[i];
                foreach (var member in members)
                {
                    if (TryGetTypeFromMember(currentType, member, memberNameToCheck, ref maybeWantDefaultCtor, isInvokeMemberExpressionAst, genericTypeArguments, result, memberNamesToCheck))
                    {
                        break;
                    }
                }
            }
        }

        private bool TryGetTypeFromMember(
            PSTypeName currentType,
            object member,
            string memberName,
            ref bool maybeWantDefaultCtor,
            bool isInvokeMemberExpressionAst,
            IList<ITypeName> genericTypeArguments,
            List<PSTypeName> result,
            List<string> memberNamesToCheck)
        {
            switch (member)
            {
                case PropertyInfo propertyInfo: // .net property
                    if (propertyInfo.Name.EqualsOrdinalIgnoreCase(memberName) && !isInvokeMemberExpressionAst)
                    {
                        result.Add(new PSTypeName(propertyInfo.PropertyType));
                        return true;
                    }

                    return false;
                case FieldInfo fieldInfo: // .net field
                    if (fieldInfo.Name.EqualsOrdinalIgnoreCase(memberName) && !isInvokeMemberExpressionAst)
                    {
                        result.Add(new PSTypeName(fieldInfo.FieldType));
                        return true;
                    }

                    return false;
                case DotNetAdapter.MethodCacheEntry methodCacheEntry: // .net method
                    if (methodCacheEntry[0].method.Name.Equals(memberName, StringComparison.OrdinalIgnoreCase))
                    {
                        maybeWantDefaultCtor = false;
                        AddTypesFromMethodCacheEntry(methodCacheEntry, genericTypeArguments, result, isInvokeMemberExpressionAst);
                        return true;
                    }

                    return false;
                case MemberAst memberAst: // this is for members defined by PowerShell classes
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
                                result.Add(
                                    propertyMemberAst.PropertyType != null
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
                case PSMemberInfo memberInfo:
                    if (!memberInfo.Name.Equals(memberName, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    ScriptBlock scriptBlock = null;
                    switch (memberInfo)
                    {
                        case PSMethod m:
                            if (m.adapterData is DotNetAdapter.MethodCacheEntry methodCacheEntry)
                            {
                                AddTypesFromMethodCacheEntry(methodCacheEntry, genericTypeArguments, result, isInvokeMemberExpressionAst);
                                return true;
                            }

                            return false;
                        case PSProperty p:
                            result.Add(new PSTypeName(p.Value.GetType()));
                            return true;
                        case PSNoteProperty noteProperty:
                            result.Add(new PSTypeName(noteProperty.Value.GetType()));
                            return true;
                        case PSAliasProperty aliasProperty:
                            memberNamesToCheck.Add(aliasProperty.ReferencedMemberName);
                            return true;
                        case PSCodeProperty codeProperty:
                            if (codeProperty.GetterCodeReference != null)
                            {
                                result.Add(new PSTypeName(codeProperty.GetterCodeReference.ReturnType));
                            }

                            return true;
                        case PSScriptProperty scriptProperty:
                            scriptBlock = scriptProperty.GetterScript;
                            break;
                        case PSScriptMethod scriptMethod:
                            scriptBlock = scriptMethod.Script;
                            break;
                        case PSInferredProperty inferredProperty:
                            result.Add(inferredProperty.TypeName);
                            break;
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
                                result.AddRange(InferTypes(scriptBlock.Ast));
                                return true;
                            }
                        }
                        finally
                        {
                            _context.CurrentThisType = thisToRestore;
                        }
                    }

                    return false;
            }

            return false;
        }

        private static void AddTypesFromMethodCacheEntry(
            DotNetAdapter.MethodCacheEntry methodCacheEntry,
            IList<ITypeName> genericTypeArguments,
            List<PSTypeName> result,
            bool isInvokeMemberExpressionAst)
        {
            if (isInvokeMemberExpressionAst)
            {
                Type[] resolvedTypeArguments = null;
                if (genericTypeArguments is not null)
                {
                    resolvedTypeArguments = new Type[genericTypeArguments.Count];
                    for (int i = 0; i < genericTypeArguments.Count; i++)
                    {
                        Type resolvedType = genericTypeArguments[i].GetReflectionType();
                        if (resolvedType is null)
                        {
                            // If any generic type argument cannot be resolved yet,
                            // we simply assume this information is unavailable.
                            resolvedTypeArguments = null;
                            break;
                        }

                        resolvedTypeArguments[i] = resolvedType;
                    }
                }

                var tempResult = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "System.Void" };
                foreach (var method in methodCacheEntry.methodInformationStructures)
                {
                    if (method.method is MethodInfo methodInfo)
                    {
                        Type retType = null;
                        if (!methodInfo.ReturnType.ContainsGenericParameters)
                        {
                            retType = methodInfo.ReturnType;
                        }
                        else if (resolvedTypeArguments is not null)
                        {
                            try
                            {
                                retType = methodInfo.MakeGenericMethod(resolvedTypeArguments).ReturnType;
                            }
                            catch
                            {
                                // If we can't build the generic method then just skip it to retain other completion results.
                                continue;
                            }
                        }

                        if (retType is not null && tempResult.Add(retType.FullName))
                        {
                            result.Add(new PSTypeName(retType));
                        }
                    }
                }

                return;
            }

            // Accessing a method as a property, we'd return a wrapper over the method.
            result.Add(new PSTypeName(typeof(PSMethod)));
        }

        private PSTypeName[] GetExpressionType(ExpressionAst expression, bool isStatic)
        {
            PSTypeName[] exprType;
            if (isStatic)
            {
                if (!(expression is TypeExpressionAst exprAsType))
                {
                    return null;
                }

                var type = exprAsType.TypeName.GetReflectionType();
                if (type == null)
                {
                    var typeName = exprAsType.TypeName as TypeName;
                    if (typeName?._typeDefinitionAst == null)
                    {
                        return null;
                    }

                    exprType = new[] { new PSTypeName(typeName._typeDefinitionAst) };
                }
                else
                {
                    exprType = new[] { new PSTypeName(type) };
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

        private void InferTypeFrom(VariableExpressionAst variableExpressionAst, List<PSTypeName> inferredTypes)
        {
            // We don't need to handle drive qualified variables, we can usually get those values
            // without needing to "guess" at the type.
            var astVariablePath = variableExpressionAst.VariablePath;
            if (!astVariablePath.IsVariable || astVariablePath.UserPath.EqualsOrdinalIgnoreCase(SpecialVariables.Null))
            {
                // Not a variable - the caller should have already tried going to session state
                // to get the item and hence it's type, but that must have failed.  Don't try again.
                return;
            }

            Ast parent = variableExpressionAst.Parent;
            if (astVariablePath.IsUnqualified &&
                (SpecialVariables.IsUnderbar(astVariablePath.UserPath)
                 || astVariablePath.UserPath.EqualsOrdinalIgnoreCase(SpecialVariables.PSItem)))
            {
                // The automatic variable $_ is assigned a value in scriptblocks, Switch loops and Catch/Trap statements
                // This loop will find whichever Ast that determines the value of $_
                // The value in scriptblocks is determined by the parents of that scriptblock, the only interesting scenarios are:
                // 1: MemberInvocation like: $Collection.Where({$_})
                // 2: Command pipelines like: dir | where {$_}
                // The value in a Switch loop is whichever item is in the condition part of the statement.
                // The value in Catch/Trap statements is always an error record.
                bool hasSeenScriptBlock = false;
                while (parent is not null)
                {
                    if (parent is CatchClauseAst or TrapStatementAst)
                    {
                        break;
                    }
                    else if (parent is SwitchStatementAst switchStatement
                        && switchStatement.Condition.Extent.EndOffset < variableExpressionAst.Extent.StartOffset)
                    {
                        parent = switchStatement.Condition;
                        break;
                    }
                    else if (parent is ErrorStatementAst switchErrorStatement && switchErrorStatement.Kind?.Kind == TokenKind.Switch)
                    {
                        if (switchErrorStatement.Conditions?.Count > 0)
                        {
                            parent = switchErrorStatement.Conditions[0];
                        }
                        break;
                    }
                    else if (parent is ScriptBlockExpressionAst)
                    {
                        hasSeenScriptBlock = true;
                    }
                    else if (hasSeenScriptBlock)
                    {
                        if (parent is InvokeMemberExpressionAst invokeMember)
                        {
                            parent = invokeMember.Expression;
                            break;
                        }
                        else if (parent is CommandAst cmdAst && cmdAst.Parent is PipelineAst pipeline && pipeline.PipelineElements.Count > 1)
                        {
                            // We've found a pipeline with multiple commands, now we need to determine what command came before the command with the scriptblock:
                            // eg Get-Partition in this example: Get-Disk | Get-Partition | Where {$_}
                            var indexOfPreviousCommand = pipeline.PipelineElements.IndexOf(cmdAst) - 1;
                            if (indexOfPreviousCommand >= 0)
                            {
                                parent = pipeline.PipelineElements[indexOfPreviousCommand];
                                break;
                            }
                        }
                    }

                    parent = parent.Parent;
                }

                if (parent is CatchClauseAst catchBlock)
                {
                    if (catchBlock.CatchTypes.Count > 0)
                    {
                        foreach (TypeConstraintAst catchType in catchBlock.CatchTypes)
                        {
                            Type exceptionType = catchType.TypeName.GetReflectionType();
                            if (typeof(Exception).IsAssignableFrom(exceptionType))
                            {
                                inferredTypes.Add(new PSTypeName(typeof(ErrorRecord<>).MakeGenericType(exceptionType)));
                            }
                        }
                    }

                    // Either no type constraint was specified, or all the specified catch types were unavailable but we still know it's an error record.
                    if (inferredTypes.Count == 0)
                    {
                        inferredTypes.Add(new PSTypeName(typeof(ErrorRecord)));
                    }
                }
                else if (parent is TrapStatementAst trap)
                {
                    if (trap.TrapType is not null)
                    {
                        Type exceptionType = trap.TrapType.TypeName.GetReflectionType();
                        if (typeof(Exception).IsAssignableFrom(exceptionType))
                        {
                            inferredTypes.Add(new PSTypeName(typeof(ErrorRecord<>).MakeGenericType(exceptionType)));
                        }
                    }
                    if (inferredTypes.Count == 0)
                    {
                        inferredTypes.Add(new PSTypeName(typeof(ErrorRecord)));
                    }
                }
                else if (parent is not null)
                {
                    inferredTypes.AddRange(GetInferredEnumeratedTypes(InferTypes(parent)));
                }

                return;
            }

            // For certain variables, we always know their type, well at least we can assume we know.
            if (astVariablePath.IsUnqualified)
            {
                var isThis = astVariablePath.UserPath.EqualsOrdinalIgnoreCase(SpecialVariables.This);
                if (!isThis || (_context.CurrentTypeDefinitionAst == null && _context.CurrentThisType == null))
                {
                    if (SpecialVariables.AllScopeVariables.TryGetValue(astVariablePath.UserPath, out Type knownType))
                    {
                        if (knownType == typeof(object))
                        {
                            if (_context.TryGetRepresentativeTypeNameFromExpressionSafeEval(variableExpressionAst, out var psType))
                            {
                                inferredTypes.Add(psType);
                            }
                        }
                        else
                        {
                            inferredTypes.Add(new PSTypeName(knownType));
                        }

                        return;
                    }

                    for (int i = 0; i < SpecialVariables.AutomaticVariables.Length; i++)
                    {
                        if (!astVariablePath.UserPath.EqualsOrdinalIgnoreCase(SpecialVariables.AutomaticVariables[i]))
                        {
                            continue;
                        }

                        var type = SpecialVariables.AutomaticVariableTypes[i];
                        if (type != typeof(object))
                        {
                            inferredTypes.Add(new PSTypeName(type));
                        }

                        break;
                    }
                }
                else
                {
                    var typeName = _context.CurrentThisType ?? new PSTypeName(_context.CurrentTypeDefinitionAst);
                    inferredTypes.Add(typeName);
                    return;
                }
            }
            else
            {
                inferredTypes.Add(new PSTypeName(_context.CurrentTypeDefinitionAst));
                return;
            }

            if (parent is null)
            {
                return;
            }

            // Look for our variable as a parameter or on the lhs of an assignment - hopefully we'll find either
            // a type constraint or at least we can use the rhs to infer the type.
            while (parent.Parent != null)
            {
                parent = parent.Parent;
            }

            int startOffset = variableExpressionAst.Extent.StartOffset;
            var targetAsts = (List<Ast>)AstSearcher.FindAll(
                parent,
                ast =>
                {
                    if (ast is ParameterAst || ast is AssignmentStatementAst || ast is CommandAst)
                    {
                        return variableExpressionAst.AstAssignsToSameVariable(ast)
                            && ast.Extent.EndOffset < startOffset;
                    }

                    if (ast is ForEachStatementAst)
                    {
                        return variableExpressionAst.AstAssignsToSameVariable(ast)
                            && ast.Extent.StartOffset < startOffset;
                    }

                    return false;
                },
                searchNestedScriptBlocks: true);

            foreach (var ast in targetAsts)
            {
                if (ast is ParameterAst parameterAst)
                {
                    var currentCount = inferredTypes.Count;
                    inferredTypes.AddRange(InferTypes(parameterAst));

                    if (inferredTypes.Count != currentCount)
                    {
                        return;
                    }
                }
            }

            var assignAsts = targetAsts.OfType<AssignmentStatementAst>().ToArray();

            // If any of the assignments lhs use a type constraint, then we use that.
            // Otherwise, we use the rhs of the "nearest" assignment
            for (int i = assignAsts.Length - 1; i >= 0; i--)
            {
                if (assignAsts[i].Left is ConvertExpressionAst lhsConvert)
                {
                    inferredTypes.Add(new PSTypeName(lhsConvert.Type.TypeName));
                    return;
                }
            }

            var foreachAst = targetAsts.OfType<ForEachStatementAst>().FirstOrDefault();
            if (foreachAst != null)
            {
                inferredTypes.AddRange(
                    GetInferredEnumeratedTypes(InferTypes(foreachAst.Condition)));
                return;
            }

            var commandCompletionAst = targetAsts.OfType<CommandAst>().FirstOrDefault();
            if (commandCompletionAst != null)
            {
                inferredTypes.AddRange(InferTypes(commandCompletionAst));
                return;
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
                inferredTypes.AddRange(InferTypes(closestAssignment.Right));
            }

            if (_context.TryGetRepresentativeTypeNameFromExpressionSafeEval(variableExpressionAst, out var evalTypeName))
            {
                inferredTypes.Add(evalTypeName);
            }
        }

        /// <summary>
        /// Gets the most specific array type possible from a group of inferred types.
        /// </summary>
        /// <param name="inferredTypes">The inferred types all the items in the array.</param>
        /// <returns>The inferred strongly typed array type.</returns>
        private static PSTypeName GetArrayType(IEnumerable<PSTypeName> inferredTypes)
        {
            PSTypeName foundType = null;
            foreach (PSTypeName inferredType in inferredTypes)
            {
                if (inferredType.Type == null)
                {
                    return new PSTypeName(typeof(object[]));
                }

                // IEnumerable<>.GetEnumerator and IDictionary.GetEnumerator will always be
                // inferred as multiple types due to explicit implementations, so if we find
                // one then assume the rest are also enumerators.
                if (typeof(IEnumerator).IsAssignableFrom(inferredType.Type))
                {
                    foundType = inferredType;
                    break;
                }

                if (foundType == null)
                {
                    foundType = inferredType;
                    continue;
                }

                // If there are mixed types then fall back to object[].
                if (foundType.Type != inferredType.Type)
                {
                    return new PSTypeName(typeof(object[]));
                }
            }

            if (foundType == null)
            {
                return new PSTypeName(typeof(object[]));
            }

            if (foundType.Type.IsArray)
            {
                return foundType;
            }

            Type enumeratedItemType = GetMostSpecificEnumeratedItemType(foundType.Type);
            if (enumeratedItemType != null)
            {
                return new PSTypeName(enumeratedItemType.MakeArrayType());
            }

            return new PSTypeName(foundType.Type.MakeArrayType());
        }

        /// <summary>
        /// Gets the most specific type item type from a type that is potentially enumerable.
        /// </summary>
        /// <param name="enumerableType">The type to infer enumerated item type from.</param>
        /// <returns>The inferred enumerated item type.</returns>
        private static Type GetMostSpecificEnumeratedItemType(Type enumerableType)
        {
            if (enumerableType.IsArray)
            {
                return enumerableType.GetElementType();
            }

            // These types implement IEnumerable, but we intentionally do not enumerate them.
            if (enumerableType == typeof(string) ||
                typeof(IDictionary).IsAssignableFrom(enumerableType) ||
                typeof(Xml.XmlNode).IsAssignableFrom(enumerableType))
            {
                return enumerableType;
            }

            if (enumerableType == typeof(Data.DataTable))
            {
                return typeof(Data.DataRow);
            }

            bool hasSeenNonGeneric = false;
            bool hasSeenDictionaryEnumerator = false;
            Type collectionInterface = GetGenericCollectionLikeInterface(
                enumerableType,
                ref hasSeenNonGeneric,
                ref hasSeenDictionaryEnumerator);

            if (collectionInterface != null)
            {
                return collectionInterface.GetGenericArguments()[0];
            }

            foreach (Type interfaceType in enumerableType.GetInterfaces())
            {
                collectionInterface = GetGenericCollectionLikeInterface(
                    interfaceType,
                    ref hasSeenNonGeneric,
                    ref hasSeenDictionaryEnumerator);

                if (collectionInterface != null)
                {
                    return collectionInterface.GetGenericArguments()[0];
                }
            }

            if (hasSeenDictionaryEnumerator)
            {
                return typeof(DictionaryEntry);
            }

            if (hasSeenNonGeneric)
            {
                return typeof(object);
            }

            return null;
        }

        /// <summary>
        /// Determines if the interface can be used to infer a specific enumerated type.
        /// </summary>
        /// <param name="interfaceType">The interface to test.</param>
        /// <param name="hasSeenNonGeneric">
        /// A reference to a value indicating whether a non-generic enumerable type has been
        /// seen. If <see paramref="interfaceType"/> is a non-generic enumerable type this
        /// value will be set to <see langword="true"/>.
        /// </param>
        /// <param name="hasSeenDictionaryEnumerator">
        /// A reference to a value indicating whether <see cref="IDictionaryEnumerator"/> has been
        /// seen. If <paramref name="interfaceType"/> is a <see cref="IDictionaryEnumerator"/> this
        /// value will be set to <see langword="true"/>.
        /// </param>
        /// <returns>
        /// The value of <paramref name="interfaceType"/> if it can be used to infer a specific
        /// enumerated type, otherwise <see langword="null"/>.
        /// </returns>
        private static Type GetGenericCollectionLikeInterface(
            Type interfaceType,
            ref bool hasSeenNonGeneric,
            ref bool hasSeenDictionaryEnumerator)
        {
            if (!interfaceType.IsInterface)
            {
                return null;
            }

            if (interfaceType.IsConstructedGenericType)
            {
                Type openGeneric = interfaceType.GetGenericTypeDefinition();
                if (openGeneric == typeof(IEnumerator<>) ||
                    openGeneric == typeof(IEnumerable<>))
                {
                    return interfaceType;
                }
            }

            if (interfaceType == typeof(IDictionaryEnumerator))
            {
                hasSeenDictionaryEnumerator = true;
            }

            if (interfaceType == typeof(IEnumerator) ||
                interfaceType == typeof(IEnumerable))
            {
                hasSeenNonGeneric = true;
            }

            return null;
        }

        private IEnumerable<PSTypeName> InferTypeFrom(IndexExpressionAst indexExpressionAst)
        {
            var targetTypes = InferTypes(indexExpressionAst.Target);
            bool foundAny = false;
            foreach (var psType in targetTypes)
            {
                if (psType is PSSyntheticTypeName syntheticType)
                {
                    foreach (var member in syntheticType.Members)
                    {
                        yield return member.PSTypeName;
                    }

                    continue;
                }

                var type = psType.Type;
                if (type != null)
                {
                    if (type.IsArray)
                    {
                        yield return new PSTypeName(type.GetElementType());

                        continue;
                    }
                    
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
                    {
                        var valueType = type.GetGenericArguments()[0];
                        if (!valueType.ContainsGenericParameters)
                        {
                            foundAny = true;
                            yield return new PSTypeName(valueType);
                        }
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

        /// <summary>
        /// Infers the types as if they were enumerated. For example, a <see cref="List{T}"/>
        /// of type <see cref="string"/> would be returned as <see cref="string"/>.
        /// </summary>
        /// <param name="enumerableTypes">
        /// The potentially enumerable types to infer enumerated type from.
        /// </param>
        /// <returns>The enumerated item types.</returns>
        internal static IEnumerable<PSTypeName> GetInferredEnumeratedTypes(IEnumerable<PSTypeName> enumerableTypes)
        {
            foreach (PSTypeName maybeEnumerableType in enumerableTypes)
            {
                Type type = maybeEnumerableType.Type;
                if (type == null)
                {
                    yield return maybeEnumerableType;
                    continue;
                }

                Type enumeratedItemType = GetMostSpecificEnumeratedItemType(type);
                yield return enumeratedItemType == null
                    ? maybeEnumerableType
                    : new PSTypeName(enumeratedItemType);
            }
        }

        private void GetInferredTypeFromScriptBlockParameter(AstParameterArgumentPair argument, List<PSTypeName> inferredTypes)
        {
            var argumentPair = argument as AstPair;
            if (!(argumentPair?.Argument is ScriptBlockExpressionAst scriptBlockExpressionAst))
            {
                return;
            }

            inferredTypes.AddRange(InferTypes(scriptBlockExpressionAst.ScriptBlock));
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

        object ICustomAstVisitor2.VisitTernaryExpression(TernaryExpressionAst ternaryExpressionAst)
        {
            return InferTypes(ternaryExpressionAst.IfTrue).Concat(InferTypes(ternaryExpressionAst.IfFalse));
        }

        object ICustomAstVisitor2.VisitPipelineChain(PipelineChainAst pipelineChainAst)
        {
            var types = new List<PSTypeName>();
            types.AddRange(InferTypes(pipelineChainAst.LhsPipelineChain));
            types.AddRange(InferTypes(pipelineChainAst.RhsPipeline));
            return GetArrayType(types);
        }

        private static CommandBaseAst GetPreviousPipelineCommand(CommandAst commandAst)
        {
            var pipe = (PipelineAst)commandAst.Parent;
            var i = pipe.PipelineElements.IndexOf(commandAst);
            return i != 0 ? pipe.PipelineElements[i - 1] : null;
        }
    }

    internal static class TypeInferenceExtension
    {
        public static bool EqualsOrdinalIgnoreCase(this string s, string t)
        {
            return string.Equals(s, t, StringComparison.OrdinalIgnoreCase);
        }

        public static IEnumerable<MethodInfo> GetGetterProperty(this Type type, string propertyName)
        {
            var res = new List<MethodInfo>();
            foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var name = m.Name;
                // Equals without string allocation
                if (name.Length == propertyName.Length + 4
                    && name.StartsWith("get_")
                    && name.IndexOf(propertyName, 4, StringComparison.Ordinal) == 4)
                {
                    res.Add(m);
                }
            }

            return res;
        }

        public static bool AstAssignsToSameVariable(this VariableExpressionAst variableAst, Ast ast)
        {
            var parameterAst = ast as ParameterAst;
            var variableAstVariablePath = variableAst.VariablePath;
            if (parameterAst != null)
            {
                return variableAstVariablePath.IsUnscopedVariable &&
                       parameterAst.Name.VariablePath.UnqualifiedPath.Equals(variableAstVariablePath.UnqualifiedPath, StringComparison.OrdinalIgnoreCase) &&
                       parameterAst.Parent.Parent.Extent.EndOffset > variableAst.Extent.StartOffset;
            }

            if (ast is ForEachStatementAst foreachAst)
            {
                return variableAstVariablePath.IsUnscopedVariable &&
                       foreachAst.Variable.VariablePath.UnqualifiedPath.Equals(variableAstVariablePath.UnqualifiedPath, StringComparison.OrdinalIgnoreCase);
            }

            if (ast is CommandAst commandAst)
            {
                string[] variableParameters = { "PV", "PipelineVariable", "OV", "OutVariable" };
                StaticBindingResult bindingResult = StaticParameterBinder.BindCommand(commandAst, false, variableParameters);

                if (bindingResult != null)
                {
                    foreach (string commandVariableParameter in variableParameters)
                    {
                        if (bindingResult.BoundParameters.TryGetValue(commandVariableParameter, out ParameterBindingResult parameterBindingResult))
                        {
                            if (string.Equals(variableAstVariablePath.UnqualifiedPath, (string)parameterBindingResult.ConstantValue, StringComparison.OrdinalIgnoreCase))
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
            if (lhs is ConvertExpressionAst convertExpr)
            {
                lhs = convertExpr.Child;
            }

            if (lhs is not VariableExpressionAst varExpr)
            {
                return false;
            }

            var candidateVarPath = varExpr.VariablePath;
            if (candidateVarPath.UserPath.Equals(variableAstVariablePath.UserPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // The following condition is making an assumption that at script scope, we didn't use $script:, but in the local scope, we did
            // If we are searching anything other than script scope, this is wrong.
            if (variableAstVariablePath.IsScript && variableAstVariablePath.UnqualifiedPath.Equals(candidateVarPath.UnqualifiedPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}
