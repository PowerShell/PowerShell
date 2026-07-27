// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.PowerShell;

namespace System.Management.Automation.engine
{
    /// <summary>
    /// Class intended to be used by editors to get signature info from methods used in a script.
    /// </summary>
    public sealed class SignatureHelp
    {
        /// <summary>
        /// An array of the found signatures. This is never null or empty.
        /// </summary>
        public SignatureInformation[] Signatures { get; }

        /// <summary>
        /// An index for <see cref="Signatures"/> for the most likely signature of the analyzed statement.
        /// Will return -1 if the input doesn't seem to match any of the signatures.
        /// </summary>
        public int ActiveSignature { get; }

        private static readonly Dictionary<string, string> typeToDocFileTable = new();

        private static readonly char[] s_methodStartChars = new char[] { '(', '{' };

        private SignatureHelp(SignatureInformation[] signatures, int activeSignature)
        {
            Signatures = signatures;
            ActiveSignature = activeSignature;
        }

        /// <summary>
        /// Gets <see cref="SignatureHelp"/> from the provided script and curor position.
        /// </summary>
        /// <param name="scriptText">The script content that should be analyzed.</param>
        /// <param name="cursorPosition">The position of the cursor inside the script.</param>
        /// <param name="includeUnusableSignatures">Set to true to include all signatures.
        /// By default unusable signatures (ones that use pointer parameters) will not be shown.</param>
        /// <returns> Returns <see cref="SignatureHelp"/> if the cursor is within a valid type signature that can be analyzed.
        /// Otherwise it returns null.</returns>
        public static SignatureHelp GetSignatureHelp(string scriptText, int cursorPosition, bool includeUnusableSignatures = false)
        {
            ScriptBlockAst baseAst = Parser.ParseInput(scriptText, out Token[] parsedTokens, out ParseError[] parseErrors);
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
                int argumentIndex = GetCurrentArgIndex(invokeMemberExpression, parsedTokens, cursorPosition);
                return GetSignatureHelpForMethod(invokeMemberExpression, argumentIndex, includeUnusableSignatures);
            }

            return null;
        }

