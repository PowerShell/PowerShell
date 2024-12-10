// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Internal;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using Microsoft.PowerShell;

namespace System.Management.Automation.Language
{
    internal static class TypeDefiner
    {
        internal const string DynamicClassAssemblyName = "PowerShell Class Assembly";
        internal const string DynamicClassAssemblyFullNamePrefix = "PowerShell Class Assembly,";

        private static int s_globalCounter = 0;

        private static readonly CustomAttributeBuilder s_hiddenCustomAttributeBuilder =
            new CustomAttributeBuilder(typeof(HiddenAttribute).GetConstructor(Type.EmptyTypes), Array.Empty<object>());

        private static readonly string s_sessionStateKeeperFieldName = "__sessionStateKeeper";
        internal static readonly string SessionStateFieldName = "__sessionState";

        private static readonly MethodInfo s_sessionStateKeeper_GetSessionState =
            typeof(SessionStateKeeper).GetMethod("GetSessionState", BindingFlags.Instance | BindingFlags.Public);

        private static bool TryConvertArg(object arg, Type type, out object result, Parser parser, IScriptExtent errorExtent)
        {
            // This code could be added to LanguagePrimitives.ConvertTo
            if (arg != null && arg.GetType() == type)
            {
                result = arg;
                return true;
            }

            if (!LanguagePrimitives.TryConvertTo(arg, type, out result))
            {
                parser.ReportError(errorExtent,
                    nameof(ParserStrings.CannotConvertValue),
                    ParserStrings.CannotConvertValue,
                    ToStringCodeMethods.Type(type));
                return false;
            }

            return true;
        }

        private static CustomAttributeBuilder GetAttributeBuilder(Parser parser, AttributeAst attributeAst, AttributeTargets attributeTargets)
        {
            var attributeType = attributeAst.TypeName.GetReflectionAttributeType();
            Diagnostics.Assert(attributeType != null, "Semantic checks should have verified attribute type exists");

            Diagnostics.Assert(
                attributeType.GetCustomAttribute<AttributeUsageAttribute>(true) == null ||
                (attributeType.GetCustomAttribute<AttributeUsageAttribute>(true).ValidOn & attributeTargets) != 0, "Semantic checks should have verified attribute usage");

            var positionalArgs = new object[attributeAst.PositionalArguments.Count];
            var cvv = new ConstantValueVisitor { AttributeArgument = false };
            for (var i = 0; i < attributeAst.PositionalArguments.Count; i++)
            {
                var posArg = attributeAst.PositionalArguments[i];
                positionalArgs[i] = posArg.Accept(cvv);
            }

            var ctorInfos = attributeType.GetConstructors();
            var newConstructors = DotNetAdapter.GetMethodInformationArray(ctorInfos);

            string errorId = null;
            string errorMsg = null;
            bool expandParamsOnBest;
            bool callNonVirtually;
            var positionalArgCount = positionalArgs.Length;

            var bestMethod = Adapter.FindBestMethod(
                newConstructors,
                invocationConstraints: null,
                allowCastingToByRefLikeType: false,
                positionalArgs,
                ref errorId,
                ref errorMsg,
                out expandParamsOnBest,
                out callNonVirtually);

            if (bestMethod == null)
            {
                parser.ReportError(new ParseError(attributeAst.Extent, errorId,
                    string.Format(CultureInfo.InvariantCulture, errorMsg, attributeType.Name, attributeAst.PositionalArguments.Count)));
                return null;
            }

            var constructorInfo = (ConstructorInfo)bestMethod.method;

            var parameterInfo = constructorInfo.GetParameters();
            var ctorArgs = new object[parameterInfo.Length];
            object arg;
            for (var argIndex = 0; argIndex < parameterInfo.Length; ++argIndex)
            {
                var resultType = parameterInfo[argIndex].ParameterType;

                // The extension method 'CustomAttributeExtensions.GetCustomAttributes(ParameterInfo, Type, Boolean)' has inconsistent
                // behavior on its return value in both FullCLR and CoreCLR. According to MSDN, if the attribute cannot be found, it
                // should return an empty collection. However, it returns null in some rare cases [when the parameter isn't backed by
                // actual metadata].
                // This inconsistent behavior affects OneCore powershell because we are using the extension method here when compiling
                // against CoreCLR. So we need to add a null check until this is fixed in CLR.
                var paramArrayAttrs = parameterInfo[argIndex].GetCustomAttributes(typeof(ParamArrayAttribute), true);
                if (paramArrayAttrs != null && paramArrayAttrs.Length > 0 && expandParamsOnBest)
                {
                    var elementType = parameterInfo[argIndex].ParameterType.GetElementType();
                    var paramsArray = Array.CreateInstance(elementType, positionalArgCount - argIndex);
                    ctorArgs[argIndex] = paramsArray;

                    for (var i = 0; i < paramsArray.Length; ++i, ++argIndex)
                    {
                        if (!TryConvertArg(positionalArgs[argIndex], elementType, out arg,
                            parser, attributeAst.PositionalArguments[argIndex].Extent))
                        {
                            return null;
                        }

                        paramsArray.SetValue(arg, i);
                    }

                    break;
                }

                if (!TryConvertArg(positionalArgs[argIndex], resultType, out arg,
                    parser, attributeAst.PositionalArguments[argIndex].Extent))
                {
                    return null;
                }

                ctorArgs[argIndex] = arg;
            }

            if (attributeAst.NamedArguments.Count == 0)
            {
                return new CustomAttributeBuilder(constructorInfo, ctorArgs);
            }

            var propertyInfoList = new List<PropertyInfo>();
            var propertyArgs = new List<object>();
            var fieldInfoList = new List<FieldInfo>();
            var fieldArgs = new List<object>();
            foreach (var namedArg in attributeAst.NamedArguments)
            {
                var name = namedArg.ArgumentName;
                var members = attributeType.GetMember(name, MemberTypes.Field | MemberTypes.Property,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                Diagnostics.Assert(members.Length == 1 && (members[0] is PropertyInfo || members[0] is FieldInfo),
                    "Semantic checks should have ensured names attribute argument exists");

                arg = namedArg.Argument.Accept(cvv);

                var propertyInfo = members[0] as PropertyInfo;
                if (propertyInfo != null)
                {
                    Diagnostics.Assert(propertyInfo.GetSetMethod() != null, "Semantic checks ensures property is settable");

                    if (!TryConvertArg(arg, propertyInfo.PropertyType, out arg, parser, namedArg.Argument.Extent))
                    {
                        return null;
                    }

                    propertyInfoList.Add(propertyInfo);
                    propertyArgs.Add(arg);
                    continue;
                }

                var fieldInfo = (FieldInfo)members[0];
                Diagnostics.Assert(!fieldInfo.IsInitOnly && !fieldInfo.IsLiteral, "Semantic checks ensures field is settable");

                if (!TryConvertArg(arg, fieldInfo.FieldType, out arg, parser, namedArg.Argument.Extent))
                {
                    return null;
                }

                fieldInfoList.Add(fieldInfo);
                fieldArgs.Add(arg);
            }

            return new CustomAttributeBuilder(constructorInfo, ctorArgs,
                propertyInfoList.ToArray(), propertyArgs.ToArray(),
                fieldInfoList.ToArray(), fieldArgs.ToArray());
        }

        internal static void DefineCustomAttributes(TypeBuilder member, ReadOnlyCollection<AttributeAst> attributes, Parser parser, AttributeTargets attributeTargets)
        {
            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    var cabuilder = GetAttributeBuilder(parser, attr, attributeTargets);
                    if (cabuilder != null)
                    {
                        member.SetCustomAttribute(cabuilder);
                    }
                }
            }
        }

