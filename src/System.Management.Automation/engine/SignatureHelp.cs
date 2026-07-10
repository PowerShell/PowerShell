// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;

namespace System.Management.Automation.engine
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class SignatureHelp
    {
        /// <summary>
        /// 
        /// </summary>
        public InvokeMemberExpressionAst InvokeExpression { get; }

        /// <summary>
        /// 
        /// </summary>
        public MethodOverload[] Overloads { get; }

        private SignatureHelp(InvokeMemberExpressionAst invokeExpression, MethodOverload[] overloads)
        {
            InvokeExpression = invokeExpression;
            Overloads = overloads;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scriptText"></param>
        /// <param name="cursorPosition"></param>
        /// <returns></returns>
        public static SignatureHelp GetSignatureHelp(string scriptText, int cursorPosition)
        {
            ScriptBlockAst baseAst = Parser.ParseInput(scriptText, out _, out ParseError[] parseErrors);
            int incompleteInputOffset = -1;
            foreach (ParseError error in parseErrors)
            {
                if (error.Extent.EndOffset <= cursorPosition
                    && error.ErrorId.Equals(nameof(ParserStrings.MissingEndParenthesisInMethodCall), StringComparison.Ordinal))
                {
                    incompleteInputOffset = error.Extent.EndOffset;
                }
            }

            var signatureFinder = new SignatureAstFinder(cursorPosition, incompleteInputOffset);
            baseAst.Visit(signatureFinder);
            if (signatureFinder.foundAst is InvokeMemberExpressionAst invokeMemberExpression)
            {
                return GetSignatureHelpForMethod(invokeMemberExpression);
            }

            return null;
        }

        private static SignatureHelp GetSignatureHelpForMethod(InvokeMemberExpressionAst methodInvokeExpression)
        {
            if (methodInvokeExpression.Member is not ExpressionAst memberExpression)
            {
                // Should never happen because the only CommandElement that isn't also an Expression is a CommandParameter
                return null;
            }

            using (PowerShell powershell = PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                ExecutionContext context = LocalPipeline.GetExecutionContextFromTLS();

                if (!SafeExprEvaluator.TrySafeEval(memberExpression, context, out object memberValue)
                    || memberValue is not string inputMethodName
                    || string.IsNullOrEmpty(inputMethodName))
                {
                    // Can't determine the name of the method being called.
                    return null;
                }

                var methodOverloads = new List<MethodOverload>();
                if (SafeExprEvaluator.TrySafeEval(methodInvokeExpression.Expression, context, out object value))
                {
                    PSMemberInfoCollection<PSMemberInfo> members;
                    if (methodInvokeExpression.Static)
                    {
                        if (PSObject.Base(value) is not Type type)
                        {
                            // Trying to invoke a static method on something that isn't a type, which is not possible.
                            return null;
                        }

                        members = PSObject.DotNetStaticAdapter.BaseGetMembers<PSMemberInfo>(type);
                    }
                    else
                    {
                        members = PSObject.AsPSObject(value).Members;
                    }

                    foreach (var member in members)
                    {
                        if (!inputMethodName.EqualsOrdinalIgnoreCase(member.Name)
                            || member is not PSMethod method
                            || method.IsSpecial)
                        {
                            continue;
                        }

                        if (method.adapterData is DotNetAdapter.MethodCacheEntry cacheEntry)
                        {
                            methodOverloads.AddRange(GetMethodOverloadsFromDotNetCacheEntry(cacheEntry, member.Name));
                        }
                        else if (method.adapterData is ComMethod comMethod)
                        {
                            methodOverloads.AddRange(GetMethodOverloadsFromComMethod(comMethod));
                        }
                    }
                }

                if (methodOverloads.Count == 0)
                {
                    IList<PSTypeName> typesToGetMembersFrom;
                    bool isStatic = methodInvokeExpression.Static;
                    if (isStatic)
                    {
                        if (methodInvokeExpression.Expression is not TypeExpressionAst typeExpression)
                        {
                            // Safe eval couldn't figure out the type, and it's not a type expression
                            // So we have no idea what type to invoke static methods for.
                            return null;
                        }

                        typesToGetMembersFrom = new PSTypeName[] { new(typeExpression.TypeName) };
                    }
                    else
                    {
                        typesToGetMembersFrom = AstTypeInference.InferTypeOf(methodInvokeExpression.Expression, powershell);
                    }

                    var inferenceContext = new TypeInferenceContext(powershell);
                    foreach (PSTypeName typename in typesToGetMembersFrom)
                    {
                        IList<object> inferredMembers = inferenceContext.GetMembersByInferredType(typename, isStatic, filter: null);
                        foreach (object member in inferredMembers)
                        {
                            if (member is DotNetAdapter.MethodCacheEntry cacheEntry)
                            {
                                MethodBase methodBase = cacheEntry.methodInformationStructures[0].method;
                                string methodName = methodBase.IsConstructor ? "new" : methodBase.Name;
                                if (!inputMethodName.EqualsOrdinalIgnoreCase(methodName))
                                {
                                    continue;
                                }

                                methodOverloads.AddRange(GetMethodOverloadsFromDotNetCacheEntry(cacheEntry, methodName));
                            }
                            else if (member is CompilerGeneratedMemberFunctionAst constructorInfo)
                            {
                                // This is a default constructor generated when the class author hasn't defined one themself.
                                if (!inputMethodName.EqualsOrdinalIgnoreCase("new"))
                                {
                                    continue;
                                }

                                PSTypeName implementingType = new(constructorInfo.DefiningType);
                                methodOverloads.Add(new MethodOverload(
                                    implementingType,
                                    implementingType,
                                    methodName: "new",
                                    parameters: null,
                                    genericArgs: null
                                    ));
                            }
                            else if (member is FunctionMemberAst PsClassMethod)
                            {
                                string methodName = PsClassMethod.IsConstructor ? "new" : PsClassMethod.Name;
                                if (!inputMethodName.EqualsOrdinalIgnoreCase(methodName))
                                {
                                    continue;
                                }

                                PSTypeName implementingType = new((TypeDefinitionAst)PsClassMethod.Parent);
                                PSTypeName returnType;
                                if (PsClassMethod.IsConstructor)
                                {
                                    returnType = implementingType;
                                }
                                else if (PsClassMethod.ReturnType is null)
                                {
                                    returnType = new PSTypeName(typeof(void));
                                }
                                else
                                {
                                    returnType = new PSTypeName(PsClassMethod.ReturnType.TypeName);
                                }

                                MethodParameter[] parameters = GetMethodParametersFromParameterAsts(PsClassMethod.Parameters);
                                methodOverloads.Add(new MethodOverload(
                                    returnType,
                                    implementingType,
                                    methodName,
                                    parameters,
                                    genericArgs: null));
                            }
                        }
                    }
                }

                if (methodOverloads.Count != 0)
                {
                    return new SignatureHelp(methodInvokeExpression, methodOverloads.ToArray());
                }
            }

            return null;
        }

        private static IEnumerable<MethodOverload> GetMethodOverloadsFromDotNetCacheEntry(DotNetAdapter.MethodCacheEntry cacheEntry, string methodName)
        {
            foreach (MethodInformation methodInfo in cacheEntry.methodInformationStructures)
            {
                MethodBase methodBase = methodInfo.method;
                ParameterInfo[] methodParamsToProcess = methodBase.GetParameters();
                var methodParams = new MethodParameter[methodParamsToProcess.Length];
                for (int i = 0; i < methodParams.Length; i++)
                {
                    bool byRef = methodInfo.parameters[i].isByRef;
                    Type paramType = byRef
                        ? methodParamsToProcess[i].ParameterType.GetElementType()
                        : methodParamsToProcess[i].ParameterType;

                    bool isParams = false;
                    if (paramType.IsArray && i == methodParams.Length - 1)
                    {
                        var paramAttributes = paramType.GetCustomAttributes(typeof(ParamArrayAttribute), inherit: false);
                        if (paramAttributes is not null && paramAttributes.Length != 0)
                        {
                            isParams = true;
                        }
                    }

                    methodParams[i] = new MethodParameter(methodParamsToProcess[i].Name, new PSTypeName(paramType), byRef, isParams);
                }

                PSTypeName returnType;
                if (methodBase is MethodInfo methodEntry)
                {
                    returnType = new PSTypeName(methodEntry.ReturnType);
                }
                else if (methodBase is ConstructorInfo constructor)
                {
                    returnType = new PSTypeName(constructor.DeclaringType);
                }
                else
                {
                    // Should never happen because it should always be either a method or constructor
                    continue;
                }

                PSTypeName[] genericArguments;
                if (methodBase.IsGenericMethodDefinition || methodBase.IsGenericMethod)
                {
                    Type[] genericArgs = methodBase.GetGenericArguments();
                    genericArguments = new PSTypeName[genericArgs.Length];
                    for (int i = 0; i < genericArgs.Length; i++)
                    {
                        genericArguments[i] = new PSTypeName(genericArgs[i]);
                    }
                }
                else
                {
                    genericArguments = null;
                }

                var implementingType = new PSTypeName(methodBase.DeclaringType);
                yield return new MethodOverload(returnType, implementingType, methodName, methodParams, genericArguments);
            }
        }

        private static IEnumerable<MethodOverload> GetMethodOverloadsFromComMethod(ComMethod comMethod)
        {
            var data = comMethod.MethodDefinitionsAsTuples();
            foreach (var item in data)
            {
                var methods = new MethodParameter[item.Item3.Count];
                for (int i = 0; i < methods.Length; i++)
                {
                    methods[i] = new MethodParameter(
                        item.Item3[i].Item1,
                        item.Item3[i].Item2,
                        isRef: false,
                        isParams: false);
                }

                yield return new MethodOverload(
                    item.Item1,
                    implementingType: null, // Is there a way for me to get this for COM objects?
                    item.Item2,
                    methods,
                    genericArgs: null
                    );
            }
        }

        private static MethodParameter[] GetMethodParametersFromParameterAsts(IList<ParameterAst> parameters)
        {
            if (parameters is null || parameters.Count == 0)
            {
                return null;
            }

            var result = new MethodParameter[parameters.Count];
            for (int i = 0; i < result.Length; i++)
            {
                ParameterAst parameter = parameters[i];
                PSTypeName parameterType = null;
                if (parameter.Attributes is not null && parameter.Attributes.Count > 0)
                {
                    foreach (var attribute in parameter.Attributes)
                    {
                        if (attribute is TypeConstraintAst typeConstraint)
                        {
                            parameterType = new PSTypeName(typeConstraint.TypeName);
                            break;
                        }
                    }
                }

                parameterType ??= new PSTypeName(typeof(object));
                result[i] = new MethodParameter(
                    parameter.Name.VariablePath.UnqualifiedPath,
                    parameterType,
                    isRef: false,
                    isParams: false);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        public sealed class MethodOverload
        {
            /// <summary>
            /// 
            /// </summary>
            public PSTypeName ReturnType { get; }
            
            /// <summary>
            /// 
            /// </summary>
            public PSTypeName ImplementingType { get; }

            /// <summary>
            /// 
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 
            /// </summary>
            public MethodParameter[] Parameters { get; }

            /// <summary>
            /// 
            /// </summary>
            public PSTypeName[] GenericArguments { get; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="returnType"></param>
            /// <param name="implementingType"></param>
            /// <param name="methodName"></param>
            /// <param name="parameters"></param>
            /// <param name="genericArgs"></param>
            public MethodOverload(PSTypeName returnType, PSTypeName implementingType, string methodName, MethodParameter[] parameters, PSTypeName[] genericArgs)
            {
                ReturnType = returnType;
                ImplementingType = implementingType;
                Name = methodName;
                Parameters = parameters;
                GenericArguments = genericArgs;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public sealed class MethodParameter
        {
            /// <summary>
            /// 
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 
            /// </summary>
            public PSTypeName Type { get; }

            /// <summary>
            /// 
            /// </summary>
            public bool IsRef { get; }

            /// <summary>
            /// 
            /// </summary>
            public bool IsParams { get; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="name"></param>
            /// <param name="type"></param>
            /// <param name="isRef"></param>
            /// <param name="isParams"></param>
            public MethodParameter(string name, PSTypeName type, bool isRef, bool isParams)
            {
                Name = name;
                Type = type;
                IsRef = isRef;
                IsParams = isParams;
            }
        }

        private sealed class SignatureAstFinder : AstVisitor2
        {
            internal Ast foundAst;
            private readonly int cursorOffset;
            private readonly int incompleteInputOffset;

            public SignatureAstFinder(int cursorPosition, int incompleteInputOffset)
            {
                cursorOffset = cursorPosition;
                this.incompleteInputOffset = incompleteInputOffset;
            }

            public override AstVisitAction VisitInvokeMemberExpression(InvokeMemberExpressionAst methodCallAst)
            {
                IScriptExtent extent = methodCallAst.Extent;
                if (methodCallAst.Member.Extent.EndOffset < cursorOffset
                    && (extent.EndOffset > cursorOffset || (extent.EndOffset == cursorOffset && !extent.Text.EndsWith(')', StringComparison.Ordinal))
                    || (incompleteInputOffset < cursorOffset && incompleteInputOffset == extent.EndOffset && !extent.Text.EndsWith(')', StringComparison.Ordinal))))
                {
                    foundAst = methodCallAst;
                }

                return AstVisitAction.Continue;
            }

            public override AstVisitAction VisitAttribute(AttributeAst attributeAst)
            {
                IScriptExtent extent = attributeAst.Extent;
                if (extent.StartOffset < cursorOffset
                    && (extent.EndOffset > cursorOffset || (extent.EndOffset == cursorOffset && !extent.Text.EndsWith(']', StringComparison.Ordinal))))
                {
                    foundAst = attributeAst;
                }

                return AstVisitAction.Continue;
            }

            public override AstVisitAction DefaultVisit(Ast ast)
            {
                if (ast.Extent.StartOffset > cursorOffset)
                {
                    // When visiting do while/until statements, the condition will be visited before the statement block.
                    // The condition itself may not be interesting if it's after the cursor, but the statement block could be.
                    return ast is PipelineBaseAst && ast.Parent is DoUntilStatementAst or DoWhileStatementAst
                        ? AstVisitAction.SkipChildren
                        : AstVisitAction.StopVisit;
                }

                return AstVisitAction.Continue;
            }
        }
    }
}