        /// <summary>
        /// Sets the folder where PowerShell gets type documentation from for use in the signaturehelp.
        /// </summary>
        /// <param name="path">The folder that contains the .XML files with the help documentation.</param>
        /// <param name="clearOldEntries">Set to true to remove old XML documentation references that were previously loaded.</param>
        public static void SetDotNetReferenceDir(string path, bool clearOldEntries)
        {
            if (clearOldEntries)
            {
                typeToDocFileTable.Clear();
            }

            DirectoryInfo docDir = new(path);
            foreach (FileInfo file in docDir.EnumerateFiles("*.xml"))
            {

                XDocument doc;
                try
                {
                    doc = XDocument.Load(file.FullName);
                }
                catch
                {
                    continue;
                }

                IEnumerable<XElement> members = doc.Root.Element("members")?.Elements("member");
                if (members is not null)
                {
                    foreach (var member in members)
                    {
                        string memberName = member.Attribute("name").Value;
                        if (memberName.StartsWith("T:"))
                        {
                            typeToDocFileTable[memberName] = file.FullName;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the index of the argument that the user is currently typing.
        /// </summary>
        /// <returns>
        /// The index of the argument the cursor is at or 0 if there's no arguments.
        /// The index may exceed the number of arguments if the cursor is after a trailing comma like: $Var.Method("Arg1", ^
        /// </returns>
        private static int GetCurrentArgIndex(InvokeMemberExpressionAst invokeMemberExpression, Token[] parsedTokens, int cursorPosition)
        {
            if (invokeMemberExpression.Arguments is null)
            {
                return 0;
            }

            ReadOnlyCollection<ExpressionAst> arguments = invokeMemberExpression.Arguments;
            for (int i = 0; i < arguments.Count; i++)
            {
                IScriptExtent currentArg = arguments[i].Extent;
                if ((currentArg.StartOffset <= cursorPosition && currentArg.EndOffset >= cursorPosition)
                    || (cursorPosition < currentArg.StartOffset))
                {
                    // Cursor is within, or before an argument like: $Var.Method( ^ "Arg1, "Arg2"
                    return i;
                }

                if (i + 1 == arguments.Count)
                {
                    // The cursor is past the last argument.
                    // Check for a trailing comma indicating that the user is entering the next parameter.
                    foreach (Token token in parsedTokens)
                    {
                        if (token.Extent.StartOffset < currentArg.EndOffset)
                        {
                            continue;
                        }

                        if (token.Extent.StartOffset > invokeMemberExpression.Extent.EndOffset)
                        {
                            return i;
                        }

                        if (token.Kind == TokenKind.Comma)
                        {
                            return i + 1;
                        }
                    }
                }

                IScriptExtent nextArg = arguments[i + 1].Extent;
                if (cursorPosition >= nextArg.StartOffset)
                {
                    continue;
                }

                // The cursor is in the whitespace between one of the arguments like this: $Var.Method("Arg1" ^ , "Arg2", "Arg3"
                // Need to check the tokens to see if we are before or after the comma.
                foreach (Token token in parsedTokens)
                {
                    if (token.Extent.StartOffset < currentArg.EndOffset)
                    {
                        continue;
                    }

                    if (token.Kind == TokenKind.Comma)
                    {
                        return token.Extent.StartOffset > cursorPosition ? i : i + 1;
                    }
                }
            }

            return 0;
        }

        private static SignatureDocumentation GetSignatureDocumentation(MethodBase methodBase)
        {
            Type declaringType = methodBase.DeclaringType;
            string typeName;
            MethodBase methodToGetDocsFor;
            if (declaringType.IsGenericType)
            {
                Type genericType = declaringType.GetGenericTypeDefinition();
                typeName = genericType.FullName;
                methodToGetDocsFor = MethodBase.GetMethodFromHandle(methodBase.MethodHandle, genericType.TypeHandle);
            }
            else
            {
                typeName = declaringType.FullName;
                methodToGetDocsFor = methodBase;
            }

            if (!typeToDocFileTable.TryGetValue($"T:{typeName}", out string docFilePath))
            {
                string assemblyLocation = declaringType.Assembly.Location;
                if (string.IsNullOrEmpty(assemblyLocation))
                {
                    return null;
                }

                docFilePath = Path.ChangeExtension(assemblyLocation, ".xml");
            }

            XDocument doc;
            try
            {
                doc = XDocument.Load(docFilePath);
            }
            catch
            {
                return null;
            }

            string lookupKey = GetMemberId(methodToGetDocsFor);

            var members = doc.Root.Element("members")?.Elements("member");
            if (members is not null)
            {
                foreach (var member in members)
                {
                    string memberName = member.Attribute("name")?.Value;
                    if (memberName.Equals(lookupKey, StringComparison.Ordinal))
                    {
                        Dictionary<string, string> parameterDocs = new(StringComparer.Ordinal);
                        IEnumerable<XElement> documentedParameters = member.Elements("param");
                        if (documentedParameters is not null)
                        {
                            foreach (XElement parameter in documentedParameters)
                            {
                                string parameterName = parameter.Attribute("name")?.Value;
                                if (string.IsNullOrEmpty(parameterName))
                                {
                                    continue;
                                }

                                parameterDocs[parameterName] = parameter.Value;
                            }
                        }

                        return new SignatureDocumentation(
                            summary: member.Element("summary")?.Value,
                            parameters: parameterDocs);
                    }
                }
            }

            return null;
        }

        private static SignatureHelp GetSignatureHelpForMethod(InvokeMemberExpressionAst methodInvokeExpression, int argumentIndex, bool includeUnusableSignatures)
        {
            if (methodInvokeExpression.Member is not ExpressionAst memberExpression)
            {
                // Should never happen because the only CommandElement that isn't also an Expression is a CommandParameter
                return null;
            }

            bool isStatic = methodInvokeExpression.Static;
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

                List<SignatureInformation> foundSignatures = [];
                if (SafeExprEvaluator.TrySafeEval(methodInvokeExpression.Expression, context, out object value))
                {
                    PSMemberInfoCollection<PSMemberInfo> members;
                    if (isStatic)
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
                            foundSignatures.AddRange(GetSignaturesFromDotNetCacheEntry(cacheEntry, member.Name, includeUnusableSignatures));
                        }
                        else if (method.adapterData is ComMethod comMethod)
                        {
                            foundSignatures.AddRange(comMethod.MethodDefinitionsAsSignatureInformation());
                        }
                    }
                }

                if (foundSignatures.Count == 0)
                {
                    IList<PSTypeName> typesToGetMembersFrom;
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

                                foundSignatures.AddRange(GetSignaturesFromDotNetCacheEntry(cacheEntry, methodName, includeUnusableSignatures));
                            }
                            else if (member is CompilerGeneratedMemberFunctionAst constructorInfo)
                            {
                                // This is a default constructor generated when the class author hasn't defined one themself.
                                if (!inputMethodName.EqualsOrdinalIgnoreCase("new"))
                                {
                                    continue;
                                }

                                foundSignatures.Add(new SignatureInformation(signature: $"{constructorInfo.DefiningType.Name} new()", parameters: []));
                            }
                            else if (member is FunctionMemberAst psClassMethod)
                            {
                                string methodName = psClassMethod.IsConstructor ? "new" : psClassMethod.Name;
                                if (!inputMethodName.EqualsOrdinalIgnoreCase(methodName))
                                {
                                    continue;
                                }

                                foundSignatures.Add(GetSignatureFromPsClassMethod(psClassMethod));
                            }
                        }
                    }
                }

                if (foundSignatures.Count != 0)
                {
                    return GetSignatureHelpFromFoundSignatures(ref foundSignatures, methodInvokeExpression, argumentIndex, powershell);
                }
            }