        internal static void DefineCustomAttributes(PropertyBuilder member, ReadOnlyCollection<AttributeAst> attributes, Parser parser, AttributeTargets attributeTargets)
        {
            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    var cabuilder = GetAttributeBuilder(parser, attr, attributeTargets);
                    if (cabuilder != null)
                    {
                        member.SetCustomAttribute(cabuilder);
                    }
                }
            }
        }

        internal static void DefineCustomAttributes(ConstructorBuilder member, ReadOnlyCollection<AttributeAst> attributes, Parser parser, AttributeTargets attributeTargets)
        {
            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    var cabuilder = GetAttributeBuilder(parser, attr, attributeTargets);
                    if (cabuilder != null)
                    {
                        member.SetCustomAttribute(cabuilder);
                    }
                }
            }
        }

        internal static void DefineCustomAttributes(MethodBuilder member, ReadOnlyCollection<AttributeAst> attributes, Parser parser, AttributeTargets attributeTargets)
        {
            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    var cabuilder = GetAttributeBuilder(parser, attr, attributeTargets);
                    if (cabuilder != null)
                    {
                        member.SetCustomAttribute(cabuilder);
                    }
                }
            }
        }

        internal static void DefineCustomAttributes(EnumBuilder member, ReadOnlyCollection<AttributeAst> attributes, Parser parser, AttributeTargets attributeTargets)
        {
            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    var cabuilder = GetAttributeBuilder(parser, attr, attributeTargets);
                    if (cabuilder != null)
                    {
                        member.SetCustomAttribute(cabuilder);
                    }
                }
            }
        }

        private sealed class DefineTypeHelper
        {
            private readonly Parser _parser;
            internal readonly TypeDefinitionAst _typeDefinitionAst;
            internal readonly TypeBuilder _typeBuilder;
            internal readonly FieldBuilder _sessionStateField;
            internal readonly FieldBuilder _sessionStateKeeperField;
            internal readonly ModuleBuilder _moduleBuilder;
            internal readonly TypeBuilder _staticHelpersTypeBuilder;
            private readonly Dictionary<string, PropertyMemberAst> _definedProperties;
            private readonly Dictionary<string, List<Tuple<FunctionMemberAst, Type[]>>> _definedMethods;
            private HashSet<Type> _interfaces;
            private Dictionary<Tuple<string, Type>, PropertyInfo> _abstractProperties;
            private Dictionary<Tuple<string, Type, string>, MethodInfo> _interfaceMethods;
            internal readonly List<(string fieldName, IParameterMetadataProvider bodyAst, bool isStatic)> _fieldsToInitForMemberFunctions;
            private bool _baseClassHasDefaultCtor;

            /// <summary>
            /// If type has fatal errors we cannot construct .NET type from it.
            /// TypeBuilder.CreateTypeInfo() would throw exception.
            /// </summary>
            public bool HasFatalErrors { get; private set; }

            public DefineTypeHelper(Parser parser, ModuleBuilder module, TypeDefinitionAst typeDefinitionAst, string typeName)
            {
                _moduleBuilder = module;
                _parser = parser;
                _typeDefinitionAst = typeDefinitionAst;

                List<Type> interfaces;
                var baseClass = this.GetBaseTypes(parser, typeDefinitionAst, out interfaces);

                _typeBuilder = module.DefineType(typeName, Reflection.TypeAttributes.Class | Reflection.TypeAttributes.Public, baseClass, interfaces.ToArray());
                _staticHelpersTypeBuilder = module.DefineType(string.Create(CultureInfo.InvariantCulture, $"{typeName}_<staticHelpers>"), Reflection.TypeAttributes.Class);
                DefineCustomAttributes(_typeBuilder, typeDefinitionAst.Attributes, _parser, AttributeTargets.Class);
                _typeDefinitionAst.Type = _typeBuilder;

                _fieldsToInitForMemberFunctions = new List<(string, IParameterMetadataProvider, bool)>();
                _definedMethods = new Dictionary<string, List<Tuple<FunctionMemberAst, Type[]>>>(StringComparer.OrdinalIgnoreCase);
                _definedProperties = new Dictionary<string, PropertyMemberAst>(StringComparer.OrdinalIgnoreCase);

                _sessionStateField = _typeBuilder.DefineField(SessionStateFieldName, typeof(SessionStateInternal), FieldAttributes.Private);
                _sessionStateKeeperField = _staticHelpersTypeBuilder.DefineField(s_sessionStateKeeperFieldName, typeof(SessionStateKeeper), FieldAttributes.Assembly | FieldAttributes.Static);
            }

            /// <summary>
            /// Return base class type, never return null.
            /// </summary>
            /// <param name="parser"></param>
            /// <param name="typeDefinitionAst"></param>
            /// <param name="interfaces">Return declared interfaces.</param>
            /// <returns></returns>
            private Type GetBaseTypes(Parser parser, TypeDefinitionAst typeDefinitionAst, out List<Type> interfaces)
            {
                // Define base types and report errors.
                Type baseClass = null;
                interfaces = new List<Type>();

                // Default base class is System.Object and it has a default ctor.
                _baseClassHasDefaultCtor = true;
                if (typeDefinitionAst.BaseTypes.Count > 0)
                {
                    // base class
                    var baseTypeAsts = typeDefinitionAst.BaseTypes;
                    var firstBaseTypeAst = baseTypeAsts[0];

                    if (firstBaseTypeAst.TypeName.IsArray)
                    {
                        parser.ReportError(firstBaseTypeAst.Extent,
                            nameof(ParserStrings.SubtypeArray),
                            ParserStrings.SubtypeArray,
                            firstBaseTypeAst.TypeName.FullName);
                        // fall to the default base type
                    }
                    else
                    {
                        baseClass = firstBaseTypeAst.TypeName.GetReflectionType();
                        if (baseClass == null)
                        {
                            parser.ReportError(firstBaseTypeAst.Extent,
                                nameof(ParserStrings.TypeNotFound),
                                ParserStrings.TypeNotFound,
                                firstBaseTypeAst.TypeName.FullName);
                            // fall to the default base type
                        }
                        else
                        {
                            if (baseClass.IsSealed)
                            {
                                parser.ReportError(firstBaseTypeAst.Extent,
                                    nameof(ParserStrings.SealedBaseClass),
                                    ParserStrings.SealedBaseClass,
                                    baseClass.Name);
                                // ignore base type if it's sealed.
                                baseClass = null;
                            }
                            else if (baseClass.IsGenericType && !baseClass.IsConstructedGenericType)
                            {
                                parser.ReportError(firstBaseTypeAst.Extent,
                                    nameof(ParserStrings.SubtypeUnclosedGeneric),
                                    ParserStrings.SubtypeUnclosedGeneric,
                                    baseClass.Name);
                                // ignore base type, we cannot inherit from unclosed generic.
                                baseClass = null;
                            }
                            else if (baseClass.IsInterface)
                            {
                                // First Ast can represent interface as well as BaseClass.
                                interfaces.Add(baseClass);
                                baseClass = null;
                            }
                        }
                    }

                    if (baseClass != null)
                    {
                        // All PS classes are TypeName instances.
                        // For PS classes we cannot use reflection API, because type is not created yet.
                        var baseTypeName = firstBaseTypeAst.TypeName as TypeName;
                        if (baseTypeName != null)
                        {
                            _baseClassHasDefaultCtor = baseTypeName.HasDefaultCtor();
                        }
                        else
                        {
                            _baseClassHasDefaultCtor = baseClass.HasDefaultCtor();
                        }
                    }

                    for (int i = 0; i < baseTypeAsts.Count; i++)
                    {
                        if (baseTypeAsts[i].TypeName.IsArray)
                        {
                            parser.ReportError(baseTypeAsts[i].Extent,
                                nameof(ParserStrings.SubtypeArray),
                                ParserStrings.SubtypeArray,
                                baseTypeAsts[i].TypeName.FullName);
                            this.HasFatalErrors = true;
                        }
                    }

                    for (int i = 1; i < baseTypeAsts.Count; i++)
                    {
                        if (baseTypeAsts[i].TypeName.IsArray)
                        {
                            parser.ReportError(baseTypeAsts[i].Extent,
                                nameof(ParserStrings.SubtypeArray),
                                ParserStrings.SubtypeArray,
                                baseTypeAsts[i].TypeName.FullName);
                        }
                        else
                        {
                            Type interfaceType = baseTypeAsts[i].TypeName.GetReflectionType();
                            if (interfaceType == null)
                            {
                                parser.ReportError(baseTypeAsts[i].Extent,
                                    nameof(ParserStrings.TypeNotFound),
                                    ParserStrings.TypeNotFound,
                                    baseTypeAsts[i].TypeName.FullName);
                            }
                            else
                            {
                                if (interfaceType.IsInterface)
                                {
                                    interfaces.Add(interfaceType);
                                }
                                else
                                {
                                    parser.ReportError(baseTypeAsts[i].Extent,
                                        nameof(ParserStrings.InterfaceNameExpected),
                                        ParserStrings.InterfaceNameExpected,
                                        interfaceType.Name);
                                }
                            }
                        }
                    }
                }

                return baseClass ?? typeof(object);
            }

            private bool ShouldImplementProperty(string name, Type type, [NotNullWhen(true)] out PropertyInfo interfaceProperty)
            {
                if (_abstractProperties == null)
                {

                    _abstractProperties = new Dictionary<Tuple<string, Type>, PropertyInfo>();
                    var allInterfaces = GetImplementingInterfaces();

                    foreach (var interfaceType in allInterfaces)
                    {
                        foreach (var property in interfaceType.GetProperties())
                        {
                            _abstractProperties.Add(Tuple.Create(property.Name, property.PropertyType), property);
                        }
                    }

                    if (_typeBuilder.BaseType.IsAbstract)
                    {
                        foreach (var property in _typeBuilder.BaseType.GetProperties())
                        {
                            if (property.GetAccessors().Any(m => m.IsAbstract))
                            {
                                _abstractProperties.Add(Tuple.Create(property.Name, property.PropertyType), property);
                            }
                        }
                    }
                }

                return _abstractProperties.TryGetValue(Tuple.Create(name, type), out interfaceProperty);
            }

            private bool ShouldImplementMethod(
                string name,
                Type returnType,
                Type[] parameterTypes,
                [NotNullWhen(true)] out MethodInfo interfaceMethod)
            {
                if (_interfaceMethods == null)
                {
                    _interfaceMethods = new Dictionary<Tuple<string, Type, string>, MethodInfo>();
                    var allInterfaces = GetImplementingInterfaces();

                    // We include NonPublic so we can also get protected interface methods.
                    BindingFlags methodFlags = BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.Static;

                    foreach (var interfaceType in allInterfaces)
                    {
                        foreach (var method in interfaceType.GetMethods(methodFlags))
                        {
                            if (!(method.IsFamily || method.IsFamilyOrAssembly || method.IsPublic))
                            {
                                // We want public and protected only, no internal or private.
                                continue;
                            }

                            Type[] methodParameters = method.GetParameters().Select(p => p.ParameterType).ToArray();
                            string methodParametersId = GetTypeArrayId(methodParameters);
                            _interfaceMethods.Add(Tuple.Create(method.Name, method.ReturnType, methodParametersId), method);
                        }
                    }
                }

                string parameterTypeId = GetTypeArrayId(parameterTypes);
                return _interfaceMethods.TryGetValue(Tuple.Create(name, returnType, parameterTypeId), out interfaceMethod);
            }

            private HashSet<Type> GetImplementingInterfaces()
            {
                if (_interfaces == null)
                {
                    _interfaces = new HashSet<Type>();

                    // TypeBuilder.GetInterfaces() returns only the interfaces that was explicitly passed to its constructor.
                    // During compilation the interface hierarchy is flattened, so we only need to resolve one level of ancestral interfaces.
                    foreach (var interfaceType in _typeBuilder.GetInterfaces())
                    {
                        foreach (var parentInterface in interfaceType.GetInterfaces())
                        {
                            _interfaces.Add(parentInterface);
                        }

                        _interfaces.Add(interfaceType);
                    }
                }

                return _interfaces;
            }

            private static string GetTypeArrayId(Type[] types)
            {
                string typeId = string.Join(string.Empty, types.Select(t => t.AssemblyQualifiedName));
                byte[] typeHash = SHA256.HashData(Encoding.UTF8.GetBytes(typeId));

                return Convert.ToHexString(typeHash);
            }

            public void DefineMembers()
            {
                // If user didn't provide any instance ctors or static ctor we will generate default ctor or static ctor respectively.
                // We can avoid explicit default ctor and static ctor, if we don't have any properties to initialize.
                bool needStaticCtor = false;
                bool needDefaultCtor = false;
                bool hasAnyMethods = false;
                List<FunctionMemberAst> staticCtors = new List<FunctionMemberAst>();
                List<FunctionMemberAst> instanceCtors = new List<FunctionMemberAst>();

                foreach (var member in _typeDefinitionAst.Members)
                {
                    var propertyMemberAst = member as PropertyMemberAst;
                    if (propertyMemberAst != null)
                    {
                        DefineProperty(propertyMemberAst);
                        if (propertyMemberAst.InitialValue != null)
                        {
                            if (propertyMemberAst.IsStatic)
                            {
                                needStaticCtor = true;
                            }
                            else
                            {
                                needDefaultCtor = true;
                            }
                        }
                    }
                    else
                    {
                        FunctionMemberAst method = member as FunctionMemberAst;
                        Diagnostics.Assert(method != null, StringUtil.Format("Unexpected subtype of MemberAst '{0}'. Expect `{1}`",
                            member.GetType().Name, typeof(FunctionMemberAst).GetType().Name));
                        if (method.IsConstructor)
                        {
                            if (method.IsStatic)
                            {
                                staticCtors.Add(method);
                            }
                            else
                            {
                                instanceCtors.Add(method);
                            }
                        }

                        hasAnyMethods = true;

                        DefineMethod(method);
                    }
                }

                // inside ctor we put logic to capture session state from execution context,
                // we cannot delegate default ctor creation to _typeBuilder, if we have any methods.
                // If there are only static methods, we still want to capture context to allow static method calls on instances in the right context.
                if (hasAnyMethods)
                {
                    needDefaultCtor = true;
                }

                if (needStaticCtor)
                {
                    foreach (var ctor in staticCtors)
                    {
                        var parameters = ((IParameterMetadataProvider)ctor.Body).Parameters;
                        // We report error for static ctors with parameters, even with default values.
                        // We don't take them into account.
                        if (parameters == null || parameters.Count == 0)
                        {
                            needStaticCtor = false;
                        }
                    }
                }

                if (needDefaultCtor)
                {
                    needDefaultCtor = instanceCtors.Count == 0;
                }

                //// Now we can decide to create explicit default ctors or report error.

                if (needStaticCtor)
                {
                    var staticCtorAst = new CompilerGeneratedMemberFunctionAst(PositionUtilities.EmptyExtent, _typeDefinitionAst, SpecialMemberFunctionType.StaticConstructor);
                    DefineConstructor(staticCtorAst, null, true, Reflection.MethodAttributes.Private | Reflection.MethodAttributes.Static, Type.EmptyTypes);
                }

                if (_baseClassHasDefaultCtor)
                {
                    if (needDefaultCtor)
                    {
                        var defaultCtorAst = new CompilerGeneratedMemberFunctionAst(PositionUtilities.EmptyExtent, _typeDefinitionAst, SpecialMemberFunctionType.DefaultConstructor);
                        DefineConstructor(defaultCtorAst, null, true, Reflection.MethodAttributes.Public, Type.EmptyTypes);
                    }
                }
                else
                {
                    if (instanceCtors.Count == 0)
                    {
                        _parser.ReportError(_typeDefinitionAst.Extent,
                            nameof(ParserStrings.BaseClassNoDefaultCtor),
                            ParserStrings.BaseClassNoDefaultCtor,
                            _typeBuilder.BaseType.Name);
                        this.HasFatalErrors = true;
                    }
                }
            }

            private void DefineProperty(PropertyMemberAst propertyMemberAst)
            {
                if (_definedProperties.ContainsKey(propertyMemberAst.Name))
                {
                    _parser.ReportError(propertyMemberAst.Extent,
                        nameof(ParserStrings.MemberAlreadyDefined),
                        ParserStrings.MemberAlreadyDefined,
                        propertyMemberAst.Name);
                    return;
                }

                _definedProperties.Add(propertyMemberAst.Name, propertyMemberAst);

                Type type;
                if (propertyMemberAst.PropertyType == null)
                {
                    type = typeof(object);
                }
                else
                {
                    type = propertyMemberAst.PropertyType.TypeName.GetReflectionType();
                    Diagnostics.Assert(type != null, "Semantic checks should have ensure type can't be null");
                }

                PropertyBuilder property = this.EmitPropertyIl(propertyMemberAst, type);
                // Define custom attributes on the property, not on the backingField
                DefineCustomAttributes(property, propertyMemberAst.Attributes, _parser, AttributeTargets.Field | AttributeTargets.Property);
            }

            private PropertyBuilder EmitPropertyIl(PropertyMemberAst propertyMemberAst, Type type)
            {
                // backing field is always private.
                var backingFieldAttributes = FieldAttributes.Private;
                // The property set and property get methods require a special set of attributes.
                var getSetAttributes = Reflection.MethodAttributes.SpecialName | Reflection.MethodAttributes.HideBySig;
                getSetAttributes |= propertyMemberAst.IsPublic ? Reflection.MethodAttributes.Public : Reflection.MethodAttributes.Private;
                MethodInfo implementingGetter = null;
                MethodInfo implementingSetter = null;
                if (ShouldImplementProperty(propertyMemberAst.Name, type, out PropertyInfo interfaceProperty))
                {
                    if (propertyMemberAst.IsStatic)
                    {
                        implementingGetter = interfaceProperty.GetGetMethod();
                        implementingSetter = interfaceProperty.GetSetMethod();
                    }
                    else
                    {
                        getSetAttributes |= Reflection.MethodAttributes.Virtual;
                    }
                }

                if (propertyMemberAst.IsStatic)
                {
                    backingFieldAttributes |= FieldAttributes.Static;
                    getSetAttributes |= Reflection.MethodAttributes.Static;
                }
                // C# naming convention for backing fields.
                string backingFieldName = string.Create(CultureInfo.InvariantCulture, $"<{propertyMemberAst.Name}>k__BackingField");
                var backingField = _typeBuilder.DefineField(backingFieldName, type, backingFieldAttributes);

                bool hasValidateAttributes = false;
                if (propertyMemberAst.Attributes != null)
                {
                    for (int i = 0; i < propertyMemberAst.Attributes.Count; i++)
                    {
                        Type attributeType = propertyMemberAst.Attributes[i].TypeName.GetReflectionAttributeType();
                        if (attributeType != null && attributeType.IsSubclassOf(typeof(ValidateArgumentsAttribute)))
                        {
                            hasValidateAttributes = true;
                            break;
                        }
                    }
                }

                // The last argument of DefineProperty is null, because the property has no parameters.
                PropertyBuilder property = _typeBuilder.DefineProperty(propertyMemberAst.Name, Reflection.PropertyAttributes.None, type, null);

                // Define the "get" accessor method.
                MethodBuilder getMethod = _typeBuilder.DefineMethod(string.Concat("get_", propertyMemberAst.Name), getSetAttributes, type, Type.EmptyTypes);
                ILGenerator getIlGen = getMethod.GetILGenerator();
                if (propertyMemberAst.IsStatic)
                {
                    // static
                    getIlGen.Emit(OpCodes.Ldsfld, backingField);
                    getIlGen.Emit(OpCodes.Ret);
                }
                else
                {
                    // instance
                    getIlGen.Emit(OpCodes.Ldarg_0);
                    getIlGen.Emit(OpCodes.Ldfld, backingField);
                    getIlGen.Emit(OpCodes.Ret);
                }

                if (implementingGetter != null)
                {
                    _typeBuilder.DefineMethodOverride(getMethod, implementingGetter);
                }

                // Define the "set" accessor method.
                MethodBuilder setMethod = _typeBuilder.DefineMethod(string.Concat("set_", propertyMemberAst.Name), getSetAttributes, null, new Type[] { type });
                ILGenerator setIlGen = setMethod.GetILGenerator();

                if (hasValidateAttributes)
                {
                    Type typeToLoad = _typeBuilder;
                    setIlGen.Emit(OpCodes.Ldtoken, typeToLoad);
                    setIlGen.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle")); // load current Type on stack
                    setIlGen.Emit(OpCodes.Ldstr, propertyMemberAst.Name); // load name of Property
                    setIlGen.Emit(propertyMemberAst.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1); // load set value
                    if (type.IsValueType)
                    {
                        setIlGen.Emit(OpCodes.Box, type);
                    }

                    setIlGen.Emit(OpCodes.Call, CachedReflectionInfo.ClassOps_ValidateSetProperty);
                }

                if (propertyMemberAst.IsStatic)
                {
                    setIlGen.Emit(OpCodes.Ldarg_0);
                    setIlGen.Emit(OpCodes.Stsfld, backingField);
                }
                else
                {
                    setIlGen.Emit(OpCodes.Ldarg_0);
                    setIlGen.Emit(OpCodes.Ldarg_1);
                    setIlGen.Emit(OpCodes.Stfld, backingField);
                }

                setIlGen.Emit(OpCodes.Ret);

                if (implementingSetter != null)
                {
                    _typeBuilder.DefineMethodOverride(setMethod, implementingSetter);
                }

                // Map the two methods created above to our PropertyBuilder to
                // their corresponding behaviors, "get" and "set" respectively.
                property.SetGetMethod(getMethod);
                property.SetSetMethod(setMethod);

                if (propertyMemberAst.IsHidden)
                {
                    property.SetCustomAttribute(s_hiddenCustomAttributeBuilder);
                }

                return property;
            }

            private bool CheckForDuplicateOverload(FunctionMemberAst functionMemberAst, Type[] newParameters)
            {
                List<Tuple<FunctionMemberAst, Type[]>> overloads;
                if (!_definedMethods.TryGetValue(functionMemberAst.Name, out overloads))
                {
                    overloads = new List<Tuple<FunctionMemberAst, Type[]>>();
                    _definedMethods.Add(functionMemberAst.Name, overloads);
                }
                else
                {
                    foreach (var overload in overloads)
                    {
                        var overloadParameters = overload.Item2;

                        // This test won't be correct when defaults are supported
                        if (newParameters.Length != overloadParameters.Length)
                        {
                            continue;
                        }

                        var sameSignature = true;
                        for (int i = 0; i < newParameters.Length; i++)
                        {
                            if (newParameters[i] != overloadParameters[i])
                            {
                                sameSignature = false;
                                break;
                            }
                        }

                        if (sameSignature)
                        {
                            // If both are both static/instance, it's an error.
                            // Otherwise, signatures can match only for the constructor.
                            if (overload.Item1.IsStatic == functionMemberAst.IsStatic ||
                                !functionMemberAst.IsConstructor)
                            {
                                _parser.ReportError(functionMemberAst.NameExtent ?? functionMemberAst.Extent,
                                    nameof(ParserStrings.MemberAlreadyDefined),
                                    ParserStrings.MemberAlreadyDefined,
                                    functionMemberAst.Name);
                                return true;
                            }
                        }
                    }
                }

                overloads.Add(Tuple.Create(functionMemberAst, newParameters));
                return false;
            }

            private Type[] GetParameterTypes(FunctionMemberAst functionMemberAst)
            {
                var parameters = ((IParameterMetadataProvider)functionMemberAst).Parameters;
                if (parameters == null)
                {
                    return Type.EmptyTypes;
                }

                bool anyErrors = false;
                var result = new Type[parameters.Count];
                for (var i = 0; i < parameters.Count; i++)
                {
                    var typeConstraint = parameters[i].Attributes.OfType<TypeConstraintAst>().FirstOrDefault();
                    var paramType = (typeConstraint != null)
                                        ? typeConstraint.TypeName.GetReflectionType()
                                        : typeof(object);
                    if (paramType == null)
                    {
                        _parser.ReportError(typeConstraint.Extent,
                            nameof(ParserStrings.TypeNotFound),
                            ParserStrings.TypeNotFound,
                            typeConstraint.TypeName.FullName);
                        anyErrors = true;
                    }
                    else if (paramType == typeof(void) || paramType.IsGenericTypeDefinition)
                    {
                        _parser.ReportError(typeConstraint.Extent,
                            nameof(ParserStrings.TypeNotAllowedForParameter),
                            ParserStrings.TypeNotAllowedForParameter,
                            typeConstraint.TypeName.FullName);
                        anyErrors = true;
                    }

                    result[i] = paramType;
                }

                return anyErrors ? null : result;
            }

            private bool MethodExistsOnBaseClassAndFinal(string methodName, Type[] parameterTypes)
            {
                Type baseType = _typeBuilder.BaseType;

                // If baseType is PS class, then method will be virtual, once we define it.
                if (baseType is TypeBuilder)
                {
                    return false;
                }

                var mi = baseType.GetMethod(methodName, parameterTypes);
                return mi != null && mi.IsFinal;
            }

            private void DefineMethod(FunctionMemberAst functionMemberAst)
            {
                var parameterTypes = GetParameterTypes(functionMemberAst);
                if (parameterTypes == null)
                {
                    // There must have been an error, just return
                    return;
                }

                if (CheckForDuplicateOverload(functionMemberAst, parameterTypes))
                {
                    return;
                }

                if (functionMemberAst.IsConstructor)
                {
                    var methodAttributes = Reflection.MethodAttributes.Public;
                    if (functionMemberAst.IsStatic)
                    {
                        var parameters = functionMemberAst.Parameters;
                        if (parameters.Count > 0)
                        {
                            IScriptExtent errorExtent = Parser.ExtentOf(parameters[0], parameters.Last());
                            _parser.ReportError(errorExtent,
                                nameof(ParserStrings.StaticConstructorCantHaveParameters),
                                ParserStrings.StaticConstructorCantHaveParameters);
                            return;
                        }

                        methodAttributes |= Reflection.MethodAttributes.Static;
                    }

                    DefineConstructor(functionMemberAst, functionMemberAst.Attributes, functionMemberAst.IsHidden, methodAttributes, parameterTypes);
                    return;
                }

                var returnType = functionMemberAst.GetReturnType();
                var attributes = functionMemberAst.IsPublic
                                     ? Reflection.MethodAttributes.Public
                                     : Reflection.MethodAttributes.Private;
                MethodInfo interfaceBaseMethod = null;
                if (functionMemberAst.IsStatic)
                {
                    attributes |= Reflection.MethodAttributes.Static;

                    ShouldImplementMethod(
                        functionMemberAst.Name,
                        returnType,
                        parameterTypes,
                        out interfaceBaseMethod);
                }
                else
                {
                    if (this.MethodExistsOnBaseClassAndFinal(functionMemberAst.Name, parameterTypes))
                    {
                        attributes |= Reflection.MethodAttributes.HideBySig;
                        attributes |= Reflection.MethodAttributes.NewSlot;
                    }

                    attributes |= Reflection.MethodAttributes.Virtual;
                }

                if (returnType == null)
                {
                    _parser.ReportError(functionMemberAst.ReturnType.Extent,
                        nameof(ParserStrings.TypeNotFound),
                        ParserStrings.TypeNotFound,
                        functionMemberAst.ReturnType.TypeName.FullName);
                    return;
                }

                var method = _typeBuilder.DefineMethod(functionMemberAst.Name, attributes, returnType, parameterTypes);
                DefineCustomAttributes(method, functionMemberAst.Attributes, _parser, AttributeTargets.Method);
                if (functionMemberAst.IsHidden)
                {
                    method.SetCustomAttribute(s_hiddenCustomAttributeBuilder);
                }

                var ilGenerator = method.GetILGenerator();
                DefineMethodBody(functionMemberAst, ilGenerator, GetMetaDataName(method.Name, parameterTypes.Length), functionMemberAst.IsStatic, parameterTypes, returnType,
                    (i, n) => method.DefineParameter(i, ParameterAttributes.None, n));

                if (interfaceBaseMethod != null)
                {
                    _typeBuilder.DefineMethodOverride(method, interfaceBaseMethod);
                }
            }

            private void DefineConstructor(IParameterMetadataProvider ipmp, ReadOnlyCollection<AttributeAst> attributeAsts, bool isHidden, Reflection.MethodAttributes methodAttributes, Type[] parameterTypes)
            {
                bool isStatic = (methodAttributes & Reflection.MethodAttributes.Static) != 0;
                var ctor = isStatic
                    ? _typeBuilder.DefineTypeInitializer()
                    : _typeBuilder.DefineConstructor(methodAttributes, CallingConventions.Standard, parameterTypes);
                DefineCustomAttributes(ctor, attributeAsts, _parser, AttributeTargets.Constructor);
                if (isHidden)
                {
                    ctor.SetCustomAttribute(s_hiddenCustomAttributeBuilder);
                }

                var ilGenerator = ctor.GetILGenerator();

                if (!isStatic)
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0); // load 'this' on stack for Stfld call

                    ilGenerator.Emit(OpCodes.Ldnull);
                    ilGenerator.Emit(OpCodes.Ldfld, _sessionStateKeeperField);
                    ilGenerator.EmitCall(OpCodes.Call, s_sessionStateKeeper_GetSessionState, null); // load 'sessionState' on stack for Stfld call

                    ilGenerator.Emit(OpCodes.Stfld, _sessionStateField);
                }

                DefineMethodBody(ipmp, ilGenerator, GetMetaDataName(ctor.Name, parameterTypes.Length), isStatic, parameterTypes, typeof(void),
                    (i, n) => ctor.DefineParameter(i, ParameterAttributes.None, n));
            }

            private static string GetMetaDataName(string name, int numberOfParameters)
            {
                int currentId = Interlocked.Increment(ref s_globalCounter);
                string metaDataName = name + "_" + numberOfParameters + "_" + currentId;
                return metaDataName;
            }

            private void DefineMethodBody(
                IParameterMetadataProvider ipmp,
                ILGenerator ilGenerator,
                string metadataToken,
                bool isStatic,
                Type[] parameterTypes,
                Type returnType,
                Action<int, string> parameterNameSetter)
            {
                var wrapperFieldName = string.Create(CultureInfo.InvariantCulture, $"<{metadataToken}>");
                var scriptBlockWrapperField = _staticHelpersTypeBuilder.DefineField(wrapperFieldName,
                                                                       typeof(ScriptBlockMemberMethodWrapper),
                                                                       FieldAttributes.Assembly | FieldAttributes.Static);

                ilGenerator.Emit(OpCodes.Ldsfld, scriptBlockWrapperField);
                if (isStatic)
                {
                    ilGenerator.Emit(OpCodes.Ldnull);                   // pass null (no this)
                    ilGenerator.Emit(OpCodes.Ldnull);                   // pass null (no sessionStateInternal)
                }
                else
                {
                    EmitLdarg(ilGenerator, 0);                            // pass this
                    ilGenerator.Emit(OpCodes.Ldarg_0);                    // pass 'this' for Ldfld call
                    ilGenerator.Emit(OpCodes.Ldfld, _sessionStateField);  // pass sessionStateInternal
                }

                int parameterCount = parameterTypes.Length;
                if (parameterCount > 0)
                {
                    var parameters = ipmp.Parameters;
                    var local = ilGenerator.DeclareLocal(typeof(object[]));

                    EmitLdc(ilGenerator, parameterCount);               // Create an array to hold all
                    ilGenerator.Emit(OpCodes.Newarr, typeof(object));  //     of the parameters
                    ilGenerator.Emit(OpCodes.Stloc, local);             // Save array for repeated use
                    int j = isStatic ? 0 : 1;
                    for (int i = 0; i < parameterCount; i++, j++)
                    {
                        ilGenerator.Emit(OpCodes.Ldloc, local);           // load array
                        EmitLdc(ilGenerator, i);                          // index to save at
                        EmitLdarg(ilGenerator, j);                        // load argument (skipping this)
                        if (parameterTypes[i].IsValueType)  // value types must be boxed
                        {
                            ilGenerator.Emit(OpCodes.Box, parameterTypes[i]);
                        }

                        ilGenerator.Emit(OpCodes.Stelem_Ref);           // save the argument in the array

                        // Set the parameter name, mostly for Get-Member
                        // Parameters are indexed beginning with the number 1 for the first parameter
                        parameterNameSetter(i + 1, parameters[i].Name.VariablePath.UserPath);
                    }

                    ilGenerator.Emit(OpCodes.Ldloc, local);         // load array
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Ldsfld, typeof(ScriptBlockMemberMethodWrapper).GetField("_emptyArgumentArray", BindingFlags.Static | BindingFlags.Public));
                }

                MethodInfo invokeHelper;
                if (returnType == typeof(void))
                {
                    invokeHelper = typeof(ScriptBlockMemberMethodWrapper).GetMethod("InvokeHelper", BindingFlags.Instance | BindingFlags.Public);
                }
                else
                {
                    invokeHelper = typeof(ScriptBlockMemberMethodWrapper).GetMethod("InvokeHelperT", BindingFlags.Instance | BindingFlags.Public).MakeGenericMethod(returnType);
                }

                ilGenerator.Emit(OpCodes.Tailcall);
                ilGenerator.EmitCall(OpCodes.Call, invokeHelper, null);
                ilGenerator.Emit(OpCodes.Ret);

                _fieldsToInitForMemberFunctions.Add((wrapperFieldName, ipmp, isStatic));
            }
        }

        private sealed class DefineEnumHelper
        {
            private readonly Parser _parser;
            private readonly TypeDefinitionAst _enumDefinitionAst;
            private readonly ModuleBuilder _moduleBuilder;
            private readonly string _typeName;

            internal DefineEnumHelper(Parser parser, ModuleBuilder module, TypeDefinitionAst enumDefinitionAst, string typeName)
            {
                _parser = parser;
                _enumDefinitionAst = enumDefinitionAst;
                _moduleBuilder = module;
                _typeName = typeName;
            }

            internal static List<DefineEnumHelper> Sort(List<DefineEnumHelper> defineEnumHelpers, Parser parser)
            {
                // This function does a topological sort of the enums to be defined.  This is needed so we
                // can allow one enum member to use the value of another w/o needing to worry about the order
                // they are declared in.  For example:
                //
                //     enum E1 { e1 = [E2]::e2 }
                //     enum E2 { e2 = 42 }
                //
                // We also want to report an error for recursive expressions, e.g.
                //
                //     enum E1 { e1 = [E2]::e2 }
                //     enum E2 { e2 = [E1]::e1 }
                //
                // Note that this code is not as permissive as it could be, e.g. we could (but do not) allow:
                //
                //     enum E1 { e1 = [E2]::e2 }
                //     enum E2 {
                //         e2 = 42
                //         e2a = [E1]::e1
                //     }
                //
                // In this case, there is no cycle in the constant values despite E1 referencing E2 and vice versa.
                //
                // The basic algorithm is to create a graph where the edges represent a dependency, using this example:
                //
                //     enum E1 { e1 = [E2]::e2 }
                //     enum E2 { e2 = 42 }
                //
                // We have an edge E1->E2.  E2 has no dependencies.

                if (defineEnumHelpers.Count == 1)
                {
                    return defineEnumHelpers;
                }

                // There won't be many nodes in our graph, so we just use a dictionary with a list of edges instead
                // of something cleaner.
                var graph = new Dictionary<TypeDefinitionAst, Tuple<DefineEnumHelper, List<TypeDefinitionAst>>>();

                // Add all of our nodes to the graph
                foreach (var helper in defineEnumHelpers)
                {
                    graph.Add(helper._enumDefinitionAst, Tuple.Create(helper, new List<TypeDefinitionAst>()));
                }

                // Now find any edges.
                foreach (var helper in defineEnumHelpers)
                {
                    foreach (var enumerator in helper._enumDefinitionAst.Members)
                    {
                        var initExpr = ((PropertyMemberAst)enumerator).InitialValue;
                        if (initExpr == null)
                        {
                            // No initializer, so no dependency (this is incorrect assumption if
                            // we wanted to be more general like C#.)
                            continue;
                        }

                        // The expression may have multiple member expressions, e.g. [E]::e1 + [E]::e2
                        foreach (var memberExpr in initExpr.FindAll(static ast => ast is MemberExpressionAst, false))
                        {
                            var typeExpr = ((MemberExpressionAst)memberExpr).Expression as TypeExpressionAst;
                            if (typeExpr != null)
                            {
                                // We only want to add edges for enums being defined in the current scope.
                                // We detect this by seeing if the ast is in our graph or not.
                                var typeName = typeExpr.TypeName as TypeName;
                                if (typeName != null
                                    && typeName._typeDefinitionAst != null
                                    && typeName._typeDefinitionAst != helper._enumDefinitionAst  // Don't add self edges
                                    && graph.ContainsKey(typeName._typeDefinitionAst))
                                {
                                    var edgeList = graph[helper._enumDefinitionAst].Item2;
                                    if (!edgeList.Contains(typeName._typeDefinitionAst))  // Only add 1 edge per enum
                                    {
                                        edgeList.Add(typeName._typeDefinitionAst);
                                    }
                                }
                            }
                        }
                    }
                }

                // Our graph is built.  The ready list will hold nodes that don't depend on anything not already
                // in the result list.  We start with a list of nodes with no edges (no dependencies).
                var result = new List<DefineEnumHelper>(defineEnumHelpers.Count);
                var readyList = new List<DefineEnumHelper>(defineEnumHelpers.Count);
                readyList.AddRange(from value in graph.Values where value.Item2.Count == 0 select value.Item1);
                while (readyList.Count > 0)
                {
                    var node = readyList[readyList.Count - 1];
                    readyList.RemoveAt(readyList.Count - 1);
                    result.Add(node);

                    // Remove all edges to this node as it is in our result list now.
                    foreach (var value in graph.Values)
                    {
                        value.Item2.Remove(node._enumDefinitionAst);

                        // If we removed the last edge, we can put this node on the ready list (assuming it
                        // wasn't already there or in our result list.)
                        if (value.Item2.Count == 0 && !result.Contains(value.Item1) && !readyList.Contains(value.Item1))
                        {
                            readyList.Add(value.Item1);
                        }
                    }
                }

                if (result.Count < defineEnumHelpers.Count)
                {
                    // There was a cycle, report an error on each enum.
                    foreach (var helper in defineEnumHelpers)
                    {
                        if (!result.Contains(helper))
                        {
                            parser.ReportError(helper._enumDefinitionAst.Extent,
                                nameof(ParserStrings.CycleInEnumInitializers),
                                ParserStrings.CycleInEnumInitializers);
                        }
                    }
                }
                else
                {
                    Diagnostics.Assert(result.Count == defineEnumHelpers.Count, "Logic error if we have more outgoing results than incoming");
                }

                return result;
            }

            internal void DefineEnum()
            {
                var typeConstraintAst = _enumDefinitionAst.BaseTypes.FirstOrDefault();
                var underlyingType = typeConstraintAst == null ? typeof(int) : typeConstraintAst.TypeName.GetReflectionType();

                var definedEnumerators = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var enumBuilder = _moduleBuilder.DefineEnum(_typeName, Reflection.TypeAttributes.Public, underlyingType);
                DefineCustomAttributes(enumBuilder, _enumDefinitionAst.Attributes, _parser, AttributeTargets.Enum);

                dynamic value = 0;
                dynamic maxValue = 0;
                switch (Type.GetTypeCode(underlyingType))
                {
                    case TypeCode.Byte:
                        maxValue = byte.MaxValue;
                        break;
                    case TypeCode.Int16:
                        maxValue = short.MaxValue;
                        break;
                    case TypeCode.Int32:
                        maxValue = int.MaxValue;
                        break;
                    case TypeCode.Int64:
                        maxValue = long.MaxValue;
                        break;
                    case TypeCode.SByte:
                        maxValue = sbyte.MaxValue;
                        break;
                    case TypeCode.UInt16:
                        maxValue = ushort.MaxValue;
                        break;
                    case TypeCode.UInt32:
                        maxValue = uint.MaxValue;
                        break;
                    case TypeCode.UInt64:
                        maxValue = ulong.MaxValue;
                        break;
                    default:
                        _parser.ReportError(
                            typeConstraintAst.Extent,
                            nameof(ParserStrings.InvalidUnderlyingType),
                            ParserStrings.InvalidUnderlyingType,
                            underlyingType);
                        break;
                }

                bool valueTooBig = false;

                foreach (var member in _enumDefinitionAst.Members)
                {
                    var enumerator = (PropertyMemberAst)member;
                    if (enumerator.InitialValue != null)
                    {
                        object constValue;
                        if (IsConstantValueVisitor.IsConstant(enumerator.InitialValue, out constValue, false, false))
                        {
                            if (!LanguagePrimitives.TryConvertTo(constValue, underlyingType, out value))
                            {
                                if (constValue != null &&
                                    LanguagePrimitives.IsNumeric(LanguagePrimitives.GetTypeCode(constValue.GetType())))
                                {
                                    _parser.ReportError(
                                        enumerator.InitialValue.Extent,
                                        nameof(ParserStrings.EnumeratorValueOutOfBounds),
                                        ParserStrings.EnumeratorValueOutOfBounds,
                                        ToStringCodeMethods.Type(underlyingType));
                                }
                                else
                                {
                                    _parser.ReportError(
                                        enumerator.InitialValue.Extent,
                                        nameof(ParserStrings.CannotConvertValue),
                                        ParserStrings.CannotConvertValue,
                                        ToStringCodeMethods.Type(underlyingType));
                                }
                            }
                        }
                        else
                        {
                            _parser.ReportError(
                                enumerator.InitialValue.Extent,
                                nameof(ParserStrings.EnumeratorValueMustBeConstant),
                                ParserStrings.EnumeratorValueMustBeConstant);
                        }

                        valueTooBig = value > maxValue;
                    }

                    if (valueTooBig)
                    {
                        _parser.ReportError(
                            enumerator.Extent,
                            nameof(ParserStrings.EnumeratorValueOutOfBounds),
                            ParserStrings.EnumeratorValueOutOfBounds,
                            ToStringCodeMethods.Type(underlyingType));
                    }

                    if (definedEnumerators.Contains(enumerator.Name))
                    {
                        _parser.ReportError(
                            enumerator.Extent,
                            nameof(ParserStrings.MemberAlreadyDefined),
                            ParserStrings.MemberAlreadyDefined,
                            enumerator.Name);
                    }
                    else if (value != null)
                    {
                        value = Convert.ChangeType(value, underlyingType);
                        definedEnumerators.Add(enumerator.Name);
                        enumBuilder.DefineLiteral(enumerator.Name, value);
                    }

                    if (value < maxValue)
                    {
                        value += 1;
                        valueTooBig = false;
                    }
                    else
                    {
                        valueTooBig = true;
                    }
                }

                _enumDefinitionAst.Type = enumBuilder.CreateTypeInfo().AsType();
            }
        }

        private static IEnumerable<CustomAttributeBuilder> GetAssemblyAttributeBuilders(string scriptFile)
        {
            var ctor = typeof(DynamicClassImplementationAssemblyAttribute).GetConstructor(Type.EmptyTypes);
            var emptyArgs = Array.Empty<object>();

            if (string.IsNullOrEmpty(scriptFile))
            {
                yield return new CustomAttributeBuilder(ctor, emptyArgs);
                yield break;
            }

            var propertyInfo = new PropertyInfo[] {
                typeof(DynamicClassImplementationAssemblyAttribute).GetProperty(nameof(DynamicClassImplementationAssemblyAttribute.ScriptFile)) };
            var propertyArgs = new object[] { scriptFile };

            yield return new CustomAttributeBuilder(ctor, emptyArgs,
                propertyInfo, propertyArgs, Array.Empty<FieldInfo>(), emptyArgs);
        }

        private static int counter = 0;

        internal static Assembly DefineTypes(Parser parser, Ast rootAst, TypeDefinitionAst[] typeDefinitions)
        {
            Diagnostics.Assert(rootAst.Parent == null, "Caller should only define types from the root ast");

            var definedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var assemblyName = new AssemblyName(DynamicClassAssemblyName)
            {
                // We could generate a unique name, but a unique version works too.
                Version = new Version(1, 0, 0, Interlocked.Increment(ref counter))
            };
            var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName,
                AssemblyBuilderAccess.RunAndCollect, GetAssemblyAttributeBuilders(rootAst.Extent.File));
            var module = assembly.DefineDynamicModule(DynamicClassAssemblyName);

            var defineTypeHelpers = new List<DefineTypeHelper>();
            var defineEnumHelpers = new List<DefineEnumHelper>();

            foreach (var typeDefinitionAst in typeDefinitions)
            {
                var typeName = GetClassNameInAssembly(typeDefinitionAst);
                if (definedTypes.Add(typeName))
                {
                    if ((typeDefinitionAst.TypeAttributes & TypeAttributes.Class) == TypeAttributes.Class)
                    {
                        defineTypeHelpers.Add(new DefineTypeHelper(parser, module, typeDefinitionAst, typeName));
                    }
                    else if ((typeDefinitionAst.TypeAttributes & TypeAttributes.Enum) == TypeAttributes.Enum)
                    {
                        defineEnumHelpers.Add(new DefineEnumHelper(parser, module, typeDefinitionAst, typeName));
                    }
                }
            }

            // Define enums before classes so members of classes can use these enum types.
            defineEnumHelpers = DefineEnumHelper.Sort(defineEnumHelpers, parser);
            foreach (var helper in defineEnumHelpers)
            {
                helper.DefineEnum();
            }

            foreach (var helper in defineTypeHelpers)
            {
                helper.DefineMembers();
            }

            foreach (var helper in defineTypeHelpers)
            {
                Diagnostics.Assert(helper._typeDefinitionAst.Type is TypeBuilder, "Type should be the TypeBuilder");
                bool runtimeTypeAssigned = false;
                if (!helper.HasFatalErrors)
                {
                    try
                    {
                        var type = helper._typeBuilder.CreateType();
                        helper._typeDefinitionAst.Type = type;
                        runtimeTypeAssigned = true;
                        var helperType = helper._staticHelpersTypeBuilder.CreateType();

                        SessionStateKeeper sessionStateKeeper = new SessionStateKeeper();
                        helperType.GetField(s_sessionStateKeeperFieldName, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, sessionStateKeeper);

                        if (helper._fieldsToInitForMemberFunctions != null)
                        {
                            foreach (var tuple in helper._fieldsToInitForMemberFunctions)
                            {
                                // If the wrapper is for a static method, we need the sessionStateKeeper to determine the right SessionState to run.
                                // If the wrapper is for an instance method, we use the SessionState that the instance is bound to, and thus don't need sessionStateKeeper.
                                var methodWrapper = tuple.isStatic
                                    ? new ScriptBlockMemberMethodWrapper(tuple.bodyAst, sessionStateKeeper)
                                    : new ScriptBlockMemberMethodWrapper(tuple.bodyAst);
                                helperType.GetField(tuple.fieldName, BindingFlags.NonPublic | BindingFlags.Static)
                                    .SetValue(null, methodWrapper);
                            }
                        }
                    }
                    catch (TypeLoadException e)
                    {
                        // This is a cheap way to get error messages about non-implemented abstract/interface methods (and maybe some other errors).
                        // We use .NET API to perform this check during type creation.
                        //
                        // Presumably this catch could go away when we will not create Type at parse time.
                        // Error checking should be moved/added to semantic checks.
                        parser.ReportError(helper._typeDefinitionAst.Extent,
                            nameof(ParserStrings.TypeCreationError),
                            ParserStrings.TypeCreationError,
                            helper._typeBuilder.Name,
                            e.Message);
                    }
                }

                if (!runtimeTypeAssigned)
                {
                    // Clean up ast
                    helper._typeDefinitionAst.Type = null;
                }
            }

            return assembly;
        }

        private static string GetClassNameInAssembly(TypeDefinitionAst typeDefinitionAst)
        {
            // Only allocate a list if necessary - in the most common case, we don't need it.
            List<string> nameParts = null;
            var parent = typeDefinitionAst.Parent;
            while (parent.Parent != null)
            {
                if (parent is IParameterMetadataProvider)
                {
                    nameParts ??= new List<string>();
                    var fnDefn = parent.Parent as FunctionDefinitionAst;
                    if (fnDefn != null)
                    {
                        parent = fnDefn;
                        nameParts.Add(fnDefn.Name);
                    }
                    else
                    {
                        nameParts.Add("<" + parent.Extent.Text.GetHashCode().ToString("x", CultureInfo.InvariantCulture) + ">");
                    }
                }

                parent = parent.Parent;
            }

            if (nameParts == null)
            {
                return typeDefinitionAst.Name;
            }

            nameParts.Reverse();
            nameParts.Add(typeDefinitionAst.Name);
            return string.Join('.', nameParts);
        }

        private static readonly OpCode[] s_ldc =
        {
            OpCodes.Ldc_I4_0, OpCodes.Ldc_I4_1, OpCodes.Ldc_I4_2, OpCodes.Ldc_I4_3, OpCodes.Ldc_I4_4,
            OpCodes.Ldc_I4_5, OpCodes.Ldc_I4_6, OpCodes.Ldc_I4_7, OpCodes.Ldc_I4_8
        };

        private static void EmitLdc(ILGenerator emitter, int c)
        {
            if (c < s_ldc.Length)
            {
                emitter.Emit(s_ldc[c]);
            }
            else
            {
                emitter.Emit(OpCodes.Ldc_I4, c);
            }
        }

        private static readonly OpCode[] s_ldarg =
        {
            OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3
        };

        private static void EmitLdarg(ILGenerator emitter, int c)
        {
            if (c < s_ldarg.Length)
            {
                emitter.Emit(s_ldarg[c]);
            }
            else
            {
                emitter.Emit(OpCodes.Ldarg, c);
            }
        }
    }

    /// <summary>
    /// The attribute for a PowerShell class to not affiliate with a particular Runspace\SessionState.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class NoRunspaceAffinityAttribute : ParsingBaseAttribute
    {
        /// <summary>
        /// Initializes a new instance of the attribute.
        /// </summary>
        public NoRunspaceAffinityAttribute()
        {
        }
    }
}