            return null;
        }

        private static SignatureHelp GetSignatureHelpFromFoundSignatures(
            ref List<SignatureInformation> foundSignatures,
            InvokeMemberExpressionAst invokeExpression,
            int argumentIndex,
            PowerShell powerShell)
        {
            int inputArgCount;
            IList<PSTypeName>[] argumentInputTypes;
            if (invokeExpression.Arguments is not null)
            {
                inputArgCount = argumentIndex == invokeExpression.Arguments.Count
                    ? argumentIndex + 1
                    : invokeExpression.Arguments.Count;
                argumentInputTypes = new IList<PSTypeName>[invokeExpression.Arguments.Count];
                for (int i = 0; i < argumentInputTypes.Length; i++)
                {
                    argumentInputTypes[i] = AstTypeInference.InferTypeOf(invokeExpression.Arguments[i], powerShell);
                }
            }
            else
            {
                inputArgCount = 0;
                argumentInputTypes = null;
            }

            int activeSignature = -1;
            for (int i = 0; i < foundSignatures.Count; i++)
            {
                if (foundSignatures[i].Parameters.Length == 0)
                {
                    foundSignatures[i].ActiveParameter = -1;
                }
                else if (argumentIndex >= foundSignatures[i].Parameters.Length)
                {
                    foundSignatures[i].ActiveParameter = foundSignatures[i].Parameters[^1].IsParams
                        ? foundSignatures[i].Parameters.Length - 1
                        : -1;
                }
                else
                {
                    foundSignatures[i].ActiveParameter = argumentIndex;
                }

                if (activeSignature != -1)
                {
                    continue;
                }

                if (inputArgCount == 0)
                {
                    if (foundSignatures[i].Parameters.Length == 0)
                    {
                        activeSignature = i;
                    }

                    continue;
                }

                if (inputArgCount > foundSignatures[i].Parameters.Length
                    && !foundSignatures[i].Parameters[^1].IsParams)
                {
                    continue;
                }

                for (int j = 0; j < foundSignatures[i].Parameters.Length; j++)
                {
                    if (foundSignatures[i].Parameters[j].IsPointer)
                    {
                        goto nextSignature;
                    }

                    if (argumentInputTypes.Length > j)
                    {
                        PSTypeName paramType = foundSignatures[i].Parameters[j].Type;
                        Type realParamType = paramType.Type;
                        bool typeMatched = false;
                        foreach (PSTypeName inferredType in argumentInputTypes[j])
                        {
                            if (realParamType is null)
                            {
                                // No type info loaded. Can only compare by name.
                                if (string.Equals(inferredType.Name, paramType.Name, StringComparison.OrdinalIgnoreCase))
                                {
                                    typeMatched = true;
                                    break;
                                }
                            }
                            else
                            {
                                Type realInferredType = inferredType.Type;
                                if (realInferredType is null)
                                {
                                    continue;
                                }

                                if (realParamType.IsAssignableFrom(realInferredType)
                                    || (realParamType.IsArray && realParamType.GetElementType().IsAssignableFrom(realInferredType))
                                    || (foundSignatures[i].Parameters[j].IsRef && realInferredType == typeof(PSReference)))
                                {
                                    typeMatched = true;
                                    break;
                                }
                            }
                        }

                        if (!typeMatched)
                        {
                            goto nextSignature;
                        }
                    }
                }

                activeSignature = i;

            nextSignature:
                ;
            }

            return new SignatureHelp(foundSignatures.ToArray(), activeSignature);
        }

        private static List<SignatureInformation> GetSignaturesFromDotNetCacheEntry(DotNetAdapter.MethodCacheEntry cacheEntry, string methodName, bool includeSigsWithPointers)
        {
            List<SignatureInformation> outputList = new(cacheEntry.methodInformationStructures.Length);
            StringBuilder signatureBuilder = new();
            foreach (MethodInformation methodInfo in cacheEntry.methodInformationStructures)
            {
                signatureBuilder.Length = 0;

                MethodBase methodBase = methodInfo.method;
                if (methodBase.IsStatic)
                {
                    _ = signatureBuilder.Append("static ");
                }

                if (methodBase is MethodInfo methodEntry)
                {
                    _ = signatureBuilder.Append(ToStringCodeMethods.Type(methodEntry.ReturnType));
                    _ = signatureBuilder.Append(' ');
                }
                else if (methodBase is ConstructorInfo constructor)
                {
                    _ = signatureBuilder.Append(ToStringCodeMethods.Type(constructor.DeclaringType));
                    _ = signatureBuilder.Append(' ');
                }

                if (methodBase.DeclaringType.IsInterface)
                {
                    _ = signatureBuilder.Append(ToStringCodeMethods.Type(methodBase.DeclaringType, dropNamespaces: true));
                    _ = signatureBuilder.Append('.');
                }

                _ = signatureBuilder.Append(methodName);
                if (methodBase.IsGenericMethodDefinition || methodBase.IsGenericMethod)
                {
                    _ = signatureBuilder.Append('[');
                    Type[] genericArgs = methodBase.GetGenericArguments();
                    for (int i = 0; i < genericArgs.Length; i++)
                    {
                        if (i > 0)
                        {
                            _ = signatureBuilder.Append(", ");
                        }

                        _ = signatureBuilder.Append(ToStringCodeMethods.Type(genericArgs[i]));
                    }

                    _ = signatureBuilder.Append(']');
                }

                SignatureDocumentation signatureDocumentation = GetSignatureDocumentation(methodBase);
                _ = signatureBuilder.Append('(');
                ParameterInfo[] methodParamsToProcess = methodBase.GetParameters();
                var methodParams = new ParameterInformation[methodParamsToProcess.Length];
                for (int i = 0; i < methodParams.Length; i++)
                {
                    int paramStartOffset = signatureBuilder.Length;
                    bool byRef = methodInfo.parameters[i].isByRef;
                    Type paramType = byRef
                        ? methodParamsToProcess[i].ParameterType.GetElementType()
                        : methodParamsToProcess[i].ParameterType;

                    if (!includeSigsWithPointers && paramType.IsPointer)
                    {
                        goto nextSignature;
                    }

                    if (byRef)
                    {
                        _ = signatureBuilder.Append("[ref] ");
                    }

                    bool isParams = false;
                    if (paramType.IsArray && i == methodParams.Length - 1)
                    {
                        var paramAttributes = paramType.GetCustomAttributes(typeof(ParamArrayAttribute), inherit: false);
                        if (paramAttributes is not null && paramAttributes.Length != 0)
                        {
                            isParams = true;
                            _ = signatureBuilder.Append("Params ");
                        }
                    }

                    _ = signatureBuilder.Append(ToStringCodeMethods.Type(paramType));
                    _ = signatureBuilder.Append(' ');
                    _ = signatureBuilder.Append(methodParamsToProcess[i].Name);

                    bool hasDefaultValue = methodParamsToProcess[i].HasDefaultValue;
                    if (hasDefaultValue)
                    {
                        _ = signatureBuilder.Append(" = ");
                        _ = signatureBuilder.Append(DotNetAdapter.GetDefaultValueStringRepresentation(methodParamsToProcess[i]));
                    }

                    string parameterDocumentation = signatureDocumentation?.Parameters.TryGetValue(methodParamsToProcess[i].Name, out string doc) == true
                        ? doc
                        : null;

                    methodParams[i] = new ParameterInformation(
                        new PSTypeName(paramType),
                        paramStartOffset,
                        signatureLength: signatureBuilder.Length - paramStartOffset,
                        documentationText: parameterDocumentation,
                        byRef,
                        isParams,
                        paramType.IsPointer,
                        hasDefaultValue);

                    if (i < methodParams.Length - 1)
                    {
                        _ = signatureBuilder.Append(", ");
                    }
                }

                _ = signatureBuilder.Append(')');

                outputList.Add(new SignatureInformation(
                    signatureBuilder.ToString(),
                    methodParams,
                    documentationText: signatureDocumentation?.Summary
                    ));
            
            nextSignature:
                ;
            }

            return outputList;
        }

        private static SignatureInformation GetSignatureFromPsClassMethod(FunctionMemberAst psClassMethod)
        {
            PSTypeName implementingType = new((TypeDefinitionAst)psClassMethod.Parent);
            StringBuilder signatureBuilder = new();
            if (psClassMethod.IsStatic)
            {
                _ = signatureBuilder.Append("static ");
            }

            if (psClassMethod.IsConstructor)
            {
                _ = signatureBuilder.Append($"{implementingType.Name} ");
            }
            else
            {
                string returnTypeString;
                if (psClassMethod.ReturnType is null)
                {
                    returnTypeString = ToStringCodeMethods.Type(typeof(void));
                }
                else
                {
                    ITypeName typeName = psClassMethod.ReturnType.TypeName;
                    Type reflectionType = typeName.GetReflectionType();
                    returnTypeString = reflectionType is null
                        ? typeName.Name
                        : ToStringCodeMethods.Type(reflectionType);
                }

                _ = signatureBuilder.Append(returnTypeString);
                _ = signatureBuilder.Append(' ');
            }

            _ = signatureBuilder.Append('(');
            var methodParams = new ParameterInformation[psClassMethod.Parameters.Count];
            for (int i = 0; i < methodParams.Length; i++)
            {
                int paramStartOffset = signatureBuilder.Length;
                ParameterAst parameter = psClassMethod.Parameters[i];
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
                Type reflectionType = parameterType.Type;
                string parameterTypeString = reflectionType is null
                    ? parameterType.Name
                    : ToStringCodeMethods.Type(reflectionType);
                _ = signatureBuilder.Append(parameterTypeString);
                _ = signatureBuilder.Append(' ');
                _ = signatureBuilder.Append(parameter.Name.VariablePath.UnqualifiedPath);

                methodParams[i] = new ParameterInformation(
                    parameterType,
                    signatureStartOffset: paramStartOffset,
                    signatureLength: signatureBuilder.Length - paramStartOffset);

                _ = signatureBuilder.Append(", ");
            }

            if (methodParams.Length > 0)
            {
                // Remove trailing ", " from parameters
                signatureBuilder.Length -= 2;
            }

            _ = signatureBuilder.Append(')');

            return new SignatureInformation(signatureBuilder.ToString(), methodParams);
        }

        /// <summary>
        /// Represents a signature for <see cref="SignatureHelp"/>
        /// </summary>
        public sealed class SignatureInformation
        {
            /// <summary>
            /// A string representation of a method and its parameters.
            /// </summary>
            public string SignatureString { get; }

            /// <summary>
            /// Help text for the signature. May be null if no documentation is available.
            /// </summary>
            public string Documentation { get; }

            /// <summary>
            /// The parameters for this signature. This is never null.
            /// </summary>
            public ParameterInformation[] Parameters { get; }

            /// <summary>
            /// An index for <see cref="Parameters"/> that represents the parameter that the cursor is at.
            /// </summary>
            public int ActiveParameter { get; internal set; } = -1;

            internal SignatureInformation(string signature, ParameterInformation[] parameters, string documentationText = null)
            {
                SignatureString = signature;
                Documentation = documentationText;
                Parameters = parameters;
            }

            /// <summary>
            /// Returns <see cref="SignatureString"/>
            /// </summary>
            public override string ToString()
            {
                return SignatureString;
            }
        }

        /// <summary>
        /// Represents a parameter for <see cref="SignatureInformation"/>
        /// </summary>
        public sealed class ParameterInformation
        {
            /// <summary>
            /// The index within <see cref="SignatureInformation.SignatureString"/> where this parameter definition starts.
            /// </summary>
            public int SignatureStartOffset { get; }

            /// <summary>
            /// The length of the parameter definition within <see cref="SignatureInformation.SignatureString"/>.
            /// Can be used in combination with <see cref="SignatureStartOffset"/> to get a string representation of the parameter definition.
            /// </summary>
            public int SignatureLength { get; }

            /// <summary>
            /// Help text for the parameter. May be null if no documentation is available.
            /// </summary>
            public string Documentation { get; }

            internal PSTypeName Type { get; }

            internal bool IsRef { get; }

            internal bool IsParams { get; }

            internal bool IsPointer { get; }

            internal bool HasDefaultValue { get; }

            internal ParameterInformation(
                PSTypeName type,
                int signatureStartOffset,
                int signatureLength,
                string documentationText = null,
                bool isRef = false,
                bool isParams = false,
                bool isPointer = false,
                bool hasDefaultValue = false)
            {
                SignatureStartOffset = signatureStartOffset;
                SignatureLength = signatureLength;
                Documentation = documentationText;
                Type = type;
                IsRef = isRef;
                IsParams = isParams;
                IsPointer = isPointer;
                HasDefaultValue = hasDefaultValue;
            }

            /// <summary>
            /// Returns the type name for the parameter.
            /// </summary>
            public override string ToString()
            {
                return Type.Name;
            }
        }

        private sealed class SignatureDocumentation
        {
            public string Summary { get; }

            public Dictionary<string, string> Parameters { get; }

            public SignatureDocumentation(string summary, Dictionary<string, string> parameters)
            {
                Summary = summary;
                Parameters = parameters;
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
                int argumentStartOffset;
                if (methodCallAst.GenericTypeArguments is null || methodCallAst.GenericTypeArguments.Count == 0)
                {
                    argumentStartOffset = methodCallAst.Member.Extent.EndOffset + 1;
                }
                else
                {
                    argumentStartOffset = methodCallAst.GenericTypeArguments[^1].Extent.EndOffset;
                    int parenStart = methodCallAst.Extent.Text.IndexOfAny(s_methodStartChars, argumentStartOffset - methodCallAst.Extent.StartOffset);
                    argumentStartOffset = methodCallAst.Extent.StartOffset + parenStart + 1;
                }

                IScriptExtent extent = methodCallAst.Extent;
                if (argumentStartOffset <= cursorOffset
                    && (extent.EndOffset > cursorOffset || (extent.EndOffset == cursorOffset && !extent.Text.EndsWith(')', StringComparison.Ordinal))
                    || (incompleteInputOffset < cursorOffset && incompleteInputOffset == extent.EndOffset && !extent.Text.EndsWith(')', StringComparison.Ordinal))))
                {
                    foundAst = methodCallAst;
                }

                return AstVisitAction.Continue;
            }

            //public override AstVisitAction VisitAttribute(AttributeAst attributeAst)
            //{
            //    IScriptExtent extent = attributeAst.Extent;
            //    if (extent.StartOffset < cursorOffset
            //        && (extent.EndOffset > cursorOffset || (extent.EndOffset == cursorOffset && !extent.Text.EndsWith(']', StringComparison.Ordinal))))
            //    {
            //        foundAst = attributeAst;
            //    }

            //    return AstVisitAction.Continue;
            //}

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

        private static string GetMemberId(MethodBase method)
        {
            var sb = new StringBuilder("M:");

            _ = sb.Append(GetTypeName(method.DeclaringType));
            _ = sb.Append('.');

            if (method.IsConstructor)
            {
                _ = sb.Append(method.IsStatic ? "#cctor" : "#ctor");
            }
            else
            {
                _ = sb.Append(method.Name);
            }

            // Generic method arity (open generic method -> ``N)
            if (method.IsGenericMethod)
            {
                _ = sb.Append("``").Append(method.GetGenericArguments().Length);
            }

            var parameters = method.GetParameters();
            if (parameters.Length > 0)
            {
                _ = sb.Append('(');
                _ = sb.Append(string.Join(",", parameters.Select(p => GetParamTypeName(p.ParameterType))));
                _ = sb.Append(')');
            }

            return sb.ToString();
        }

        private static string GetTypeName(Type type)
        {
            // Use generic type definition so open params show as `0, `1, etc.
            var t = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            return GetRawFullName(t);
        }

        private static string GetRawFullName(Type type)
        {
            string name = type.FullName ?? $"{type.Namespace}.{type.Name}";
            return name.Replace('+', '.'); // nested types
        }

        private static string GetParamTypeName(Type type)
        {
            if (type.IsByRef)
                return GetParamTypeName(type.GetElementType()) + "@";

            if (type.IsPointer)
                return GetParamTypeName(type.GetElementType()) + "*";

            if (type.IsArray)
            {
                int rank = type.GetArrayRank();
                string elem = GetParamTypeName(type.GetElementType());
                return rank == 1
                    ? elem + "[]"
                    : elem + "[" + string.Join(",", Enumerable.Repeat("0:", rank)) + "]";
            }

            if (type.IsGenericParameter)
            {
                return type.DeclaringMethod != null
                    ? "``" + type.GenericParameterPosition   // method-level generic param
                    : "`" + type.GenericParameterPosition;   // type-level generic param
            }

            if (type.IsGenericType)
            {
                var def = type.GetGenericTypeDefinition();
                string baseName = StripGenericArity(GetRawFullName(def));
                string args = string.Join(",", type.GetGenericArguments().Select(GetParamTypeName));
                return baseName + "{" + args + "}";
            }

            return GetRawFullName(type);
        }

        private static string StripGenericArity(string typeName)
        {
            int backtickIndex = typeName.LastIndexOf('`');
            return backtickIndex >= 0 ? typeName.Substring(0, backtickIndex) : typeName;
        }
    }
}
